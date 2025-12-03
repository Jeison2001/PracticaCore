using Domain.Entities;

namespace Domain.Common.ScientificArticle
{
    public class ScientificArticleWithDetails
    {
        public Domain.Entities.ScientificArticle ScientificArticle { get; set; } = null!;
        public InscriptionModality? InscriptionModality { get; set; }
        public StateStage? StateStage { get; set; }
        public StageModality? StageModality { get; set; }
        public Modality? Modality { get; set; }
        public StateInscription? StateInscription { get; set; }
        public AcademicPeriod? AcademicPeriod { get; set; }
        public List<UserInscriptionModality> UserInscriptionModalities { get; set; } = new();
        public List<TeachingAssignment> TeachingAssignments { get; set; } = new();
        public List<Document> Documents { get; set; } = new();
    }
}
