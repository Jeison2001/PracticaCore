namespace Application.Shared.DTOs.RequiredDocumentsByState
{
    public class RequiredDocumentsByStateDto : BaseDto<int>
    {
        public int DocumentTypeId { get; set; }
        public string DocumentCode { get; set; } = string.Empty;
        public string DocumentName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsRequired { get; set; }
        public int? OrderDisplay { get; set; }
        public string DocumentClassName { get; set; } = string.Empty;
        public string RequiredForState { get; set; } = string.Empty;
        public string StateName { get; set; } = string.Empty;
        public new int? IdUserCreatedAt { get; set; }

    }
}