# Patrones de Diseño - PracticaCore

## Introducción

PracticaCore implementa varios patrones de diseño para mantener el código limpio, testeable y escalable.

## Clean Architecture

PracticaCore sigue los principios de **Clean Architecture** donde las capas externas dependen de las internas, nunca al revés:

- **API Layer** → depende de Application
- **Application Layer** → depende de Domain
- **Infrastructure Layer** → implementa interfaces de Domain
- **Domain Layer** → núcleo independiente (sin dependencias externas)

> 📖 Para más detalles sobre la arquitectura de capas y sus responsabilidades, ver **[ARQUITECTURA.md](ARQUITECTURA.md)**.

Los siguientes patrones se implementan dentro de esta arquitectura:

## CQRS (Command Query Responsibility Segregation)

### Separación de Comandos y Consultas

**Commands** (Escritura):
```csharp
// Application/Shared/Commands/Proposals/CreateProposalCommand.cs
public record CreateProposalCommand(ProposalDto Dto) : IRequest<int>;

// Handler
public class CreateProposalCommandHandler : IRequestHandler<CreateProposalCommand, int>
{
    private readonly IRepository<Proposal, int> _repository;
    private readonly IUnitOfWork _unitOfWork;
    
    public async Task<int> Handle(CreateProposalCommand request, CancellationToken ct)
    {
        var entity = _mapper.Map<Proposal>(request.Dto);
        await _repository.AddAsync(entity);
        await _unitOfWork.CommitAsync(ct);
        return entity.Id;
    }
}
```

**Queries** (Lectura):
```csharp
// Application/Shared/Queries/Proposals/GetProposalByIdQuery.cs
public record GetProposalByIdQuery(int Id) : IRequest<ProposalDto>;

// Handler
public class GetProposalByIdQueryHandler : IRequestHandler<GetProposalByIdQuery, ProposalDto>
{
    private readonly IRepository<Proposal, int> _repository;
    
    public async Task<ProposalDto> Handle(GetProposalByIdQuery request, CancellationToken ct)
    {
        var entity = await _repository.GetByIdAsync(request.Id);
        return _mapper.Map<ProposalDto>(entity);
    }
}
```

## Repository Pattern

### Interfaz Genérica

```csharp
// Domain/Interfaces/Repositories/IRepository.cs
public interface IRepository<T, TId> where T : BaseEntity<TId> where TId : struct
{
    Task<T?> GetByIdAsync(TId id, CancellationToken ct = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);
    Task<PaginatedResult<T>> GetPaginatedAsync(PaginatedRequest request, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Delete(T entity);
}
```

### Implementación Base

```csharp
// Infrastructure/Repositories/BaseRepository.cs
public class BaseRepository<T, TId> : IRepository<T, TId> 
    where T : BaseEntity<TId> 
    where TId : struct
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;
    
    public async Task<T?> GetByIdAsync(TId id, CancellationToken ct = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, ct);
    }
    // ... otras implementaciones
}
```

### Repositorio Específico

```csharp
// Domain/Interfaces/Repositories/IProposalRepository.cs
public interface IProposalRepository : IRepository<Proposal, int>
{
    Task<ProposalWithDetails?> GetProposalWithDetailsAsync(int id, CancellationToken ct = default);
}

// Infrastructure/Repositories/ProposalRepository.cs
public class ProposalRepository : BaseRepository<Proposal, int>, IProposalRepository
{
    public async Task<ProposalWithDetails?> GetProposalWithDetailsAsync(int id, CancellationToken ct)
    {
        return await _dbSet
            .Include(p => p.UserInscriptionModalities)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }
}
```

## Unit of Work Pattern

```csharp
// Domain/Interfaces/Repositories/IUnitOfWork.cs
public interface IUnitOfWork
{
    IRepository<T, TId> GetRepository<T, TId>() where T : BaseEntity<TId> where TId : struct;
    Task<int> CommitAsync(CancellationToken ct = default);
}

// Infrastructure/Repositories/UnitOfWork.cs
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    
    public IRepository<T, TId> GetRepository<T, TId>() 
        where T : BaseEntity<TId> where TId : struct
    {
        return new BaseRepository<T, TId>(_context);
    }
    
    public async Task<int> CommitAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }
}
```

