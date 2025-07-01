using Application.Shared.DTOs.AcademicPractice;
using Application.Shared.DTOs.TeachingAssignment;
using Application.Shared.DTOs.User;
using Application.Shared.DTOs;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;

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
                .ForMember(dest => dest.InscriptionModalityId, opt => opt.MapFrom(src => src.InscriptionModality.Id))
                .ForMember(dest => dest.ModalityName, opt => opt.MapFrom(src => src.Modality.Name))
                .ForMember(dest => dest.StateInscriptionName, opt => opt.MapFrom(src => src.StateInscription.Name))
                .ForMember(dest => dest.AcademicPeriodCode, opt => opt.MapFrom(src => src.AcademicPeriod.Code))
                .ForMember(dest => dest.InscriptionApprovalDate, opt => opt.MapFrom(src => src.InscriptionModality.ApprovalDate))
                .ForMember(dest => dest.InscriptionObservations, opt => opt.MapFrom(src => src.InscriptionModality.Observations))
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
