// filepath: c:\Users\LENOVO\source\repos\PracticaCore\Application\Shared\DTOs\StateInscription\StateInscriptionDto.cs
namespace Application.Shared.DTOs.StateInscriptions
{
    public record StateInscriptionDto : BaseDto<int>
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsInitialState { get; set; } = false;
        public bool IsFinalStateForStage { get; set; } = false;
    }
}
