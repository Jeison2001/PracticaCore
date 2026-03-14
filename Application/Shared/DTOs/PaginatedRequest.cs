namespace Application.Shared.DTOs;

public record PaginatedRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; }
    public bool IsDescending { get; set; } = false;
    public Dictionary<string, string>? Filters { get; set; }
}
