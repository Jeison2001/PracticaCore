using Domain.Entities;
using Domain.Interfaces;
using Domain.Interfaces.Notifications;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Application.Common.Services
{
    public class StudentDataService : IStudentDataService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<StudentDataService> _logger;

        public StudentDataService(IUnitOfWork unitOfWork, ILogger<StudentDataService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<(string Names, string Emails, int Count)> GetStudentDataByProposalAsync(int proposalId)
        {
            try
            {
                // Obtener la propuesta para verificar que existe
                var proposalRepo = _unitOfWork.GetRepository<Proposal, int>();
                var proposal = await proposalRepo.GetByIdAsync(proposalId);
                if (proposal == null)
                {
                    _logger.LogWarning("Proposal not found with ID: {ProposalId}", proposalId);
                    return (string.Empty, string.Empty, 0);
                }

                // Obtener la InscriptionModality asociada a la propuesta
                var inscriptionModalityRepo = _unitOfWork.GetRepository<InscriptionModality, int>();
                var inscriptionModality = await inscriptionModalityRepo
                    .GetFirstOrDefaultAsync(im => im.Proposal != null && im.Proposal.Id == proposalId, CancellationToken.None);

                if (inscriptionModality == null)
                {
                    _logger.LogDebug("No InscriptionModality found for Proposal ID: {ProposalId}", proposalId);
                    return (string.Empty, string.Empty, 0);
                }

                // Obtener usuarios asociados a la inscripción
                var userInscriptionModalityRepo = _unitOfWork.GetRepository<UserInscriptionModality, int>();
                var userInscriptions = await userInscriptionModalityRepo
                    .GetAllAsync(uim => uim.IdInscriptionModality == inscriptionModality.Id);

                if (!userInscriptions.Any())
                {
                    _logger.LogDebug("No students found for Proposal ID: {ProposalId}", proposalId);
                    return (string.Empty, string.Empty, 0);
                }

                var userIds = userInscriptions.Select(uim => uim.IdUser);
                return await GetStudentDataByUserIdsAsync(userIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving student data for Proposal ID: {ProposalId}", proposalId);
                return (string.Empty, string.Empty, 0);
            }
        }

        public async Task<(string Names, string Emails, int Count)> GetStudentDataByInscriptionAsync(int inscriptionId)
        {
            try
            {
                // Migrar lógica desde InscriptionNotificationService.AddStudentDataAsync()
                var userInscriptionModalityRepo = _unitOfWork.GetRepository<UserInscriptionModality, int>();
                
                // Obtener usuarios asociados a la inscripción
                var userInscriptions = await userInscriptionModalityRepo
                    .GetAllAsync(uim => uim.IdInscriptionModality == inscriptionId);

                if (!userInscriptions.Any())
                {
                    _logger.LogDebug("No students found for Inscription ID: {InscriptionId}", inscriptionId);
                    return (string.Empty, string.Empty, 0);
                }

                var userIds = userInscriptions.Select(uim => uim.IdUser);
                return await GetStudentDataByUserIdsAsync(userIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving student data for Inscription ID: {InscriptionId}", inscriptionId);
                return (string.Empty, string.Empty, 0);
            }
        }

        public async Task<(string Names, string Emails, int Count)> GetStudentDataByUserIdsAsync(IEnumerable<int> userIds)
        {
            try
            {
                if (!userIds.Any())
                {
                    return (string.Empty, string.Empty, 0);
                }

                var userRepo = _unitOfWork.GetRepository<User, int>();
                var studentNames = new StringBuilder();
                var studentEmails = new StringBuilder();
                var count = 0;

                foreach (var userId in userIds.Distinct())
                {
                    var user = await userRepo.GetByIdAsync(userId);
                    if (user != null)
                    {
                        if (count > 0)
                        {
                            studentNames.Append(", ");
                            studentEmails.Append(", ");
                        }
                        
                        studentNames.Append($"{user.FirstName} {user.LastName}");
                        studentEmails.Append(user.Email);
                        count++;
                    }
                }

                var names = studentNames.ToString();
                var emails = studentEmails.ToString();

                _logger.LogDebug("Retrieved {Count} students from {UserIdCount} user IDs", count, userIds.Count());
                return (names, emails, count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving student data for user IDs: {UserIds}", string.Join(", ", userIds));
                return (string.Empty, string.Empty, 0);
            }
        }
    }
}
