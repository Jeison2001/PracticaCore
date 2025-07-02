using Application.Shared.DTOs.AcademicPractice;
using Application.Shared.DTOs.Document;
using AutoMapper;
using Domain.Common;
using Domain.Interfaces;
using MediatR;

namespace Application.Shared.Queries.AcademicPractice.Handlers
{
    public class GetAllAcademicPracticesQueryHandler : IRequestHandler<GetAllAcademicPracticesQuery, PaginatedResult<AcademicPracticeWithDetailsResponseDto>>
    {
        private readonly IAcademicPracticeRepository _academicPracticeRepository;
        private readonly IMapper _mapper;

        public GetAllAcademicPracticesQueryHandler(
            IAcademicPracticeRepository academicPracticeRepository,
            IMapper mapper)
        {
            _academicPracticeRepository = academicPracticeRepository;
            _mapper = mapper;
        }

        public async Task<PaginatedResult<AcademicPracticeWithDetailsResponseDto>> Handle(GetAllAcademicPracticesQuery request, CancellationToken cancellationToken)
        {
            // 1. Obtener las prácticas académicas paginadas con detalles
            var paginatedResult = await _academicPracticeRepository.GetAllAcademicPracticesWithDetailsPaginatedAsync(
                request.PageNumber,
                request.PageSize,
                request.SortBy,
                request.IsDescending,
                request.Filters,
                cancellationToken);

            // 2. Mapear cada resultado a DTO usando AutoMapper
            var mappedItems = new List<AcademicPracticeWithDetailsResponseDto>();

            foreach (var practiceDetail in paginatedResult.Items)
            {
                // Mapear la entidad principal
                var practiceDetailsDto = _mapper.Map<AcademicPracticeDetailsDto>(practiceDetail.AcademicPractice);
                
                // Agregar información de estado y fase
                practiceDetailsDto.StateStageCode = practiceDetail.StateStage?.Code ?? string.Empty;
                practiceDetailsDto.StateStageName = practiceDetail.StateStage?.Name ?? string.Empty;
                practiceDetailsDto.IdStageModality = practiceDetail.StageModality?.Id;
                practiceDetailsDto.StageModalityCode = practiceDetail.StageModality?.Code;
                practiceDetailsDto.StageModalityName = practiceDetail.StageModality?.Name;
                practiceDetailsDto.StageOrder = practiceDetail.StageModality?.StageOrder;

                // Mapear estudiantes usando UserDto existente
                var studentsDto = practiceDetail.UserInscriptionModalities
                    .Where(uim => uim.User != null)
                    .Select(uim => _mapper.Map<Application.Shared.DTOs.User.UserDto>(uim.User))
                    .ToList();

                // Mapear docentes usando TeachingAssignmentTeacherDto existente
                var teachersDto = practiceDetail.TeachingAssignments
                    .Where(t => t.StatusRegister)
                    .Select(ta => _mapper.Map<Application.Shared.DTOs.TeachingAssignment.TeachingAssignmentTeacherDto>(ta))
                    .ToList();

                // Mapear documentos usando DocumentDto existente
                var documentsDto = practiceDetail.Documents
                    .Select(doc => _mapper.Map<DocumentDto>(doc))
                    .ToList();

                // Mapear progreso de fase
                var phaseProgress = await _academicPracticeRepository.GetPhaseProgressAsync(practiceDetail.AcademicPractice.Id, cancellationToken);
                var phaseProgressDto = _mapper.Map<AcademicPracticePhaseProgressDto>(phaseProgress);

                var responseDto = new AcademicPracticeWithDetailsResponseDto
                {
                    AcademicPractice = practiceDetailsDto,
                    InscriptionModalityId = practiceDetail.InscriptionModality.Id,
                    ModalityName = practiceDetail.Modality?.Name ?? string.Empty,
                    StateInscriptionName = practiceDetail.StateInscription?.Name ?? string.Empty,
                    AcademicPeriodCode = practiceDetail.AcademicPeriod?.Code ?? string.Empty,
                    InscriptionApprovalDate = practiceDetail.InscriptionModality.ApprovalDate,
                    InscriptionObservations = practiceDetail.InscriptionModality.Observations,
                    Students = studentsDto,
                    Teachers = teachersDto,
                    Documents = documentsDto,
                    PhaseProgress = phaseProgressDto
                };

                mappedItems.Add(responseDto);
            }

            // 3. Crear resultado paginado con DTOs mapeados
            var result = new PaginatedResult<AcademicPracticeWithDetailsResponseDto>
            {
                Items = mappedItems,
                TotalRecords = paginatedResult.TotalRecords,
                PageNumber = paginatedResult.PageNumber,
                PageSize = paginatedResult.PageSize
            };

            return result;
        }
    }
}
