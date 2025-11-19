# Módulo de Documentos

Este módulo gestiona la carga, almacenamiento, recuperación y actualización de documentos dentro del sistema. A diferencia de los módulos CRUD estándar, este módulo implementa lógica personalizada para manejar archivos binarios y metadatos asociados.

## 🏗️ Arquitectura Específica

El módulo de documentos se desvía del patrón genérico `GenericController` para permitir el manejo de `multipart/form-data` y la integración con el servicio de almacenamiento.

### Componentes Principales

1.  **Controller**: `DocumentController`
    *   No hereda de `GenericController`.
    *   Expone endpoints específicos (`Upload`, `Update`, `DownloadFile`).
    *   Maneja excepciones específicas de validación y almacenamiento.

2.  **Commands**:
    *   `CreateDocumentWithFileCommand`: Encapsula el DTO de subida (`DocumentUploadDto`).
    *   `UpdateDocumentWithFileCommand`: Encapsula el DTO de actualización (`DocumentUpdateDto`).

3.  **Validators**:
    *   **DTO Validators** (`DocumentUploadDtoValidator`): Validan el archivo (extensión, tamaño) y metadatos.
    *   **Command Validators** (`CreateDocumentWithFileCommandValidator`): Delegan la validación al DTO validator. Esto es crucial para que el pipeline de MediatR (`ValidationBehavior`) intercepte y valide la petición antes de llegar al Handler.

4.  **Handlers**:
    *   `CreateDocumentWithFileCommandHandler`:
        1.  Valida referencias (ej. `DocumentType`).
        2.  Llama a `IFileStorageService` para guardar el archivo físico.
        3.  Crea la entidad `Document` con la ruta/nombre del archivo guardado.
        4.  Guarda en base de datos.

5.  **Storage Service**: `IFileStorageService`
    *   Abstrae el almacenamiento físico (Local, Cloud).

## 🔄 Flujo de Subida de Archivos

1.  **Cliente** envía `POST /api/Document` con `multipart/form-data`.
2.  **Controller** recibe `DocumentUploadDto`.
3.  **Controller** crea `CreateDocumentWithFileCommand(dto)`.
4.  **MediatR Pipeline**:
    *   Ejecuta `CreateDocumentWithFileCommandValidator`.
    *   Si falla: Lanza `ValidationException`.
    *   Si pasa: Llama al Handler.
5.  **Handler**:
    *   Guarda el archivo usando `IFileStorageService.SaveFileAsync`.
    *   Persiste la entidad `Document` en BD.
6.  **Controller** retorna `201 Created` con los datos del documento.

## 🛡️ Validaciones Implementadas

*   **Extensiones permitidas**: `.pdf`, `.doc`, `.docx`.
*   **Tamaño máximo**: 10 MB.
*   **Integridad**: Se valida que el archivo no sea nulo.

## 📝 Ejemplo de Uso (cURL)

### Subir Documento
```bash
curl -X POST "http://localhost:5191/api/Document" \
  -F "File=@midocumento.pdf" \
  -F "IdInscriptionModality=38" \
  -F "IdDocumentType=5" \
  -F "Name=Mi Documento" \
  -F "Version=1.0" \
  -F "IdUserCreatedAt=1"
```

### Descargar Documento
```bash
curl "http://localhost:5191/api/Document/File/{id}" -o descarga.pdf
```
