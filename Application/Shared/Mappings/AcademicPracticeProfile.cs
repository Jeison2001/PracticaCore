using Application.Shared.DTOs.AcademicPractices;
using Application.Shared.DTOs.TeachingAssignments;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces.Repositories;

namespace Application.Shared.Mappings
{
    public class AcademicPracticeProfile : Profile
    {
        public AcademicPracticeProfile()
        {
            // Basic entity mapping - sin mapeo de StateStageId porque ya existe IdStateStage
            CreateMap<AcademicPractice, AcademicPracticeDto>()
                .ReverseMap();

            // Details mapping  
            CreateMap<AcademicPractice, AcademicPracticeDetailsDto>()
                .ReverseMap();

            // Complex with details mapping
            CreateMap<AcademicPracticeWithDetails, AcademicPracticeWithDetailsResponseDto>()
                .ForMember(dest => dest.AcademicPractice, opt => opt.MapFrom(src => src.AcademicPractice))
                .ForMember(dest => dest.InscriptionModalityId, opt => opt.MapFrom(src => src.InscriptionModality != null ? src.InscriptionModality.Id : 0))
                .ForMember(dest => dest.ModalityName, opt => opt.MapFrom(src => src.Modality != null ? src.Modality.Name : string.Empty))
                .ForMember(dest => dest.StateInscriptionName, opt => opt.MapFrom(src => src.StateInscription != null ? src.StateInscription.Name : string.Empty))
                .ForMember(dest => dest.AcademicPeriodCode, opt => opt.MapFrom(src => src.AcademicPeriod != null ? src.AcademicPeriod.Code : string.Empty))
                .ForMember(dest => dest.InscriptionApprovalDate, opt => opt.MapFrom(src => src.InscriptionModality != null ? src.InscriptionModality.ApprovalDate : null))
                .ForMember(dest => dest.InscriptionObservations, opt => opt.MapFrom(src => src.InscriptionModality != null ? src.InscriptionModality.Observations : null))
                .ForMember(dest => dest.Students, opt => opt.MapFrom(src => src.UserInscriptionModalities.Select(uim => uim.User).Where(u => u != null)))
                .ForMember(dest => dest.Teachers, opt => opt.MapFrom(src => src.TeachingAssignments))
                .ForMember(dest => dest.Documents, opt => opt.MapFrom(src => src.Documents))
                .ForMember(dest => dest.PhaseProgress, opt => opt.Ignore()); // Mapeo manual

            // Teacher assignment mapping usando TeachingAssignmentTeacherDto existente
            CreateMap<TeachingAssignment, TeachingAssignmentTeacherDto>()
                .ForMember(dest => dest.IdUser, opt => opt.MapFrom(src => src.IdTeacher))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.Teacher != null ? $"{src.Teacher.FirstName} {src.Teacher.LastName}" : string.Empty))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Teacher != null ? src.Teacher.Email : string.Empty))
                .ForMember(dest => dest.AssignmentType, opt => opt.MapFrom(src => src.TypeTeachingAssignment != null ? src.TypeTeachingAssignment.Name : string.Empty));

            // Phase progress mapping
            CreateMap<AcademicPracticePhaseProgress, AcademicPracticePhaseProgressDto>()
                .ForMember(dest => dest.PhaseDetails, opt => opt.MapFrom(src => src.PhaseDetails));

            CreateMap<PhaseDetail, AcademicPracticePhaseDetailDto>()
                .ForMember(dest => dest.States, opt => opt.MapFrom(src => src.States));

            CreateMap<StateDetail, AcademicPracticeStateDetailDto>();
        }
    }
}
