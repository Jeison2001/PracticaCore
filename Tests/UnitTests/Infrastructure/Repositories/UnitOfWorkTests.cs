using Domain.Common;
using Domain.Entities;
using Domain.Events;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Tests.UnitTests.Infrastructure.Repositories
{
    public class UnitOfWorkTests
    {
        private static AppDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task CommitAsync_PersistsFirst_PublishesEvents_AndRunsAfterCommitActions()
        {
            using var context = CreateInMemoryContext();
            context.Set<StateStage>().Add(new StateStage
            {
                Id = 1,
                Code = "ST_1",
                Name = "State 1",
                StatusRegister = true
            });
            context.Set<Proposal>().Add(new Proposal
            {
                Id = 100,
                Title = "Prop 1",
                IdResearchLine = 1,
                IdResearchSubLine = 1,
                IdStateStage = 1,
                StatusRegister = true
            });
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var logSteps = new List<string>();
            var mediatorMock = new Mock<IMediator>();
            UnitOfWork? uow = null;

            mediatorMock
                .Setup(m => m.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .Returns<object, CancellationToken>(async (evt, ct) =>
                {
                    if (evt is ProposalStateChangedEvent e)
                    {
                        // El nuevo estado ya debe haber sido persistido en SaveChanges antes de despachar el evento
                        var persistedBeforeHandler = await context.Set<Proposal>().AsNoTracking().FirstAsync(x => x.Id == e.InscriptionModalityId, ct);
                        persistedBeforeHandler.IdStateStage.Should().Be(2);
                        logSteps.Add("HandlerObservedSavedState");

                        // Mutación adicional en el handler dentro del mismo ciclo CommitAsync
                        context.Set<Role>().Add(new Role { Id = 50, Code = "HANDLER_ROLE", Name = "Handler Role", StatusRegister = true });

                        // Llamada reentrante a CommitAsync no debe reiniciar el bucle ni ejecutar post-commit antes de tiempo
                        await uow!.CommitAsync(ct);

                        // Encolar acción post-commit
                        await uow.ExecuteAfterCommitAsync(async innerCt =>
                        {
                            // Al ejecutarse el callback, la mutación del handler ya fue guardada y _isCommitting ya es false
                            var roleInDb = await context.Set<Role>().AsNoTracking().FirstOrDefaultAsync(r => r.Id == 50, innerCt);
                            roleInDb.Should().NotBeNull();
                            logSteps.Add("PostCommitRan");

                            // Debe poder hacer un CommitAsync nuevo normalmente desde el callback post-commit
                            context.Set<Role>().Add(new Role { Id = 51, Code = "POST_COMMIT_ROLE", Name = "Post Commit", StatusRegister = true });
                            await uow.CommitAsync(innerCt);
                        }, ct);
                    }
                });

            uow = new UnitOfWork(context, mediatorMock.Object, NullLogger<UnitOfWork>.Instance);

            var proposal = await context.Set<Proposal>().FindAsync(100);
            proposal!.IdStateStage = 2;

            await uow.CommitAsync();

            logSteps.Should().Equal("HandlerObservedSavedState", "PostCommitRan");
            (await context.Set<Role>().FindAsync(50)).Should().NotBeNull();
            (await context.Set<Role>().FindAsync(51)).Should().NotBeNull();
        }

        [Fact]
        public async Task CommitAsync_WhenEventHandlerThrows_ClearsAndDoesNotExecutePostCommitActions()
        {
            using var context = CreateInMemoryContext();
            context.Set<Proposal>().Add(new Proposal
            {
                Id = 200,
                Title = "Prop 2",
                IdResearchLine = 1,
                IdResearchSubLine = 1,
                IdStateStage = 1,
                StatusRegister = true
            });
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var postCommitExecuted = false;
            var mediatorMock = new Mock<IMediator>();
            UnitOfWork? uow = null;

            mediatorMock
                .Setup(m => m.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .Returns<object, CancellationToken>(async (_, ct) =>
                {
                    await uow!.ExecuteAfterCommitAsync(_ =>
                    {
                        postCommitExecuted = true;
                        return Task.CompletedTask;
                    }, ct);

                    throw new InvalidOperationException("Handler failed");
                });

            uow = new UnitOfWork(context, mediatorMock.Object, NullLogger<UnitOfWork>.Instance);

            var proposal = await context.Set<Proposal>().FindAsync(200);
            proposal!.IdStateStage = 2;

            var act = () => uow.CommitAsync();
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Handler failed");
            postCommitExecuted.Should().BeFalse();

            // Un CommitAsync posterior exitoso no debe ejecutar la acción huérfana del fallo previo
            await uow.CommitAsync();
            postCommitExecuted.Should().BeFalse();
        }

        [Fact]
        public async Task ExecuteAfterCommitAsync_WhenNotCommitting_ExecutesImmediately()
        {
            using var context = CreateInMemoryContext();
            var mediatorMock = new Mock<IMediator>();
            var uow = new UnitOfWork(context, mediatorMock.Object, NullLogger<UnitOfWork>.Instance);

            var executed = false;
            await uow.ExecuteAfterCommitAsync(_ =>
            {
                executed = true;
                return Task.CompletedTask;
            });

            executed.Should().BeTrue();
        }

        [Fact]
        public async Task CommitAsync_MultiRoundDomainEvents_DispatchesSecondRoundAndPersistsChainedChanges()
        {
            using var context = CreateInMemoryContext();
            context.Set<Proposal>().Add(new Proposal
            {
                Id = 300,
                Title = "Prop MultiRound",
                IdResearchLine = 1,
                IdResearchSubLine = 1,
                IdStateStage = 1,
                StatusRegister = true
            });
            context.Set<ProjectFinal>().Add(new ProjectFinal
            {
                Id = 300,
                IdStateStage = 1,
                StatusRegister = true
            });
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var dispatchedEvents = new List<string>();
            var mediatorMock = new Mock<IMediator>();
            mediatorMock
                .Setup(m => m.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .Returns<object, CancellationToken>(async (evt, ct) =>
                {
                    if (evt is ProposalStateChangedEvent propEvt)
                    {
                        dispatchedEvents.Add($"Round1:Proposal:{propEvt.NewStateStageId}");

                        // Handler de ronda 1 modifica ProjectFinal disparando ProjectFinalStateChangedEvent para la ronda 2
                        var pf = await context.Set<ProjectFinal>().FindAsync([300], ct);
                        pf!.IdStateStage = 2;
                        context.Set<Role>().Add(new Role { Id = 60, Code = "R1_ROLE", Name = "Round 1 Role", StatusRegister = true });
                    }
                    else if (evt is ProjectFinalStateChangedEvent pfEvt)
                    {
                        dispatchedEvents.Add($"Round2:ProjectFinal:{pfEvt.NewStateStageId}");

                        // La mutación de ronda 1 ya debió guardarse antes de despachar la ronda 2
                        var roleR1 = await context.Set<Role>().AsNoTracking().FirstOrDefaultAsync(r => r.Id == 60, ct);
                        roleR1.Should().NotBeNull();

                        context.Set<Role>().Add(new Role { Id = 61, Code = "R2_ROLE", Name = "Round 2 Role", StatusRegister = true });
                    }
                });

            var uow = new UnitOfWork(context, mediatorMock.Object, NullLogger<UnitOfWork>.Instance);

            var proposal = await context.Set<Proposal>().FindAsync(300);
            proposal!.IdStateStage = 2;

            await uow.CommitAsync();

            dispatchedEvents.Should().Equal("Round1:Proposal:2", "Round2:ProjectFinal:2");
            mediatorMock.Verify(m => m.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            (await context.Set<ProjectFinal>().AsNoTracking().FirstAsync(x => x.Id == 300)).IdStateStage.Should().Be(2);
            (await context.Set<Role>().FindAsync(60)).Should().NotBeNull();
            (await context.Set<Role>().FindAsync(61)).Should().NotBeNull();
        }

        [Fact]
        public async Task CommitAsync_WhenEventsExceedMaxRounds_StopsAtTenRoundsWithoutInfiniteLoop()
        {
            using var context = CreateInMemoryContext();
            context.Set<Proposal>().Add(new Proposal
            {
                Id = 400,
                Title = "Prop MaxRounds",
                IdResearchLine = 1,
                IdResearchSubLine = 1,
                IdStateStage = 1,
                StatusRegister = true
            });
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var publishCount = 0;
            var mediatorMock = new Mock<IMediator>();
            mediatorMock
                .Setup(m => m.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .Returns<object, CancellationToken>(async (evt, ct) =>
                {
                    if (evt is ProposalStateChangedEvent)
                    {
                        publishCount++;
                        // Cada publicación genera un nuevo evento sobre la entidad rastreada
                        var prop = await context.Set<Proposal>().FindAsync([400], ct);
                        prop!.IdStateStage += 1;
                    }
                });

            var uow = new UnitOfWork(context, mediatorMock.Object, NullLogger<UnitOfWork>.Instance);

            var proposal = await context.Set<Proposal>().FindAsync(400);
            proposal!.IdStateStage = 2;

            await uow.CommitAsync();

            publishCount.Should().Be(10);
            mediatorMock.Verify(m => m.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Exactly(10));
            proposal.DomainEvents.Should().HaveCount(1);
        }

        [Fact]
        public async Task BeginTransactionAsync_DefersPostCommitActionsAcrossIntermediateCommits_UntilTransactionCommitAsync()
        {
            using var context = CreateInMemoryContext();
            context.Set<Proposal>().Add(new Proposal
            {
                Id = 500,
                Title = "Prop Explicit Tx",
                IdResearchLine = 1,
                IdResearchSubLine = 1,
                IdStateStage = 1,
                StatusRegister = true
            });
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var steps = new List<string>();
            var mediatorMock = new Mock<IMediator>();
            UnitOfWork? uow = null;

            mediatorMock
                .Setup(m => m.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .Returns<object, CancellationToken>(async (evt, ct) =>
                {
                    if (evt is ProposalStateChangedEvent)
                    {
                        steps.Add("EventDispatched");
                        await uow!.ExecuteAfterCommitAsync(_ =>
                        {
                            steps.Add("PostCommitFromEvent");
                            return Task.CompletedTask;
                        }, ct);
                    }
                });

            uow = new UnitOfWork(context, mediatorMock.Object, NullLogger<UnitOfWork>.Instance);

            await using (var tx = await uow.BeginTransactionAsync())
            {
                var proposal = await context.Set<Proposal>().FindAsync(500);
                proposal!.IdStateStage = 2;

                await uow.CommitAsync();
                steps.Should().Equal("EventDispatched");

                await uow.ExecuteAfterCommitAsync(_ =>
                {
                    steps.Add("PostCommitDirectDuringTx");
                    return Task.CompletedTask;
                });

                context.Set<Role>().Add(new Role { Id = 70, Code = "TX_ROLE", Name = "Tx Role", StatusRegister = true });
                await uow.CommitAsync();
                steps.Should().Equal("EventDispatched");

                steps.Add("BeforeTxCommit");
                await tx.CommitAsync();
                steps.Add("AfterTxCommit");
            }

            steps.Should().Equal(
                "EventDispatched",
                "BeforeTxCommit",
                "PostCommitFromEvent",
                "PostCommitDirectDuringTx",
                "AfterTxCommit");
        }

        [Fact]
        public async Task BeginTransactionAsync_WhenRolledBackOrDisposedWithoutCommit_ClearsPostCommitActions()
        {
            using var context = CreateInMemoryContext();
            var mediatorMock = new Mock<IMediator>();
            var uow = new UnitOfWork(context, mediatorMock.Object, NullLogger<UnitOfWork>.Instance);

            var executed = false;

            // Caso 1: Rollback explícito limpia _postCommitActions
            await using (var tx = await uow.BeginTransactionAsync())
            {
                await uow.ExecuteAfterCommitAsync(_ =>
                {
                    executed = true;
                    return Task.CompletedTask;
                });
                context.Set<Role>().Add(new Role { Id = 80, Code = "RB_ROLE", Name = "Rollback Role", StatusRegister = true });
                await uow.CommitAsync();
                executed.Should().BeFalse();

                await tx.RollbackAsync();
            }
            executed.Should().BeFalse();

            // Caso 2: Dispose sin CommitAsync limpia _postCommitActions y no contamina un CommitAsync posterior
            await using (var tx2 = await uow.BeginTransactionAsync())
            {
                await uow.ExecuteAfterCommitAsync(_ =>
                {
                    executed = true;
                    return Task.CompletedTask;
                });
            }
            executed.Should().BeFalse();

            context.Set<Role>().Add(new Role { Id = 81, Code = "AFTER_RB_ROLE", Name = "After Rollback", StatusRegister = true });
            await uow.CommitAsync();
            executed.Should().BeFalse();
        }
    }
}
