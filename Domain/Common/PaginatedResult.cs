namespace Domain.Common
{
    public record PaginatedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int TotalRecords { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalRecords / (double)PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        /// <summary>Solo paginación keyset: true si la consulta trajo pageSize+1 filas
        /// (hay más páginas sin ejecutar COUNT). Null en modo OFFSET clásico.</summary>
        public bool? HasMoreRows { get; set; }
    }
}