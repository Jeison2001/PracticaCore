using Application.Shared.DTOs;
using Application.Shared.DTOs.UserInscriptionModality;
using Domain.Entities;
using AutoMapper;
using System.Reflection;

namespace Application.Shared.Mappings
{
    public class GenericProfile : Profile
    {
        public GenericProfile()
        {
            // Mapeo base
            CreateMap(typeof(BaseEntity<>), typeof(BaseDto<>)).ReverseMap();

            // Obtener entidades del ensamblado Domain.Entities
            var entityAssembly = Assembly.GetAssembly(typeof(BaseEntity<>))!;
            var entityTypes = entityAssembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract &&
                            t.BaseType?.IsGenericType == true &&
                            t.BaseType.GetGenericTypeDefinition() == typeof(BaseEntity<>));

            // Obtener DTOs del ensamblado Application.Shared
            var dtoAssembly = Assembly.GetAssembly(typeof(BaseDto<>))!;
            var dtoTypes = dtoAssembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract &&
                            t.BaseType?.IsGenericType == true &&
                            t.BaseType.GetGenericTypeDefinition() == typeof(BaseDto<>));

            foreach (var entityType in entityTypes)
            {
                // Buscar DTO por nombre (Ej: Country → CountryDto)
                var dtoName = $"{entityType.Name}Dto";
                var dtoType = dtoTypes.FirstOrDefault(d => d.Name == dtoName);

                if (dtoType != null)
                {
                    CreateMap(entityType, dtoType).ReverseMap();
                }
            }
            CreateMap<UserInscriptionModality, UserInscriptionModalityDto>().ReverseMap();
        }
    }
}
