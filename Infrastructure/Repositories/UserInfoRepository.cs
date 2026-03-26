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
            // Acceso directo al DbContext para usar Include explícito (evita lazy loading)
            var userPermissionRepo = _unitOfWork.GetRepository<UserPermission, int>();
            var baseRepo = (BaseRepository<UserPermission, int>)userPermissionRepo;
            var context = baseRepo.Context;
            var userRoleRepo = _unitOfWork.GetRepository<UserRole, int>();
            var rolePermissionRepo = _unitOfWork.GetRepository<RolePermission, int>();

            // Permisos directos del usuario con Permission cargada via Include
            var directUserPermissions = await context.Set<UserPermission>()
                .Where(up => up.IdUser == userId)
                .Include(up => up.Permission)
                .ToListAsync();

            // Permisos por rol
            var userRoleIds = (await userRoleRepo.GetAllAsync(ur => ur.IdUser == userId))
                .Select(ur => ur.IdRole)
                .ToList();

            var rolePermissionIds = new List<int>();
            if (userRoleIds.Any())
            {
                rolePermissionIds = (await rolePermissionRepo.GetAllAsync(rp => userRoleIds.Contains(rp.IdRole)))
                    .Select(rp => rp.IdPermission)
                    .ToList();
            }

            // Cargar permisos por rol con su relación Permission
            var roleUserPermissions = new List<UserPermission>();
            if (rolePermissionIds.Any())
            {
                var rolePermissions = await context.Set<RolePermission>()
                    .Where(rp => userRoleIds.Contains(rp.IdRole))
                    .Include(rp => rp.Permission)
                    .ToListAsync();

                roleUserPermissions = rolePermissions
                    .Select(rp => new UserPermission
                    {
                        Id = 0,
                        IdUser = userId,
                        IdPermission = rp.IdPermission,
                        Permission = rp.Permission
                    })
                    .ToList();
            }

            // Combinar resultados
            var result = new List<PermissionWithUserPermission>();

            // Permisos directos (ya tienen Permission cargada via Include)
            foreach (var up in directUserPermissions)
            {
                if (up.Permission != null)
                {
                    result.Add(new PermissionWithUserPermission(up.Permission, up));
                }
            }

            // Permisos por rol
            foreach (var up in roleUserPermissions)
            {
                if (up.Permission != null)
                {
                    // Verificar que no sea duplicado de permiso directo
                    if (!result.Any(r => r.Permission.Id == up.Permission.Id))
                    {
                        result.Add(new PermissionWithUserPermission(up.Permission, up));
                    }
                }
            }

            return result;
        }

        public async Task<UserLoginData> GetUserLoginDataAsync(int userId)
        {
            var userRoleRepo = _unitOfWork.GetRepository<UserRole, int>();
            var roleRepo = _unitOfWork.GetRepository<Role, int>();
            var userPermissionRepo = _unitOfWork.GetRepository<UserPermission, int>();
            var permissionRepo = _unitOfWork.GetRepository<Permission, int>();
            var rolePermissionRepo = _unitOfWork.GetRepository<RolePermission, int>();

            var roles = (await userRoleRepo.GetAllAsync(ur => ur.IdUser == userId && ur.StatusRegister))
                .Join(await roleRepo.GetAllAsync(r => r.StatusRegister),
                    ur => ur.IdRole,
                    r => r.Id,
                    (ur, r) => new RoleInfoResult { Name = r.Name, Code = r.Code })
                .ToList();

            var directPermissionIds = (await userPermissionRepo.GetAllAsync(up => up.IdUser == userId && up.StatusRegister))
                .Select(up => up.IdPermission)
                .ToList();

            var userRoleIds = (await userRoleRepo.GetAllAsync(ur => ur.IdUser == userId && ur.StatusRegister))
                .Select(ur => ur.IdRole)
                .ToList();

            var rolePermissionIds = new List<int>();
            if (userRoleIds.Any())
            {
                rolePermissionIds = (await rolePermissionRepo.GetAllAsync(rp => userRoleIds.Contains(rp.IdRole) && rp.StatusRegister))
                    .Select(rp => rp.IdPermission)
                    .ToList();
            }

            var allPermissionIds = directPermissionIds.Union(rolePermissionIds).Distinct().ToList();
            var permissions = (await permissionRepo.GetAllAsync(p => allPermissionIds.Contains(p.Id) && p.StatusRegister))
                .Select(p => new PermissionInfo { Code = p.Code, ParentCode = p.ParentCode })
                .ToList();

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