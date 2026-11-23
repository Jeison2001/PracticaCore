using Domain.Entities;
using Domain.Interfaces.Services;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services
{
    public class AcademicPeriodService : IAcademicPeriodService
    {
        private readonly AppDbContext _context;
        private readonly TimeProvider _timeProvider;

        public AcademicPeriodService(AppDbContext context, TimeProvider? timeProvider = null)
        {
            _context = context;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        public async Task<AcademicPeriod?> GetActiveAcademicPeriodAsync()
        {
            var nowUtc = _timeProvider.GetUtcNow();

            return await _context.Set<AcademicPeriod>()
                .Where(ap => ap.StatusRegister &&
                           ap.StartDate <= nowUtc &&
                           ap.EndDate >= nowUtc)
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

