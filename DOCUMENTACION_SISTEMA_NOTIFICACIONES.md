# 🔔 **DOCUMENTACIÓN TÉCNICA - SISTEMA DE NOTIFICACIONES**

## 📋 **ÍNDICE**

1. [Arquitectura General](#arquitectura-general)
2. [Componentes del Sistema](#componentes-del-sistema)
3. [Flujo de Notificaciones](#flujo-de-notificaciones)
4. [Guía para Agregar Nuevas Entidades](#guía-para-agregar-nuevas-entidades)
5. [Patrones de Diseño Aplicados](#patrones-de-diseño-aplicados)
6. [Testing](#testing)
7. [Troubleshooting](#troubleshooting)

---

## 🏗️ **ARQUITECTURA GENERAL**

### **Principios SOLID Aplicados:**

- **🎯 Single Responsibility:** Cada handler maneja un solo tipo de entidad
- **🔒 Open/Closed:** Agregar nuevas entidades no requiere modificar código existente
- **🔄 Liskov Substitution:** Todos los handlers implementan la misma interfaz
- **🧩 Interface Segregation:** Interfaces específicas y enfocadas
- **⬇️ Dependency Inversion:** Dependencias en interfaces, no implementaciones

### **Diagrama de Arquitectura:**

```
┌─────────────────────────────────────────────────────────────────┐
│                    CAPA DE APLICACIÓN                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────────┐    ┌─────────────────────────────────────┐  │
│  │  CommandHandler │───▶│      NotificationDispatcher        │  │
│  │   (Trigger)     │    │      (Strategy Resolver)           │  │
│  └─────────────────┘    └─────────────────────────────────────┘  │
│                                        │                        │
│                                        ▼                        │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │              ENTITY-SPECIFIC HANDLERS                      │  │
│  ├─────────────────────────────────────────────────────────────┤  │
│  │  ┌─────────────────┐  ┌─────────────────┐                  │  │
│  │  │ ProposalChange  │  │InscriptionChange│                  │  │
│  │  │    Handler      │  │    Handler      │                  │  │
│  │  └─────────────────┘  └─────────────────┘                  │  │
│  └─────────────────────────────────────────────────────────────┘  │
│                                        │                        │
│                                        ▼                        │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │               SUPPORT SERVICES                             │  │
│  ├─────────────────────────────────────────────────────────────┤  │
│  │  ┌─────────────────┐  ┌─────────────────┐                  │  │
│  │  │ EventDataBuilder│  │ StudentData     │                  │  │
│  │  │   Services      │  │   Service       │                  │  │
│  │  └─────────────────┘  └─────────────────┘                  │  │
│  └─────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────┐
│                   INFRASTRUCTURE LAYER                         │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │           EmailNotificationQueueService                    │  │
│  │              (Hangfire Integration)                        │  │
│  └─────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🧱 **COMPONENTES DEL SISTEMA**

### **1. INTERFACES BASE (Domain Layer)**

#### **📄 `INotificationDispatcher.cs`**
```csharp
/// <summary>
/// Dispatcher genérico que resuelve automáticamente el handler apropiado
/// PATRÓN: Strategy + Factory
/// </summary>
public interface INotificationDispatcher : IScopedService
{
    Task DispatchEntityChangeAsync<T, TId>(T oldEntity, T newEntity, CancellationToken cancellationToken = default) 
        where T : BaseEntity<TId> where TId : struct;
    Task DispatchEntityCreationAsync<T, TId>(T entity, CancellationToken cancellationToken = default) 
        where T : BaseEntity<TId> where TId : struct;
}
```

#### **📄 `IEntityChangeHandler.cs`**
```csharp
/// <summary>
/// Handler específico para cada tipo de entidad
/// PATRÓN: Strategy
/// PRINCIPIO: Single Responsibility
/// </summary>
public interface IEntityChangeHandler<T, TId> : IScopedService 
    where T : BaseEntity<TId> where TId : struct
{
    Task HandleChangeAsync(T oldEntity, T newEntity, CancellationToken cancellationToken = default);
    Task HandleCreationAsync(T entity, CancellationToken cancellationToken = default);
}
```

#### **📄 `IStudentDataService.cs`**
```csharp
/// <summary>
/// Servicio compartido para obtener datos de estudiantes
/// PATRÓN: Service Layer
/// PRINCIPIO: Don't Repeat Yourself
/// </summary>
public interface IStudentDataService : IScopedService
{
    Task<(string Names, string Emails, int Count)> GetStudentDataByProposalAsync(int proposalId);
    Task<(string Names, string Emails, int Count)> GetStudentDataByInscriptionAsync(int inscriptionId);
    Task<(string Names, string Emails, int Count)> GetStudentDataByUserIdsAsync(IEnumerable<int> userIds);
}
```

#### **📄 `IProposalEventDataBuilder.cs` & `IInscriptionEventDataBuilder.cs`**
```csharp
/// <summary>
/// Builders específicos para construir datos de eventos
/// PATRÓN: Builder
/// PRINCIPIO: Single Responsibility
/// </summary>
public interface IProposalEventDataBuilder : IScopedService
{
    Task<Dictionary<string, object>> BuildProposalEventDataAsync(int proposalId, string eventType);
}
```

#### **📄 `IInscriptionCreationService.cs`**
```csharp
/// <summary>
/// Servicio especializado para creación de inscripciones
/// PATRÓN: Specialized Service
/// USO: Solo para CreateInscriptionWithStudentsHandler
/// </summary>
public interface IInscriptionCreationService : IScopedService
{
    Task ProcessInscriptionCreationAsync(int inscriptionId, int modalityId, int academicPeriodId, 
        IEnumerable<int> studentIds, CancellationToken cancellationToken = default);
}
```

---

### **2. IMPLEMENTACIONES (Application Layer)**

#### **🚀 `NotificationDispatcher.cs` - EL CORAZÓN DEL SISTEMA**

```csharp
/// <summary>
/// RESPONSABILIDAD: Resolver automáticamente el handler apropiado para cada tipo de entidad
/// PATRÓN: Strategy + Dependency Injection Factory
/// BENEFICIO: Extensibilidad sin modificar código existente
/// </summary>
public class NotificationDispatcher : INotificationDispatcher
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(IServiceScopeFactory serviceScopeFactory, ILogger<NotificationDispatcher> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public async Task DispatchEntityChangeAsync<T, TId>(T oldEntity, T newEntity, CancellationToken cancellationToken = default) 
        where T : BaseEntity<TId> where TId : struct
    {
        // ✅ CRÍTICO: Usar IServiceScopeFactory para scope independiente del request
        using var scope = _serviceScopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetService<IEntityChangeHandler<T, TId>>();
        
        if (handler != null)
        {
            await handler.HandleChangeAsync(oldEntity, newEntity, cancellationToken);
        }
    }
}
```

**🔑 PUNTOS CLAVE:**
- **Automatic Resolution:** Resuelve automáticamente el handler apropiado via DI
- **Scope Management:** Crea su propio scope para evitar problemas de contexto disposed
- **Extensible:** Agregar nuevos handlers no requiere modificar este código
- **Fail-Safe:** Errores en notificaciones no afectan operaciones principales

#### **🎯 `ProposalChangeHandler.cs` - HANDLER ESPECÍFICO**

```csharp
/// <summary>
/// RESPONSABILIDAD: Manejar notificaciones específicas de Proposal
/// PRINCIPIO: Single Responsibility
/// TRIGGERS: Cambios de estado en StateStageEnum
/// </summary>
public class ProposalChangeHandler : IEntityChangeHandler<Proposal, int>
{
    public async Task HandleChangeAsync(Proposal oldEntity, Proposal newEntity, CancellationToken cancellationToken = default)
    {
        // Solo procesar si cambió el estado
        if (oldEntity.IdStateStage != newEntity.IdStateStage)
        {
            var eventName = GetProposalEventName((StateStageEnum)newEntity.IdStateStage);
            if (!string.IsNullOrEmpty(eventName))
            {
                var eventData = await _eventDataBuilder.BuildProposalEventDataAsync(newEntity.Id, eventName);
                var jobId = _queueService.EnqueueEventNotification(eventName, eventData);
            }
        }
    }

    private string GetProposalEventName(StateStageEnum stateStage)
    {
        return stateStage switch
        {
            StateStageEnum.PROP_RADICADA => "PROPOSAL_SUBMITTED",
            StateStageEnum.PROP_PERTINENTE => "PROPOSAL_APPROVED",
            StateStageEnum.PROP_NO_PERTINENTE => "PROPOSAL_REJECTED",
            _ => string.Empty
        };
    }
}
```

#### **📝 `InscriptionChangeHandler.cs` - HANDLER ESPECÍFICO**

```csharp
/// <summary>
/// RESPONSABILIDAD: Manejar notificaciones específicas de InscriptionModality
/// PRINCIPIO: Single Responsibility
/// TRIGGERS: Cambios de estado en StateInscription
/// </summary>
public class InscriptionChangeHandler : IEntityChangeHandler<InscriptionModality, int>
{
    public async Task HandleChangeAsync(InscriptionModality oldEntity, InscriptionModality newEntity, CancellationToken cancellationToken = default)
    {
        // Solo procesar si cambió el estado
        if (oldEntity.IdStateInscription != newEntity.IdStateInscription)
        {
            var eventName = await GetInscriptionEventNameAsync(newEntity.IdStateInscription, cancellationToken);
            if (!string.IsNullOrEmpty(eventName))
            {
                var eventData = await _eventDataBuilder.BuildInscriptionEventDataAsync(newEntity.Id, eventName);
                var jobId = _queueService.EnqueueEventNotification(eventName, eventData);
            }
        }
    }

    public Task HandleCreationAsync(InscriptionModality entity, CancellationToken cancellationToken = default)
    {
        // Esta lógica NO se ejecutará porque eliminamos la llamada del CreateEntityCommandHandler
        // Solo se maneja via CreateInscriptionWithStudentsHandler usando InscriptionCreationService
        _logger.LogDebug("InscriptionModality creation handling skipped - managed by specific handler");
        return Task.CompletedTask;
    }
}
```

#### **🏗️ `StudentDataService.cs` - SERVICIO COMPARTIDO**

```csharp
/// <summary>
/// RESPONSABILIDAD: Proveer datos de estudiantes para cualquier handler
/// PRINCIPIO: Don't Repeat Yourself
/// BENEFICIO: Lógica centralizada y reutilizable
/// </summary>
public class StudentDataService : IStudentDataService
{
    public async Task<(string Names, string Emails, int Count)> GetStudentDataByProposalAsync(int proposalId)
    {
        // Obtener InscriptionModality asociada a la propuesta
        var inscriptionModality = await inscriptionModalityRepo
            .GetFirstOrDefaultAsync(im => im.Proposal != null && im.Proposal.Id == proposalId, CancellationToken.None);

        if (inscriptionModality == null) return (string.Empty, string.Empty, 0);

        // Obtener usuarios asociados a la inscripción
        var userInscriptions = await userInscriptionModalityRepo
            .GetAllAsync(uim => uim.IdInscriptionModality == inscriptionModality.Id);

        var userIds = userInscriptions.Select(uim => uim.IdUser);
        return await GetStudentDataByUserIdsAsync(userIds);
    }
}
```

#### **🔧 `ProposalEventDataBuilder.cs` & `InscriptionEventDataBuilder.cs`**

```csharp
/// <summary>
/// RESPONSABILIDAD: Construir datos específicos para eventos de cada entidad
/// PRINCIPIO: Single Responsibility
/// PATRÓN: Builder
/// </summary>
public class ProposalEventDataBuilder : IProposalEventDataBuilder
{
    public async Task<Dictionary<string, object>> BuildProposalEventDataAsync(int proposalId, string eventType)
    {
        // Construir diccionario con todos los datos necesarios para el email
        var eventData = new Dictionary<string, object>
        {
            ["ProposalId"] = proposal.Id,
            ["ProposalTitle"] = proposal.Title ?? string.Empty,
            ["ProposalDescription"] = proposal.GeneralObjective ?? string.Empty,
            ["ProposalStateStage"] = stateStage?.Name ?? string.Empty,
            ["EventType"] = eventType,
            ["StudentNames"] = studentNames,
            ["StudentEmails"] = studentEmails,
            ["StudentCount"] = studentCount,
            // ... más datos específicos
        };
        return eventData;
    }
}
```

#### **⚙️ `InscriptionCreationService.cs` - SERVICIO ESPECIALIZADO**

```csharp
/// <summary>
/// RESPONSABILIDAD: Manejar específicamente la creación de inscripciones con estudiantes
/// USO: Solo para CreateInscriptionWithStudentsHandler
/// MOTIVO: Evitar duplicación de notificaciones
/// </summary>
public class InscriptionCreationService : IInscriptionCreationService
{
    public async Task ProcessInscriptionCreationAsync(int inscriptionId, int modalityId, int academicPeriodId, 
        IEnumerable<int> studentIds, CancellationToken cancellationToken = default)
    {
        var eventData = await _eventDataBuilder.BuildBasicInscriptionDataAsync(
            inscriptionId, modalityId, academicPeriodId, studentIds);

        var jobId = _queueService.EnqueueEventNotification("INSCRIPTION_CREATED", eventData);
    }
}
```

---

## 🔄 **FLUJO DE NOTIFICACIONES**

### **🎯 FLUJO DEL SISTEMA**

El sistema utiliza notificaciones basadas en eventos de aplicación procesados por Hangfire:

```
1. Usuario actualiza/crea entidad via API
           ↓
2. CommandHandler.Handle()
   - Actualiza/Crea entidad en BD
   - Llama ProcessNotificationsAsync()
           ↓
3. ProcessNotificationsAsync()
   - Ejecuta Task.Run() para no bloquear response
   - Llama NotificationDispatcher
           ↓
4. NotificationDispatcher
   - Crea nuevo scope de DI
   - Resuelve handler apropiado
   - Ejecuta handler correspondiente
           ↓
5. EntityChangeHandler
   - Verifica cambios relevantes
   - Construye datos del evento
   - Encola notificación en Hangfire
           ↓
6. EmailNotificationQueueService
   - Procesa notificación asíncronamente
   - Envía email via SMTP
```

---

## 🚀 **GUÍA PARA AGREGAR NUEVAS ENTIDADES**

### **📝 EJEMPLO: Agregar soporte para entidad `User`**

#### **Paso 1: Crear Handler Específico**

```csharp
// Application/Common/Services/Notifications/UserChangeHandler.cs
public class UserChangeHandler : IEntityChangeHandler<User, int>
{
    private readonly IEmailNotificationQueueService _queueService;
    private readonly IUserEventDataBuilder _eventDataBuilder;
    private readonly ILogger<UserChangeHandler> _logger;

    public UserChangeHandler(
        IEmailNotificationQueueService queueService,
        IUserEventDataBuilder eventDataBuilder,
        ILogger<UserChangeHandler> logger)
    {
        _queueService = queueService;
        _eventDataBuilder = eventDataBuilder;
        _logger = logger;
    }

    public async Task HandleChangeAsync(User oldEntity, User newEntity, CancellationToken cancellationToken = default)
    {
        // Lógica específica para cambios en User
        if (oldEntity.Email != newEntity.Email)
        {
            var eventData = await _eventDataBuilder.BuildUserEventDataAsync(newEntity.Id, "USER_EMAIL_CHANGED");
            var jobId = _queueService.EnqueueEventNotification("USER_EMAIL_CHANGED", eventData);
            
            _logger.LogInformation("User email change notification enqueued for ID: {UserId}, JobId: {JobId}", 
                newEntity.Id, jobId);
        }
    }

    public async Task HandleCreationAsync(User entity, CancellationToken cancellationToken = default)
    {
        var eventData = await _eventDataBuilder.BuildUserEventDataAsync(entity.Id, "USER_CREATED");
        var jobId = _queueService.EnqueueEventNotification("USER_CREATED", eventData);
        
        _logger.LogInformation("User creation notification enqueued for ID: {UserId}, JobId: {JobId}", 
            entity.Id, jobId);
    }
}
```

#### **Paso 2: Crear EventDataBuilder Específico**

```csharp
// Domain/Interfaces/Notifications/IUserEventDataBuilder.cs
public interface IUserEventDataBuilder : IScopedService
{
    Task<Dictionary<string, object>> BuildUserEventDataAsync(int userId, string eventType);
}

// Application/Common/Services/Notifications/UserEventDataBuilder.cs
public class UserEventDataBuilder : IUserEventDataBuilder
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserEventDataBuilder> _logger;

    public UserEventDataBuilder(IUnitOfWork unitOfWork, ILogger<UserEventDataBuilder> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Dictionary<string, object>> BuildUserEventDataAsync(int userId, string eventType)
    {
        var userRepo = _unitOfWork.GetRepository<User, int>();
        var user = await userRepo.GetByIdAsync(userId);
        
        if (user == null)
            throw new ArgumentException($"User with ID {userId} not found");

        return new Dictionary<string, object>
        {
            ["UserId"] = user.Id,
            ["UserName"] = $"{user.FirstName} {user.LastName}",
            ["UserEmail"] = user.Email,
            ["EventType"] = eventType,
            ["CreatedAt"] = user.CreatedAt,
            ["UpdatedAt"] = user.UpdatedAt ?? DateTime.UtcNow
        };
    }
}
```

#### **Paso 3: ¡YA ESTÁ! 🎉**

**✅ NO SE REQUIERE MODIFICAR:**
- `NotificationDispatcher` - Se resuelve automáticamente
- `UpdateEntityCommandHandler` - Funciona con cualquier entidad
- `CreateEntityCommandHandler` - Funciona con cualquier entidad
- Ningún código existente

**✅ AUTO-REGISTRO:**
- Como `UserChangeHandler` implementa `IEntityChangeHandler<User, int>` que hereda de `IScopedService`, se autoregistra automáticamente en el contenedor de DI.

**✅ FUNCIONAMIENTO INMEDIATO:**
- Al actualizar un `User`, el dispatcher resolverá automáticamente `UserChangeHandler`
- Al crear un `User`, el dispatcher resolverá automáticamente `UserChangeHandler`

### **🎯 PLANTILLA RÁPIDA PARA NUEVAS ENTIDADES**

```csharp
// 1. Handler
public class {Entity}ChangeHandler : IEntityChangeHandler<{Entity}, int>
{
    // Constructor con dependencias específicas
    // HandleChangeAsync() - lógica para cambios
    // HandleCreationAsync() - lógica para creación
}

// 2. EventDataBuilder (opcional, si requiere datos complejos)
public interface I{Entity}EventDataBuilder : IScopedService
{
    Task<Dictionary<string, object>> Build{Entity}EventDataAsync(int entityId, string eventType);
}

public class {Entity}EventDataBuilder : I{Entity}EventDataBuilder
{
    // Implementación de construcción de datos
}

// ✅ AUTO-REGISTRO: Todo funciona automáticamente
```

---

## 🏗️ **PATRONES DE DISEÑO APLICADOS**

### **1. Strategy Pattern**
- **Dónde:** `IEntityChangeHandler<T, TId>`
- **Beneficio:** Diferentes algoritmos de notificación por entidad
- **Extensibilidad:** Agregar nuevas estrategias sin modificar código existente

### **2. Factory Pattern**
- **Dónde:** `NotificationDispatcher`
- **Beneficio:** Creación automática de handlers apropiados
- **Flexibilidad:** Resolución dinámica via dependency injection

### **3. Builder Pattern**
- **Dónde:** `EventDataBuilder` services
- **Beneficio:** Construcción compleja de datos de eventos
- **Mantenibilidad:** Lógica centralizada de construcción

### **4. Service Layer Pattern**
- **Dónde:** `StudentDataService`
- **Beneficio:** Reutilización de lógica común
- **Separation of Concerns:** Separación clara de responsabilidades

### **5. Command Pattern**
- **Dónde:** `UpdateEntityCommandHandler`, `CreateEntityCommandHandler`
- **Beneficio:** Encapsulación de operaciones
- **Undo/Redo:** Posibilidad de implementar operaciones reversibles

---

## 🚨 **TROUBLESHOOTING**

### **⚠️ Problema: ObjectDisposedException**

**Síntoma:**
```
Cannot access a disposed object. Object name: 'IServiceProvider'.
```

**Causa:** 
El scope del request HTTP se dispone antes de que termine la tarea en background con `Task.Run()`.

**Solución:**
✅ **YA IMPLEMENTADA** - `NotificationDispatcher` usa `IServiceScopeFactory` en lugar de `IServiceProvider`:
```csharp
public class NotificationDispatcher : INotificationDispatcher
{
    private readonly IServiceScopeFactory _serviceScopeFactory; // ✅ CORRECTO

    public NotificationDispatcher(IServiceScopeFactory serviceScopeFactory, ILogger<NotificationDispatcher> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public async Task DispatchEntityChangeAsync<T, TId>(T oldEntity, T newEntity, CancellationToken ct = default)
    {
        // ✅ Scope independiente del request HTTP
        using var scope = _serviceScopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetService<IEntityChangeHandler<T, TId>>();
        // ...
    }
}
```

**Por qué funciona:**
- `IServiceScopeFactory` es singleton y no depende del scope del request
- Puede crear scopes independientes que funcionan en background tasks
- Resuelve completamente el problema de `ObjectDisposedException`

### **⚠️ Problema: Handler no se encuentra**

**Síntoma:**
```
No handler configured for entity type: User
```

**Causa:** 
Handler no está registrado o no implementa la interfaz correcta.

**Solución:**
1. Verificar que el handler implementa `IEntityChangeHandler<T, TId>`
2. Verificar que la interfaz hereda de `IScopedService`
3. Verificar que el proyecto está compilando correctamente

### **⚠️ Problema: Notificaciones duplicadas**

**Síntoma:**
Se envían múltiples emails para el mismo evento.

**Causa:**
Múltiples handlers o llamadas duplicadas.

**Solución:**
1. Verificar que solo hay un handler por tipo de entidad
2. Para InscriptionModality, asegurar que solo `InscriptionCreationService` maneja la creación
3. Revisar logs para identificar fuentes duplicadas

### **⚠️ Problema: Emails no se envían**

**Síntoma:**
Handler se ejecuta pero no llegan emails.

**Causa:**
Problema en `EmailNotificationQueueService` o configuración SMTP.

**Solución:**
1. Verificar logs de Hangfire
2. Verificar configuración SMTP
3. Verificar que `IEmailNotificationQueueService` está registrado correctamente

---

## 📊 **MÉTRICAS Y MONITOREO**

### **🔍 Logs Importantes**

```csharp
// NotificationDispatcher
"Dispatching change notification for {EntityType} ID: {Id}"
"No handler configured for entity type: {EntityType}"

// ProposalChangeHandler  
"Evento {EventName} encolado para propuesta ID: {ProposalId}, JobId: {JobId}"

// InscriptionCreationService
"Inscription creation notification enqueued - Inscription ID: {InscriptionId}, Students: {StudentCount}, JobId: {JobId}"
```

### **📈 KPIs de Performance**

- **Tiempo de response:** Las notificaciones no deben afectar el tiempo de response de APIs
- **Éxito de notificaciones:** % de notificaciones procesadas exitosamente
- **Latencia de emails:** Tiempo entre trigger y envío de email
- **Memory usage:** Verificar que no hay memory leaks en background tasks

---

## 🎯 **CASOS DE USO COMUNES ejemplos**

### **📝 Agregar nuevo tipo de notificación para entidad existente**

```csharp
// En ProposalChangeHandler, agregar nuevo caso
private string GetProposalEventName(StateStageEnum stateStage)
{
    return stateStage switch
    {
        StateStageEnum.PROP_RADICADA => "PROPOSAL_SUBMITTED",
        StateStageEnum.PROP_PERTINENTE => "PROPOSAL_APPROVED",
        StateStageEnum.PROP_NO_PERTINENTE => "PROPOSAL_REJECTED",
        StateStageEnum.PROP_EN_REVISION => "PROPOSAL_UNDER_REVIEW", // ✅ NUEVO
        _ => string.Empty
    };
}
```

### **🔧 Modificar datos de evento**

```csharp
// En ProposalEventDataBuilder, agregar nuevos campos
var eventData = new Dictionary<string, object>
{
    ["ProposalId"] = proposal.Id,
    ["ProposalTitle"] = proposal.Title ?? string.Empty,
    // ... campos existentes
    ["ReviewerName"] = reviewer?.Name ?? string.Empty, // ✅ NUEVO
    ["ReviewDate"] = DateTime.UtcNow, // ✅ NUEVO
};
```

### **⚡ Deshabilitar notificaciones temporalmente**

```csharp
// En cualquier handler, agregar feature flag
public async Task HandleChangeAsync(Proposal oldEntity, Proposal newEntity, CancellationToken cancellationToken = default)
{
    if (!_featureFlags.IsEnabled("ProposalNotifications")) return; // ✅ CIRCUIT BREAKER
    
    // Lógica normal...
}
```

---

## ✅ **CHECKLIST DE IMPLEMENTACIÓN**

### **Para nuevas entidades:**
- [ ] Crear `{Entity}ChangeHandler : IEntityChangeHandler<{Entity}, int>`
- [ ] Implementar `HandleChangeAsync()` con lógica específica
- [ ] Implementar `HandleCreationAsync()` con lógica específica
- [ ] Crear `I{Entity}EventDataBuilder` si requiere datos complejos
- [ ] Implementar `{Entity}EventDataBuilder` con construcción de datos
- [ ] Verificar logs y métricas - opcional
- [ ] Documentar casos especiales - opcional

### **Para modificaciones:**
- [ ] Identificar handler afectado
- [ ] Modificar lógica sin romper compatibilidad
- [ ] Verificar que no se afectan otras entidades

---

## 📝 **CAMPOS COMPLETADOS EN TEMPLATES DE EMAIL**

Durante la implementación se identificaron y completaron campos faltantes en los EventDataBuilders para soportar todos los placeholders de los templates de email:

### **📧 InscriptionEventDataBuilder - Campos Agregados:**
- ✅ `{StudentsCount}` - Alias de `StudentCount` para compatibilidad
- ✅ `{InscriptionDate}` - Fecha de creación formateada (dd/MM/yyyy)
- ✅ `{AcademicPeriod}` - Código del periodo académico
- ✅ `{ApprovalDate}` - Fecha de aprobación o última actualización
- ✅ `{ApprovalComments}` - Observaciones de la inscripción
- ✅ `{ReviewDate}` - Fecha de última actualización
- ✅ `{RejectionComments}` - Observaciones (reutiliza campo Observations)

### **📧 ProposalEventDataBuilder - Campos Agregados:**
- ✅ `{StudentsCount}` - Alias de `StudentCount` para compatibilidad
- ✅ `{GeneralObjective}` - Objetivo general de la propuesta
- ✅ `{SpecificObjectives}` - Lista de objetivos específicos (separados por ";")
- ✅ `{SubmissionDate}` - Fecha de radicación (CreatedAt formateada)
- ✅ `{ApprovalDate}` - Fecha de aprobación (UpdatedAt formateada)
- ✅ `{Observation}` - Observaciones del comité académico
- ✅ `{RejectionComments}` - Observaciones (reutiliza campo Observation)
- ✅ `{AcademicPeriod}` - Código del periodo académico

### **🎯 Compatibilidad:**
Los templates de email ahora tienen **100% de compatibilidad** con todos los placeholders definidos en los scripts SQL de configuración (`41 config EmailNotifications.sql` y `42 config EmailNotificacionProposal.sql`).

---

Este sistema de notificaciones proporciona:

**✅ Arquitectura Robusta:** Principios SOLID aplicados correctamente  
**✅ Extensibilidad:** Agregar nuevas entidades es trivial  
**✅ Mantenibilidad:** Responsabilidades claras y separadas  
**✅ Performance:** Notificaciones asíncronas que no bloquean APIs  
**✅ Reliability:** Manejo robusto de errores y contextos  
**✅ Testability:** Componentes pequeños y enfocados  

El sistema está **listo para producción** y **preparado para el futuro**. 🚀

---

**📅 Última actualización:** Agosto 4, 2025  
**👥 Mantenido por:** Equipo de Desarrollo PracticaCore  
**📧 Contacto:** Para preguntas sobre implementación o extensiones
