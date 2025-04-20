using Domain.Entities;
using Domain.Interfaces;
using Domain.Interfaces.Auth;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public async Task<List<string>> GetUserPermissionsAsync(int userId)
        {
            var userPermissionRepo = _unitOfWork.GetRepository<UserPermission, int>();
            var permissionRepo = _unitOfWork.GetRepository<Permission, int>();
            var userRoleRepo = _unitOfWork.GetRepository<UserRole, int>();
            var rolePermissionRepo = _unitOfWork.GetRepository<RolePermission, int>();

            // Obtener permisos directos asignados al usuario
            var directPermissions = (await userPermissionRepo.GetAllAsync(up => up.IdUser == userId))
                .Join(await permissionRepo.GetAllAsync(),
                    up => up.IdPermission,
                    p => p.Id,
                    (up, p) => p.Code)
                .ToList();

            // Obtener permisos a través de roles
            var userRoles = (await userRoleRepo.GetAllAsync(ur => ur.IdUser == userId))
                .Select(ur => ur.IdRole)
                .ToList();

            var rolePermissions = new List<string>();
            if (userRoles.Any())
            {
                rolePermissions = (await rolePermissionRepo.GetAllAsync(rp => userRoles.Contains(rp.IdRole)))
                    .Join(await permissionRepo.GetAllAsync(),
                        rp => rp.IdPermission,
                        p => p.Id,
                        (rp, p) => p.Code)
                    .ToList();
            }

            // Combinar ambos conjuntos de permisos y eliminar duplicados
            return directPermissions.Union(rolePermissions).ToList();
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

            // Crear nuevo usuario con datos básicos
            user = new User
            {
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                // Valores por defecto para campos requeridos
                IdIdentificationType = 1, // Asume que existe al menos un tipo de identificación
                Identification = "Por asignar",
                IdAcademicProgram = 1 // Asume que existe al menos un programa académico
            };

            await _unitOfWork.GetRepository<User, int>().AddAsync(user);
            await _unitOfWork.CommitAsync();

            return user;
        }
    }
}