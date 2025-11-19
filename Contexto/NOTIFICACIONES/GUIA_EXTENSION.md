# 🚀 **GUÍA RÁPIDA - EXTENSIÓN DEL SISTEMA DE NOTIFICACIONES**

## 🎯 **IMPLEMENTAR NUEVA ENTIDAD EN 3 PASOS**

### **1️⃣ Crear Handler**

#### ⚠️ **IMPORTANTE: Copia Profunda en Actualizaciones**

Cuando implementes un handler de actualización, **crea una copia profunda del objeto original** antes de modificarlo para que la comparación sea válida:

```csharp
// ✅ CORRECTO - En tu CommandHandler antes de mapear:
var originalEntityJson = JsonSerializer.Serialize(existingEntity);
var originalEntity = JsonSerializer.Deserialize<TipoEntidad>(originalEntityJson)!;

// Luego mapea los cambios:
_mapper.Map(dto, existingEntity);
```

#### 📄 **Template del Handler**

```csharp
// Application/Common/Services/Notifications/{Entity}ChangeHandler.cs
public class {Entity}ChangeHandler : IEntityChangeHandler<{Entity}, int>
{
    private readonly IEmailNotificationQueueService _queueService;
    private readonly ILogger<{Entity}ChangeHandler> _logger;

    public {Entity}ChangeHandler(
        IEmailNotificationQueueService queueService,
        ILogger<{Entity}ChangeHandler> logger)
    {
        _queueService = queueService;
        _logger = logger;
    }

    public async Task HandleChangeAsync({Entity} oldEntity, {Entity} newEntity, CancellationToken cancellationToken = default)
    {
        // 🎯 LÓGICA AQUÍ - Verificar qué cambió y decidir si notificar
        if (oldEntity.PropiedadImportante != newEntity.PropiedadImportante)
        {
            var eventData = new Dictionary<string, object>
            {
                ["{Entity}Id"] = newEntity.Id,
                ["OldValue"] = oldEntity.PropiedadImportante,
                ["NewValue"] = newEntity.PropiedadImportante,
                ["EventType"] = "ENTITY_PROPERTY_CHANGED"
            };
            
            _queueService.EnqueueEventNotification("ENTITY_PROPERTY_CHANGED", eventData);
            _logger.LogInformation("{Entity} property change notification enqueued for ID: {EntityId}", newEntity.Id);
        }
    }

    public async Task HandleCreationAsync({Entity} entity, CancellationToken cancellationToken = default)
    {
        // 🎯 LÓGICA AQUÍ - Notificar creación si es necesario
        var eventData = new Dictionary<string, object>
        {
            ["{Entity}Id"] = entity.Id,
            ["EventType"] = "ENTITY_CREATED"
        };
        
        _queueService.EnqueueEventNotification("ENTITY_CREATED", eventData);
        _logger.LogInformation("{Entity} creation notification enqueued for ID: {EntityId}", entity.Id);
    }
}
```

### **2️⃣ Crear EventDataBuilder (Solo si necesitas datos complejos)**

```csharp
// Domain/Interfaces/Notifications/I{Entity}EventDataBuilder.cs
public interface I{Entity}EventDataBuilder : IScopedService
{
    Task<Dictionary<string, object>> Build{Entity}EventDataAsync(int entityId, string eventType);
}

// Application/Common/Services/Notifications/{Entity}EventDataBuilder.cs
public class {Entity}EventDataBuilder : I{Entity}EventDataBuilder
{
    private readonly IUnitOfWork _unitOfWork;

    public {Entity}EventDataBuilder(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Dictionary<string, object>> Build{Entity}EventDataAsync(int entityId, string eventType)
    {
        var repo = _unitOfWork.GetRepository<{Entity}, int>();
        var entity = await repo.GetByIdAsync(entityId);
        
        return new Dictionary<string, object>
        {
            ["{Entity}Id"] = entity.Id,
            ["{Entity}Name"] = entity.Name,
            ["EventType"] = eventType,
            ["CreatedAt"] = entity.CreatedAt
            // Agregar más campos según necesites
        };
    }
}
```

### **3️⃣ ¡Listo! ✅**

- **✅ Auto-registro:** Se registra automáticamente en DI
- **✅ Auto-resolución:** El dispatcher lo encuentra automáticamente  
- **✅ Zero config:** No modificar código existente

---

## � **EJEMPLOS PRÁCTICOS**

### **🎯 EJEMPLO 1: Notificación Simple (User)**

