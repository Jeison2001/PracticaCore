# PracticaCore

## Introducción
PracticaCore es un proyecto diseñado para implementar una arquitectura limpia (Clean Architecture) con soporte para operaciones genéricas, paginación, filtros avanzados y manejo de errores. Este proyecto está orientado a facilitar la escalabilidad y reutilización del código.

---

## 📚 Documentación

### 🏗️ Arquitectura y Desarrollo
- **[Arquitectura](Contexto/ARQUITECTURA.md)** - Visión general de Clean Architecture y capas del sistema
- **[Convenciones de Namespaces](Contexto/CONVENCIONES_NAMESPACES.md)** - Estándares de nomenclatura y organización
- **[Organización de Carpetas](Contexto/ORGANIZACION_CARPETAS.md)** - Guía rápida: ¿Dónde agregar código?
- **[Patrones de Diseño](Contexto/PATRONES_DISEÑO.md)** - CQRS, Repository, UnitOfWork, Decorator y más
- **[Guía de Desarrollo](Contexto/GUIA_DESARROLLO.md)** - Tutorial paso a paso para agregar nuevas entidades
- **[Guía de API](Contexto/GUIA_API.md)** - Autenticación, Swagger y uso de endpoints
- **[Testing](Contexto/TESTING.md)** - Cómo ejecutar las pruebas unitarias

### ⚙️ Infraestructura
- **[Almacenamiento de Archivos](Contexto/INFRAESTRUCTURA/ALMACENAMIENTO_ARCHIVOS.md)** - Configuración de proveedores (Local, Google Cloud, Azure, AWS)

### 📦 Módulos Específicos
- **[Módulo de Documentos](Contexto/MODULOS/DOCUMENTOS.md)** - Gestión de carga, descarga y validación de archivos

### 🔔 Sistema de Notificaciones
- **[Documentación General](Contexto/NOTIFICACIONES/SISTEMA_NOTIFICACIONES.md)** - Arquitectura y funcionamiento
- **[Guía de Extensión](Contexto/NOTIFICACIONES/GUIA_EXTENSION.md)** - Cómo agregar nuevas notificaciones
- **[Caso Práctica Académica](Contexto/NOTIFICACIONES/CASO_PRACTICA_ACADEMICA.md)** - Implementación específica

### 💼 Negocio
- **[Flujo Práctica Académica](Contexto/NEGOCIO/FLUJO_COMPLETO_ACADEMIC_PRACTICE.md)** - Diagramas y explicación del flujo
- **[Mapeo Modelo Negocio](Contexto/NEGOCIO/MAPEO_MODELO_NEGOCIO_MEJORADO.md)** - Relación entre entidades y reglas

---

## 🚀 Configuración Rápida

### Prerrequisitos
- .NET 8.0 SDK
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

3. **Aplicar migraciones**
   ```bash
   dotnet ef database update --project Infrastructure --startup-project Api
   ```

4. **Ejecutar el proyecto**
   ```bash
   dotnet run --project Api/Api.csproj
   ```

5. **Acceder a Swagger**
   - Navegar a: `https://localhost:5001/swagger`

### Ejecutar Tests
```bash
dotnet test
```

---

## 🎯 Inicio Rápido para Desarrolladores

**¿Primera vez en el proyecto?** Lee en este orden:
1. [ARQUITECTURA.md](Contexto/ARQUITECTURA.md) - Entender las capas
2. [ORGANIZACION_CARPETAS.md](Contexto/ORGANIZACION_CARPETAS.md) - Saber dónde va cada cosa
3. [GUIA_DESARROLLO.md](Contexto/GUIA_DESARROLLO.md) - Agregar tu primera entidad

**¿Necesitas agregar funcionalidad?**
- Nueva entidad → [GUIA_DESARROLLO.md](Contexto/GUIA_DESARROLLO.md)
- Nuevo servicio → [ORGANIZACION_CARPETAS.md](Contexto/ORGANIZACION_CARPETAS.md)
- Notificaciones → [NOTIFICACIONES/GUIA_EXTENSION.md](Contexto/NOTIFICACIONES/GUIA_EXTENSION.md)

---

## 📞 Soporte

Para más información, consulta la documentación en la carpeta `Contexto/`.