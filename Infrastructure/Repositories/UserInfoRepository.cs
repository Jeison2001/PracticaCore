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

        public async Task<List<Permission>> GetUserPermissionsFullInfoAsync(int userId)
        {
            var userPermissionRepo = _unitOfWork.GetRepository<UserPermission, int>();
            var permissionRepo = _unitOfWork.GetRepository<Permission, int>();
            var userRoleRepo = _unitOfWork.GetRepository<UserRole, int>();
            var rolePermissionRepo = _unitOfWork.GetRepository<RolePermission, int>();

            var directPermissionIds = (await userPermissionRepo.GetAllAsync(up => up.IdUser == userId))
                .Select(up => up.IdPermission)
                .ToList();

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

            var allPermissionIds = directPermissionIds.Union(rolePermissionIds).Distinct().ToList();
            var permissions = (await permissionRepo.GetAllAsync(p => allPermissionIds.Contains(p.Id))).ToList();

            return permissions;
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

            await _unitOfWork.GetRepository<User, int>().AddAsync(user);
            await _unitOfWork.CommitAsync();

            return user;
        }
    }
}