## Decorator Pattern (Cache)

```csharp
// Infrastructure/Repositories/Cache/CachedRepository.cs
public class CachedRepository<T, TId> : IRepository<T, TId> 
    where T : BaseEntity<TId> where TId : struct
{
    private readonly IRepository<T, TId> _innerRepository;
    private readonly ICacheService _cacheService;
    
    public async Task<T?> GetByIdAsync(TId id, CancellationToken ct = default)
    {
        var cacheKey = $"{typeof(T).Name}_{id}";
        var cached = await _cacheService.GetAsync<T>(cacheKey);
        
        if (cached != null) return cached;
        
        var entity = await _innerRepository.GetByIdAsync(id, ct);
        if (entity != null)
        {
            await _cacheService.SetAsync(cacheKey, entity, TimeSpan.FromMinutes(10));
        }
        return entity;
    }
}
```

## Mediator Pattern (MediatR)

```csharp
// API Controller
[HttpPost]
public async Task<IActionResult> Create([FromBody] ProposalDto dto)
{
    var command = new CreateProposalCommand(dto);
    var id = await _mediator.Send(command);
    return Ok(new ApiResponse<int> { Data = id });
}
```

**Flujo:**
1. Controller recibe request
2. Crea Command/Query
3. Envía a Mediator
4. Mediator encuentra Handler
5. Handler ejecuta lógica
6. Retorna resultado

## Domain Events Pattern (Patrón Observer)

Los Domain Events gestionados por MediatR implementan el Patrón Observer a nivel de código. Esto permite acoplamiento débil entre el cambio de estado de una entidad y las reglas de negocio que se ejecutan como resultado.

### Definición del Evento (Dominio)

```csharp
// Domain/Common/BaseEvent.cs
public abstract record BaseEvent : INotification;

// Domain/Events/InscriptionStateChangedEvent.cs
public record InscriptionStateChangedEvent(
    int InscriptionModalityId, 
    int ModalityId, 
    int NewStateInscriptionId, 
    int ChangedByUserId) : BaseEvent;
```

### Agregando el Evento (Entidad)

```csharp
// Domain/Entities/BaseEntity.cs
public abstract class BaseEntity<TId> where TId : struct
{
    private readonly List<BaseEvent> _domainEvents = new();
    public IReadOnlyCollection<BaseEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(BaseEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}

// Ejemplo de uso en un handler de Application o servicio:
inscription.IdStateInscription = newStateId;
inscription.AddDomainEvent(new InscriptionStateChangedEvent(
    inscription.Id, inscription.IdModality, newStateId, userId));
```

### Dispatcher (UnitOfWork)

El `UnitOfWork` intercepta el `SaveChanges` de EF Core, recolecta todos los `DomainEvents` pendientes y los publica vía MediatR en la misma transacción:

```csharp
// Infrastructure/Repositories/UnitOfWork.cs
public async Task<int> CommitAsync(CancellationToken ct = default)
{
    var domainEvents = _context.ChangeTracker.Entries<BaseEntity<int>>()
        .SelectMany(x => x.Entity.DomainEvents).ToList();

    var result = await _context.SaveChangesAsync(ct);

    foreach (var domainEvent in domainEvents)
    {
        await _mediator.Publish(domainEvent, ct);
    }
    
    // ... clear events ...
    return result;
}
```

### Múltiples Handlers Reaccionan al Evento (Application)

```csharp
// Application/Features/Proposals/EventHandlers/StartProposalPhaseOnApprovalHandler.cs
public class StartProposalPhaseOnApprovalHandler : INotificationHandler<InscriptionStateChangedEvent>
{
    public async Task Handle(InscriptionStateChangedEvent notification, CancellationToken ct)
    {
        // ... Solo reaccionar si la modalidad es Proyecto de Grado
        if (modality.Code != ModalityCodes.ProyectoGrado) return;
        
        // Ejecutar lógica de negocio como avance de fase, creación de registros, etc.
    }
}
```

## Dependency Injection

