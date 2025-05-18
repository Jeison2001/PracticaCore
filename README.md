# PracticaCore

## Introducción
PracticaCore es un proyecto diseñado para implementar una arquitectura limpia (Clean Architecture) con soporte para operaciones genéricas, paginación, filtros avanzados y manejo de errores. Este proyecto está orientado a facilitar la escalabilidad y reutilización del código.

## Configuración y Ejecución
1. Clona el repositorio:
   ```bash
   git clone <URL_DEL_REPOSITORIO>
   ```
2. Configura las cadenas de conexión en `appsettings.json` y `appsettings.Development.json`.
3. Ejecuta el proyecto desde Visual Studio o usando el siguiente comando:
   ```bash
   dotnet run --project Api/Api.csproj
   ```

## Paginación y Filtros
El sistema soporta paginación y filtrado avanzado mediante parámetros de consulta.

### Paginación
- **Clase `PaginatedRequest`**: Define los parámetros de solicitud, como el número de página, tamaño de página, campo de ordenamiento y dirección.
- **Clase `PaginatedResult`**: Estructura de la respuesta paginada, que incluye:
  - Lista de elementos de la página actual.
  - Total de registros en el conjunto de datos.
  - Número de página actual y total de páginas.
  - Indicadores de existencia de páginas previas y siguientes.

Ejemplo de solicitud:
```http
GET /api/products?PageNumber=1&PageSize=10
```

### Filtros
El sistema implementa un mecanismo de filtrado dinámico y potente mediante operadores.

Formato: `Filters[propiedad@operador]=valor`

| Operador    | Descripción               | Ejemplo                          |
|-------------|---------------------------|----------------------------------|
| eq          | Igual a (default)         | `Filters[Name]=John`            |
| ne          | No igual a                | `Filters[Name@ne]=John`         |
| gt          | Mayor que                 | `Filters[Price@gt]=100`         |
| like        | Contiene (para texto)     | `Filters[Name@like]=phone`      |

Ejemplo de solicitud con filtros:
```http
GET /api/products?Filters[Price@gt]=50&Filters[Name@like]=phone
```

Los filtros pueden combinarse para realizar consultas complejas:
```http
GET /api/products?Filters[Price@gt]=50&Filters[Category]=Electronics
```

## Estructura del Proyecto
El proyecto sigue los principios de Clean Architecture, dividiendo las responsabilidades en capas bien definidas:

