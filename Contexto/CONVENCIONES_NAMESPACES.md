# Convenciones de Namespaces - PracticaCore

## Introducción

Este documento define las convenciones de namespaces utilizadas en PracticaCore después de la estandarización estructural.

## Estructura General

```
PracticaCore/
├── Domain/              → Domain.*
├── Application/         → Application.*
├── Infrastructure/      → Infrastructure.*
└── Api/                 → Api.*
```

## Domain Layer

### Namespace Base: `Domain`

#### Entidades y Modelos
```csharp
Domain.Entities              // Entidades del dominio
Domain.Enums                 // Enumeraciones
Domain.Common                // Clases comunes
Domain.Configuration         // Configuraciones
Domain.Exceptions            // Excepciones personalizadas
```

#### Interfaces - Estructura en 3 Subcarpetas

```csharp
Domain.Interfaces.Common
  └── ServiceInterfaces.cs   // IScopedService, ISingletonService, ITransientService

Domain.Interfaces.Repositories
  ├── IRepository<T, TId>    // Interfaz genérica
  ├── IUnitOfWork            // Unit of Work
  └── I{Entity}Repository    // Repositorios específicos

Domain.Interfaces.Services
  ├── IAcademicPeriodService.cs
  ├── IUserService.cs
  ├── Auth/
  │   ├── IAuthService.cs
  │   └── IJwtService.cs
  ├── Cache/
  │   └── ICacheService.cs
  ├── Notifications/
  │   ├── INotificationService.cs
  │   ├── Builders/
  │   ├── Dispatcher/
  │   └── Handlers/
  └── Storage/
      └── IFileStorageService.cs
```

**Regla clave:** 
- ✅ Acceso a datos → `Domain.Interfaces.Repositories`
- ✅ Servicios de dominio → `Domain.Interfaces.Services`
- ✅ Marcadores de ciclo de vida → `Domain.Interfaces.Common`

## Application Layer

### Namespace Base: `Application`

```csharp
Application.Shared.Commands
  ├── {Entity}/
  │   └── Handlers/
  └── Handlers/

Application.Shared.Queries
  ├── {Entity}/
  │   └── Handlers/
  └── Handlers/

Application.Shared.DTOs
  └── {Entity}/

Application.Shared.Mappings

Application.Common.Behaviors
Application.Common.Services
  └── Notifications/

Application.Validations.BaseValidators
Application.Validations.SpecificValidators.{Entity}
```

**Patrón de nombres:**
- Commands: `{Action}{Entity}Command`
- Queries: `Get{Entity}By{Criteria}Query`
- Handlers: `{CommandOrQuery}Handler`
- DTOs: `{Entity}Dto` o `{Entity}{Purpose}Dto`

## Infrastructure Layer

### Namespace Base: `Infrastructure`

```csharp
Infrastructure.Data
  └── Configurations/

Infrastructure.Repositories
  ├── UnitOfWork.cs
  └── Cache/

Infrastructure.Services
  ├── Auth/
  ├── Cache/
  ├── Notifications/
  └── Storage/

Infrastructure.Extensions
```

## API Layer

### Namespace Base: `Api`

```csharp
Api.Controllers
  └── Utilities/

Api.Extensions
Api.Middlewares
Api.Responses
Api.SwaggerFilters
```

## Migración de Namespaces Obsoletos

### Cambios Recientes (2025)

| Obsoleto | Actual |
|----------|--------|
| `Domain.Interfaces.Auth` | `Domain.Interfaces.Services.Auth` |
| `Domain.Interfaces.Cache` | `Domain.Interfaces.Services.Cache` |
| `Domain.Interfaces.Notifications` | `Domain.Interfaces.Services.Notifications` |
| `Domain.Interfaces.Storage` | `Domain.Interfaces.Services.Storage` |
| `Domain.Interfaces.Services.Notifications.Services` | `Domain.Interfaces.Services.Notifications` |
| `Infrastructure.Services.UnitOfWork` | `Infrastructure.Repositories.UnitOfWork` |

## Ejemplos

### Repositorio

```csharp
// Domain/Interfaces/Repositories/IProposalRepository.cs
namespace Domain.Interfaces.Repositories
{
    public interface IProposalRepository : IRepository<Proposal, int> { }
}

// Infrastructure/Repositories/ProposalRepository.cs
namespace Infrastructure.Repositories
{
    public class ProposalRepository : BaseRepository<Proposal, int>, IProposalRepository { }
}
```

### Servicio

```csharp
// Domain/Interfaces/Services/Auth/IJwtService.cs
namespace Domain.Interfaces.Services.Auth
{
    public interface IJwtService : IScopedService { }
}

// Infrastructure/Services/Auth/JwtService.cs
namespace Infrastructure.Services.Auth
{
    public class JwtService : IJwtService { }
}
```

### Command Handler

```csharp
// Application/Shared/Commands/Proposals/CreateProposalCommand.cs
namespace Application.Shared.Commands.Proposals
{
    public record CreateProposalCommand(ProposalDto Dto) : IRequest<int>;
}

// Application/Shared/Commands/Proposals/Handlers/CreateProposalCommandHandler.cs
namespace Application.Shared.Commands.Proposals.Handlers
{
    public class CreateProposalCommandHandler : IRequestHandler<CreateProposalCommand, int> { }
}
```

## Resumen

- **Domain.Interfaces**: `Common`, `Repositories`, `Services`
- **Application**: `Shared` (CQRS), `Common` (servicios), `Validations`
- **Infrastructure**: Refleja estructura de Domain
- **API**: Controllers, Extensions, Middlewares
