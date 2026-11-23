using Domain.Entities;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests.UnitTests.Infrastructure.Services
{
    public class AcademicPeriodServiceTests
    {
        private sealed class ManualTimeProvider(DateTimeOffset initialUtcNow) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() => initialUtcNow;
        }

        private static AppDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"academic-period-{Guid.NewGuid():N}")
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task GetActiveAcademicPeriodAsync_ComparesExactUtcInstants_AcrossColombiaMidnightBoundary()
        {
            using var ctx = CreateInMemoryContext();

            // Periodo activo del 2026-01-15 00:00 COT (05:00Z) al 2026-06-30 23:59:59 COT (2026-07-01 04:59:59Z)
            var startUtc = new DateTimeOffset(2026, 1, 15, 5, 0, 0, TimeSpan.Zero);
            var endColombiaStoredWithOffset = new DateTimeOffset(2026, 6, 30, 23, 59, 59, TimeSpan.FromHours(-5));

            ctx.Set<AcademicPeriod>().Add(new AcademicPeriod
            {
                Id = 1,
                Code = "2026-1",
                StartDate = startUtc,
                EndDate = endColombiaStoredWithOffset,
                StatusRegister = true
            });
            await ctx.SaveChangesAsync();

            // Caso 1: 2026-06-30 22:00 COT == 2026-07-01 03:00Z -> sigue activo
            var serviceStillActive = new AcademicPeriodService(
                ctx,
                new ManualTimeProvider(new DateTimeOffset(2026, 7, 1, 3, 0, 0, TimeSpan.Zero)));
            var activeAt22Col = await serviceStillActive.GetActiveAcademicPeriodAsync();
            activeAt22Col.Should().NotBeNull();
            activeAt22Col!.Code.Should().Be("2026-1");

            // Caso 2: 2026-07-01 00:00:01 COT == 2026-07-01 05:00:01Z -> ya expiró aunque siga siendo el mismo día UTC
            var serviceExpiredSameUtcDay = new AcademicPeriodService(
                ctx,
                new ManualTimeProvider(new DateTimeOffset(2026, 7, 1, 5, 0, 1, TimeSpan.Zero)));
            var expired = await serviceExpiredSameUtcDay.GetActiveAcademicPeriodAsync();
            expired.Should().BeNull();

            // Caso 3: 2026-01-15 04:00Z -> mismo día UTC que StartDate, pero 1 hora antes del instante de inicio
            var serviceBeforeStartSameUtcDay = new AcademicPeriodService(
                ctx,
                new ManualTimeProvider(new DateTimeOffset(2026, 1, 15, 4, 0, 0, TimeSpan.Zero)));
            var notStarted = await serviceBeforeStartSameUtcDay.GetActiveAcademicPeriodAsync();
            notStarted.Should().BeNull();
        }
    }
}
