using Api.Controllers;
using Application.Common.Services.Notifications;
using Application.Common.Services.Notifications.Dispatcher;
using Application.Shared.Commands;
using Application.Shared.Commands.Handlers;
using Application.Shared.Commands.ProjectPresentations;
using Application.Shared.Commands.ProjectPresentations.Handlers;
using Application.Shared.DTOs;
using Application.Shared.DTOs.ProjectPresentations;
using Application.Shared.DTOs.Roles;
using Application.Shared.DTOs.Users;
using Application.Shared.Mappings;
using Application.Shared.Queries.Users;
using AutoMapper;
using Domain.Common;
using Domain.Common.Notifications;
using Domain.Common.Users;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Auth;
using Domain.Interfaces.Services.Jobs;
using Domain.Interfaces.Services.Notifications;
using Domain.Interfaces.Services.Notifications.Handlers;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Tests.UnitTests.Application.Services
{
    public class Block4And5RegressionTests
    {
        [Fact]
        public async Task NotificationDispatcher_WhenHandlerThrows_RethrowsForHangfireRetry()
        {
            var services = new ServiceCollection();
            var handlerMock = new Mock<IEntityChangeHandler<Proposal, int>>();
            handlerMock
                .Setup(h => h.HandleChangeAsync(It.IsAny<Proposal>(), It.IsAny<Proposal>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("SMTP down"));
            handlerMock
                .Setup(h => h.HandleCreationAsync(It.IsAny<Proposal>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Create notification failure"));

            services.AddScoped(_ => handlerMock.Object);
            using var provider = services.BuildServiceProvider();

            var dispatcher = new NotificationDispatcher(
                provider.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<NotificationDispatcher>.Instance);

            var entity = new Proposal { Id = 1 };

            await FluentActions.Awaiting(() => dispatcher.DispatchEntityChangeAsync<Proposal, int>(entity, entity))
                .Should().ThrowAsync<InvalidOperationException>().WithMessage("SMTP down");

            await FluentActions.Awaiting(() => dispatcher.DispatchEntityCreationAsync<Proposal, int>(entity))
                .Should().ThrowAsync<InvalidOperationException>().WithMessage("Create notification failure");
        }

        [Fact]
        public async Task EmailNotificationEventService_WhenSendEmailReturnsFalse_ThrowsInvalidOperationException()
        {
            var configRepo = new Mock<IRepository<EmailNotificationConfig, int>>();
            configRepo
                .Setup(r => r.GetAllAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<EmailNotificationConfig, bool>>?>(),
                    It.IsAny<Func<IQueryable<EmailNotificationConfig>, IOrderedQueryable<EmailNotificationConfig>>?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>()))
                .ReturnsAsync([new EmailNotificationConfig
                {
                    Id = 7,
                    EventName = "TEST_EVT",
                    SubjectTemplate = "Hola",
                    BodyTemplate = "Cuerpo",
                    StatusRegister = true
                }]);

            var ruleRepo = new Mock<IRepository<EmailRecipientRule, int>>();
            ruleRepo
                .Setup(r => r.GetAllAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<EmailRecipientRule, bool>>?>(),
                    It.IsAny<Func<IQueryable<EmailRecipientRule>, IOrderedQueryable<EmailRecipientRule>>?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>()))
                .ReturnsAsync([new EmailRecipientRule { Id = 1, EmailNotificationConfigId = 7, Priority = 1 }]);

            var uowMock = new Mock<IUnitOfWork>();
            uowMock.Setup(u => u.GetRepository<EmailNotificationConfig, int>()).Returns(configRepo.Object);
            uowMock.Setup(u => u.GetRepository<EmailRecipientRule, int>()).Returns(ruleRepo.Object);

            var resolverMock = new Mock<IEmailRecipientResolverService>();
            resolverMock
                .Setup(r => r.ResolveRecipientsAsync(It.IsAny<List<EmailRecipientRule>>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<object?>()))
                .ReturnsAsync(new EmailRecipientsResult { To = ["user@test.edu"] });

            var notifMock = new Mock<INotificationService>();
            notifMock.Setup(n => n.SendEmailAsync(It.IsAny<EmailNotification>())).ReturnsAsync(false);

            var service = new EmailNotificationEventService(
                uowMock.Object,
                resolverMock.Object,
                notifMock.Object,
                NullLogger<EmailNotificationEventService>.Instance);

            await FluentActions.Awaiting(() => service.ProcessEventAsync("TEST_EVT", new Dictionary<string, object>()))
                .Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task CreateEntityCommandHandler_WhenCommitThrowsUnexpectedException_DoesNotWrapInInvalidOperationException()
        {
            var mapper = new MapperConfiguration(cfg => cfg.AddProfile<GenericProfile>(), NullLoggerFactory.Instance).CreateMapper();
            var repoMock = new Mock<IRepository<Role, int>>();
            var uowMock = new Mock<IUnitOfWork>();
            uowMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TimeoutException("DB timeout"));

            var handler = new CreateEntityCommandHandler<Role, int, RoleDto>(
                repoMock.Object,
                mapper,
                uowMock.Object,
                NullLogger<CreateEntityCommandHandler<Role, int, RoleDto>>.Instance);

            var cmd = new CreateEntityCommand<Role, int, RoleDto>(
                new RoleDto { Name = "Test" },
                new CurrentUserInfo { UserId = 1 });

            await FluentActions.Awaiting(() => handler.Handle(cmd, CancellationToken.None))
                .Should().ThrowAsync<TimeoutException>().WithMessage("DB timeout");
        }

        [Fact]
        public async Task PatchProjectPresentationCommandHandler_MapsIdInscriptionModalityFromId()
        {
            var mapper = new MapperConfiguration(cfg => cfg.AddProfile<GenericProfile>(), NullLoggerFactory.Instance).CreateMapper();
            var presentation = new ProjectPresentation
            {
                Id = 42,
                IdStateStage = 1,
                AttemptNumber = 1,
                StatusRegister = true
            };

            var presRepoMock = new Mock<IRepository<ProjectPresentation, int>>();
            presRepoMock.Setup(r => r.GetByIdAsync(42)).ReturnsAsync(presentation);

            var stateRepoMock = new Mock<IRepository<StateStage, int>>();
            stateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new StateStage
            {
                Id = 1,
                Code = StateStageCodes.SustPendienteProgramacion,
                Name = "Pendiente",
                StatusRegister = true
            });

            var uowMock = new Mock<IUnitOfWork>();
            uowMock.Setup(u => u.GetRepository<StateStage, int>()).Returns(stateRepoMock.Object);

            var handler = new PatchProjectPresentationCommandHandler(
                presRepoMock.Object,
                uowMock.Object,
                mapper,
                Mock.Of<IJobEnqueuer>(),
                NullLogger<PatchProjectPresentationCommandHandler>.Instance);

            var dto = await handler.Handle(
                new PatchProjectPresentationCommand(
                    42,
                    new ProjectPresentationPatchDto { LocationOrLink = "Sala 1" },
                    new CurrentUserInfo
                    {
                        UserId = 1,
                        Permissions = new HashSet<string> { PermissionCodes.ProyectoGrado.N4PGPSG }
                    }),
                CancellationToken.None);

            dto.Id.Should().Be(42);
            dto.IdInscriptionModality.Should().Be(42);
        }

        [Fact]
        public async Task UserController_GetAll_PreservesLongCursorIdWithoutIntTruncation()
        {
            var mediatorMock = new Mock<IMediator>();
            GetAllUsersWithRolesQuery? capturedQuery = null;
            mediatorMock
                .Setup(m => m.Send(It.IsAny<GetAllUsersWithRolesQuery>(), It.IsAny<CancellationToken>()))
                .Callback<IRequest<PaginatedResult<UserDto>>, CancellationToken>((q, _) =>
                    capturedQuery = (GetAllUsersWithRolesQuery)q)
                .ReturnsAsync(new PaginatedResult<UserDto>
                {
                    Items = [],
                    TotalRecords = 0,
                    PageNumber = 1,
                    PageSize = 10
                });

            var controller = new UserController(
                mediatorMock.Object,
                Mock.Of<IJobEnqueuer>(),
                Mock.Of<IRefreshTokenService>());

            var largeCursorId = (long)int.MaxValue + 12345L;
            await controller.GetAll(new PaginatedRequest { PageNumber = 1, PageSize = 10, CursorId = largeCursorId });

            capturedQuery.Should().NotBeNull();
            capturedQuery!.CursorId.Should().Be(largeCursorId);
        }
    }
}
