namespace BioStack.Domain.Entities;

public sealed class ProfileGoal
{
    public Guid Id { get; set; }
    public Guid ProfileId { get; set; }
    public PersonProfile PersonProfile { get; set; } = null!;
    public string GoalDefinitionId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
