using Domain.Common.Auth;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class UserInfoRepository : IUserInfoRepository
    {
        private readonly IUnitOfWork _unitOfWork;

        public UserInfoRepository(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<string>> GetUserRolesAsync(int userId)
        {
            var userRoleRepo = _unitOfWork.GetRepository<UserRole, int>();
            var roleRepo = _unitOfWork.GetRepository<Role, int>();

            var userRoles = (await userRoleRepo.GetAllAsync(ur => ur.IdUser == userId))
                .Join(await roleRepo.GetAllAsync(),
                    ur => ur.IdRole,
                    r => r.Id,
                    (ur, r) => r.Name)
                .ToList();

            return userRoles ?? new List<string>();
        }

        public async Task<List<PermissionWithUserPermission>> GetUserPermissionsWithUserPermissionsAsync(int userId)
        {
            var userPermissionRepo = _unitOfWork.GetRepository<UserPermission, int>();
            var baseRepo = (BaseRepository<UserPermission, int>)userPermissionRepo;
            var context = baseRepo.Context;

            // Permisos directos del usuario con Permission cargada via Include
            var directUserPermissions = await context.Set<UserPermission>()
                .AsNoTracking()
                .Where(up => up.IdUser == userId)
                .Include(up => up.Permission)
                .ToListAsync();

            // Permisos por roles activos del usuario
            var userRoleIds = await context.Set<UserRole>()
                .AsNoTracking()
                .Where(ur => ur.IdUser == userId && ur.StatusRegister)
                .Select(ur => ur.IdRole)
                .ToListAsync();

            if (userRoleIds.Any())
            {
                var rolePermissionIds = await context.Set<RolePermission>()
                    .AsNoTracking()
                    .Where(rp => userRoleIds.Contains(rp.IdRole) && rp.StatusRegister)
                    .Select(rp => rp.IdPermission)
                    .Distinct()
                    .ToListAsync();

                if (rolePermissionIds.Any())
                {
                    var activeCatalog = await context.Set<Permission>()
                        .AsNoTracking()
                        .Where(p => p.StatusRegister)
                        .ToListAsync();

                    var resolvedRolePermissions = PermissionTreeHelper.ResolveActiveWithAncestorsByIds(
                        activeCatalog,
                        rolePermissionIds);

                    var existingPermissionIds = directUserPermissions
                        .Select(dup => dup.IdPermission)
                        .ToHashSet();

                    var missingPermissions = resolvedRolePermissions
                        .Where(p => !existingPermissionIds.Contains(p.Id))
                        .ToList();

                    if (missingPermissions.Any())
                    {
                        var now = DateTimeOffset.UtcNow;
                        foreach (var perm in missingPermissions)
                        {
                            var newUp = new UserPermission
                            {
                                IdUser = userId,
                                IdPermission = perm.Id,
                                StatusRegister = true,
                                OperationRegister = "Inicialización automática por Rol",
                                CreatedAt = now,
                                IdUserCreatedAt = 1
                            };
                            context.Set<UserPermission>().Add(newUp);
                        }
                        await context.SaveChangesAsync();

                        // Recargar con los nuevos IDs autoincrementales generados por la BD
                        directUserPermissions = await context.Set<UserPermission>()
                            .AsNoTracking()
                            .Where(up => up.IdUser == userId)
                            .Include(up => up.Permission)
                            .ToListAsync();
                    }
                }
            }

            // Cada permiso tiene su UserPermission real con Id > 0
            var result = directUserPermissions
                .Where(up => up.Permission != null)
                .Select(up => new PermissionWithUserPermission(up.Permission, up))
                .ToList();

            return result;
        }

        public async Task<UserLoginData> GetUserLoginDataAsync(int userId)
        {
            var userRoleRepo = _unitOfWork.GetRepository<UserRole, int>();
            var roleRepo = _unitOfWork.GetRepository<Role, int>();
            var userPermissionRepo = _unitOfWork.GetRepository<UserPermission, int>();
            var permissionRepo = _unitOfWork.GetRepository<Permission, int>();
            var rolePermissionRepo = _unitOfWork.GetRepository<RolePermission, int>();

            var activeUserRoles = (await userRoleRepo.GetAllAsync(ur => ur.IdUser == userId && ur.StatusRegister)).ToList();
            var roles = activeUserRoles
                .Join(await roleRepo.GetAllAsync(r => r.StatusRegister),
                    ur => ur.IdRole,
                    r => r.Id,
                    (ur, r) => new RoleInfoResult { Name = r.Name, Code = r.Code })
                .ToList();

            var userPerms = (await userPermissionRepo.GetAllAsync(up => up.IdUser == userId)).ToList();

            List<int> allPermissionIds;
            if (userPerms.Any())
            {
                // Fuente de verdad individual del usuario: solo los activos
                allPermissionIds = userPerms
                    .Where(up => up.StatusRegister)
                    .Select(up => up.IdPermission)
                    .Distinct()
                    .ToList();
            }
            else
            {
                // Fallback para usuarios que aún no tienen UserPermission
                var userRoleIds = activeUserRoles
                    .Select(ur => ur.IdRole)
                    .ToList();

                allPermissionIds = new List<int>();
                if (userRoleIds.Any())
                {
                    allPermissionIds = (await rolePermissionRepo.GetAllAsync(rp => userRoleIds.Contains(rp.IdRole) && rp.StatusRegister))
                        .Select(rp => rp.IdPermission)
                        .Distinct()
                        .ToList();
                }
            }

            var activePermissions = (await permissionRepo.GetAllAsync(p => p.StatusRegister)).ToList();
            var permissions = PermissionTreeHelper.NormalizeActiveLoginPermissions(activePermissions, allPermissionIds);

            return new UserLoginData
            {
                Roles = roles,
                Permissions = permissions
            };
        }

        public async Task<User?> FindUserByEmailAsync(string email)
        {
            var userRepo = _unitOfWork.GetRepository<User, int>();
            var users = await userRepo.GetAllAsync(u => u.Email == email);

            return users?.FirstOrDefault();
        }

        public async Task<User?> FindUserByIdentificationAsync(string identification)
        {
            var userRepo = _unitOfWork.GetRepository<User, int>();
            var users = await userRepo.GetAllAsync(u => u.Identification == identification);

            return users?.FirstOrDefault();
        }

        public async Task<User> CreateUserIfNotExistsAsync(string email, string firstName, string lastName)
        {
            var user = await FindUserByEmailAsync(email);

            if (user != null)
                return user;

            var random = new Random();
            var tempIdentification = random.Next(1000000000, int.MaxValue).ToString();

            user = new User
            {
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                IdIdentificationType = 1,
                Identification = tempIdentification,
                IdAcademicProgram = 1
            };

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Persistir usuario para obtener ID generado por la base de datos
                await _unitOfWork.GetRepository<User, int>().AddAsync(user);
                await _unitOfWork.CommitAsync();

                // Asignar rol STUDENT por defecto para nuevos usuarios
                var roleRepo = _unitOfWork.GetRepository<Role, int>();
                var studentRole = await roleRepo.GetFirstOrDefaultAsync(r => r.Code == "STUDENT", CancellationToken.None);
                
                if (studentRole != null)
                {
                    var userRole = new UserRole
                    {
                        IdUser = user.Id,
                        IdRole = studentRole.Id,
                        IdUserCreatedAt = 1 // System user
                    };
                    await _unitOfWork.GetRepository<UserRole, int>().AddAsync(userRole);
                    await _unitOfWork.CommitAsync();
                }

                await transaction.CommitAsync();
                return user;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}