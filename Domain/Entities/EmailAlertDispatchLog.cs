namespace Domain.Entities
{
    public class EmailAlertDispatchLog : BaseEntity<int>
    {
        public int IdInscriptionModality { get; set; }
        public string AlertCode { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public string StageStateSnapshot { get; set; } = string.Empty;
        public string DeduplicationKey { get; set; } = string.Empty;
        public string? RecipientsSummary { get; set; }
        public int BusinessDaysElapsed { get; set; }

        public virtual InscriptionModality? InscriptionModality { get; set; }
    }
}