- **Api/**: Contiene los controladores y configuración de la API. Incluye middleware global para manejo de excepciones y filtros de Swagger.
- **Application/**: Casos de uso, DTOs, validaciones y mapeos. Implementa patrones como CQRS y validación asíncrona.
- **Domain/**: Entidades, interfaces y lógica de negocio. Define contratos y reglas de negocio.
- **Infrastructure/**: Repositorios, servicios y configuración de base de datos. Incluye decoradores como `CachedRepository` para mejorar el rendimiento.
- **Tests/**: Pruebas unitarias e integrales para garantizar la calidad del código.

## Convenciones
- **Nombres de Clases**: PascalCase.
- **Nombres de Variables**: camelCase.
- **Carpetas**: Singular (e.g., `Controller`, `Service`).

## Puntos Fuertes del Proyecto

### Arquitectura Modular
El proyecto está diseñado para ser altamente modular, lo que facilita la escalabilidad y el mantenimiento. Cada capa tiene responsabilidades claras y está desacoplada de las demás.

### Operaciones Genéricas
- **Controladores Genéricos**: Los controladores como `GenericController` manejan operaciones CRUD comunes, reduciendo la duplicación de código.
- **Repositorios Genéricos**: Implementan operaciones estándar como paginación, filtrado y ordenamiento dinámico.

### Manejo de Errores
- **Middleware Global de Excepciones**: Garantiza respuestas consistentes y estructuradas para errores.
- **Auditoría Automática**: Los repositorios actualizan automáticamente los campos de auditoría como `CreatedAt` y `UpdatedAt`.

### Automatización
- **Registro Automático**: Servicios, validadores y handlers se registran automáticamente mediante convenciones.
- **Mapeo Automático**: AutoMapper configura mapeos genéricos entre entidades y DTOs.

### Automatización de registro de servicios y handlers

El proyecto utiliza métodos de registro automático para minimizar la configuración manual y facilitar la escalabilidad:

- **Servicios**: Se registran automáticamente según su ciclo de vida (`ITransientService`, `IScopedService`, `ISingletonService`) usando convenciones e interfaces marcadoras.
- **Handlers y Validadores**: Los handlers genéricos de MediatR y los validadores de FluentValidation se registran automáticamente para todas las entidades y DTOs que sigan la convención de heredar de `BaseEntity<>` y `BaseDto<>` respectivamente.
- **Convención**: Para que el registro automático funcione, las clases deben heredar de las bases genéricas y estar en los ensamblados escaneados.

Esto permite que, al agregar una nueva entidad y su DTO siguiendo la convención, los comandos, queries, handlers y validadores genéricos sean registrados y funcionales sin configuración adicional.

### Rendimiento
- **Caché**: Decoradores como `CachedRepository` mejoran el rendimiento al reducir las consultas repetidas a la base de datos.
- **Consultas Optimizadas**: Uso de `.AsNoTracking()` para mejorar el rendimiento en consultas de solo lectura.

> ⚠️ **Nota sobre el caché:**
> El decorador `CachedRepository` solo aplica caché automáticamente a las operaciones estándar del repositorio genérico (consultas simples de una entidad, sin Includes ni joins complejos).
> 
> Métodos personalizados en repositorios como `ProposalRepository`, `UserRoleRepository` o `TeachingAssignmentRepository` que usan Includes, joins o proyecciones complejas **NO** se benefician del caché genérico. Si se requiere caché para estos casos, debe implementarse manualmente y considerar cuidadosamente la coherencia e invalidación de los datos.

## Manejo global de errores

El proyecto utiliza un middleware global para capturar y responder a las excepciones de forma uniforme. Los códigos de estado y formatos de respuesta son los siguientes:

- **400 Bad Request**: Errores de validación (por ejemplo, validaciones de FluentValidation).
  ```json
  {
    "success": false,
    "errors": ["El campo X es requerido."]
  }
  ```
- **401 Unauthorized**: Acceso no autorizado.
  ```json
  {
    "success": false,
    "errors": ["No autorizado."]
  }
  ```
- **404 Not Found**: Recurso no encontrado (por ejemplo, KeyNotFoundException).
  ```json
  {
    "success": false,
    "errors": ["No se encontró el recurso solicitado."]
  }
  ```
- **500 Internal Server Error**: Errores inesperados del servidor.
  ```json
  {
    "success": false,
    "errors": ["Mensaje de error interno."]
  }
  ```

Todas las respuestas de error siguen el formato del objeto `ApiResponse`, asegurando consistencia en toda la API.

## Buenas prácticas para consultas en repositorios

- Utiliza `.AsNoTracking()` en todas las consultas de solo lectura para mejorar el rendimiento y reducir el uso de memoria.
- Aplica paginación y filtros antes de ejecutar la consulta para evitar traer datos innecesarios de la base de datos.
- Usa extensiones como `ToPaginatedResultAsync` para construir respuestas paginadas de manera eficiente.
- Realiza proyecciones y mapeos solo después de obtener los datos necesarios, evitando cargar relaciones innecesarias.

Estas prácticas ya están implementadas en repositorios como `ProposalRepository`, `UserRoleRepository` y `TeachingAssignmentRepository`.

## Refactorización y mantenimiento

- Identifica clases o métodos grandes y divídelos en componentes más pequeños y reutilizables.
- Abstrae patrones repetidos en servicios, extensiones o utilidades para evitar duplicación de lógica.
- Mantén el código limpio, con responsabilidades claras y siguiendo los principios de SOLID y Clean Architecture.
- Revisa periódicamente clases como `JwtService`, `ProposalRepository` y otros servicios complejos para detectar oportunidades de simplificación y mejora.
- Documenta los cambios y refactorizaciones relevantes para facilitar el mantenimiento futuro.

## Contribución
1. Crea un fork del repositorio.
2. Crea una rama para tu funcionalidad:
   ```bash
   git checkout -b feature/nueva-funcionalidad
   ```
3. Envía un pull request con tus cambios.

---

## Créditos
Este proyecto fue desarrollado siguiendo principios de arquitectura limpia y mejores prácticas de desarrollo de software.