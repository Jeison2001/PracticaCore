# Arquitectura del Sistema (PracticaCore)

Este proyecto sigue los principios de **Clean Architecture** para garantizar la escalabilidad, mantenibilidad y testabilidad. El diseño se centra en la independencia de frameworks, UI y bases de datos.

## 🏗️ Principios de Clean Architecture

### Regla de Dependencia

```
┌─────────────────────────────────────┐
│           API Layer                 │
│  (Presentación - Controllers)       │
└────────────┬────────────────────────┘
             │ depende de ↓
┌────────────▼────────────────────────┐
│      Application Layer              │
│  (Casos de Uso - CQRS)              │
└────────────┬────────────────────────┘
             │ depende de ↓
┌────────────▼────────────────────────┐
│      Infrastructure Layer           │
│  (Implementaciones)                 │
└────────────┬────────────────────────┘
             │ implementa ↓
┌────────────▼────────────────────────┐
│         Domain Layer                │
│  (Entidades e Interfaces)           │
└─────────────────────────────────────┘
```

**Regla de Oro:** Las capas externas dependen de las internas, **nunca al revés**.

---

## 📦 Capas del Sistema

### 1. Domain Layer (Núcleo del Negocio)

**Responsabilidades:**
- Definir entidades de negocio
- Declarar interfaces (contratos para repositorios y servicios)
- Establecer reglas de negocio
- **NO** tiene dependencias de otras capas ni frameworks

**Contiene:**
- `Entities/` - Entidades de dominio que heredan de `BaseEntity<TId>`
- `Interfaces/` - Contratos para repositorios y servicios
- `Enums/` - Enumeraciones del dominio
- `Exceptions/` - Excepciones de negocio
- `Common/` - Utilidades compartidas del dominio

> 📖 **Detalles:** Ver [CONVENCIONES_NAMESPACES.md](CONVENCIONES_NAMESPACES.md) para organización de namespaces
>
> **⚠️ Regla Crítica:** El Dominio **NUNCA** debe retornar tipos definidos en Application (DTOs) ni tipos `dynamic`. Si un servicio de dominio necesita retornar datos complejos, debe definir su propio objeto de resultado (ej: `AuthenticationResult`) en `Domain/Common`.


---

### 2. Application Layer (Casos de Uso)

**Responsabilidades:**
- Orquestar flujos de negocio
- Implementar CQRS con MediatR (Commands y Queries)
- Validar datos de entrada con FluentValidation
- Mapear entre entidades y DTOs con AutoMapper

**Contiene:**
- `Commands/` - Operaciones de escritura (Create, Update, Delete)
- `Queries/` - Operaciones de lectura (Get, GetAll, GetPaginated)
- `DTOs/` - Data Transfer Objects
- `Validations/` - Validadores de FluentValidation
- `Mappings/` - Perfiles de AutoMapper
- `Behaviors/` - Pipelines de MediatR (ej: ValidationBehavior)

> 📖 **Detalles:** Ver [PATRONES_DISEÑO.md](PATRONES_DISEÑO.md) para CQRS y Mediator

---

### 3. Infrastructure Layer (Implementaciones)

**Responsabilidades:**
- Implementar interfaces definidas en Domain
- Acceso a datos con Entity Framework Core
- Servicios externos (Email, Storage, Cache, Auth)
- Configuración de persistencia

**Contiene:**
- `Data/` - DbContext y configuraciones de EF Core
- `Repositories/` - Implementaciones de repositorios
- `Services/` - Implementaciones de servicios (Auth, Cache, Storage, Notifications)
- `Extensions/` - Métodos de extensión para configuración

> 📖 **Detalles:** Ver [ORGANIZACION_CARPETAS.md](ORGANIZACION_CARPETAS.md) para estructura completa

---

### 4. API Layer (Presentación)

**Responsabilidades:**
- Exponer endpoints REST
- Configurar middleware (autenticación, manejo de errores)
- Inyección de dependencias
- Documentación con Swagger

**Contiene:**
- `Controllers/` - Controladores REST (heredan de `GenericController`)
- `Extensions/` - Configuración de servicios y DI
- `Middlewares/` - Middleware personalizado
- `Responses/` - Modelos de respuesta API

---

## ⚙️ Componentes Clave

### Entidades Base

Todas las entidades heredan de `BaseEntity<TId>` que proporciona:
- Identificador genérico (`Id`)
- Auditoría automática (`CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`)
- Soft delete (`StatusRegister`)

### Repositorio Genérico

`IRepository<T, TId>` proporciona operaciones CRUD estándar:
- Paginación automática
- Filtrado dinámico
- Ordenamiento
- Soporte para includes (eager loading)

### Controladores Genéricos

`GenericController<TEntity, TDto, TId>` expone automáticamente:
- `GET /api/{entity}` - Listar con paginación
- `GET /api/{entity}/{id}` - Obtener por ID
- `POST /api/{entity}` - Crear
- `PUT /api/{entity}/{id}` - Actualizar
- `DELETE /api/{entity}/{id}` - Eliminar (soft delete)

---

## 🚀 Patrones Implementados

El proyecto implementa los siguientes patrones de diseño:

- **Clean Architecture** - Separación de responsabilidades en capas
- **CQRS** - Separación de comandos y consultas
- **Repository Pattern** - Abstracción de acceso a datos
- **Unit of Work** - Gestión de transacciones
- **Decorator Pattern** - Cache sobre repositorios
- **Mediator Pattern** - Desacoplamiento con MediatR
- **Dependency Injection** - Inversión de control
- **Validation Pipeline** - Validación centralizada

> 📖 **Detalles:** Ver [PATRONES_DISEÑO.md](PATRONES_DISEÑO.md) para ejemplos de implementación

---

## 📚 Documentación Relacionada

Para información específica, consulta:

- **[CONVENCIONES_NAMESPACES.md](CONVENCIONES_NAMESPACES.md)** - Estándares de namespaces por capa
- **[ORGANIZACION_CARPETAS.md](ORGANIZACION_CARPETAS.md)** - Estructura de carpetas y dónde agregar código
- **[PATRONES_DISEÑO.md](PATRONES_DISEÑO.md)** - Explicación detallada de patrones
- **[GUIA_DESARROLLO.md](GUIA_DESARROLLO.md)** - Tutorial paso a paso para agregar entidades

---

## 🧪 Testing

La estrategia de pruebas se detalla en [TESTING.md](TESTING.md):
- **Unitarias** - `Tests/UnitTests`
- **Integración** - `Tests/Integration` (incluye soporte para controladores genéricos)
