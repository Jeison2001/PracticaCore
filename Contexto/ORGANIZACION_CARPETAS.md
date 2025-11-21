# Organización de Carpetas - PracticaCore

## Guía Rápida: ¿Dónde Agregar Código Nuevo?

### Nueva Entidad

1. **Domain/Entities/** → `{Entity}.cs`
2. **Domain/Interfaces/Repositories/** → `I{Entity}Repository.cs` (si necesita consultas custom)
3. **Infrastructure/Repositories/** → `{Entity}Repository.cs`
4. **Infrastructure/Data/Configurations/** → `{Entity}Configuration.cs`
5. **Application/Shared/DTOs/{Entity}/** → `{Entity}Dto.cs`
6. **Application/Shared/Mappings/** → `{Entity}Profile.cs`
7. **Application/Validations/SpecificValidators/{Entity}/** → `{Entity}Validator.cs`
8. **Api/Controllers/** → `{Entity}Controller.cs`

### Nuevo Servicio de Dominio

1. **Domain/Interfaces/Services/** → `I{Service}.cs` (o en subcarpeta por feature)
2. **Infrastructure/Services/** → `{Service}.cs` (misma estructura)

### Nuevo Command/Query

1. **Application/Shared/Commands/{Entity}/** → `{Action}{Entity}Command.cs`
2. **Application/Shared/Commands/{Entity}/Handlers/** → `{Action}{Entity}CommandHandler.cs`
3. **Application/Validations/SpecificValidators/{Entity}/** → `{Action}{Entity}CommandValidator.cs`

## Estructura Detallada por Capa

### Domain Layer

```
Domain/
├── Common/
│   ├── FilterBuilder.cs
│   ├── PaginatedResult.cs
│   ├── Auth/
│   │   └── AuthenticationResult.cs
│   └── Notifications/
│       ├── EmailNotification.cs
│       ├── EmailAttachment.cs
│       └── EmailRecipientsResult.cs
├── Configuration/
│   ├── AzureBlobOptions.cs
│   ├── GoogleCloudOptions.cs
│   └── LocalStorageOptions.cs
├── Entities/
│   ├── BaseEntity.cs
│   ├── AcademicPractice.cs
│   ├── Proposal.cs
│   └── ... (40+ entidades)
├── Enums/
│   ├── AcademicPracticeStateStageEnum.cs
│   └── StateStageCodeEnum.cs
├── Exceptions/
│   └── NotFoundException.cs
└── Interfaces/
    ├── Common/
    │   └── ServiceInterfaces.cs
    ├── Repositories/
    │   ├── IRepository.cs
    │   ├── IUnitOfWork.cs
    │   └── I{Entity}Repository.cs
    └── Services/
        ├── IAcademicPeriodService.cs
        ├── IUserService.cs
        ├── IIdGeneratorService.cs
        ├── Auth/
│       │   ├── IAuthService.cs
│       │   └── ITokenValidator.cs
        ├── Cache/
        ├── Notifications/
        └── Storage/
```

**Reglas:**
- ✅ Entidades siempre en `Entities/`
- ✅ Interfaces de repositorio en `Interfaces/Repositories/`
- ✅ Interfaces de servicios en `Interfaces/Services/` (con subcarpetas por feature)
- ❌ No poner lógica de negocio en entidades (solo propiedades y validaciones básicas)

### Application Layer

```
Application/
├── Common/
│   ├── Behaviors/
│   │   └── ValidationBehavior.cs
│   └── Services/
│       ├── IdGeneratorService.cs
│       └── Notifications/
│           ├── EmailNotificationEventService.cs
│           ├── Builders/
│           ├── Dispatcher/
│           └── Handlers/
├── Shared/
│   ├── Commands/
│   │   ├── CreateEntityCommand.cs (genérico)
│   │   ├── UpdateEntityCommand.cs (genérico)
│   │   ├── {Entity}/
│   │   │   ├── Create{Entity}Command.cs
│   │   │   └── Handlers/
│   │   │       └── Create{Entity}CommandHandler.cs
│   │   └── Handlers/
│   │       ├── CreateEntityCommandHandler.cs
│   │       └── UpdateEntityCommandHandler.cs
│   ├── Queries/
│   │   ├── GetAllEntitiesQuery.cs (genérico)
│   │   ├── {Entity}/
│   │   │   ├── Get{Entity}ByIdQuery.cs
│   │   │   └── Handlers/
│   │   │       └── Get{Entity}ByIdQueryHandler.cs
│   │   └── Handlers/
│   ├── DTOs/
│   │   ├── BaseDto.cs
│   │   ├── PaginatedRequest.cs
│   │   └── {Entity}/
│   │       ├── {Entity}Dto.cs
│   │       └── {Entity}WithDetailsResponseDto.cs
│   └── Mappings/
│       ├── GenericProfile.cs
│       └── {Entity}Profile.cs
└── Validations/
    ├── BaseValidators/
    │   ├── BaseCreateCommandValidator.cs
    │   └── BaseUpdateCommandValidator.cs
    ├── Common/
    │   ├── EmailNotificationValidator.cs
    │   └── EmailAttachmentValidator.cs
    └── SpecificValidators/
        └── {Entity}/
            ├── {Entity}Validator.cs
            └── Create{Entity}CommandValidator.cs
```

**Reglas:**
- ✅ Commands/Queries genéricos en raíz, específicos en carpeta por entidad
- ✅ DTOs siempre en carpeta por entidad
- ✅ Handlers junto a sus Commands/Queries
- ✅ Validadores base en `BaseValidators/`, específicos en `SpecificValidators/{Entity}/`

### Infrastructure Layer

```
Infrastructure/
├── Data/
│   ├── AppDbContext.cs
│   └── Configurations/
│       ├── BaseEntityConfiguration.cs
│       └── {Entity}Configuration.cs
├── Repositories/
│   ├── BaseRepository.cs
│   ├── UnitOfWork.cs
│   ├── {Entity}Repository.cs
│   └── Cache/
│       ├── CachedRepository.cs
│       ├── CachedRepositoryFactory.cs
│       └── CachedUnitOfWork.cs
├── Services/
│   ├── AcademicPeriodService.cs
│   ├── UserService.cs
│   ├── Auth/
│   │   ├── GoogleAuthService.cs
│   │   ├── GoogleTokenValidator.cs
│   │   └── JwtService.cs
│   ├── Cache/
│   │   └── MemoryCacheService.cs
│   ├── Notifications/
│   │   ├── EmailNotificationQueueService.cs
│   │   └── SmtpNotificationService.cs
│   └── Storage/
│       ├── LocalFileStorageService.cs
│       ├── GoogleCloudFileStorageService.cs
│       ├── AzureBlobFileStorageService.cs
│       └── AwsS3FileStorageService.cs
└── Extensions/
    ├── CacheServiceExtensions.cs
    ├── EmailNotificationServiceExtensions.cs
    └── QueryableExtensions.cs
```

**Reglas:**
- ✅ Repositorios en `Repositories/`, UnitOfWork también
- ✅ Servicios en `Services/` con misma estructura que interfaces en Domain
- ✅ Configuraciones EF Core en `Data/Configurations/`
- ✅ Extensions para DI en `Extensions/`

### API Layer

```
Api/
├── Controllers/
│   ├── GenericController.cs
│   ├── {Entity}Controller.cs
│   └── Utilities/
│       └── ServicesController.cs
├── Extensions/
│   ├── ServiceExtensions.cs
│   └── HangfireExtensions.cs
├── Middlewares/
│   ├── GlobalExceptionMiddleware.cs
│   └── HangfireAuthorizationFilter.cs
├── Responses/
│   └── ApiResponse.cs
├── SwaggerFilters/
│   ├── HideReadOnlyPropertiesFilter.cs
│   └── SwaggerExcludeFromPostAttribute.cs
├── Program.cs
└── appsettings.json
```

**Reglas:**
- ✅ Un controller por entidad
- ✅ Configuración de servicios en `Extensions/ServiceExtensions.cs`
- ✅ Middlewares globales en `Middlewares/`

## Árbol de Decisión

### ¿Dónde va mi código?

```
¿Es una entidad de base de datos?
├─ Sí → Domain/Entities/
└─ No
   ├─ ¿Es una interfaz?
   │  ├─ ¿De repositorio? → Domain/Interfaces/Repositories/
   │  ├─ ¿De servicio? → Domain/Interfaces/Services/
   │  └─ ¿Marcador de DI? → Domain/Interfaces/Common/
   │
   ├─ ¿Es una implementación?
   │  ├─ ¿De repositorio? → Infrastructure/Repositories/
   │  ├─ ¿De servicio? → Infrastructure/Services/
   │  └─ ¿Configuración EF? → Infrastructure/Data/Configurations/
   │
   ├─ ¿Es lógica de aplicación?
   │  ├─ ¿Command/Query? → Application/Shared/Commands o Queries/{Entity}/
   │  ├─ ¿DTO? → Application/Shared/DTOs/{Entity}/
   │  ├─ ¿Validación? → Application/Validations/SpecificValidators/{Entity}/
   │  └─ ¿Mapping? → Application/Shared/Mappings/
   │
   └─ ¿Es API?
      ├─ ¿Controller? → Api/Controllers/
      ├─ ¿Middleware? → Api/Middlewares/
      └─ ¿Configuración? → Api/Extensions/
```

## Patrones de Nombres de Archivos

### Domain
- Entidades: `{Entity}.cs` (PascalCase)
- Interfaces: `I{Name}.cs`
- Enums: `{Name}Enum.cs`

### Application
- Commands: `{Action}{Entity}Command.cs`
- Queries: `Get{Entity}By{Criteria}Query.cs`
- Handlers: `{CommandOrQueryName}Handler.cs`
- DTOs: `{Entity}Dto.cs` o `{Entity}{Purpose}Dto.cs`
- Validators: `{Entity}Validator.cs` o `{Command}Validator.cs`

### Infrastructure
- Repositories: `{Entity}Repository.cs`
- Services: `{Name}Service.cs`
- Configurations: `{Entity}Configuration.cs`

### API
- Controllers: `{Entity}Controller.cs`

## Ejemplos Prácticos

### Agregar Nueva Entidad "Course"

1. **Domain/Entities/Course.cs**
```csharp
public class Course : BaseEntity<int>
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
```

2. **Infrastructure/Data/Configurations/CourseConfiguration.cs**
```csharp
public class CourseConfiguration : BaseEntityConfiguration<Course, int>
{
    public override void Configure(EntityTypeBuilder<Course> builder)
    {
        base.Configure(builder);
        builder.Property(c => c.Code).HasMaxLength(20);
    }
}
```

3. **Application/Shared/DTOs/Courses/CourseDto.cs**
```csharp
public record CourseDto : BaseDto<int>
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
```

4. **Api/Controllers/CourseController.cs**
```csharp
[ApiController]
[Route("api/[controller]")]
public class CourseController : GenericController<Course, CourseDto, int>
{
    public CourseController(IMediator mediator) : base(mediator) { }
}
```

### Agregar Nuevo Servicio "ReportService"

1. **Domain/Interfaces/Services/IReportService.cs**
```csharp
namespace Domain.Interfaces.Services
{
    public interface IReportService : IScopedService
    {
        Task<byte[]> GenerateReportAsync(int id);
    }
}
```

2. **Infrastructure/Services/ReportService.cs**
```csharp
namespace Infrastructure.Services
{
    public class ReportService : IReportService
    {
        public async Task<byte[]> GenerateReportAsync(int id)
        {
            // Implementación
        }
    }
}
```

## Resumen

- **Entidades** → `Domain/Entities/`
- **Interfaces** → `Domain/Interfaces/{Repositories|Services|Common}/`
- **Implementaciones** → `Infrastructure/{Repositories|Services}/`
- **CQRS** → `Application/Shared/{Commands|Queries}/{Entity}/`
- **DTOs** → `Application/Shared/DTOs/{Entity}/`
- **Controllers** → `Api/Controllers/`
