# Infraestructura: Almacenamiento de Archivos

El sistema implementa una abstracción `IFileStorageService` que permite cambiar el proveedor de almacenamiento mediante configuración, sin modificar el código de la aplicación.

## Proveedores Soportados

| Proveedor | Estado | Clave Configuración |
|-----------|--------|---------------------|
| **Local** | ✅ Activo | `Local` |
| **Google Cloud** | ✅ Activo | `Google` |
| **Azure Blob** | ✅ Activo | `Azure` |
| **AWS S3** | ⚠️ Pendiente | `AWS` |

## Configuración (`appsettings.json`)

La sección `FileStorage` controla el comportamiento.

```json
"FileStorage": {
  "Provider": "Local", // Opciones: "Local", "Google", "Azure", "AWS"
  
  // Configuración para Local
  "LocalPath": "Uploads",

  // Configuración para Google Cloud
  "GoogleCloud": {
    "BucketName": "nombre-bucket",
    "ProjectId": "id-proyecto",
    "CredentialsPath": "ruta/a/credenciales.json"
  },

  // Configuración para Azure
  "AzureBlob": {
    "ConnectionString": "DefaultEndpointsProtocol=https;...",
    "ContainerName": "archivos"
  }
}
```

## Uso en Código

Se recomienda utilizar el servicio dentro de un **Command Handler** (patrón CQRS), no directamente en el controlador.

```csharp
public class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, string>
{
    private readonly IFileStorageService _storage;

    public UploadFileCommandHandler(IFileStorageService storage)
    {
        _storage = storage;
    }

    public async Task<string> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        // El servicio maneja el stream y retorna el nombre único o URL del archivo guardado
        string storedFileName = await _storage.SaveFileAsync(
            request.File.OpenReadStream(), 
            request.File.FileName, 
            cancellationToken
        );
        
        return storedFileName;
    }
}
```

## Notas de Implementación
- **Local**: Los archivos se guardan en `Api/Uploads` (o la ruta configurada). Asegúrese de que la carpeta tenga permisos de escritura.
- **Google/Azure**: Requieren credenciales válidas. En producción, se recomienda usar variables de entorno o secretos gestionados en lugar de archivos JSON locales.