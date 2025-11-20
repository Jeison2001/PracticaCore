# Guía de Desarrollo: Integración de Nueva Entidad

Este documento detalla el flujo de trabajo para agregar una nueva entidad al sistema `PracticaCore`, asegurando el cumplimiento de la arquitectura.

## 1. Capa de Dominio (Domain)
Crear la entidad en `Domain/Entities`. Debe heredar de `BaseEntity<TId>`.

```csharp
namespace Domain.Entities
{
    public class NuevaEntidad : BaseEntity<int>
    {
        public string Nombre { get; set; } = string.Empty;
        // Otras propiedades...
    }
}
```

## 2. Capa de Aplicación (Application)

### 2.1 DTOs
Crear el DTO en `Application/Shared/DTOs/[NombreEntidad]`.

```csharp
namespace Application.Shared.DTOs.NuevaEntidad
{
    public class NuevaEntidadDto : BaseDto<int>
    {
        public string Nombre { get; set; } = string.Empty;
    }
}
```

### 2.2 Validaciones
Crear el validador en `Application/Validations/SpecificValidators/[NombreEntidad]`.

```csharp
using FluentValidation;

namespace Application.Validations.SpecificValidators.NuevaEntidad
{
    public class NuevaEntidadValidator : AbstractValidator<NuevaEntidadDto>
    {
        public NuevaEntidadValidator()
        {
            RuleFor(x => x.Nombre).NotEmpty().WithMessage("El nombre es requerido.");
        }
    }
}
```
> **Nota**: Para las operaciones CRUD estándar (GenericController), este validador se invoca automáticamente gracias a los validadores genéricos (`BaseCreateCommandValidator`). No es necesario crear validadores de comandos adicionales.


## 3. Capa de Infraestructura (Infrastructure)
Crear la configuración de Entity Framework en `Infrastructure/Data/Configurations`.
**Importante**: Heredar de `BaseEntityConfiguration<TEntity, TId>` para incluir automáticamente la configuración de auditoría.

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Data.Configurations
{
    public class NuevaEntidadConfiguration : BaseEntityConfiguration<NuevaEntidad, int>
    {
        public override void Configure(EntityTypeBuilder<NuevaEntidad> builder)
        {
            base.Configure(builder); // Configura ID y auditoría

            builder.ToTable("NuevaEntidad"); // PascalCase
            
            // Las columnas deben ser minúsculas para PostgreSQL
            builder.Property(e => e.Nombre)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("nombre");
        }
    }
}
```

## 4. Capa de API (Api)
Crear el controlador en `Api/Controllers`. Heredar de `GenericController` para obtener CRUD automático.

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

## 5. Pruebas de Integración (Tests)
Crear la clase de prueba en `Tests/Integration/[NuevaEntidad]`. Heredar de `GenericControllerIntegrationTests`.

```csharp
using Tests.Integration;
using Domain.Entities;
using Application.Shared.DTOs.NuevaEntidad;

namespace Tests.Integration.NuevaEntidad
{
    public class NuevaEntidadControllerTests : GenericControllerIntegrationTests<Domain.Entities.NuevaEntidad, NuevaEntidadDto>
    {
        public NuevaEntidadControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected override string BaseUrl => "/api/NuevaEntidad";

        protected override NuevaEntidadDto CreateValidDto()
        {
            return new NuevaEntidadDto { Nombre = "Test" };
        }

        protected override Domain.Entities.NuevaEntidad CreateValidEntity()
        {
            return new Domain.Entities.NuevaEntidad { Nombre = "Test", StatusRegister = true };
        }
    }
}
```

## 6. Registro Automático
Gracias a la arquitectura del proyecto, **no es necesario registrar manualmente** los repositorios, validadores o mapeos, siempre que sigan las convenciones de nombres y herencia descritas anteriormente.

## 7. Casos Especiales y Módulos Personalizados

Para funcionalidades que no se ajustan al patrón CRUD genérico (como subida de archivos, procesos complejos, o reportes), se recomienda seguir el patrón **CQRS** manualmente:

1.  **Definir Command/Query**: Crear la clase que implemente `IRequest<TResponse>`.
2.  **Crear Handler**: Implementar `IRequestHandler<TRequest, TResponse>`.
3.  **Validación**: Crear un validador para el Command (`AbstractValidator<TCommand>`).
4.  **Controller**: Crear un endpoint específico en el controlador que envíe el comando a través de `_mediator.Send()`.

Ejemplo detallado: **[Módulo de Documentos](MODULOS/DOCUMENTOS.md)**.

---

## 8. Patrones para Nullable Reference Types

Este proyecto tiene habilitado `<Nullable>enable</Nullable>`. Sigue estos patrones para evitar warnings CS8618:

### DTOs
```csharp
public class UserDto : BaseDto<int>
{
    // Strings obligatorios
    public string Name { get; set; } = string.Empty;
    
    // Strings opcionales
    public string? Observations { get; set; }
    
    // Colecciones
    public List<RoleDto> Roles { get; set; } = new();
    
    // Referencias obligatorias (se asignarán antes de usar)
    public AcademicProgramDto AcademicProgram { get; set; } = null!;
}
```

### Entidades
```csharp
public class User : BaseEntity<int>
{
    // Propiedades de valor
    public string Email { get; set; } = string.Empty;
    
    // Navegación obligatoria (patrón EF Core)
    public int IdAcademicProgram { get; set; }
    public virtual AcademicProgram AcademicProgram { get; set; } = null!;
    
    // Colecciones de navegación
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
```

### Controladores
```csharp
// Usar null-coalescing para parámetros opcionales
var query = new GetAllQuery
{
    SortBy = request.SortBy ?? string.Empty,
    Filters = request.Filters ?? new Dictionary<string, string>()
};
```

### AutoMapper
```csharp
// Null-checks para propiedades opcionales
CreateMap<Source, Destination>()
    .ForMember(dest => dest.Name, 
        opt => opt.MapFrom(src => src.Entity != null ? src.Entity.Name : string.Empty));
```

**Referencia completa:** Ver análisis de warnings resueltos en commits recientes.