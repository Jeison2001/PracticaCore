namespace Application.Shared.DTOs.Evaluations
{
    /// <summary>
    /// Ítem del historial de observaciones/retroalimentaciones (tabla transversal Evaluation).
    /// </summary>
    public record ObservationHistoryItemDto
    {
        public int Id { get; set; }
        public int EntityId { get; set; }
        public int IdEvaluator { get; set; }
        public string? EvaluatorName { get; set; }
        public int IdEvaluationType { get; set; }
        public string? EvaluationTypeCode { get; set; }
        public string? EvaluationTypeName { get; set; }
        public string? Result { get; set; }
        public string? Observations { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
