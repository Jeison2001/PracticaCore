namespace Domain.Configuration
{
    /// <summary>
    /// Opciones de configuración para Azure Blob Storage
    /// </summary>
    public class AzureBlobOptions
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string ContainerName { get; set; } = string.Empty;
        public string? BaseUrl { get; set; }
    }
}
