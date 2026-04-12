namespace BioStack.Domain.Entities;

public sealed class ProtocolItem
{
    public Guid Id { get; set; }
    public Guid ProtocolId { get; set; }
    public Guid CompoundRecordId { get; set; }
    public Guid? CalculatorResultId { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string CompoundNameSnapshot { get; set; } = string.Empty;
    public string CompoundCategorySnapshot { get; set; } = string.Empty;
    public DateTime? CompoundStartDateSnapshot { get; set; }
    public DateTime? CompoundEndDateSnapshot { get; set; }
    public string CompoundStatusSnapshot { get; set; } = string.Empty;
    public string CompoundNotesSnapshot { get; set; } = string.Empty;
    public string CompoundGoalSnapshot { get; set; } = string.Empty;
    public string CompoundSourceSnapshot { get; set; } = string.Empty;
    public decimal? CompoundPricePaidSnapshot { get; set; }

    public Protocol? Protocol { get; set; }
    public CompoundRecord? CompoundRecord { get; set; }
}
