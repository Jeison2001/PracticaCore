# Ejemplo de uso de filtros avanzados

El sistema soporta filtrado avanzado mediante operadores en los parámetros de consulta.

## Operadores disponibles

Formato: `Filters[propiedad@operador]=valor`

| Operador | Descripción | Ejemplo |
|----------|-------------|---------|
| eq | Igual a (default) | `Filters[Name]=John` o `Filters[Name@eq]=John` |
| ne | No igual a | `Filters[Name@ne]=John` |
| gt | Mayor que | `Filters[Price@gt]=100` |
| ge | Mayor o igual que | `Filters[Price@ge]=100` |
| lt | Menor que | `Filters[Price@lt]=100` |
| le | Menor o igual que | `Filters[Price@le]=100` |
| like | Contiene (para texto) | `Filters[Name@like]=oh` |
| startswith | Comienza con (para texto) | `Filters[Name@startswith]=Jo` |
| endswith | Termina con (para texto) | `Filters[Name@endswith]=hn` |

## Ejemplos de uso

- Obtener todos los productos con precio mayor a 50:
  ```
  GET /api/products?Filters[Price@gt]=50
  ```

- Obtener todos los productos con nombre que contenga "phone":
  ```
  GET /api/products?Filters[Name@like]=phone
  ```

- Combinar múltiples filtros (AND lógico):
  ```
  GET /api/products?Filters[Price@gt]=50&Filters[Category]=Electronics
  ```

- Filtrar por fecha:
  ```
  GET /api/products?Filters[CreatedDate@gt]=2023-01-01
  ```