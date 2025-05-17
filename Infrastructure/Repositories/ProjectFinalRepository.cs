using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ProjectFinalRepository : BaseRepository<ProjectFinal, int>, IProjectFinalRepository
    {
        private readonly AppDbContext _context;
        public ProjectFinalRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<ProjectFinal>> GetByUserIdAsync(int userId, bool? status = null)
        {
            // TODO: Implementar consulta real
            return await _context.ProjectFinals.Where(p => p.IdUserCreatedAt == userId).ToListAsync();
        }

        public async Task<List<ProjectFinal>> GetByTeacherIdAsync(int teacherId, int pageNumber, int pageSize, string? sortBy, bool isDescending, Dictionary<string, string>? filters)
        {
            // TODO: Implementar consulta real
            return await _context.ProjectFinals.Take(pageSize).ToListAsync();
        }

        public async Task<List<ProjectFinal>> GetAllWithDetailsAsync(int pageNumber, int pageSize, string? sortBy, bool isDescending, Dictionary<string, string>? filters)
        {
            // TODO: Implementar consulta real
            return await _context.ProjectFinals.Take(pageSize).ToListAsync();
        }
    }
}
