namespace BioStack.Contracts.Requests;

public sealed record CreateCheckInRequest(
    DateTime Date,
    decimal Weight,
    int SleepQuality,
    int Energy,
    int Appetite,
    int Recovery,
    int? Focus = null,
    int? ThoughtClarity = null,
    int? SkinQuality = null,
    int? DigestiveHealth = null,
    int? Strength = null,
    int? Endurance = null,
    int? JointPain = null,
    int? Eyesight = null,
    string SideEffects = "",
    string[]? PhotoUrls = null,
    string GiSymptoms = "",
    string Mood = "",
    string Notes = ""
);
