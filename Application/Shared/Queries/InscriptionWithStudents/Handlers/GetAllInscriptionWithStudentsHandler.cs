using System.Reflection;
using Application.Shared.DTOs.InscriptionModalities;
using Application.Shared.DTOs.InscriptionWithStudents;
using Application.Shared.DTOs.UserInscriptionModalities;
using Domain.Common;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Shared.Queries.InscriptionWithStudents.Handlers
{
    /// <summary>
    /// Listado paginado de inscripciones con estudiantes. Delega el filtrado, ordenamiento y
    /// paginación al repositorio (SQL con joins); aquí solo se valida el contrato de filtros
    /// y se mapea a DTO.
    /// </summary>
    public class GetAllInscriptionWithStudentsHandler
        : IRequestHandler<GetAllInscriptionWithStudentsQuery, PaginatedResult<InscriptionWithStudentsResponseDto>>
    {
        private static readonly HashSet<string> EnrichedFilterKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "StudentName", "ModalityName", "StateInscriptionName", "AcademicPeriodCode",
        };

        private readonly IInscriptionWithStudentsRepository _repository;
        private readonly ILogger<GetAllInscriptionWithStudentsHandler> _logger;

        public GetAllInscriptionWithStudentsHandler(
            IInscriptionWithStudentsRepository repository,
            ILogger<GetAllInscriptionWithStudentsHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<PaginatedResult<InscriptionWithStudentsResponseDto>> Handle(
            GetAllInscriptionWithStudentsQuery request,
            CancellationToken cancellationToken)
        {
            var filters = request.Filters ?? new Dictionary<string, string>();

            // Validación del contrato de filtros: nada se ignora en silencio (ver log).
            foreach (var key in filters.Keys)
            {
                var prop = key.Contains('@') ? key.Split('@', 2)[0] : key;
                if (EnrichedFilterKeys.Contains(prop))
                    continue;
                if (typeof(InscriptionModality).GetProperty(prop,
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase) == null)
                {
                    _logger.LogWarning(
                        "Filtro no reconocido ignorado en el listado de inscripciones: {FilterKey}",
                        key);
                }
            }

            // Valores inválidos (fecha/bool/número mal formados) tampoco pasan en silencio.
            foreach (var key in FilterBuilder.GetInvalidFilterKeys<InscriptionModality, int>(filters))
            {
                _logger.LogWarning(
                    "Valor de filtro inválido ignorado en el listado de inscripciones: {FilterKey}={Value}",
                    key, filters[key]);
            }

            var page = await _repository.GetFilteredPageAsync(
                request.PageNumber,
                request.PageSize,
                request.SortBy,
                request.IsDescending,
                filters,
                cancellationToken);

            var items = page.Items.Select(MapToDto).ToList();

            return new PaginatedResult<InscriptionWithStudentsResponseDto>
            {
                Items = items,
                TotalRecords = page.TotalRecords,
                PageNumber = page.PageNumber,
                PageSize = page.PageSize,
            };
        }

        private static InscriptionWithStudentsResponseDto MapToDto(Domain.Common.Inscriptions.InscriptionWithStudents row)
        {
            var im = row.InscriptionModality;

            return new InscriptionWithStudentsResponseDto
            {
                InscriptionModality = new InscriptionModalityDto
                {
                    Id = im.Id,
                    IdModality = im.IdModality,
                    IdStateInscription = im.IdStateInscription,
                    IdAcademicPeriod = im.IdAcademicPeriod,
                    IdStageModality = im.IdStageModality,
                    ApprovalDate = im.ApprovalDate,
                    Observations = im.Observations,
                    CreatedAt = im.CreatedAt,
                    UpdatedAt = im.UpdatedAt,
                    StatusRegister = im.StatusRegister,
                },
                AcademicPeriodCode = row.AcademicPeriodCode,
                ModalityName = row.ModalityName,
                StateInscriptionName = row.StateInscriptionName,
                StateInscriptionCode = row.StateInscriptionCode,
                StageModalityName = row.StageModalityName,
                StageOrder = row.StageOrder,
                Students = row.Students.Select(MapStudent).ToList(),
            };
        }

        private static UserInscriptionModalityDto MapStudent(UserInscriptionModality student)
        {
            var dto = new UserInscriptionModalityDto
            {
                Id = student.Id,
                IdInscriptionModality = student.IdInscriptionModality,
                IdUser = student.IdUser,
                CreatedAt = student.CreatedAt,
                UpdatedAt = student.UpdatedAt,
                StatusRegister = student.StatusRegister,
            };

            if (student.User != null)
            {
                dto.UserName = $"{student.User.FirstName} {student.User.LastName}".Trim();
                dto.Identification = student.User.Identification ?? string.Empty;
                dto.IdIdentificationType = student.User.IdIdentificationType;
                dto.Email = student.User.Email ?? string.Empty;
                dto.CurrentAcademicPeriod = student.User.CurrentAcademicPeriod ?? string.Empty;
                dto.CumulativeAverage = student.User.CumulativeAverage;
                dto.ApprovedCredits = student.User.ApprovedCredits;
                dto.TotalAcademicCredits = student.User.TotalAcademicCredits;
            }
            else
            {
                dto.UserName = "Usuario no encontrado";
            }

            return dto;
        }
    }
}
