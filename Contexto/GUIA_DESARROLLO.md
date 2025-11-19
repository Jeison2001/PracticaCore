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

## 5. Registro Automático
Gracias a la arquitectura del proyecto, **no es necesario registrar manualmente** los repositorios, validadores o mapeos, siempre que sigan las convenciones de nombres y herencia descritas anteriormente.

## 6. Casos Especiales y Módulos Personalizados

Para funcionalidades que no se ajustan al patrón CRUD genérico (como subida de archivos, procesos complejos, o reportes), se recomienda seguir el patrón **CQRS** manualmente:

1.  **Definir Command/Query**: Crear la clase que implemente `IRequest<TResponse>`.
2.  **Crear Handler**: Implementar `IRequestHandler<TRequest, TResponse>`.
3.  **Validación**: Crear un validador para el Command (`AbstractValidator<TCommand>`).
4.  **Controller**: Crear un endpoint específico en el controlador que envíe el comando a través de `_mediator.Send()`.

Ejemplo detallado: **[Módulo de Documentos](MODULOS/DOCUMENTOS.md)**.