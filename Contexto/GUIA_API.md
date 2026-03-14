# Guía de Uso de la API

## 📚 Documentación Interactiva (Swagger)

La API cuenta con documentación interactiva generada automáticamente con Swagger UI.

- **URL Local**: `https://localhost:7001/swagger` (o el puerto configurado)
- **Definición JSON**: `/swagger/v1/swagger.json`

### Características
- **Exploración de Endpoints**: Ver todos los controladores y métodos disponibles.
- **Pruebas en Vivo**: Ejecutar peticiones directamente desde el navegador.
- **Esquemas**: Ver la estructura de los objetos de petición y respuesta.

---

## 🔐 Autenticación y Seguridad

El sistema utiliza **JWT (JSON Web Tokens)** para la autenticación.

### Obtener un Token
Para acceder a los endpoints protegidos, primero debe autenticarse:

1. **Endpoint**: `POST /api/Auth/Login`
2. **Body**:
   ```json
   {
     "email": "usuario@ejemplo.com",
     "password": "tu_password"
   }
   ```
3. **Respuesta**: Recibirá un token JWT.

### Usar el Token en Swagger
1. Haga clic en el botón **Authorize** (candado) en la parte superior derecha.
2. Ingrese el token con el prefijo `Bearer`:
   ```
   Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
   ```
3. Haga clic en **Authorize**. Ahora todas las peticiones incluirán el header de autorización.

### Usar el Token en Postman/cURL
Incluya el header `Authorization` en sus peticiones:
```bash
Authorization: Bearer <tu_token_jwt>
```

---

## 🛠️ Estructura de Respuestas

La API utiliza una estructura de respuesta estandarizada (wrapper) para todas las operaciones:

### Respuesta Exitosa
```json
{
  "data": { ... },      // Objeto o lista solicitada
  "succeeded": true,
  "message": null,
  "errors": null
}
```

### Respuesta de Error
```json
{
  "data": null,
  "succeeded": false,
  "message": "Descripción del error",
  "errors": [ "Detalle 1", "Detalle 2" ]
}
```

### Paginación
Para endpoints que retornan listas (`GetAll`), la respuesta incluye metadatos de paginación:
```json
{
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 5,
  "totalRecords": 50,
  "data": [ ... ]
}
```
