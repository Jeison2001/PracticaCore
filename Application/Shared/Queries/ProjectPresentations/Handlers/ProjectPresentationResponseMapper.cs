using Application.Shared.DTOs.ProjectFinals;
using Application.Shared.DTOs.ProjectPresentations;
using Application.Shared.DTOs.Proposals;
using Application.Shared.DTOs.StateStages;
using Application.Shared.DTOs.Teachers;
using Application.Shared.DTOs.UserInscriptionModalities;
using Domain.Common.Teachers;
using Domain.Entities;

namespace Application.Shared.Queries.ProjectPresentations.Handlers
{
    /// <summary>
    /// Mapeador compartido de entidades de ProjectPresentation al DTO de detalle completo.
    /// </summary>
    internal static class ProjectPresentationResponseMapper
    {
        public static ProjectPresentationWithDetailsResponseDto MapToDto(
            (ProjectPresentation Presentation, ProjectFinal? ProjectFinal, Proposal Proposal, List<UserInscriptionModality> Students, List<AssignedTeacher> Teachers) entry)
        {
            var studentsDto = entry.Students.Select(uim => new UserInscriptionModalityDto
            {
                Id = uim.Id,
                IdInscriptionModality = uim.IdInscriptionModality,
                IdUser = uim.IdUser,
                UserName = uim.User != null ? $"{uim.User.FirstName} {uim.User.LastName}".Trim() : string.Empty,
                Identification = uim.User?.Identification ?? string.Empty,
                Email = uim.User?.Email ?? string.Empty
            }).ToList();

            var teachersDto = entry.Teachers.Select(t => new AssignedTeacherDto
            {
                Id = t.Id,
                IdTeacher = t.IdTeacher,
                FullName = t.FullName,
                Email = t.Email,
                CargoId = t.CargoId,
                CargoCode = t.CargoCode,
                CargoName = t.CargoName,
                RevocationDate = t.RevocationDate,
                IsConfirmed = t.IsConfirmed
            }).ToList();

            var presentation = entry.Presentation;
            var projectFinal = entry.ProjectFinal;
            var proposal = entry.Proposal;

            return new ProjectPresentationWithDetailsResponseDto
            {
                ProjectPresentation = new ProjectPresentationDetailsDto
                {
                    Id = presentation.Id,
                    IdInscriptionModality = presentation.IdInscriptionModality,
                    IdStateStage = presentation.IdStateStage,
                    ScheduledDateTime = presentation.ScheduledDateTime,
                    ActualDateTime = presentation.ActualDateTime,
                    LocationOrLink = presentation.LocationOrLink,
                    PresentationModality = presentation.PresentationModality,
                    AttemptNumber = presentation.AttemptNumber,
                    Observations = presentation.Observations,
                    IdUserCreatedAt = presentation.IdUserCreatedAt ?? 0,
                    CreatedAt = presentation.CreatedAt,
                    IdUserUpdatedAt = presentation.IdUserUpdatedAt,
                    UpdatedAt = presentation.UpdatedAt,
                    OperationRegister = presentation.OperationRegister ?? string.Empty,
                    StatusRegister = presentation.StatusRegister,
                    StateStage = presentation.StateStage != null ? new StateStageDto
                    {
                        Id = presentation.StateStage.Id,
                        Code = presentation.StateStage.Code,
                        Name = presentation.StateStage.Name,
                        Description = presentation.StateStage.Description,
                        IsInitialState = presentation.StateStage.IsInitialState,
                        IsFinalStateForStage = presentation.StateStage.IsFinalStateForStage,
                        IsFinalStateForModalityOverall = presentation.StateStage.IsFinalStateForModalityOverall,
                        IsRejectionState = presentation.StateStage.IsRejectionState
                    } : null
                },
                ProjectFinal = projectFinal != null ? new ProjectFinalDetailsDto
                {
                    Id = projectFinal.Id,
                    IdStateStage = projectFinal.IdStateStage,
                    ReportApprovalDate = projectFinal.ReportApprovalDate,
                    FinalPhaseApprovalDate = projectFinal.FinalPhaseApprovalDate,
                    Observations = projectFinal.Observations,
                    IdUserCreatedAt = projectFinal.IdUserCreatedAt ?? 0,
                    CreatedAt = projectFinal.CreatedAt,
                    IdUserUpdatedAt = projectFinal.IdUserUpdatedAt,
                    UpdatedAt = projectFinal.UpdatedAt,
                    OperationRegister = projectFinal.OperationRegister ?? string.Empty,
                    StatusRegister = projectFinal.StatusRegister,
                    StateStage = projectFinal.StateStage != null ? new StateStageDto
                    {
                        Id = projectFinal.StateStage.Id,
                        Code = projectFinal.StateStage.Code,
                        Name = projectFinal.StateStage.Name,
                        Description = projectFinal.StateStage.Description
                    } : null
                } : null,
                Proposal = new ProposalWithDetailsResponseDto
                {
                    Proposal = new ProposalDto
                    {
                        Id = proposal.Id,
                        Title = proposal.Title,
                        Description = proposal.Description,
                        GeneralObjective = proposal.GeneralObjective,
                        SpecificObjectives = proposal.SpecificObjectives,
                        IdResearchLine = proposal.IdResearchLine,
                        IdResearchSubLine = proposal.IdResearchSubLine,
                        IdStateStage = proposal.IdStateStage
                    },
                    StateStageName = proposal.StateStage?.Name ?? string.Empty,
                    StateStageCode = proposal.StateStage?.Code,
                    ResearchLineName = proposal.ResearchLine?.Name ?? string.Empty,
                    ResearchSubLineName = proposal.ResearchSubLine?.Name ?? string.Empty,
                    Students = studentsDto,
                    Teachers = teachersDto
                },
                Students = studentsDto,
                Teachers = teachersDto
            };
        }
    }
}
