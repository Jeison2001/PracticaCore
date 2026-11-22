using Domain.Entities;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Services.Auth;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Tests.UnitTests.Infrastructure.Services.Auth
{
    public class RefreshTokenServiceTests
    {
        private static AppDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private static IConfiguration BuildConfiguration(string? maxActiveSessions = null, bool includeMaxSessionsKey = false)
        {
            var settings = new Dictionary<string, string?>
            {
                ["Jwt:RefreshTokenExpirationDays"] = "7"
            };

            if (includeMaxSessionsKey || maxActiveSessions != null)
            {
                settings["Jwt:MaxActiveSessions"] = maxActiveSessions;
            }

            return new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();
        }

        private static RefreshTokenService CreateService(AppDbContext context, IConfiguration configuration)
        {
            var unitOfWork = new UnitOfWork(context, Mock.Of<IMediator>(), NullLogger<UnitOfWork>.Instance);
            return new RefreshTokenService(context, unitOfWork, configuration);
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("", true)]
        [InlineData("   ", true)]
        public async Task GenerateAsync_WhenMaxActiveSessionsNotConfiguredOrBlank_DefaultsToTwoAndEvictsOldestActiveToken(
            string? rawValue,
            bool includeKey)
        {
            using var context = CreateInMemoryContext();
            var service = CreateService(context, BuildConfiguration(rawValue, includeKey));
            const int userId = 10;

            var first = await service.GenerateAsync(userId);
            var second = await service.GenerateAsync(userId);
            var third = await service.GenerateAsync(userId);

            var remaining = await context.RefreshTokens
                .AsNoTracking()
                .Where(rt => rt.IdUser == userId)
                .ToListAsync();

            remaining.Should().HaveCount(RefreshTokenService.DefaultMaxActiveSessions);
            remaining.Select(rt => rt.Token).Should().BeEquivalentTo(new[] { second.Token, third.Token });
            remaining.Select(rt => rt.Token).Should().NotContain(first.Token);
        }

        [Fact]
        public async Task GenerateAsync_WhenMaxActiveSessionsIsOne_EvictsFirstTokenOnSecondLogin()
        {
            using var context = CreateInMemoryContext();
            var service = CreateService(context, BuildConfiguration("1"));
            const int userId = 20;

            var first = await service.GenerateAsync(userId);
            var second = await service.GenerateAsync(userId);

            var remaining = await context.RefreshTokens
                .AsNoTracking()
                .Where(rt => rt.IdUser == userId)
                .ToListAsync();

            remaining.Should().ContainSingle();
            remaining[0].Token.Should().Be(second.Token);
            remaining.Select(rt => rt.Token).Should().NotContain(first.Token);
        }

        [Fact]
        public async Task GenerateAsync_WhenMaxActiveSessionsIsZero_AllowsUnlimitedConcurrentSessions()
        {
            using var context = CreateInMemoryContext();
            var service = CreateService(context, BuildConfiguration("0"));
            const int userId = 30;

            var t1 = await service.GenerateAsync(userId);
            var t2 = await service.GenerateAsync(userId);
            var t3 = await service.GenerateAsync(userId);
            var t4 = await service.GenerateAsync(userId);

            var remaining = await context.RefreshTokens
                .AsNoTracking()
                .Where(rt => rt.IdUser == userId && rt.RevokedAt == null)
                .ToListAsync();

            remaining.Should().HaveCount(4);
            remaining.Select(rt => rt.Token).Should().BeEquivalentTo(new[] { t1.Token, t2.Token, t3.Token, t4.Token });
        }

        [Theory]
        [InlineData("-1")]
        [InlineData("-5")]
        [InlineData("invalid")]
        [InlineData("2.5")]
        public void Constructor_WhenMaxActiveSessionsIsInvalid_ThrowsInvalidOperationException(string invalidValue)
        {
            using var context = CreateInMemoryContext();
            var config = BuildConfiguration(invalidValue);
            var unitOfWork = new UnitOfWork(context, Mock.Of<IMediator>(), NullLogger<UnitOfWork>.Instance);

            var act = () => new RefreshTokenService(context, unitOfWork, config);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage($"Jwt:MaxActiveSessions tiene un valor inválido: '{invalidValue}'. Debe ser un entero mayor o igual a cero.");
        }

        [Fact]
        public async Task GenerateAsync_PreservesAlreadyRevokedTokensForReuseDetection_AndOnlyEvictsActiveExcessForTargetUser()
        {
            using var context = CreateInMemoryContext();
            var service = CreateService(context, BuildConfiguration("2"));
            const int userId = 40;
            const int otherUserId = 41;

            var revokedHistorical = new RefreshToken
            {
                IdUser = userId,
                Token = "revoked-historical-token",
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-30),
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
                RevokedAt = DateTimeOffset.UtcNow.AddMinutes(-20),
                StatusRegister = true,
                OperationRegister = "SEED"
            };
            var otherUserActive = new RefreshToken
            {
                IdUser = otherUserId,
                Token = "other-user-active-token",
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-25),
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
                StatusRegister = true,
                OperationRegister = "SEED"
            };
            context.RefreshTokens.AddRange(revokedHistorical, otherUserActive);
            await context.SaveChangesAsync();

            var active1 = await service.GenerateAsync(userId);
            var active2 = await service.GenerateAsync(userId);
            var active3 = await service.GenerateAsync(userId);

            var userTokens = await context.RefreshTokens
                .AsNoTracking()
                .Where(rt => rt.IdUser == userId)
                .ToListAsync();

            // El token revocado histórico se conserva intacto para detección de reúso/fraude
            userTokens.Should().ContainSingle(rt => rt.Token == revokedHistorical.Token && rt.RevokedAt != null);

            // De los activos, solo quedan los 2 más recientes; el primero activo fue eliminado físicamente
            var activeUserTokens = userTokens.Where(rt => rt.RevokedAt == null).ToList();
            activeUserTokens.Should().HaveCount(2);
            activeUserTokens.Select(rt => rt.Token).Should().BeEquivalentTo(new[] { active2.Token, active3.Token });
            userTokens.Select(rt => rt.Token).Should().NotContain(active1.Token);

            // El token de otro usuario no se ve afectado
            var otherTokens = await context.RefreshTokens
                .AsNoTracking()
                .Where(rt => rt.IdUser == otherUserId)
                .ToListAsync();
            otherTokens.Should().ContainSingle(rt => rt.Token == otherUserActive.Token && rt.RevokedAt == null);
        }
    }
}
