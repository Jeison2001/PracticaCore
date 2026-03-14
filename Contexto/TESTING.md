# Contexto de Pruebas Automatizadas - PracticaCore

Este documento describe la arquitectura de pruebas implementada para garantizar la calidad y estabilidad del backend de `PracticaCore`.

## 1. Resumen de Ejecución
Actualmente, el sistema cuenta con una suite de pruebas robusta y completa:
- **Total de Tests:** 240+
- **Estado:** ✅ Todos pasando (100% Success Rate)
- **Tecnologías:** xUnit, FluentAssertions, Microsoft.AspNetCore.Mvc.Testing (Integration), Moq (Unit).

## 2. Tipos de Pruebas

### 2.1. Pruebas Unitarias (`Tests/UnitTests`)
Verifican la lógica de negocio aislada, principalmente en la capa de **Domain** y **Application**.
- **Herramientas**: xUnit, Moq, FluentAssertions.
- **Cobertura**: Entidades, Validadores, Handlers, Servicios de Dominio.

### 2.2. Pruebas de Integración (`Tests/Integration`)
Verifican el flujo completo de la aplicación, desde el controlador hasta la base de datos (simulada), pasando por la pipeline de MediatR y Validaciones.
- **Herramientas**: `Microsoft.AspNetCore.Mvc.Testing`, `WebApplicationFactory`.
- **Base de Datos**: `InMemoryDatabase` (aislada por test).
- **Convención de Nombres**: `[Entidad]ControllerTests.cs` (ej. `UserControllerTests.cs`).

## 3. Infraestructura de Pruebas de Integración

### 3.1. CustomWebApplicationFactory
Ubicación: `Tests/Integration/CustomWebApplicationFactory.cs`
Esta clase levanta un servidor de prueba en memoria que simula el entorno real de la API pero aislado:
- **Base de Datos:** Usa `InMemoryDatabase` para garantizar que cada ejecución sea limpia y no afecte datos reales.
- **Autenticación:** Configura un esquema JWT de prueba para validar endpoints protegidos sin necesitar tokens reales de producción.
- **Entorno:** Fuerza el entorno "Testing" para evitar la carga de servicios pesados como Hangfire o almacenamiento en nube real.

### 3.2. IntegrationTestBase
Ubicación: `Tests/Integration/IntegrationTestBase.cs`
Clase base para todos los tests de integración. Proporciona:
- Cliente HTTP preconfigurado (`_client`).
- Acceso al Scope de servicios para consultar la base de datos en memoria (`_factory`).
- Métodos de utilidad comunes.

### 3.3. Fix de Estabilidad (ServiceExtensions)
Se implementó un filtro en `Api/Extensions/ServiceExtensions.cs` para ignorar ensamblados dinámicos (proxies de Moq/Castle) durante el escaneo de tipos. Esto previene errores `ReflectionTypeLoadException` que causaban fallos intermitentes en los tests.

## 4. Estrategia de Pruebas

### 4.1. Pruebas Genéricas (GenericControllerIntegrationTests)
Para evitar escribir tests repetitivos para cada entidad CRUD, se implementó una clase base genérica:
`GenericControllerIntegrationTests<TEntity, TDto>`

Esta clase utiliza **Reflection** y **Genéricos** para probar automáticamente las operaciones estándar de cualquier controlador que herede de `GenericController`:
- `GetAll_ReturnsOkAndList`: Verifica listado paginado.
- `GetById_ReturnsOkAndEntity`: Verifica obtención por ID.
- `Create_ReturnsCreated`: Verifica creación exitosa.
- `Update_ReturnsOk`: Verifica actualización exitosa.
- `UpdateStatus_ReturnsOk`: Verifica el borrado lógico (Soft Delete).

**Ventaja:** Al agregar una nueva entidad, solo se necesita heredar de esta clase base en el proyecto de tests y los 5 tests CRUD se generan automáticamente.

### 4.2. Pruebas Específicas (Business Logic)
Para controladores con lógica de negocio compleja o validaciones estrictas, se implementaron tests específicos que van más allá del CRUD básico.

#### AcademicPractice (`AcademicPracticeControllerTests`)
- **Validación de Campos:** Verifica que falten títulos o nombres de institución retorne `400 Bad Request`.
- **Lógica de Fechas:** Verifica que la fecha de fin no sea anterior a la de inicio.
- **Persistencia (Happy Path):** Test `UpdateInstitutionInfo_WithValidData_ShouldUpdateDatabase` que:
    1. Semilla datos en la BD en memoria.
    2. Ejecuta un PUT real.
    3. Consulta la BD para asegurar que los cambios se guardaron correctamente.

#### Inscription (`InscriptionControllerTests`)
- **Validación de Listas:** Verifica que no se pueda crear una inscripción sin estudiantes.
- **Validación Anidada:** Verifica que los datos de los estudiantes dentro de la lista sean válidos (Identificación requerida).

## 5. Cómo Ejecutar las Pruebas

### Ejecutar todos los tests
```powershell
dotnet test Tests/Tests.csproj
```

### Ejecutar solo tests de integración
```powershell
dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~Integration"
```

### Ejecutar un test específico
```powershell
dotnet test Tests/Tests.csproj --filter "Displayname~UpdateInstitutionInfo_WithValidData"
```
