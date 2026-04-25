namespace BioStack.Contracts.Requests;

using System.Text.Json.Serialization;

public enum ProtocolInputType
{
    Paste = 0,
    FileUpload = 1,
    CameraScan = 2,
    Link = 3
}

[method: JsonConstructor]
public sealed record AnalyzeProtocolRequest(
    ProtocolInputType InputType = ProtocolInputType.Paste,
    string? InputText = null,
    string? LinkUrl = null,
    string? SourceName = null,
    string? Goal = null,
    string? Sex = null,
    int? Age = null,
    double? Weight = null,
    int? MaxCompounds = null,
    List<string>? RequiredCompoundIds = null,
    List<string>? ExcludedCompoundIds = null,
    List<string>? ExistingStackContext = null)
{
    public AnalyzeProtocolRequest(string inputText)
        : this(ProtocolInputType.Paste, inputText)
    {
    }
}
