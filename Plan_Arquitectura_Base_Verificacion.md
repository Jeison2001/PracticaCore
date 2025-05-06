Verifica si el diseño es cumple con una estructura de proyecto basada en los principios de Clean Architecture, con el objetivo de maximizar la reutilización de código, reducir la repetición y facilitar la escalabilidad. Para ello, quiero implementar una serie de componentes y patrones genéricos, especialmente para operaciones CRUD, integrando mecanismos automáticos de configuración, validación, mapeo y manejo de errores.
✅ Estructura General del Proyecto (Capas Clean Architecture)
* Dominio: Entidades, Objetos de Valor, Interfaces de Repositorios y Casos de Uso.
* Aplicación: Casos de Uso (Handlers), DTOs, Mapeadores y Validadores.
* Infraestructura: Repositorios, Contexto de Base de Datos, Servicios Externos.
* Presentación: Controladores (API), Middlewares, Filtros y Configuración.
⚙️ Componentes Genéricos a Implementar
📦 Entidades y DTOs Base

public abstract class BaseEntity<TId> where TId : struct { public TId Id { get; set; } public int IdUserCreatedAt { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public int? IdUserUpdatedAt { get; set; } public DateTime? UpdatedAt { get; set; } public string OperationRegister { get; set; } = string.Empty; public bool StatusRegister { get; set; } = true; } public class BaseDto<TId> where TId : struct { public TId Id { get; set; } public int IdUserCreatedAt { get; set; } public DateTime CreatedAt { get; private set; } = DateTime.UtcNow; public int? IdUserUpdatedAt { get; set; } public DateTime? UpdatedAt { get; set; } public string OperationRegister { get; set; } = string.Empty; public bool StatusRegister { get; set; } = true; }
🗂 Infraestructura y Persistencia
* Repositorio Genérico + UnitOfWork: Interfaces e implementación para CRUD estándar, con auditoría automática.
* Contexto de Base de Datos (DbContext) configurado para interceptar cambios y aplicar lógica de auditoría.
🧠 Manejadores Genéricos CRUD (CQRS)
* GetAllHandler<TDto, TEntity>
* GetByIdHandler<TDto, TEntity>
* CreateHandler<TDto, TEntity>
* UpdateHandler<TDto, TEntity>
* DeleteHandler<TDto, TEntity>
<!-- * GetPaginatedHandler<TDto, TEntity> -->
Cada handler:
* Usa Mapeadores Automapper o personalizados.
* Integra validación asíncrona y reglas de negocio.
* Actualiza automáticamente los campos de auditoría.
🌐 Capa de Presentación y Middleware
* Controlador Base: Utiliza los manejadores genéricos y reduce la necesidad de lógica duplicada.
* Middleware Global de Excepciones: Para retornar respuestas estructuradas.
* Filtros de Acción: Para validaciones globales, logging o manejo de caché.
🚀 Funcionalidades Avanzadas
* Paginación, Filtrado y Ordenamiento Dinámico.
* Caché Genérico por Consulta o Endpoint.
* Estandarización de Respuestas API.
* Validación Asíncrona con acceso a DB o servicios externos.
🤖 Automatización y Productividad
* 🔄 Auto-registro de:
   * Mapeadores (e.g. con AssemblyScanner)
   * Handlers y Validadores (mediante Scrutor, MediatR, FluentValidation)
* 🧪 Generación de Tests Unitarios e Integración (plantillas T4 o dotnet new).
* 🧬 Swagger/OpenAPI con metadata enriquecida.
* 🧰 Scripts de Generación de Código para scaffolding genérico de entidades, DTOs y controladores.
📐 Patrones y Prácticas Soportadas
* CQRS: Separación clara entre comandos y consultas.
* Specification Pattern: Encapsular lógica de filtrado y reglas de negocio complejas.
* Repository + UnitOfWork: Garantía de consistencia transaccional.
🎯 Requerimiento

