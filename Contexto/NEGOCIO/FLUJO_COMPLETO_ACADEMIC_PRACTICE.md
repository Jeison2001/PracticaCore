# FLUJO COMPLETO DE PRÁCTICA ACADÉMICA

### 📋 RESUMEN

**Base de Datos:**
- StageModality: 1-7 (PG: 1-4, PA: 5-7)
- DocumentType: 21 documentos configurados
- Foreign keys actualizadas

**Backend API:**
- `/api/DocumentType?filters[idStageModality@eq]=5` → Fase 0 (4 documentos)
- `/api/DocumentType?filters[idStageModality@eq]=6` → Fase 1 (1 documento visible)
- `/api/DocumentType?filters[idStageModality@eq]=7` → Fase 2 (1 documento visible)
- `/api/AcademicPractice/ByUser/1` → Datos de usuario

**Frontend:**
- Enum StageModalityEnum con valores (5,6,7)
- Dropdowns de documentos
- Subida de archivos operativa
- Contadores de progreso
- Validación de formularios

---

## 🚀 GUÍA PASO A PASO

### **PASO 1: PREPARACIÓN DEL ENTORNO**
```bash
# Terminal 1: Backend API
cd "C:\Users\LENOVO\source\repos\PracticaCore\Api"
dotnet run

# Terminal 2: Frontend Angular  
cd "C:\Users\LENOVO\source\repos\pegi_web"
ng serve
```

**URLs de acceso:**
- Backend: http://localhost:5191
- Frontend: http://localhost:4200
- Database: PostgreSQL en Neon (ep-wandering-darkness-aeo3lcbl-pooler.c-2.us-east-2.aws.neon.tech)

### **PASO 2: AUTENTICACIÓN**
1. Navegar a: http://localhost:4200
2. Hacer clic en "Iniciar sesión con Google"
3. Seleccionar cuenta: `jeisondfuentes@unicesar.edu.co`
4. Completar autenticación con 2FA
5. Verificar llegada al dashboard

### **PASO 3: FASE 0 - INSCRIPCIÓN Y APROBACIÓN INICIAL**

**URL:** http://localhost:4200/practices/phase-0/register-phase-0

**Acceso:** Dashboard → Prácticas Académicas → Fase 0 → "Registrar información"

**Datos de prueba:**
```
Título: Desarrollo de Sistema Web para Gestión de Inventarios - TechnoSoft Inc
Empresa: TechnoSoft Inc  
Contacto: Dr. María García - maria.garcia@technosoft.com - +57 300 123 4567
```

**Documentos disponibles (4):**
- Carta de Aceptación de la Empresa
- Formato ARL Diligenciado
- Solicitud de Inicio de Práctica
- Plan de Trabajo de Práctica Académica

**Verificaciones:**
- Progreso muestra "0/4" inicialmente
- Dropdown lista los 4 documentos correctos

### **PASO 4: FASE 1 - DESARROLLO DE LA PRÁCTICA**

**URL:** http://localhost:4200/practices/phase-1/register-phase-1

**Acceso:** Dashboard → Prácticas Académicas → Fase 1 → "Registrar información"

**Documentos disponibles (1 visible):**
- Informe de Seguimiento de Práctica

**Verificaciones:**
- Progreso muestra "0/1"
- Solo documentos de entregables (idDocumentClass=2) visibles

### **PASO 5: FASE 2 - FORMALIZACIÓN Y EVALUACIÓN**

**URL:** http://localhost:4200/practices/phase-2/register-phase-2

**Acceso:** Dashboard → Prácticas Académicas → Fase 2 → "Registrar información"

**Documentos disponibles (1 visible):**
- Informe Final de Práctica Académica

**Verificaciones:**
- Progreso muestra "0/1"
- Formulario listo para subida de documentos

---

## PETICIONES DE RED

### **APIs:**

