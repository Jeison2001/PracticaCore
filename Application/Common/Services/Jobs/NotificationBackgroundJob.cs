using Application.Common.Services.Jobs;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Notifications.Dispatcher;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Application.Common.Services.Jobs
{
    public class NotificationBackgroundJob : INotificationBackgroundJob
    {
        private readonly INotificationDispatcher _dispatcher;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NotificationBackgroundJob> _logger;

        public NotificationBackgroundJob(
            INotificationDispatcher dispatcher,
            IServiceProvider serviceProvider,
            ILogger<NotificationBackgroundJob> logger)
        {
            _dispatcher = dispatcher;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task HandleEntityCreationAsync<T, TId>(TId id) where T : BaseEntity<TId> where TId : struct
        {
            try 
            {
                var repository = _serviceProvider.GetService<IRepository<T, TId>>();
                
                if (repository == null)
                {
                    _logger.LogError("Repository not found for type {EntityType}", typeof(T).Name);
                    return;
                }

                var entity = await repository.GetByIdAsync(id);
                if (entity == null)
                {
                    _logger.LogWarning("Entity {EntityType} with ID {Id} not found during background processing", typeof(T).Name, id);
                    return;
                }

                await _dispatcher.DispatchEntityCreationAsync<T, TId>(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing creation job for {EntityType} ID {Id}", typeof(T).Name, id);
                throw;
            }
        }

        public async Task HandleProposalChangeAsync(int proposalId, int oldStateId)
        {
            try
            {
                var repository = _serviceProvider.GetService<IRepository<Proposal, int>>();
                
                if (repository == null)
                {
                     _logger.LogError("Repository not found for Proposal");
                     return;
                }

                var entity = await repository.GetByIdAsync(proposalId);
                if (entity == null)
                {
                    _logger.LogWarning("Proposal with ID {Id} not found during background processing", proposalId);
                    return;
                }

                var oldEntity = new Proposal { Id = proposalId, IdStateStage = oldStateId };
                await _dispatcher.DispatchEntityChangeAsync<Proposal, int>(oldEntity, entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing proposal change job for ID {Id}", proposalId);
                throw;
            }
        }

        public async Task HandleTeachingAssignmentChangeAsync(int assignmentId, int oldTeacherId)
        {
             try
            {
                var repository = _serviceProvider.GetService<IRepository<TeachingAssignment, int>>();
                
                if (repository == null)
                {
                     _logger.LogError("Repository not found for TeachingAssignment");
                     return;
                }

                var entity = await repository.GetByIdAsync(assignmentId);
                if (entity == null)
                {
                    _logger.LogWarning("TeachingAssignment with ID {Id} not found during background processing", assignmentId);
                    return;
                }

                var oldEntity = new TeachingAssignment { Id = assignmentId, IdTeacher = oldTeacherId };
                await _dispatcher.DispatchEntityChangeAsync<TeachingAssignment, int>(oldEntity, entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing teaching assignment change job for ID {Id}", assignmentId);
                throw;
            }
        }

        public async Task HandleAcademicPracticeChangeAsync(int practiceId, int oldStateId)
        {
            try
            {
                var repository = _serviceProvider.GetService<IRepository<AcademicPractice, int>>();

                if (repository == null)
                {
                    _logger.LogError("Repository not found for AcademicPractice");
                    return;
                }

                var entity = await repository.GetByIdAsync(practiceId);
                if (entity == null)
                {
                    _logger.LogWarning("AcademicPractice with ID {Id} not found during background processing", practiceId);
                    return;
                }

                var oldEntity = new AcademicPractice { Id = practiceId, IdStateStage = oldStateId };
                await _dispatcher.DispatchEntityChangeAsync<AcademicPractice, int>(oldEntity, entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing academic practice change job for ID {Id}", practiceId);
                throw;
            }
        }
    }
}
