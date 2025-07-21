using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;

namespace Infrastructure.Services.Background
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<UserIdentificationResult> GetUserIdByIdentification(int idIdentificationType, string identification)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.IdIdentificationType == idIdentificationType && u.Identification == identification);

            if (user == null)
            {
                // Manejar el caso en que no se encuentra el usuario
                return new UserIdentificationResult { Id = 0, UserName = "" }; // O lanzar una excepción, dependiendo de la lógica de la aplicación
            }

            return new UserIdentificationResult { Id = user.Id, UserName = user.FirstName + " " + user.LastName };
        }
    }
}