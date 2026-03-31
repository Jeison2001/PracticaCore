namespace Application.Shared.DTOs.ScientificArticles
{
    public record ScientificArticleDto : BaseDto<int>
    {
        public int IdStateStage { get; set; }
        public string? ArticleTitle { get; set; }
        public string? JournalName { get; set; }
        public string? ISSN { get; set; }
        public string? JournalCategory { get; set; }
        public DateTimeOffset? AcceptanceDate { get; set; }
        public string? Observations { get; set; }
    }
}

