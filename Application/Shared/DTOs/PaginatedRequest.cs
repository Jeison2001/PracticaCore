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
}
