# PracticaCore

## Introducción
PracticaCore es un proyecto diseñado para implementar una arquitectura limpia (Clean Architecture) con soporte para operaciones genéricas, paginación, filtros avanzados y manejo de errores. Este proyecto está orientado a facilitar la escalabilidad y reutilización del código.

---

## 🚀 Configuración Rápida

### Prerrequisitos
- .NET 10.0 SDK
- PostgreSQL 12+
- (Opcional) Docker para desarrollo local

### Instalación

1. **Clonar el repositorio**
   ```bash
   git clone <repository-url>
   cd PracticaCore
   ```

2. **Configurar base de datos**
   - Editar `Api/appsettings.json` con tu cadena de conexión
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Database=practicacore;Username=user;Password=pass"
   }
   ```

3. **Ejecutar el proyecto**
   ```bash
   dotnet run --project Api/Api.csproj
   ```

4. **Acceder a Swagger**
   - Navegar a: `https://localhost:5001/swagger`

### Ejecutar Tests
```bash
dotnet test
```

---

## 📤 Despliegue

### Publicar en Release
```bash
dotnet publish Api/Api.csproj --configuration Release --output publish
```

### Publicar en carpeta específica
```bash
dotnet publish Api/Api.csproj --configuration Release --output <ruta_destino>
```