# Configuración del Almacenamiento de Archivos

Este documento describe la configuración y uso del sistema de almacenamiento de archivos en el proyecto PracticaCore, diseñado con una arquitectura flexible que permite usar diferentes proveedores de almacenamiento.

## Proveedores Soportados

El sistema actualmente soporta los siguientes proveedores de almacenamiento:

- **Almacenamiento Local**: Almacena archivos en el sistema de archivos local
- **Google Cloud Storage**: Almacena archivos en un bucket de Google Cloud
- **Azure Blob Storage**: Preparado para implementación (esqueleto disponible)
- **AWS S3**: Preparado para implementación (esqueleto disponible)

## Configuración en appsettings.json

La configuración del almacenamiento se realiza en `appsettings.json` bajo la sección `FileStorage`:

```json
"FileStorage": {
  "Provider": "Local", // Opciones: "Local", "Google", "Azure", "AWS"
  "LocalPath": "Uploads", // Ruta local, solo necesario para Provider="Local"
  "GoogleCloud": {      // Solo necesario para Provider="Google"
    "BucketName": "pegi_bucked",
    "ProjectId": "gen-lang-client-0285943733",
    "CredentialsPath": "C:\\ruta\\a\\credenciales.json"
  }
}
```

## Configuración por Proveedor

### Almacenamiento Local

Para usar almacenamiento local, configure `appsettings.json` así:

```json
"FileStorage": {
  "Provider": "Local",
  "LocalPath": "Uploads"
}
```

Esto guardará los archivos en la carpeta `Uploads` relativa a la ruta de ejecución de la aplicación.

### Google Cloud Storage

#### Requisitos previos:
1. Tener un proyecto en Google Cloud
2. Crear un bucket en Google Cloud Storage
3. Crear una cuenta de servicio con permisos en el bucket
4. Descargar el archivo de credenciales JSON de la cuenta de servicio

#### Configuración en desarrollo:

1. Actualice `appsettings.json`:
```json
"FileStorage": {
  "Provider": "Google",
  "GoogleCloud": {
    "BucketName": "pegi_bucked",
    "ProjectId": "gen-lang-client-0285943733",
    "CredentialsPath": "C:\\ruta\\a\\credenciales.json"
  }
}
```

2. Asegúrese de que el archivo de credenciales exista en la ruta especificada.

#### Configuración en producción:

1. **Opción recomendada**: Configure la variable de entorno `GOOGLE_APPLICATION_CREDENTIALS`

   * En Windows:
     ```
     setx GOOGLE_APPLICATION_CREDENTIALS "D:\ruta\a\credenciales.json" /M
     ```

   * En Linux:
     ```bash
     export GOOGLE_APPLICATION_CREDENTIALS="/ruta/a/credenciales.json"
     ```
     
   * En un servicio Linux:
     ```
     # Agregar en /etc/systemd/system/servicio-api.service
     [Service]
     Environment="GOOGLE_APPLICATION_CREDENTIALS=/ruta/a/credenciales.json"
     ```

2. Alternativamente, especifique la ruta en `appsettings.json` (menos seguro para producción).

### Azure Blob Storage y AWS S3

Estas implementaciones están preparadas como esqueletos en el sistema. Para habilitarlas:

1. Complete la implementación en sus respectivos servicios
2. Actualice `appsettings.json` con los datos necesarios
3. Cambie el valor de `Provider` a "Azure" o "AWS" según corresponda

## Seguridad y Mejores Prácticas

### Archivo de Credenciales

- **Desarrollo**: Guarde el archivo de credenciales en una ubicación segura fuera del código fuente
- **Producción**: Mueva el archivo de credenciales a una ubicación segura como `/etc/secrets/` y establezca permisos restrictivos:
  ```bash
  sudo mkdir -p /etc/secrets/
  sudo cp credenciales.json /etc/secrets/
  sudo chmod 600 /etc/secrets/credenciales.json
  ```

### Variables de Entorno vs. Configuración en Archivo

- Use variables de entorno en producción para mayor seguridad
- Use la configuración en archivo principalmente para desarrollo

## Uso en el Código

El sistema utiliza la interfaz `IFileStorageService` para abstraer el acceso al almacenamiento:

```csharp
// Inyección de dependencia en controlador o servicio
public class MiControlador : ControllerBase
{
    private readonly IFileStorageService _fileStorageService;
    
    public MiControlador(IFileStorageService fileStorageService)
    {
        _fileStorageService = fileStorageService;
    }
    
    // Guardar un archivo
    public async Task<string> GuardarArchivo(IFormFile archivo)
    {
        using var stream = archivo.OpenReadStream();
        return await _fileStorageService.SaveFileAsync(stream, archivo.FileName, CancellationToken.None);
    }
    
    // Recuperar un archivo
    public async Task<Stream> ObtenerArchivo(string fileName)
    {
        return await _fileStorageService.GetFileAsync(fileName, CancellationToken.None);
    }
}
```

## Solución de Problemas

### Google Cloud Storage

1. **Error: No se encontró la ruta de credenciales**
   - Verifique que el archivo de credenciales existe en la ruta especificada
   - Pruebe a establecer la variable de entorno GOOGLE_APPLICATION_CREDENTIALS

2. **Error de permisos**
   - Verifique que la cuenta de servicio tiene al menos el rol `Storage Object Admin` o `Storage Object Creator/Viewer`

3. **Error de referencia a Google.Cloud.Storage.V1**
   - Asegúrese de haber instalado el paquete NuGet: `dotnet add package Google.Cloud.Storage.V1`