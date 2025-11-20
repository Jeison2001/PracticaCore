# Arquitectura del Sistema (PracticaCore)

Este proyecto sigue los principios de **Clean Architecture** para garantizar la escalabilidad, mantenibilidad y testabilidad. El diseño se centra en la independencia de frameworks, UI y bases de datos.

## 🏗️ Estructura de Capas y Estándares de Implementación

### 1. Domain (Núcleo)
Contiene la lógica de negocio empresarial y las definiciones de entidades. No tiene dependencias de otras capas.

- **Entities**
    *   **Ubicación**: `Domain/Entities/`
    *   **Implementación**: Deben heredar de `BaseEntity<TId>`.
- **Interfaces**
    *   **Ubicación**: `Domain/Interfaces/`
    *   **Descripción**: Contratos para repositorios (`IGenericRepository`) y servicios.
- **Enums**
    *   **Ubicación**: `Domain/Enums/`

### 2. Application (Casos de Uso)
Orquesta el flujo de datos hacia y desde las entidades del dominio.

- **DTOs**
    *   **Ubicación**: `Application/Shared/DTOs/[NombreModulo]/`
    *   **Implementación**: Deben heredar de `BaseDto<TId>`.
- **Validators**
    *   **Ubicación**: `Application/Validations/SpecificValidators/[NombreModulo]/`
    *   **Implementación**: Heredar de `AbstractValidator<TDto>`.
    *   **Tipos**:
        *   **Genérica**: `BaseCreateCommandValidator` y `BaseUpdateCommandValidator` (automáticos).
        *   **Específica**: Para lógica personalizada, usar `RuleFor(x => x.Dto).SetValidator(new MiDtoValidator())`.
- **Commands (CQRS)**
    *   **Ubicación**: `Application/Shared/Commands/[NombreModulo]/`
    *   **Estructura Obligatoria**:
        ```text
        [NombreModulo]/
        ├── [NombreAccion]Command.cs          # Implementa IRequest<TResponse>
        └── Handlers/                         # Subcarpeta para la lógica
            └── [NombreAccion]CommandHandler.cs # Implementa IRequestHandler<TRequest, TResponse>
        ```
- **Mappings**
    *   **Ubicación**: `Application/Shared/Mappings/`
    *   **Implementación**: Configuraciones de AutoMapper.
- **Services**
    *   **Ubicación**: `Application/Common/Services/`
    *   **Ejemplo**: `NotificationDispatcher`.

### 3. Infrastructure (Persistencia y Servicios Externos)
Implementa las interfaces definidas en el dominio.

- **Configurations (EF Core)**
    *   **Ubicación**: `Infrastructure/Data/Configurations/`
    *   **Implementación**: Heredar de `BaseEntityConfiguration<TEntity, TId>` para auditoría automática.
- **Repositories**
    *   **Ubicación**: `Infrastructure/Repositories/`
    *   **Implementación**: Heredar de `GenericRepository<TEntity>` o implementar interfaz específica.
- **Services**
    *   **Ubicación**: `Infrastructure/Services/`
    *   **Descripción**: Implementaciones de servicios externos (Email, Storage).

### 4. Api (Presentación)
Punto de entrada de la aplicación.

- **Controllers**
    *   **Ubicación**: `Api/Controllers/`
    *   **Implementación**: Heredar de `GenericController<TEntity, TId, TDto>` para CRUD automático.
- **Extensions**
    *   **Ubicación**: `Api/Extensions/`
    *   **Descripción**: Configuración de inyección de dependencias.

---

## ⚙️ Componentes Clave

### Entidades Base
Todas las entidades heredan de `BaseEntity<T>`, que proporciona auditoría automática:
```csharp
public abstract class BaseEntity<TId>
{
    public TId Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool StatusRegister { get; set; }
    // ... otros campos de auditoría
}
```

### Repositorio Genérico
El patrón Repository se implementa de forma genérica para operaciones CRUD estándar, permitiendo:
- Paginación automática.
- Filtrado dinámico.
- Ordenamiento.

### Controladores Genéricos
`GenericController<TEntity, TId, TDto>` expone endpoints CRUD estándar automáticamente, reduciendo el código repetitivo.

### Sistema de Notificaciones
Implementa un patrón Dispatcher/Handler para desacoplar los eventos de dominio del envío de notificaciones (ver documentación específica en `NOTIFICACIONES/`).

---

## 🚀 Patrones Utilizados
- **Repository & Unit of Work**: Abstracción de la capa de datos.
- **CQRS**: Implementado completamente con **MediatR** para separar Comandos (Escritura) y Consultas (Lectura).
- **Mediator**: Desacoplamiento entre controladores y lógica de aplicación.
- **Strategy**: Para el despacho de notificaciones.
- **Builder**: Para la construcción de datos de eventos.

## 🧪 Testing
La estrategia de pruebas se detalla en `Contexto/TESTING.md`.
- **Unitarias**: `Tests/UnitTests`
- **Integración**: `Tests/Integration` (incluye soporte para controladores genéricos).

