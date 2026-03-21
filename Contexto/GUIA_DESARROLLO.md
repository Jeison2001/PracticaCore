# Guía de Desarrollo: Integración de Nueva Entidad

Este documento detalla el flujo de trabajo para agregar una nueva entidad al sistema `PracticaCore`, asegurando el cumplimiento de la arquitectura.

> 📖 **Antes de empezar:** Familiarízate con:
> - [ORGANIZACION_CARPETAS.md](ORGANIZACION_CARPETAS.md) - ¿Dónde va cada archivo?
> - [CONVENCIONES_NAMESPACES.md](CONVENCIONES_NAMESPACES.md) - ¿Qué namespace usar?
> - [ARQUITECTURA.md](ARQUITECTURA.md) - Entender las capas

---

## Flujo Completo: Agregar Nueva Entidad

### 1. Capa de Dominio (Domain)

**Crear entidad** en `Domain/Entities/`

```csharp
namespace Domain.Entities
{
    public class Course : BaseEntity<int>
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int Credits { get; set; }
    }
}
```

> ✅ **Regla:** Siempre heredar de `BaseEntity<TId>`

---

### 2. Capa de Aplicación (Application)

#### 2.1 Crear DTO en `Application/Shared/DTOs/Course/`

```csharp
namespace Application.Shared.DTOs.Course
{
    public record CourseDto : BaseDto<int>
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int Credits { get; set; }
    }
}
```

> ✅ **Regla:** Heredar de `BaseDto<TId>`, usar `record` y agrupar por entidad

#### 2.2 Crear Validador en `Application/Validations/SpecificValidators/Course/`

```csharp
using FluentValidation;
using Application.Shared.DTOs.Course;

namespace Application.Validations.SpecificValidators.Course
{
    public class CourseValidator : AbstractValidator<CourseDto>
    {
        public CourseValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre es requerido")
                .MaximumLength(100).WithMessage("Máximo 100 caracteres");
                
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("El código es requerido")
                .MaximumLength(20).WithMessage("Máximo 20 caracteres");
                
            RuleFor(x => x.Credits)
                .GreaterThan(0).WithMessage("Los créditos deben ser mayor a 0");
        }
    }
}
```

> ℹ️ **Nota:** Para CRUD genérico, este validador se invoca automáticamente. No necesitas crear validadores de comandos adicionales.

---

### 3. Capa de Infraestructura (Infrastructure)

**Crear configuración EF Core** en `Infrastructure/Data/Configurations/`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Data.Configurations
{
    public class CourseConfiguration : BaseEntityConfiguration<Course, int>
    {
        public override void Configure(EntityTypeBuilder<Course> builder)
        {
            base.Configure(builder); // ⚠️ IMPORTANTE: Configura ID y auditoría
            
            builder.ToTable("Course"); // PascalCase para tabla
            
            // Columnas en minúsculas para PostgreSQL
            builder.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("name");
                
            builder.Property(e => e.Code)
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnName("code");
                
            builder.Property(e => e.Credits)
                .IsRequired()
                .HasColumnName("credits");
        }
    }
}
```

> ✅ **Regla:** Siempre heredar de `BaseEntityConfiguration<TEntity, TId>` y llamar `base.Configure(builder)`

---

### 4. Capa de API (Api)

**Crear controlador** en `Api/Controllers/`

```csharp
using Api.Controllers;
using Application.Shared.DTOs.Course;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CourseController : GenericController<Course, CourseDto, int>
    {
        public CourseController(IMediator mediator) : base(mediator)
        {
        }
    }
}
```

> ✅ **Listo!** Heredando de `GenericController` obtienes automáticamente:
> - `GET /api/course` - Listar con paginación
> - `GET /api/course/{id}` - Obtener por ID
> - `POST /api/course` - Crear
> - `PUT /api/course/{id}` - Actualizar
> - `DELETE /api/course/{id}` - Eliminar (soft delete)

---

### 5. Pruebas de Integración (Tests)

**Crear clase de prueba** en `Tests/Integration/Course/`

```csharp
using Tests.Integration;
using Domain.Entities;
using Application.Shared.DTOs.Course;

namespace Tests.Integration.Course
{
    public class CourseControllerTests : GenericControllerIntegrationTests<Domain.Entities.Course, CourseDto>
    {
        public CourseControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected override string BaseUrl => "/api/Course";

        protected override CourseDto CreateValidDto()
        {
            return new CourseDto 
            { 
                Name = "Test Course",
                Code = "TST101",
                Credits = 3
            };
        }

        protected override Domain.Entities.Course CreateValidEntity()
        {
            return new Domain.Entities.Course 
            { 
                Name = "Test Course",
                Code = "TST101",
                Credits = 3,
                StatusRegister = true 
            };
        }
    }
}
```

---

## 6. Registro Automático

✅ **No es necesario registrar manualmente:**
- Repositorios
- Validadores
- Mapeos (AutoMapper)

El proyecto usa **Scrutor** para registro automático basado en convenciones.

---

## 7. Casos Especiales: CQRS Manual

Para funcionalidades que **NO** se ajustan al patrón CRUD genérico (ej: subida de archivos, reportes, procesos complejos), usa CQRS manualmente.

### Ejemplo: Comando Personalizado

**1. Crear Command** en `Application/Shared/Commands/Course/`

```csharp
namespace Application.Shared.Commands.Course
{
    public record EnrollStudentCommand(int CourseId, int StudentId) : IRequest<bool>;
}
```

**2. Crear Handler** en `Application/Shared/Commands/Course/Handlers/`

```csharp
namespace Application.Shared.Commands.Course.Handlers
{
    public class EnrollStudentCommandHandler : IRequestHandler<EnrollStudentCommand, bool>
    {
        private readonly IRepository<Domain.Entities.Course, int> _repository;
        private readonly IUnitOfWork _unitOfWork;
        
        public async Task<bool> Handle(EnrollStudentCommand request, CancellationToken ct)
        {
            // Lógica personalizada
            var course = await _repository.GetByIdAsync(request.CourseId, ct);
            course.AddDomainEvent(new StudentEnrolledEvent(request.CourseId, request.StudentId));
            
            // CommitAsync disparará todos los DomainEvents registrados automáticamente
            await _unitOfWork.CommitAsync(ct);
            return true;
        }
    }
}
```

**3. Crear Validador** en `Application/Validations/SpecificValidators/Course/`

```csharp
namespace Application.Validations.SpecificValidators.Course
{
    public class EnrollStudentCommandValidator : AbstractValidator<EnrollStudentCommand>
    {
        public EnrollStudentCommandValidator()
        {
            RuleFor(x => x.CourseId).GreaterThan(0);
            RuleFor(x => x.StudentId).GreaterThan(0);
        }
    }
}
```

**4. Usar en Controller**

```csharp
[HttpPost("{id}/enroll")]
public async Task<IActionResult> EnrollStudent(int id, [FromBody] int studentId)
{
    var command = new EnrollStudentCommand(id, studentId);
    var result = await _mediator.Send(command);
    return Ok(new ApiResponse<bool> { Data = result });
}
```

> 📖 **Ejemplo completo:** Ver [MODULOS/DOCUMENTOS.md](MODULOS/DOCUMENTOS.md)

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

---

## Resumen del Flujo

1. ✅ **Domain** → Crear entidad
2. ✅ **Application** → Crear DTO y validador
3. ✅ **Infrastructure** → Crear configuración EF Core
4. ✅ **API** → Crear controlador
5. ✅ **Tests** → Crear pruebas de integración
6. ✅ **Ejecutar** → `dotnet build` y `dotnet test`