namespace Application.Shared.DTOs;

public record PaginatedRequest
{
    public int PageNumber { get; set; } = 1;

    /// <summary>Tamaño de página. Un valor &lt;= 0 significa "sin paginación": la
    /// respuesta entrega todos los registros en una sola página.</summary>
    public int PageSize { get; set; } = 10;

    public string? SortBy { get; set; }
    public bool IsDescending { get; set; } = false;
    public Dictionary<string, string>? Filters { get; set; }

    /// <summary>Paginación keyset: Id de la última fila de la página anterior.
    /// Informado (junto a CursorCreatedAt), el listado pagina por cursor con coste
    /// constante en cualquier profundidad, en lugar de OFFSET. El orden queda
    /// forzado a (CreatedAt DESC, Id DESC) para que el cursor sea estable.</summary>
    public long? CursorId { get; set; }

    /// <summary>Paginación keyset: CreatedAt de la última fila de la página anterior.</summary>
    public DateTimeOffset? CursorCreatedAt { get; set; }

    /// <summary>Omite el COUNT del total (páginas posteriores en modo keyset):
    /// el frontend conserva el total de la primera página. TotalRecords llega en -1.</summary>
    public bool SkipTotalCount { get; set; }
}
