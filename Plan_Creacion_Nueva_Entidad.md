# Plan de Trabajo: Integracion de una Nueva Entidad

Este documento describe los pasos necesarios para crear e integrar una nueva entidad en el sistema.

## 1. Capa de Dominio
1. Crear una clase para la entidad directamente en `Domain.Entities`.
   - Heredar de `BaseEntity`.
   - Definir las propiedades necesarias.

**Ejemplo:**
```csharp
namespace Domain.Entities
{
    public class NuevaEntidad : BaseEntity<int>
    {
        public string Propiedad1 { get; set; } = string.Empty;
        public int Propiedad2 { get; set; }
    }
}
```

## 2. Capa de DTO
1. Crear una nueva carpeta en `Application/Shared/DTOs` con el nombre de la entidad.
2. Dentro de esta carpeta, crear un DTO para la entidad.
   - Heredar de `BaseDto`.
   - Asegurarse de que las propiedades coincidan con las de la entidad.

**Ejemplo:**
```csharp
namespace Application.Shared.DTOs.NuevaEntidad
{
    public class NuevaEntidadDto : BaseDto<int>
    {
        public string Propiedad1 { get; set; } = string.Empty;
        public int Propiedad2 { get; set; }
    }
}
```

## 3. Configuración del Mapeo para la Base de Datos
1. Agregar la configuración de mapeo dentro de la clase `BaseEntityConfiguration` ubicada en `Infrastructure/Data/Configurations/BaseEntityConfiguration.cs` O en su propia clase de configuracion.
    - Crear una clase específica siguiendo el patrón de las demás configuraciones.
    - Incluir las reglas de mapeo específicas para la nueva entidad.
    - Si tiene FK debe colocarse explícitamente la relación Ejemplo:
    builder.Property(p => p.IdFaculty).HasColumnName("idfaculty");
    builder.HasOne(p => p.Faculty)
                        .WithMany(f => f.AcademicPrograms)
                        .HasForeignKey(p => p.IdFaculty);
    - **Nota:** Los nombres de las tablas deben mantenerse en PascalCase (ejemplo: "StateProjectFinal"), pero los nombres de las columnas deben ir en minúsculas para cumplir con las convenciones de PostgreSQL.
    - Heredar de `BaseEntityConfiguration<Entidad, int>` para las configuraciones de entidades, y llamar a `base.Configure(builder);` al inicio del método `Configure`.
    - No es necesario mapear explícitamente las propiedades comunes como `CreatedAt`, `UpdatedAt`, `IdUserCreatedAt`, `IdUserUpdatedAt`, `OperationRegister` y `StatusRegister`, ya que se configuran en la clase base.
**Ejemplo:**
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class BaseEntityConfiguration : IEntityTypeConfiguration<NuevaEntidad>
    {
        public void Configure(EntityTypeBuilder<NuevaEntidad> builder)
        {
            builder.ToTable("NuevaEntidad"); // PascalCase para la tabla
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Propiedad1).HasColumnName("propiedad1").IsRequired().HasMaxLength(100); // minúsculas para columnas
        }
    }
}
```

## 4. Capa de Validaciones
1. Crear una nueva carpeta en `Application/Validations/specificsValidators`.
2. Dentro de esta carpeta, crear una subcarpeta con el nombre de la entidad base y clase de validación para el Dto.

**Ejemplo:**
```csharp
using FluentValidation;

namespace Application.Validations.specificsValidators.NuevaEntidad
{
    public class NuevaEntidadValidator : AbstractValidator<NuevaEntidadDto>
    {
        public NuevaEntidadValidator()
        {
            RuleFor(x => x.Propiedad1).NotEmpty().WithMessage("Propiedad1 es requerida.");
            RuleFor(x => x.Propiedad2).GreaterThan(0).WithMessage("Propiedad2 debe ser mayor a 0.");
        }
    }
}
```

## 5. Capa de Controlador
1. Crear un controlador directamente en `Api.Controllers` que herede de `GenericController` para manejar las operaciones CRUD de la entidad.

**Ejemplo:**
```csharp
using Api.Controllers;
using Application.Shared.DTOs.NuevaEntidad;
using Domain.Entities;
using MediatR;

namespace Api.Controllers
{
    public class NuevaEntidadController : GenericController<NuevaEntidad, int, NuevaEntidadDto>
    {
        public NuevaEntidadController(IMediator mediator) : base(mediator)
        {
        }
    }
}
```

## 6. Pruebas (OMITIR POR EL MOMENTO)
<!-- 1. Crear una nueva carpeta en `Tests/UnitTests` para cada capa con el nombre de la entidad.
2. Dentro de esta carpeta, agregar pruebas unitarias para validar la lógica de negocio y las operaciones CRUD. -->

## Consideraciones Finales
- Verificar que las nuevas implementaciones no afecten funcionalidades existentes.
- Realizar Compilación, ejecución del proyecto.