using Application.Shared.DTOs.AcademicAverages;
using Application.Shared.DTOs.CoTerminals;
using Application.Shared.DTOs.SaberPros;
using Application.Shared.DTOs.ScientificArticles;
using Application.Shared.DTOs.Seminars;
using AutoMapper;
using Domain.Common.AcademicAverage;
using Domain.Common.CoTerminal;
using Domain.Common.SaberPro;
using Domain.Common.ScientificArticle;
using Domain.Common.Seminar;
using Domain.Entities;

namespace Application.Shared.Mappings
{
    public class MinorModalitiesProfile : Profile
    {
        public MinorModalitiesProfile()
        {
            // CoTerminal
            CreateMap<CoTerminalWithDetails, CoTerminalWithDetailsDto>()
                .ForMember(dest => dest.CoTerminal, opt => opt.MapFrom(src => src.CoTerminal))
                .ForMember(dest => dest.InscriptionModalityId, opt => opt.MapFrom(src => src.InscriptionModality != null ? src.InscriptionModality.Id : 0))
                .ForMember(dest => dest.ModalityName, opt => opt.MapFrom(src => src.Modality != null ? src.Modality.Name : string.Empty))
                .ForMember(dest => dest.StateInscriptionName, opt => opt.MapFrom(src => src.StateInscription != null ? src.StateInscription.Name : string.Empty))
                .ForMember(dest => dest.AcademicPeriodCode, opt => opt.MapFrom(src => src.AcademicPeriod != null ? src.AcademicPeriod.Code : string.Empty))
                .ForMember(dest => dest.InscriptionApprovalDate, opt => opt.MapFrom(src => src.InscriptionModality != null ? src.InscriptionModality.ApprovalDate : null))
                .ForMember(dest => dest.InscriptionObservations, opt => opt.MapFrom(src => src.InscriptionModality != null ? src.InscriptionModality.Observations : null))
                .ForMember(dest => dest.Students, opt => opt.MapFrom(src => src.UserInscriptionModalities.Select(uim => uim.User).Where(u => u != null)))
                .ForMember(dest => dest.Teachers, opt => opt.MapFrom(src => src.TeachingAssignments))
                .ForMember(dest => dest.Documents, opt => opt.MapFrom(src => src.Documents));

            // Seminar
            CreateMap<SeminarWithDetails, SeminarWithDetailsDto>()
                .ForMember(dest => dest.Seminar, opt => opt.MapFrom(src => src.Seminar))
                .ForMember(dest => dest.InscriptionModalityId, opt => opt.MapFrom(src => src.InscriptionModality != null ? src.InscriptionModality.Id : 0))
                .ForMember(dest => dest.ModalityName, opt => opt.MapFrom(src => src.Modality != null ? src.Modality.Name : string.Empty))
                .ForMember(dest => dest.StateInscriptionName, opt => opt.MapFrom(src => src.StateInscription != null ? src.StateInscription.Name : string.Empty))
                .ForMember(dest => dest.AcademicPeriodCode, opt => opt.MapFrom(src => src.AcademicPeriod != null ? src.AcademicPeriod.Code : string.Empty))
                .ForMember(dest => dest.InscriptionApprovalDate, opt => opt.MapFrom(src => src.InscriptionModality != null ? src.InscriptionModality.ApprovalDate : null))
                .ForMember(dest => dest.InscriptionObservations, opt => opt.MapFrom(src => src.InscriptionModality != null ? src.InscriptionModality.Observations : null))
                .ForMember(dest => dest.Students, opt => opt.MapFrom(src => src.UserInscriptionModalities.Select(uim => uim.User).Where(u => u != null)))
                .ForMember(dest => dest.Teachers, opt => opt.MapFrom(src => src.TeachingAssignments))
                .ForMember(dest => dest.Documents, opt => opt.MapFrom(src => src.Documents));

            // ScientificArticle
            CreateMap<ScientificArticleWithDetails, ScientificArticleWithDetailsDto>()
                .ForMember(dest => dest.ScientificArticle, opt => opt.MapFrom(src => src.ScientificArticle))
                .ForMember(dest => dest.InscriptionModalityId, opt => opt.MapFrom(src => src.InscriptionModality != null ? src.InscriptionModality.Id : 0))
                .ForMember(dest => dest.ModalityName, opt => opt.MapFrom(src => src.Modality != null ? src.Modality.Name : string.Empty))
                .ForMember(dest => dest.StateInscriptionName, opt => opt.MapFrom(src => src.StateInscription != null ? src.StateInscription.Name : string.Empty))
                .ForMember(dest => dest.AcademicPeriodCode, opt => opt.MapFrom(src => src.AcademicPeriod != null ? src.AcademicPeriod.Code : string.Empty))
                .ForMember(dest => dest.InscriptionApprovalDate, opt => opt.MapFrom(src => src.InscriptionModality != null ? src.InscriptionModality.ApprovalDate : null))
                .ForMember(dest => dest.InscriptionObservations, opt => opt.MapFrom(src => src.InscriptionModality != null ? src.InscriptionModality.Observations : null))
                .ForMember(dest => dest.Students, opt => opt.MapFrom(src => src.UserInscriptionModalities.Select(uim => uim.User).Where(u => u != null)))
                .ForMember(dest => dest.Teachers, opt => opt.MapFrom(src => src.TeachingAssignments))
                .ForMember(dest => dest.Documents, opt => opt.MapFrom(src => src.Documents));

            // AcademicAverage
            CreateMap<AcademicAverageWithDetails, AcademicAverageWithDetailsDto>()
                .ForMember(dest => dest.AcademicAverage, opt => opt.MapFrom(src => src.AcademicAverage))
                .ForMember(dest => dest.InscriptionModalityId, opt => opt.MapFrom(src => src.InscriptionModality != null ? src.InscriptionModality.Id : 0))
                .ForMember(dest => dest.ModalityName, opt => opt.MapFrom(src => src.Modality != null ? src.Modality.Name : string.Empty))
                .ForMember(dest => dest.StateInscriptionName, opt => opt.MapFrom(src => src.StateInscription != null ? src.StateInscription.Name : string.Empty))
                .ForMember(dest => dest.AcademicPeriodCode, opt => opt.MapFrom(src => src.AcademicPeriod != null ? src.AcademicPeriod.Code : string.Empty))
                .ForMember(dest => dest.InscriptionApprovalDate, opt => opt.MapFrom(src => src.InscriptionModality != null ? src.InscriptionModality.ApprovalDate : null))
                .ForMember(dest => dest.InscriptionObservations, opt => opt.MapFrom(src => src.InscriptionModality != null ? src.InscriptionModality.Observations : null))
                .ForMember(dest => dest.Students, opt => opt.MapFrom(src => src.UserInscriptionModalities.Select(uim => uim.User).Where(u => u != null)))
                .ForMember(dest => dest.Teachers, opt => opt.MapFrom(src => src.TeachingAssignments))
                .ForMember(dest => dest.Documents, opt => opt.MapFrom(src => src.Documents));

            // SaberPro
            CreateMap<SaberProWithDetails, SaberProWithDetailsDto>()
                .ForMember(dest => dest.SaberPro, opt => opt.MapFrom(src => src.SaberPro))
                .ForMember(dest => dest.InscriptionModalityId, opt => opt.MapFrom(src => src.InscriptionModality != null ? src.InscriptionModality.Id : 0))
                .ForMember(dest => dest.ModalityName, opt => opt.MapFrom(src => src.Modality != null ? src.Modality.Name : string.Empty))
                .ForMember(dest => dest.StateInscriptionName, opt => opt.MapFrom(src => src.StateInscription != null ? src.StateInscription.Name : string.Empty))
                .ForMember(dest => dest.AcademicPeriodCode, opt => opt.MapFrom(src => src.AcademicPeriod != null ? src.AcademicPeriod.Code : string.Empty))
                .ForMember(dest => dest.InscriptionApprovalDate, opt => opt.MapFrom(src => src.InscriptionModality != null ? src.InscriptionModality.ApprovalDate : null))
                .ForMember(dest => dest.InscriptionObservations, opt => opt.MapFrom(src => src.InscriptionModality != null ? src.InscriptionModality.Observations : null))
                .ForMember(dest => dest.Students, opt => opt.MapFrom(src => src.UserInscriptionModalities.Select(uim => uim.User).Where(u => u != null)))
                .ForMember(dest => dest.Teachers, opt => opt.MapFrom(src => src.TeachingAssignments))
                .ForMember(dest => dest.Documents, opt => opt.MapFrom(src => src.Documents));
        }
    }
}
