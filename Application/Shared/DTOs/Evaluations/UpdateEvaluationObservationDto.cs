namespace Application.Shared.DTOs.Evaluations
{
    /// <summary>
    /// Payload para editar el texto de una observación existente mientras la entidad siga abierta.
    /// </summary>
    public record UpdateEvaluationObservationDto
    {
        public string Observations { get; set; } = string.Empty;
    }
}