### Registro Automático con Scrutor

```csharp
// Api/Extensions/ServiceExtensions.cs
services.Scan(scan => scan
    .FromAssembliesOf(typeof(IRepository<,>), typeof(BaseRepository<,>))
    .AddClasses(classes => classes.AssignableTo<IScopedService>())
    .AsImplementedInterfaces()
    .WithScopedLifetime()
);
```

### Marcadores de Ciclo de Vida

```csharp
// Domain/Interfaces/Common/ServiceInterfaces.cs
public interface IScopedService { }
public interface ISingletonService { }
public interface ITransientService { }
```

## Validation Pipeline (FluentValidation)

```csharp
// Application/Common/Behaviors/ValidationBehavior.cs
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(result => result.Errors)
            .Where(f => f != null)
            .ToList();
            
        if (failures.Any())
            throw new ValidationException(failures);
            
        return await next();
    }
}
```

## Resumen de Patrones

| Patrón | Ubicación | Propósito |
|--------|-----------|-----------|
| **Clean Architecture** | Todas las capas | Separación de responsabilidades |
| **CQRS** | Application/Shared | Separar lectura/escritura |
| **Repository** | Domain/Interfaces, Infrastructure | Abstracción de acceso a datos |
| **Unit of Work** | Domain/Interfaces, Infrastructure | Transacciones |
| **Decorator** | Infrastructure/Repositories/Cache | Agregar funcionalidad (cache) |
| **Mediator** | Application (MediatR) | Desacoplar requests/handlers |
| **Notification** | Application/Common/Services | Eventos de dominio |
| **Dependency Injection** | Api/Extensions | Inversión de control |
| **Validation Pipeline** | Application/Common/Behaviors | Validación centralizada |
| **Domain Result** | Domain/Common | Tipado fuerte para resultados complejos |
| **Adapter** | Infrastructure/Services | Abstracción de librerías externas |

## Domain Result Pattern

Para evitar que el **Domain Layer** dependa de **Application Layer** (DTOs) o pierda seguridad de tipos (`dynamic`), utilizamos objetos de resultado definidos en el propio dominio.

**Problema:**
Un servicio de dominio (`IAuthService`) necesita retornar datos complejos (Token, Usuario, Roles).
- Si retorna `AuthResponse` (DTO de Application) → Viola Clean Architecture (Domain depende de Application).
- Si retorna `dynamic` → Pierde type-safety y contrato explícito.

**Solución:**
Crear un POCO en `Domain/Common` que represente el resultado.

```csharp
// Domain/Common/Auth/AuthenticationResult.cs
public record AuthenticationResult
{
    public string Token { get; init; }
    public UserInfoResult User { get; init; }
    // ...
}

// Domain/Interfaces/Services/Auth/IAuthService.cs
Task<AuthenticationResult> AuthenticateAsync(string token);
```

El controlador (API) es responsable de mapear este resultado de dominio a la respuesta de la API (`AuthResponse`).

## Adapter Pattern (Abstracción de Servicios Externos)

Para proteger el dominio de cambios en librerías externas (como Google Auth, Azure Blob Storage), utilizamos interfaces de dominio que abstraen la implementación concreta.

**Ejemplo:**
En lugar de usar `GoogleJsonWebSignature.Payload` (tipo de librería externa) en una interfaz, creamos nuestro propio tipo y una interfaz adaptadora.

```csharp
// Domain/Common/Auth/TokenPayload.cs (Nuestro tipo)
public record TokenPayload(string Email, string Name, ...);

// Domain/Interfaces/Services/Auth/ITokenValidator.cs (Nuestra interfaz)
public interface ITokenValidator
{
    Task<TokenPayload> ValidateAsync(string token);
}

// Infrastructure/Services/Auth/GoogleTokenValidator.cs (El adaptador)
public class GoogleTokenValidator : ITokenValidator
{
    public async Task<TokenPayload> ValidateAsync(string token)
    {
        var googlePayload = await GoogleJsonWebSignature.ValidateAsync(token, ...);
        return new TokenPayload(googlePayload.Email, ...); // Mapeo
    }
}
```