```csharp
public class UserChangeHandler : IEntityChangeHandler<User, int>
{
    private readonly IEmailNotificationQueueService _queueService;
    private readonly ILogger<UserChangeHandler> _logger;

    public async Task HandleChangeAsync(User oldEntity, User newEntity, CancellationToken cancellationToken = default)
    {
        // 📧 Email cambió
        if (oldEntity.Email != newEntity.Email)
        {
            var eventData = new Dictionary<string, object>
            {
                ["UserId"] = newEntity.Id,
                ["UserName"] = $"{newEntity.FirstName} {newEntity.LastName}",
                ["OldEmail"] = oldEntity.Email,
                ["NewEmail"] = newEntity.Email,
                ["EventType"] = "USER_EMAIL_CHANGED"
            };
            
            _queueService.EnqueueEventNotification("USER_EMAIL_CHANGED", eventData);
        }

        // ✅ Estado cambió  
        if (oldEntity.IsActive != newEntity.IsActive)
        {
            var eventType = newEntity.IsActive ? "USER_ACTIVATED" : "USER_DEACTIVATED";
            var eventData = new Dictionary<string, object>
            {
                ["UserId"] = newEntity.Id,
                ["UserName"] = $"{newEntity.FirstName} {newEntity.LastName}",
                ["UserEmail"] = newEntity.Email,
                ["EventType"] = eventType
            };
            
            _queueService.EnqueueEventNotification(eventType, eventData);
        }
    }

    public async Task HandleCreationAsync(User entity, CancellationToken cancellationToken = default)
    {
        var eventData = new Dictionary<string, object>
        {
            ["UserId"] = entity.Id,
            ["UserName"] = $"{entity.FirstName} {entity.LastName}",
            ["UserEmail"] = entity.Email,
            ["EventType"] = "USER_CREATED"
        };
        
        _queueService.EnqueueEventNotification("USER_CREATED", eventData);
        _logger.LogInformation("User creation notification enqueued for ID: {UserId}", entity.Id);
    }
}
```

### **🎯 EJEMPLO 2: Con EventDataBuilder (Document)**

```csharp
public class DocumentChangeHandler : IEntityChangeHandler<Document, int>
{
    private readonly IEmailNotificationQueueService _queueService;
    private readonly IDocumentEventDataBuilder _eventDataBuilder;

    public async Task HandleChangeAsync(Document oldEntity, Document newEntity, CancellationToken cancellationToken = default)
    {
        // 📋 Estado de aprobación cambió
        if (oldEntity.IsApproved != newEntity.IsApproved)
        {
            var eventType = newEntity.IsApproved ? "DOCUMENT_APPROVED" : "DOCUMENT_REJECTED";
            var eventData = await _eventDataBuilder.BuildDocumentEventDataAsync(newEntity.Id, eventType);
            _queueService.EnqueueEventNotification(eventType, eventData);
        }
    }

    public async Task HandleCreationAsync(Document entity, CancellationToken cancellationToken = default)
    {
        var eventData = await _eventDataBuilder.BuildDocumentEventDataAsync(entity.Id, "DOCUMENT_UPLOADED");
        _queueService.EnqueueEventNotification("DOCUMENT_UPLOADED", eventData);
    }
}

// EventDataBuilder correspondiente
public class DocumentEventDataBuilder : IDocumentEventDataBuilder
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStudentDataService _studentDataService;

    public async Task<Dictionary<string, object>> BuildDocumentEventDataAsync(int documentId, string eventType)
    {
        var docRepo = _unitOfWork.GetRepository<Document, int>();
        var document = await docRepo.GetByIdAsync(documentId);
        
        // 👥 Obtener estudiantes relacionados
        var (studentNames, studentEmails, studentCount) = 
            await _studentDataService.GetStudentDataByProposalAsync(document.ProposalId);

        return new Dictionary<string, object>
        {
            ["DocumentId"] = document.Id,
            ["DocumentName"] = document.Name,
            ["DocumentType"] = document.DocumentType?.Name ?? string.Empty,
            ["ProposalId"] = document.ProposalId,
            ["StudentNames"] = studentNames,
            ["StudentEmails"] = studentEmails,
            ["StudentCount"] = studentCount,
            ["EventType"] = eventType,
            ["UploadedAt"] = document.CreatedAt,
            ["ApprovedAt"] = document.UpdatedAt
        };
    }
}
```

---

## 🛠️ **PATRONES COMUNES**

### **🔄 Detectar Cambio de Estado**
```csharp
if (oldEntity.Status != newEntity.Status)
{
    var eventType = GetEventTypeForStatus(newEntity.Status);
    var eventData = BuildEventData(newEntity, eventType);
    _queueService.EnqueueEventNotification(eventType, eventData);
}
```

