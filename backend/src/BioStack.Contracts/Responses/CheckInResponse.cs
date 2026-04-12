namespace BioStack.Contracts.Responses;

public sealed record CheckInResponse(
    Guid Id,
    Guid PersonId,
    Guid? ProtocolRunId,
    DateTime Date,
    decimal Weight,
    int SleepQuality,
    int Energy,
    int Appetite,
    int Recovery,
    int? Focus,
    int? ThoughtClarity,
    int? SkinQuality,
    int? DigestiveHealth,
    int? Strength,
    int? Endurance,
    int? JointPain,
    int? Eyesight,
    string SideEffects,
    string[] TaggedPhotoUrls,
    string GiSymptoms,
    string Mood,
    string Notes,
    DateTime CreatedAtUtc
);
