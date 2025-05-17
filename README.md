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

### Rendimiento
- **Caché**: Decoradores como `CachedRepository` mejoran el rendimiento al reducir las consultas repetidas a la base de datos.
- **Consultas Optimizadas**: Uso de `.AsNoTracking()` para mejorar el rendimiento en consultas de solo lectura.

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