### **📝 Detectar Cambio de Campo Específico**
```csharp
if (oldEntity.ImportantField != newEntity.ImportantField)
{
    var eventData = new Dictionary<string, object>
    {
        ["EntityId"] = newEntity.Id,
        ["OldValue"] = oldEntity.ImportantField,
        ["NewValue"] = newEntity.ImportantField,
        ["EventType"] = "FIELD_CHANGED"
    };
    _queueService.EnqueueEventNotification("FIELD_CHANGED", eventData);
}
```

### **👥 Incluir Datos de Estudiantes**
```csharp
public class MyHandler : IEntityChangeHandler<MyEntity, int>
{
    private readonly IStudentDataService _studentDataService;

    public async Task HandleChangeAsync(MyEntity oldEntity, MyEntity newEntity, CancellationToken ct = default)
    {
        var (studentNames, studentEmails, studentCount) = 
            await _studentDataService.GetStudentDataByProposalAsync(newEntity.ProposalId);

        var eventData = new Dictionary<string, object>
        {
            // ... otros datos
            ["StudentNames"] = studentNames,
            ["StudentEmails"] = studentEmails,
            ["StudentCount"] = studentCount,
        };
    }
}
```

---

## 🚨 **QUÉ NO HACER**

### **❌ NO modificar NotificationDispatcher**
```csharp
// ❌ NUNCA hagas esto
public class NotificationDispatcher : INotificationDispatcher
{
    public async Task DispatchEntityChangeAsync<T, TId>(T oldEntity, T newEntity, CancellationToken ct = default)
    {
        // ❌ NO agregar lógica específica aquí
        if (typeof(T) == typeof(User))
        {
            // Lógica específica de User
        }
    }
}
```

### **❌ NO llamar handlers directamente**
```csharp
// ❌ NUNCA hagas esto en CommandHandlers
public class UpdateEntityCommandHandler<T, TId>
{
    public async Task<EntityResponseDto<T, TId>> Handle(UpdateEntityCommand<T, TId> request, CancellationToken ct)
    {
        // Update entity...
        
        // ❌ NO hacer esto
        var userHandler = _serviceProvider.GetService<UserChangeHandler>();
        await userHandler.HandleChangeAsync(oldEntity, newEntity, ct);
        
        // ✅ HACER esto
        await _notificationDispatcher.DispatchEntityChangeAsync(oldEntity, newEntity, ct);
    }
}
```

### **❌ NO registrar manualmente en DI**
```csharp
// ❌ NO hacer esto en Program.cs
services.AddScoped<IEntityChangeHandler<User, int>, UserChangeHandler>();

// ✅ AUTO-REGISTRO: Se registra automáticamente por implementar IScopedService
```

---

## 🔧 **DEBUGGING Y TROUBLESHOOTING**

### **🔍 Verificar que el Handler se Ejecuta**
```csharp
public async Task HandleChangeAsync(User oldEntity, User newEntity, CancellationToken ct = default)
{
    _logger.LogInformation("UserChangeHandler.HandleChangeAsync called for User ID: {UserId}", newEntity.Id);
    
    // Tu lógica...
    
    _logger.LogInformation("Notification enqueued for User ID: {UserId}, JobId: {JobId}", newEntity.Id, jobId);
}
```

### **🔍 Verificar Auto-registro**
```csharp
// En cualquier controlador, inyectar para verificar
public class TestController : ControllerBase
{
    public TestController(IServiceProvider serviceProvider)
    {
        var handler = serviceProvider.GetService<IEntityChangeHandler<User, int>>();
        Console.WriteLine($"Handler registered: {handler != null}");
    }
}
```

### **� Logs Importantes**
- `NotificationDispatcher`: "Dispatching change notification for {EntityType}"
- `EmailNotificationQueueService`: Job enqueueing logs
- `Hangfire Dashboard`: Job execution status

---

## ✅ **CHECKLIST FINAL**

**Para agregar nueva entidad:**
- [ ] ✅ Crear `{Entity}ChangeHandler : IEntityChangeHandler<{Entity}, int>`
- [ ] ✅ Implementar `HandleChangeAsync()` y `HandleCreationAsync()`  
- [ ] ✅ (Opcional) Crear `I{Entity}EventDataBuilder` y su implementación
- [ ] ✅ Compilar proyecto
- [ ] ✅ Probar actualizando/creando la entidad
- [ ] ✅ Verificar logs que el handler se ejecuta
- [ ] ✅ Verificar en Hangfire que se encolan jobs
- [ ] ✅ Verificar que llegan emails

