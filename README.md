# PracticaCore

## Introducción
PracticaCore es un proyecto diseñado para implementar una arquitectura limpia (Clean Architecture) con soporte para operaciones genéricas, paginación, filtros avanzados y manejo de errores. Este proyecto está orientado a facilitar la escalabilidad y reutilización del código.

## 📚 Documentación y Contexto

La documentación detallada del proyecto se encuentra organizada en la carpeta `Contexto/`:

### 🏗️ Arquitectura y Desarrollo
- **[Arquitectura Base](Contexto/ARQUITECTURA.md)**: Visión general de la arquitectura limpia, capas y patrones utilizados.
- **[Guía de Desarrollo](Contexto/GUIA_DESARROLLO.md)**: Pasos para crear nuevas entidades, DTOs, controladores y validaciones.
- **[Guía de API](Contexto/GUIA_API.md)**: Autenticación, Swagger y uso de endpoints.
- **[Testing](Contexto/TESTING.md)**: Cómo ejecutar las pruebas unitarias.

### ⚙️ Infraestructura
- **[Almacenamiento de Archivos](Contexto/INFRAESTRUCTURA/ALMACENAMIENTO_ARCHIVOS.md)**: Configuración de proveedores de almacenamiento (Local, Google Cloud, Azure, AWS).

### 📦 Módulos Específicos
- **[Módulo de Documentos](Contexto/MODULOS/DOCUMENTOS.md)**: Gestión de carga, descarga y validación de archivos.

### 🔔 Sistema de Notificaciones
- **[Documentación General](Contexto/NOTIFICACIONES/SISTEMA_NOTIFICACIONES.md)**: Arquitectura y funcionamiento del sistema de notificaciones.
- **[Guía de Extensión](Contexto/NOTIFICACIONES/GUIA_EXTENSION.md)**: Cómo agregar nuevas notificaciones para nuevas entidades.
- **[Caso Práctica Académica](Contexto/NOTIFICACIONES/CASO_PRACTICA_ACADEMICA.md)**: Implementación específica para el módulo de prácticas.

### 💼 Negocio
- **[Flujo Práctica Académica](Contexto/NEGOCIO/FLUJO_COMPLETO_ACADEMIC_PRACTICE.md)**: Diagramas y explicación del flujo de negocio.
- **[Mapeo Modelo Negocio](Contexto/NEGOCIO/MAPEO_MODELO_NEGOCIO_MEJORADO.md)**: Relación entre entidades y reglas de negocio.

---

## Configuración Rápida
1. Clona el repositorio.
2. Configura las cadenas de conexión en `appsettings.json`.
3. Ejecuta el proyecto:
   ```bash
   dotnet run --project Api/Api.csproj
   ```

## Estructura del Proyecto
- **Api/**: Controladores y configuración.
- **Application/**: Casos de uso, DTOs y validaciones.
- **Domain/**: Entidades y lógica de negocio.
- **Infrastructure/**: Persistencia y servicios externos.
- **Tests/**: Pruebas unitarias e integrales.

---
**Nota:** Para detalles profundos sobre cada componente, consulte los archivos en `Contexto/`.