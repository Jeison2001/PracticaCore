namespace Application.Shared.DTOs.ScientificArticles
{
    public record ScientificArticlePatchDto
    {
        public int? IdStateStage { get; set; }
        public string? Observations { get; set; }
    }
}
