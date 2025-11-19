# Guía de Pruebas (Testing)

El proyecto incluye una suite de pruebas automatizadas para garantizar la calidad y estabilidad del código.

## 🧪 Tipos de Pruebas

### Pruebas Unitarias (`Tests/UnitTests`)
Verifican la lógica de negocio aislada, principalmente en la capa de **Domain** y **Application**.
- **Herramientas**: xUnit, Moq, FluentAssertions.
- **Cobertura**: Entidades, Validadores, Handlers, Servicios de Dominio.

## 🚀 Ejecución de Pruebas

### Desde Visual Studio
1. Abra el **Explorador de pruebas** (Test Explorer).
2. Haga clic en **Ejecutar todas** (Run All).

### Desde Línea de Comandos (CLI)
Para ejecutar todas las pruebas:
```bash
dotnet test
```

Para ejecutar un proyecto específico:
```bash
dotnet test Tests/Tests.csproj
```

## 📝 Estructura de una Prueba
Las pruebas siguen el patrón **AAA (Arrange, Act, Assert)**:

```csharp
[Fact]
public void CreateUser_ShouldReturnId_WhenDataIsValid()
{
    // Arrange (Preparar)
    var userDto = new UserDto { ... };
    var handler = new CreateUserHandler(...);

    // Act (Actuar)
    var result = await handler.Handle(userDto, CancellationToken.None);

    // Assert (Verificar)
    result.Succeeded.Should().BeTrue();
    result.Data.Should().BeGreaterThan(0);
}
```

## 🔌 Pruebas de Integración (Manuales)

Para probar endpoints que requieren interacción con infraestructura real (como subida de archivos o base de datos), se recomienda usar herramientas como **cURL** o **Postman**.

### Ejemplo: Prueba de Subida de Archivos
```bash
curl -X POST "http://localhost:5191/api/Document" \
  -F "File=@test.pdf" \
  -F "IdInscriptionModality=38" \
  -F "IdDocumentType=5" \
  -F "Name=TestDoc" \
  -F "Version=1.0" \
  -F "IdUserCreatedAt=1"
```
