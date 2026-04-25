# Sistema de Notificaciones - Práctica Académica

**Modalidad:** PRACTICA_ACADEMICA  

---

## 📋 Índice

1. [Resumen Ejecutivo](#resumen-ejecutivo)
2. [Arquitectura del Sistema](#arquitectura-del-sistema)
3. [Modelo de Templates Genéricos](#modelo-de-templates-genéricos)
4. [Mapeo de Estados a Eventos](#mapeo-de-estados-a-eventos)
5. [Placeholders Dinámicos](#placeholders-dinámicos)
6. [Configuración de Destinatarios](#configuración-de-destinatarios)
7. [Scripts y Archivos Involucrados](#scripts-y-archivos-involucrados)
8. [Mantenimiento y Limpieza](#mantenimiento-y-limpieza)
9. [Troubleshooting](#troubleshooting)

---

## 🎯 Resumen Ejecutivo

### Objetivo
Sistema de notificaciones por correo electrónico para el proceso de Práctica Académica, usando un modelo híbrido de 5 templates genéricos que se adaptan a 11 estados diferentes mediante placeholders dinámicos.

### Modelo Implementado
- **5 Templates Genéricos** (en lugar de 11 específicos)
- **11 Estados Mapeados** a los 5 templates
- **Placeholders Dinámicos** que cambian según el contexto
- **Sintaxis de Placeholders:** `{Key}` (llaves simples)

### Ventajas
✅ Reducción de código repetitivo  
✅ Mantenimiento simplificado  
✅ Reutilización de templates  
✅ Fácil extensión a nuevas fases  

---

## 🏗️ Arquitectura del Sistema

### Componentes Principales

```
┌─────────────────────────────────────────────────────────────┐
│  AcademicPractice (Cambio de Estado)                        │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│  AcademicPracticeChangeHandler                              │
│  - Mapea Estado → Nombre de Evento Genérico                 │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│  AcademicPracticeEventDataBuilder                           │
│  - Construye Dictionary<string, object> con placeholders    │
│  - Obtiene datos de estudiantes, proyecto, fechas, etc.     │
│  - Genera contenido dinámico según estado/fase              │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│  EmailNotificationEventService                              │
│  - Busca template en BD por eventName                       │
│  - Reemplaza placeholders {Key} con valores reales          │
│  - Procesa destinatarios según reglas                       │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│  EmailService (Hangfire)                                    │
│  - Envía correos de forma asíncrona                         │
└─────────────────────────────────────────────────────────────┘
```

---

## 📧 Modelo de Templates Genéricos

### 5 Templates Implementados

| # | Nombre del Evento | Descripción | Estados Asociados |
|---|-------------------|-------------|-------------------|
| 1 | `PRACTICA_EN_REVISION` | Documenta fase en revisión por comité | 3 estados |
| 2 | `PRACTICA_OBSERVACIONES` | Notifica correcciones/ajustes requeridos | 3 estados |
| 3 | `PRACTICA_APROBADA` | Confirma aprobación de fase | 3 estados |
| 4 | `PRACTICA_APROBADO_FINAL` | Notifica aprobación final del proceso | 1 estado |
| 5 | `PRACTICA_NO_APROBADA` | Informa rechazo de fase | 3 estados |

**Total:** 5 templates → 11 estados cubiertos

---

## 🗺️ Mapeo de Estados a Eventos

### Fase: Inscripción

| Estado en BD | Código Enum | Template Usado |
|-------------|-------------|----------------|
| Inscripción en revisión | `PA_INSCRIPCION_EN_REVISION` | `PRACTICA_EN_REVISION` |
| Inscripción con observaciones | `PA_INSCRIPCION_OBSERVACIONES` | `PRACTICA_OBSERVACIONES` |
| Inscripción aprobada | `PA_INSCRIPCION_APROBADA` | `PRACTICA_APROBADA` |
| Inscripción rechazada | `PA_INSCRIPCION_RECHAZADA` | `PRACTICA_NO_APROBADA` |

### Fase: Desarrollo

| Estado en BD | Código Enum | Template Usado |
|-------------|-------------|----------------|
| Desarrollo en revisión | `PA_DESARROLLO_EN_REVISION` | `PRACTICA_EN_REVISION` |
| Desarrollo con observaciones | `PA_DESARROLLO_OBSERVACIONES` | `PRACTICA_OBSERVACIONES` |
| Desarrollo aprobado | `PA_DESARROLLO_APROBADA` | `PRACTICA_APROBADA` |
| Desarrollo no aprobado | `PA_DESARROLLO_NO_APROBADA` | `PRACTICA_NO_APROBADA` |

### Fase: Informe Final

| Estado en BD | Código Enum | Template Usado |
|-------------|-------------|----------------|
| Informe en revisión | `PA_INFORME_FINAL_EN_REVISION` | `PRACTICA_EN_REVISION` |
| Informe con observaciones | `PA_INFORME_FINAL_OBSERVACIONES` | `PRACTICA_OBSERVACIONES` |

### Estados Finales

| Estado en BD | Código Enum | Template Usado |
|-------------|-------------|----------------|
| Práctica aprobada (final) | `PA_APROBADO` | `PRACTICA_APROBADO_FINAL` |
| Práctica no aprobada (final) | `PA_NO_APROBADO` | `PRACTICA_NO_APROBADA` |

### Estados SIN Notificación

Los siguientes estados NO generan notificaciones (son transiciones automáticas):

- `PA_INSCRIPCION_PEND_DOC`
- `PA_DESARROLLO_PEND_DOC`
- `PA_INFORME_FINAL_PEND_DOC`

---

## 🔤 Placeholders Dinámicos

### Sintaxis
- **Formato:** `{PlaceholderName}` (llaves simples)
- **Ejemplo:** `{StudentNames}`, `{ProjectTitle}`, `{Phase}`

### Placeholders Globales (Disponibles en todos los templates)

| Placeholder | Descripción | Ejemplo |
|------------|-------------|---------|
| `{StudentNames}` | Nombre(s) del/los estudiante(s) | "Juan Pérez, María García" |
| `{StudentEmails}` | Email(s) del/los estudiante(s) | "juan@ejemplo.com, maria@ejemplo.com" |
| `{ProjectTitle}` | Título del proyecto/práctica | "Desarrollo de Sistema Web" |
| `{SubmissionDate}` | Fecha de radicación | "23/10/2025 14:30" |
| `{ApprovalDate}` | Fecha de aprobación | "01/11/2025" |
| `{Observations}` | Observaciones generales | Texto de observaciones |

### Placeholders Dinámicos por Fase

| Placeholder | Descripción | Varía según |
|------------|-------------|-------------|
| `{Phase}` | Nombre de la fase | Estado actual |
| `{PhaseDescription}` | Descripción contextual | Estado actual |
| `{NextSteps}` | Pasos siguientes (HTML) | Estado actual |
| `{ObservationAction}` | Acción requerida | Estado actual |
| `{ImportantNote}` | Nota importante | Estado actual |
| `{CongratulationMessage}` | Mensaje de felicitación | Estado actual |
| `{ReasonDescription}` | Razón de rechazo | Estado actual |
| `{PossibleCausesTitle}` | Título de causas | Estado actual |
| `{PossibleCauses}` | Lista de causas (HTML) | Estado actual |
| `{AvailableOptions}` | Opciones disponibles (HTML) | Estado actual |
| `{ImportantMessage}` | Mensaje importante | Estado actual |
| `{AdministrativeNextSteps}` | Pasos administrativos | Estado actual |
| `{SpecificObservations}` | Observaciones específicas BD | `academicPractice.Observations` |
| `{EvaluatorObservations}` | Observaciones evaluador BD | `academicPractice.EvaluatorObservations` |

### Ejemplo de Variación de `{Phase}` y `{PhaseDescription}`

| Estado | `{Phase}` | `{PhaseDescription}` |
|--------|-----------|---------------------|
| `PA_INSCRIPCION_EN_REVISION` | "Inscripción" | "su solicitud de inscripción para la modalidad de Práctica Académica" |
| `PA_DESARROLLO_EN_REVISION` | "Desarrollo" | "su fase de desarrollo de práctica académica" |
| `PA_INFORME_FINAL_EN_REVISION` | "Informe Final" | "su informe final de práctica académica" |

### Ejemplo de Variación de `{NextSteps}`

**Estado: PA_INSCRIPCION_EN_REVISION**
```html
<li>El comité revisará su documentación y le informará sobre la decisión en un plazo máximo de 5 días hábiles</li>
<li>Revisar el estado de su solicitud en el sistema</li>
<li>Mantener disponibilidad para posibles consultas</li>
```

**Estado: PA_DESARROLLO_APROBADA**
```html
<li>Ya puede proceder a preparar su informe final de práctica</li>
<li>Solicite la certificación de la práctica a la institución</li>
<li>Prepare toda la documentación requerida para la evaluación final</li>
```

---

## 👥 Configuración de Destinatarios

### Regla General
**Destinatario Principal (TO):** Estudiante(s) de la práctica académica  
**Tipo de Regla:** `EVENT_PARTICIPANT` con valor `STUDENT`

### Configuración en BD

```sql
INSERT INTO "EmailRecipientRule" (
    emailnotificationconfigid, 
    recipienttype, 
    ruletype, 
    rulevalue, 
    priority, 
    idusercreatedat, 
    createdat, 
    operationregister, 
    statusregister
)
SELECT 
    enc.id, 
    'TO', 
    'EVENT_PARTICIPANT', 
    'STUDENT', 
    1, 
    1, 
    NOW(), 
    'SISTEMA_PA_HIBRIDO', 
    true
FROM "EmailNotificationConfig" enc
WHERE enc.operationregister = 'SISTEMA_PA_HIBRIDO'
  AND enc.eventname IN (
      'PRACTICA_EN_REVISION',
      'PRACTICA_OBSERVACIONES',
      'PRACTICA_APROBADA',
      'PRACTICA_APROBADO_FINAL',
      'PRACTICA_NO_APROBADA'
  );
```

### Futura Extensión
Para agregar más destinatarios (directores, comité, etc.), agregar nuevas reglas con prioridades diferentes:

```sql
-- Ejemplo: Agregar director en copia (CC)
INSERT INTO "EmailRecipientRule" (...)
VALUES (..., 'CC', 'BY_ROLE', 'DIRECTOR', 2, ...);
```

---

## 📁 Scripts y Archivos Involucrados

### Backend (C#)

| Archivo | Ruta | Responsabilidad |
|---------|------|----------------|
| `AcademicPracticeChangeHandler.cs` | `Application/EventHandlers/` | Mapea estados → eventos |
| `AcademicPracticeEventDataBuilder.cs` | `Application/Common/Services/` | Construye datos dinámicos |
| `EmailNotificationEventService.cs` | `Application/Common/Services/` | Procesa templates y envía |
| `StateStageCodeEnum.cs` | `Domain/Enums/` | Enum de estados |

### Base de Datos

| Archivo | Ruta | Descripción |
|---------|------|-------------|
| `45_CONFIG_NOTIFICACIONES_PA_MODELO_HIBRIDO.sql` | `Tables v2/` | Script de configuración de templates y destinatarios |
| `EmailNotificationConfig` | Tabla BD | Almacena templates de emails |
| `EmailRecipientRule` | Tabla BD | Define reglas de destinatarios |

### Documentación

| Archivo | Ruta | Contenido |
|---------|------|-----------|
| `DOCUMENTACION_NOTIFICACIONES_PRACTICA_ACADEMICA.md` | Raíz del proyecto | Este documento |

---

## 🔧 Mantenimiento y Limpieza

### Eliminar Configuraciones de PA (Recomendado)

```sql
-- Elimina SOLO las configuraciones de Práctica Académica
DELETE FROM "EmailRecipientRule" 
WHERE emailnotificationconfigid IN (
    SELECT id FROM "EmailNotificationConfig" 
    WHERE operationregister = 'SISTEMA_PA_HIBRIDO'
);

DELETE FROM "EmailNotificationConfig" 
WHERE operationregister = 'SISTEMA_PA_HIBRIDO';
```

### Deshabilitar sin Eliminar

```sql
-- Solo deshabilita (datos se conservan)
UPDATE "EmailNotificationConfig" 
SET isactive = false 
WHERE operationregister = 'SISTEMA_PA_HIBRIDO';
```

### Verificar Configuraciones Activas

```sql
-- Ver templates y destinatarios configurados
SELECT 
    enc.EventName,
    enc.SubjectTemplate,
    enc.IsActive,
    COUNT(err.Id) as TotalRules,
    STRING_AGG(err.RuleType || ':' || err.RuleValue, ', ') as Rules
FROM "EmailNotificationConfig" enc
LEFT JOIN "EmailRecipientRule" err ON enc.Id = err.EmailNotificationConfigId
WHERE enc.OperationRegister = 'SISTEMA_PA_HIBRIDO'
  AND enc.StatusRegister = true
GROUP BY enc.EventName, enc.SubjectTemplate, enc.IsActive
ORDER BY enc.EventName;
```

---

## 🔍 Troubleshooting

### Problema: Placeholders aparecen como texto literal en el email

**Causa:** Mismatch entre sintaxis en template y servicio de procesamiento.

**Solución:**
- ✅ Templates usan: `{Key}` (llave simple)
- ✅ Servicio busca: `{Key}` (llave simple)
- ❌ NO usar: `{{Key}}` (doble llave)

**Verificación:**
```csharp
// En EmailNotificationEventService.cs
var placeholder = $"{{{kvp.Key}}}";  // Genera {Key}
```

### Problema: No se envían correos

**Checklist:**
1. ✅ ¿El estado tiene evento mapeado en `AcademicPracticeChangeHandler`?
2. ✅ ¿El template existe en BD con `isactive = true`?
3. ✅ ¿Hay reglas de destinatarios configuradas?
4. ✅ ¿Hangfire está procesando trabajos?
5. ✅ ¿La configuración SMTP es correcta?

**Verificar logs:**
```csharp
_logger.LogInformation("Processing notification for event: {EventName}", eventName);
```

### Problema: Contenido dinámico no cambia según la fase

**Causa:** `AcademicPracticeEventDataBuilder.GetPhaseInfo()` no tiene caso para ese estado.

**Solución:** Agregar el estado al switch statement:
```csharp
return code switch
{
    StateStageCodeEnum.NUEVO_ESTADO => new PhaseInfo
    {
        Phase = "Nombre Fase",
        PhaseDescription = "Descripción",
        NextSteps = "<li>Paso 1</li>",
        // ... más placeholders
    },
    // ... otros casos
};
```

### Problema: Emails duplicados

**Causa:** Múltiples reglas de destinatarios para el mismo tipo.

**Solución:** Verificar y limpiar duplicados:
```sql
SELECT emailnotificationconfigid, recipienttype, ruletype, rulevalue, COUNT(*)
FROM "EmailRecipientRule"
WHERE statusregister = true
GROUP BY emailnotificationconfigid, recipienttype, ruletype, rulevalue
HAVING COUNT(*) > 1;
```

---

## 📊 Estadísticas del Sistema

- **Templates Genéricos:** 5
- **Estados Cubiertos:** 11
- **Placeholders Globales:** 6
- **Placeholders Dinámicos:** 14
- **Fases del Proceso:** 3 (Inscripción, Desarrollo, Informe Final)
- **Reducción de Código:** ~60% vs modelo específico

---

## 🚀 Extensiones Futuras

### Agregar Nueva Fase

1. Crear estados en `StateStageCodeEnum`
2. Agregar casos en `AcademicPracticeChangeHandler.GetAcademicPracticeEventNameAsync()`
3. Agregar info en `AcademicPracticeEventDataBuilder.GetPhaseInfo()`
4. Los templates existentes se reutilizan automáticamente ✅

### Agregar Nuevo Tipo de Notificación

Si necesitas un template completamente nuevo (ej: "PRACTICA_CANCELADA"):

1. Agregar evento en `AcademicPracticeChangeHandler`
2. Crear template en BD con INSERT INTO `EmailNotificationConfig`
3. Configurar destinatarios en `EmailRecipientRule`
4. Agregar info dinámica en `AcademicPracticeEventDataBuilder`

### Personalizar Destinatarios

Ejemplo: Director en copia para aprobaciones:
```sql
INSERT INTO "EmailRecipientRule" (
    emailnotificationconfigid, 
    recipienttype, 
    ruletype, 
    rulevalue, 
    priority
)
SELECT 
    id, 
    'CC', 
    'BY_ROLE', 
    'DIRECTOR', 
    2
FROM "EmailNotificationConfig"
WHERE eventname = 'PRACTICA_APROBADA';
```

---

