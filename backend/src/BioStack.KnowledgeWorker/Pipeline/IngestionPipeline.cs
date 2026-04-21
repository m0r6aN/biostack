namespace BioStack.KnowledgeWorker.Pipeline;

using System.Text.Json;
using BioStack.Domain.Entities;
using BioStack.KnowledgeWorker.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// One stage outcome: either a successfully prepared <see cref="KnowledgeEntry"/>
/// (with its source <see cref="SubstanceRecord"/>) or a rejection with reasons.
/// </summary>
public sealed record PreparedRecord(
    int                 SourceIndex,
    SubstanceRecord     Record,
    KnowledgeEntry      Entry,
    TrustClass          TrustClass,
    IReadOnlyList<string> StrippedFields,
    IReadOnlyList<string> ReviewReasons);

public sealed record RejectedRecord(
    int                         SourceIndex,
    string                      CanonicalNameOrEmpty,
    IReadOnlyList<ValidationError> Errors);

public sealed record PipelineResult(
    IReadOnlyList<PreparedRecord> Accepted,
    IReadOnlyList<RejectedRecord> Rejected);

/// <summary>
/// Executes the full load → validate → deserialize → normalize → trust-gate →
/// canonicalize sequence. Does NOT touch the database; persistence is the
/// responsibility of the calling job. This keeps the pipeline testable in
/// isolation against any seed file.
/// </summary>
public interface IIngestionPipeline
{
    PipelineResult Run(string seedFilePath);
}

public sealed class IngestionPipeline : IIngestionPipeline
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = false,
    };

    private readonly ISubstanceRecordLoader     _loader;
    private readonly ISubstanceRecordValidator  _validator;
    private readonly ISubstanceRecordNormalizer _normalizer;
    private readonly ITrustGate                 _trustGate;
    private readonly ISubstanceCanonicalizer    _canonicalizer;
    private readonly ILogger<IngestionPipeline> _logger;

    public IngestionPipeline(
        ISubstanceRecordLoader     loader,
        ISubstanceRecordValidator  validator,
        ISubstanceRecordNormalizer normalizer,
        ITrustGate                 trustGate,
        ISubstanceCanonicalizer    canonicalizer,
        ILogger<IngestionPipeline> logger)
    {
        _loader        = loader;
        _validator     = validator;
        _normalizer    = normalizer;
        _trustGate     = trustGate;
        _canonicalizer = canonicalizer;
        _logger        = logger;
    }

    public PipelineResult Run(string seedFilePath)
    {
        var loaded = _loader.Load(seedFilePath);
        _logger.LogInformation("[Pipeline] Loaded {Count} raw record(s) from {Path}", loaded.Count, seedFilePath);

        var accepted = new List<PreparedRecord>(loaded.Count);
        var rejected = new List<RejectedRecord>();

        foreach (var raw in loaded)
        {
            var canonicalNameHint = TryReadCanonicalName(raw);

            // 1. Schema validation — reject honestly before any mapping.
            var validation = _validator.Validate(raw.Node);
            if (!validation.IsValid)
            {
                _logger.LogWarning(
                    "[Pipeline] REJECT idx={Idx} name='{Name}': {Summary}",
                    raw.Index, canonicalNameHint, validation.Summary());
                rejected.Add(new RejectedRecord(raw.Index, canonicalNameHint, validation.Errors));
                continue;
            }

            // 2. Deserialize to the typed model.
            SubstanceRecord typed;
            try
            {
                typed = raw.Node.Deserialize<SubstanceRecord>(JsonOpts)
                        ?? throw new InvalidOperationException("Deserializer returned null.");
            }
            catch (Exception ex)
            {
                var err = new ValidationError("/", "deserialization", ex.Message);
                _logger.LogWarning("[Pipeline] REJECT idx={Idx} name='{Name}': {Err}", raw.Index, canonicalNameHint, err);
                rejected.Add(new RejectedRecord(raw.Index, canonicalNameHint, new[] { err }));
                continue;
            }

            // 3. Normalize — idempotent mutations on the typed instance.
            _normalizer.Normalize(typed);

            // 4. Trust gate — strip ClassA-only fields when record is ClassB.
            var gated = _trustGate.Apply(typed);

            // 5. Canonicalize — project into the persisted KnowledgeEntry shape.
            var entry = _canonicalizer.ToKnowledgeEntry(gated.Record);

            accepted.Add(new PreparedRecord(
                SourceIndex:    raw.Index,
                Record:         gated.Record,
                Entry:          entry,
                TrustClass:     gated.RecordClass,
                StrippedFields: gated.StrippedFields,
                ReviewReasons:  gated.ReviewReasons));

            _logger.LogInformation(
                "[Pipeline] ACCEPT idx={Idx} name='{Name}' class={Class} stripped={Stripped}",
                raw.Index, canonicalNameHint, gated.RecordClass, gated.StrippedFields.Count);
        }

        _logger.LogInformation(
            "[Pipeline] Completed — Accepted={Accepted} Rejected={Rejected}",
            accepted.Count, rejected.Count);

        return new PipelineResult(accepted, rejected);
    }

    private static string TryReadCanonicalName(LoadedRecord raw)
    {
        try
        {
            return raw.Node["identity"]?["canonicalName"]?.GetValue<string>() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
