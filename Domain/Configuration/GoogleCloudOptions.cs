namespace Domain.Configuration
{
    /// <summary>
    /// Opciones de configuración para Google Cloud Storage
    /// </summary>
    public class GoogleCloudOptions
    {
        public string BucketName { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public string? CredentialsPath { get; set; }
    }
}