**DocumentType por Fase:**
```http
GET /api/DocumentType?filters[idDocumentClass@eq]=2&filters[idStageModality@eq]=5&filters[statusRegister@eq]=true&pageNumber=1&pageSize=100
→ Status: 200 OK (Fase 0 - 4 documentos)

GET /api/DocumentType?filters[idDocumentClass@eq]=2&filters[idStageModality@eq]=6&filters[statusRegister@eq]=true&pageNumber=1&pageSize=100
→ Status: 200 OK (Fase 1 - 1 documento)

GET /api/DocumentType?filters[idDocumentClass@eq]=2&filters[idStageModality@eq]=7&filters[statusRegister@eq]=true&pageNumber=1&pageSize=100
→ Status: 200 OK (Fase 2 - 1 documento)
```

**Academic Practice:**
```http
GET /api/AcademicPractice/ByUser/1
→ Status: 200 OK
```

**Autenticación:**
```http
POST /api/Auth/google
→ Status: 200 OK
```

---

## 🗄️ ESTRUCTURA DE BASE DE DATOS

### **StageModality (IDs consecutivos 1-7):**
```sql
1 | PG_FASE_PROPUESTA      | Proyecto de Grado
2 | PG_FASE_ANTEPROYECTO   | Proyecto de Grado
3 | PG_FASE_PROYECTO_INFORME | Proyecto de Grado
4 | PG_FASE_SUSTENTACION   | Proyecto de Grado
5 | PA_FASE_INSCRIPCION    | Práctica Académica
6 | PA_FASE_DESARROLLO     | Práctica Académica
7 | PA_FASE_EVALUACION     | Práctica Académica
```

### **DocumentType para Práctica Académica:**

**Fase 0 (idStageModality=5) - 6 documentos:**
- Carta de Aceptación de la Empresa (Entregable)
- Formato ARL Diligenciado (Entregable)
- Solicitud de Inicio de Práctica (Entregable)  
- Plan de Trabajo de Práctica Académica (Entregable)
- Evaluación de Documentos Iniciales (Evaluación)
- Evaluación del Plan de Trabajo (Evaluación)

**Fase 1 (idStageModality=6) - 2 documentos:**
- Informe de Seguimiento de Práctica (Entregable)
- Evaluación de Seguimiento (Evaluación)

**Fase 2 (idStageModality=7) - 3 documentos:**
- Informe Final de Práctica Académica (Entregable)
- Evaluación del Informe Final (Evaluación)
- Evaluación de Socialización (Evaluación)

**Nota:** Solo los "Entregables" (idDocumentClass=2) son visibles en la interfaz de usuario.

---

## 🔧 CONFIGURACIÓN DEL FRONTEND

### **StageModalityEnum:**
```typescript
export enum StageModalityEnum {
  // Proyecto de Grado
  PG_FASE_PROPUESTA = 1,
  PG_FASE_ANTEPROYECTO = 2,
  PG_FASE_PROYECTO_INFORME = 3,
  PG_FASE_SUSTENTACION = 4,

  // Práctica Académica
  PA_FASE_INICIAL = 5,      // PA_FASE_INSCRIPCION
  PA_FASE_DESARROLLO = 6,   // PA_FASE_DESARROLLO
  PA_FASE_FINAL = 7,        // PA_FASE_EVALUACION
}
```

---

## CASOS DE PRUEBA

### **Funcionalidad de Subida de Documentos:**
1. Selección de tipo de documento desde dropdown
2. Subida de archivo PDF exitosa
3. Actualización de contador de progreso
4. Visualización de documento subido
5. Opción de eliminar documento

### **Navegación Entre Fases:**
1. Dashboard → Fase 0
2. Dashboard → Fase 1
3. Dashboard → Fase 2
4. URLs específicas funcionando

### **Validación de Formularios:**
1. Campos obligatorios validando
2. Botón "Enviar" deshabilitado hasta completar
3. Mensajes de error apropiados

---

## MEJORAS IDENTIFICADAS

### **Menor Prioridad:**
1. **Documentos de Evaluación:** Actualmente solo se muestran entregables. Las evaluaciones (idDocumentClass=4) están en BD pero no en UI.
2. **Progreso Total:** Fase 1 y 2 muestran "0/1" pero podrían mostrar el total real incluyendo evaluaciones.
3. **IDs DocumentType:** Los IDs no son perfectamente consecutivos (saltan por el sequence automático).

