using Application.Shared.DTOs.AcademicPractice;
using Application.Shared.DTOs.TeachingAssignment;
using Application.Shared.DTOs.User;
using Application.Shared.DTOs;
using AutoMapper;
using Domain.Interfaces;
using MediatR;

namespace Application.Shared.Queries.AcademicPractice.Handlers
{
    public class GetAcademicPracticeWithDetailsQueryHandler : IRequestHandler<GetAcademicPracticeWithDetailsQuery, AcademicPracticeWithDetailsResponseDto>
    {
        private readonly IAcademicPracticeRepository _academicPracticeRepository;
        private readonly IMapper _mapper;

        public GetAcademicPracticeWithDetailsQueryHandler(
            IAcademicPracticeRepository academicPracticeRepository,
            IMapper mapper)
        {
            _academicPracticeRepository = academicPracticeRepository;
            _mapper = mapper;
        }

        public async Task<AcademicPracticeWithDetailsResponseDto> Handle(GetAcademicPracticeWithDetailsQuery request, CancellationToken cancellationToken)
        {
            // 1. Obtener la práctica académica con todos sus detalles
            var practiceDetail = await _academicPracticeRepository.GetAcademicPracticeWithDetailsAsync(request.Id, cancellationToken);
            
            if (practiceDetail == null)
            {
                throw new KeyNotFoundException($"No se encontró la práctica académica con ID {request.Id}");
            }

            // 2. Mapear usando AutoMapper
            var practiceDetailsDto = _mapper.Map<AcademicPracticeDetailsDto>(practiceDetail.AcademicPractice);
            
            // Agregar información de estado y fase
            practiceDetailsDto.StateStageCode = practiceDetail.StateStage?.Code ?? string.Empty;
            practiceDetailsDto.StateStageName = practiceDetail.StateStage?.Name ?? string.Empty;
            practiceDetailsDto.IdStageModality = practiceDetail.StageModality?.Id;
            practiceDetailsDto.StageModalityCode = practiceDetail.StageModality?.Code;
            practiceDetailsDto.StageModalityName = practiceDetail.StageModality?.Name;
            practiceDetailsDto.StageOrder = practiceDetail.StageModality?.StageOrder;

            // 3. Mapear estudiantes usando UserDto existente
            var studentsDto = practiceDetail.UserInscriptionModalities
                .Where(uim => uim.User != null)
                .Select(uim => _mapper.Map<UserDto>(uim.User))
                .ToList();

            // 4. Mapear docentes usando TeachingAssignmentTeacherDto existente
            var teachersDto = practiceDetail.TeachingAssignments
                .Where(t => t.StatusRegister)
                .Select(ta => _mapper.Map<TeachingAssignmentTeacherDto>(ta))
                .ToList();

            // 5. Mapear documentos usando DocumentDto existente
            var documentsDto = practiceDetail.Documents
                .Where(d => d.StatusRegister)
                .Select(doc => _mapper.Map<DocumentDto>(doc))
                .ToList();

            // 6. Obtener progreso de fases
            var phaseProgress = await _academicPracticeRepository.GetPhaseProgressAsync(request.Id, cancellationToken);
            var phaseProgressDto = _mapper.Map<AcademicPracticePhaseProgressDto>(phaseProgress);

            // 7. Crear el DTO de respuesta completo
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

            return responseDto;
        }
    }
}
