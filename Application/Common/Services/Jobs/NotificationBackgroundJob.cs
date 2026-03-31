using Application.Common.Services.Jobs;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Notifications;
using Domain.Interfaces.Services.Notifications.Builders;
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

        public async Task HandleProposalCreationAsync(int proposalId)
        {
            try
            {
                _logger.LogInformation("Processing proposal creation notification for Proposal ID: {ProposalId}", proposalId);

                var queueService = _serviceProvider.GetService<IEmailNotificationQueueService>();
                var eventDataBuilder = _serviceProvider.GetService<IProposalEventDataBuilder>();

                if (queueService == null || eventDataBuilder == null)
                {
                    _logger.LogError("Required services not found for proposal creation notification");
                    return;
                }

                var eventData = await eventDataBuilder.BuildProposalEventDataAsync(proposalId, "PROPOSAL_SUBMITTED");
                queueService.EnqueueEventNotification("PROPOSAL_SUBMITTED", eventData);

                _logger.LogInformation("Proposal creation notification enqueued - Proposal ID: {ProposalId}", proposalId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing proposal creation notification for ID {Id}", proposalId);
                throw;
            }
        }

        public async Task HandleTeachingAssignmentCreationAsync(int assignmentId)
        {
            try
            {
                _logger.LogInformation("Processing teaching assignment creation notification for Assignment ID: {AssignmentId}", assignmentId);

                var queueService = _serviceProvider.GetService<IEmailNotificationQueueService>();
                var eventDataBuilder = _serviceProvider.GetService<ITeachingAssignmentEventDataBuilder>();

                if (queueService == null || eventDataBuilder == null)
                {
                    _logger.LogError("Required services not found for teaching assignment creation notification");
                    return;
                }

                var eventData = await eventDataBuilder.BuildTeachingAssignmentEventDataAsync(assignmentId, "TEACHING_ASSIGNMENT_ASSIGNED");
                queueService.EnqueueEventNotification("TEACHING_ASSIGNMENT_ASSIGNED", eventData);

                _logger.LogInformation("Teaching assignment creation notification enqueued - Assignment ID: {AssignmentId}", assignmentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing teaching assignment creation notification for ID {Id}", assignmentId);
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

        public async Task HandlePreliminaryProjectChangeAsync(int preliminaryId, int oldStateId)
        {
            try
            {
                var repository = _serviceProvider.GetService<IRepository<PreliminaryProject, int>>();

                if (repository == null)
                {
                    _logger.LogError("Repository not found for PreliminaryProject");
                    return;
                }

                var entity = await repository.GetByIdAsync(preliminaryId);
                if (entity == null)
                {
                    _logger.LogWarning("PreliminaryProject with ID {Id} not found during background processing", preliminaryId);
                    return;
                }

                var oldEntity = new PreliminaryProject { Id = preliminaryId, IdStateStage = oldStateId };
                await _dispatcher.DispatchEntityChangeAsync<PreliminaryProject, int>(oldEntity, entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing preliminary project change job for ID {Id}", preliminaryId);
                throw;
            }
        }

        public async Task HandleProjectFinalChangeAsync(int projectFinalId, int oldStateId)
        {
            try
            {
                var repository = _serviceProvider.GetService<IRepository<ProjectFinal, int>>();

                if (repository == null)
                {
                    _logger.LogError("Repository not found for ProjectFinal");
                    return;
                }

                var entity = await repository.GetByIdAsync(projectFinalId);
                if (entity == null)
                {
                    _logger.LogWarning("ProjectFinal with ID {Id} not found during background processing", projectFinalId);
                    return;
                }

                var oldEntity = new ProjectFinal { Id = projectFinalId, IdStateStage = oldStateId };
                await _dispatcher.DispatchEntityChangeAsync<ProjectFinal, int>(oldEntity, entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing project final change job for ID {Id}", projectFinalId);
                throw;
            }
        }

        public async Task HandleInscriptionChangeAsync(int inscriptionId, int oldStateId)
        {
            try
            {
                var repository = _serviceProvider.GetService<IRepository<InscriptionModality, int>>();

                if (repository == null)
                {
                    _logger.LogError("Repository not found for InscriptionModality");
                    return;
                }

                var entity = await repository.GetByIdAsync(inscriptionId);
                if (entity == null)
                {
                    _logger.LogWarning("InscriptionModality with ID {Id} not found during background processing", inscriptionId);
                    return;
                }

                var oldEntity = new InscriptionModality { Id = inscriptionId, IdStateInscription = oldStateId };
                await _dispatcher.DispatchEntityChangeAsync<InscriptionModality, int>(oldEntity, entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing inscription change job for ID {Id}", inscriptionId);
                throw;
            }
        }

        public async Task HandleInscriptionCreationAsync(int inscriptionId, int modalityId, int academicPeriodId, IList<int> studentIds)
        {
            try
            {
                _logger.LogInformation("Processing inscription creation notification for Inscription ID: {InscriptionId}", inscriptionId);

                var queueService = _serviceProvider.GetService<IEmailNotificationQueueService>();
                var eventDataBuilder = _serviceProvider.GetService<IInscriptionEventDataBuilder>();

                if (queueService == null || eventDataBuilder == null)
                {
                    _logger.LogError("Required services not found for inscription creation notification");
                    return;
                }

                var eventData = await eventDataBuilder.BuildBasicInscriptionDataAsync(
                    inscriptionId,
                    modalityId,
                    academicPeriodId,
                    studentIds);

                queueService.EnqueueEventNotification("INSCRIPTION_CREATED", eventData);

                _logger.LogInformation("Inscription creation notification enqueued - Inscription ID: {InscriptionId}", inscriptionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing inscription creation notification for ID {Id}", inscriptionId);
                throw;
            }
        }
    }
}
