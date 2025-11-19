# Arquitectura del Sistema (PracticaCore)

Este proyecto sigue los principios de **Clean Architecture** para garantizar la escalabilidad, mantenibilidad y testabilidad. El diseño se centra en la independencia de frameworks, UI y bases de datos.

## 🏗️ Estructura de Capas

### 1. Domain (Núcleo)
Contiene la lógica de negocio empresarial y las definiciones de entidades. No tiene dependencias de otras capas.
- **Entities**: Objetos de negocio (`BaseEntity<T>`).
- **Interfaces**: Contratos para repositorios y servicios (`IGenericRepository`, `INotificationDispatcher`).
- **Enums**: Enumeraciones del dominio.

### 2. Application (Casos de Uso)
Orquesta el flujo de datos hacia y desde las entidades del dominio.
- **DTOs**: Objetos de Transferencia de Datos (`BaseDto<T>`).
- **Validators**: Reglas de validación con FluentValidation.
  - **Validación Genérica**: `BaseCreateCommandValidator` y `BaseUpdateCommandValidator` validan propiedades comunes.
  - **Validación Específica**: Para comandos personalizados (no genéricos), se debe crear un validador que implemente `AbstractValidator<TCommand>` y delegue la validación del DTO usando `RuleFor(x => x.Dto).SetValidator(new MiDtoValidator())`.
- **Services**: Lógica de aplicación y orquestación (`NotificationDispatcher`).
- **Mappings**: Configuraciones de AutoMapper.

### 3. Infrastructure (Persistencia y Servicios Externos)
Implementa las interfaces definidas en el dominio.
- **Data**: Contexto de base de datos (EF Core) y configuraciones de entidades.
- **Repositories**: Implementación de repositorios genéricos y específicos.
- **Services**: Implementaciones de servicios externos (Email, Storage).

### 4. Api (Presentación)
Punto de entrada de la aplicación.
- **Controllers**: Controladores REST (`GenericController<T>`).
- **Middleware**: Manejo global de excepciones.
- **Extensions**: Configuración de inyección de dependencias.

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
- **CQRS (Parcial)**: Separación conceptual en handlers de notificaciones.
- **Strategy**: Para el despacho de notificaciones.
- **Builder**: Para la construcción de datos de eventos.
