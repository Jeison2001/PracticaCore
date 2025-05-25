using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services
{
    public class AcademicPeriodService : IAcademicPeriodService
    {
        private readonly AppDbContext _context;

        public AcademicPeriodService(AppDbContext context)
        {
            _context = context;
        }        public async Task<AcademicPeriod?> GetActiveAcademicPeriodAsync()
        {
            var currentDate = DateTime.UtcNow.Date;
            
            return await _context.Set<AcademicPeriod>()
                .Where(ap => ap.StatusRegister && 
                           currentDate >= ap.StartDate.Date && 
                           currentDate <= ap.EndDate.Date)
                .OrderBy(ap => ap.StartDate)
                .FirstOrDefaultAsync();
        }

        public async Task<AcademicPeriod?> GetAcademicPeriodByIdAsync(int id)
        {
            return await _context.Set<AcademicPeriod>()
                .FirstOrDefaultAsync(ap => ap.Id == id && ap.StatusRegister);
        }
    }
}
