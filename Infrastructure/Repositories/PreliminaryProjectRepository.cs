using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class PreliminaryProjectRepository : BaseRepository<PreliminaryProject, int>, IPreliminaryProjectRepository
    {
        private readonly AppDbContext _context;
        public PreliminaryProjectRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<PreliminaryProject>> GetByUserIdAsync(int userId, bool? status = null)
        {
            // TODO: Implementar consulta real
            return await _context.PreliminaryProjects.Where(p => p.IdUserCreatedAt == userId).ToListAsync();
        }

        public async Task<List<PreliminaryProject>> GetByTeacherIdAsync(int teacherId, int pageNumber, int pageSize, string? sortBy, bool isDescending, Dictionary<string, string>? filters)
        {
            // TODO: Implementar consulta real
            return await _context.PreliminaryProjects.Take(pageSize).ToListAsync();
        }

        public async Task<List<PreliminaryProject>> GetAllWithDetailsAsync(int pageNumber, int pageSize, string? sortBy, bool isDescending, Dictionary<string, string>? filters)
        {
            // TODO: Implementar consulta real
            return await _context.PreliminaryProjects.Take(pageSize).ToListAsync();
        }
    }
}