**¡Eso es todo!** 🎉

---

**💡 TIP:** Agrega breakpoints en tu handler y actualiza una entidad desde la API. El debugger debería parar automáticamente.

---

## � **CONFIGURACIÓN DE EMAIL TEMPLATES Y DESTINATARIOS**

### **🔧 Valores de Reglas de Destinatarios**

**Para `RuleType = "BY_ENTITY_RELATION"`:**
- `ASSIGNED_TEACHER` - Email del docente asignado
- `STUDENT_ASSIGNED` - Emails de estudiantes  
- `EVALUATOR_ASSIGNED` - Emails de evaluadores (TypeTeachingAssignment = EVALUADOR)
- `JURY_EVALUATOR_ASSIGNED` - Emails de jurados evaluadores
- `DIRECTOR_ASSIGNED` - Emails de directores
- `CO_DIRECTOR_ASSIGNED` - Emails de co-directores
- `ASESOR_ASSIGNED` - Emails de asesores
- `PROPOSAL_DIRECTOR` - Email del director de propuesta
- `FACULTY_COORDINATOR` - Email del coordinador de facultad

**Para `RuleType = "EVENT_PARTICIPANT"`:**
- `STUDENT` - Estudiantes del evento
- `DIRECTOR` - Director del evento  
- `COORDINATOR` - Coordinador del evento

**Para `RuleType = "BY_ROLE"`:**
- Cualquier nombre de rol existente en la tabla `Role`

**Para `RuleType = "FIXED_EMAIL"`:**
- Email directo (ej: `admin@universidad.edu`)

### **📋 Placeholders Comunes**

**Para eventos de TeachingAssignment:**
- `{TeacherName}`, `{TeacherEmail}`, `{AssignmentType}`, `{ProjectTitle}`
- `{ModalityName}`, `{CurrentStage}`, `{AcademicPeriod}`, `{AssignmentDate}`
- `{StudentNames}`, `{StudentEmails}`, `{StudentsCount}`

**Para eventos de Anteproyecto/Proyecto:**
- `{ProjectTitle}`, `{SubmissionDate}`, `{EvaluationResult}`, `{Observations}`
- `{StudentNames}`, `{StudentEmails}`, `{StudentsCount}`
- `{InscriptionModalityId}`, `{EvaluatorName}`, `{EvaluatorEmail}`

**Para eventos generales:**
- `{UserId}`, `{UserName}`, `{UserEmail}`, `{EventType}`
- `{ProposalId}`, `{ProposalTitle}`, `{InscriptionId}`

---

## 📄 **CASO ESPECIAL: NOTIFICACIONES AUTOMÁTICAS DE DOCUMENTOS**

### **🎯 Documentos de Anteproyecto y Proyecto Final**

**⚠️ ACTUALIZACIÓN:** El sistema ahora usa handlers específicos para `PreliminaryProject` y `ProjectFinal` que se disparan por cambios de estado desde el frontend, no por subida de documentos.

#### **📋 Eventos Configurados**

##### `ANTEPROYECTO_SUBMITTED`
- **Trigger**: Cambio de estado a `AP_RADICADO_PEND_ASIG_EVAL` 
- **Destinatarios**: Comité (TO), con copia a secretaría

##### `ANTEPROYECTO_EVALUATION_RESULT`
- **Trigger**: Cambio de estado a `AP_APROBADO` o `AP_CON_OBSERVACIONES`
- **Destinatarios**: Estudiante (TO)

##### `PROYECTO_FINAL_SUBMITTED`  
- **Trigger**: Cambio de estado a `PFINF_RADICADO_EN_EVALUACION`
- **Destinatarios**: Evaluadores/Jurados (TO)

##### `PROYECTO_FINAL_EVALUATION_RESULT`
- **Trigger**: Cambio de estado a `PFINF_INFORME_APROBADO` o `PFINF_INFORME_CON_OBSERVACIONES`
- **Destinatarios**: Estudiante (TO)

#### **🔧 Handlers Implementados**

```csharp
// Ya implementados y listos para usar:
// - PreliminaryProjectChangeHandler
// - ProjectFinalChangeHandler
// - PreliminaryProjectEventDataBuilder  
// - ProjectFinalEventDataBuilder
```

#### **⚙️ Configuración del Mapeo**

Para configurar los IDs reales de StateStage, ejecutar el script:
```sql
-- Tables v2/50_GET_STATESTAGE_IDS_FOR_HANDLERS.sql
-- Este script genera el mapeo correcto de IDs a eventos
```

---

**💡 ¡Los handlers están listos para funcionar una vez configurado el mapeo de IDs!**
