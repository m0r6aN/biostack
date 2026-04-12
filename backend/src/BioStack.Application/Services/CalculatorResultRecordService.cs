namespace BioStack.Application.Services;

using System.Text.Json;
using BioStack.Contracts.Requests;
using BioStack.Contracts.Responses;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Repositories;

public sealed class CalculatorResultRecordService : ICalculatorResultRecordService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ICalculatorResultRecordRepository _calculatorResultRepository;
    private readonly ICompoundRecordRepository _compoundRepository;
    private readonly IPersonProfileRepository _profileRepository;

    public CalculatorResultRecordService(
        ICalculatorResultRecordRepository calculatorResultRepository,
        ICompoundRecordRepository compoundRepository,
        IPersonProfileRepository profileRepository)
    {
        _calculatorResultRepository = calculatorResultRepository;
        _compoundRepository = compoundRepository;
        _profileRepository = profileRepository;
    }

    public async Task<CalculatorResultRecordResponse> SaveAsync(Guid personId, SaveCalculatorResultRequest request, CancellationToken cancellationToken = default)
    {
        var profile = await _profileRepository.GetByIdAsync(personId, cancellationToken);
        if (profile is null)
            throw new InvalidOperationException($"Profile with ID {personId} not found");

        if (request.CompoundRecordId.HasValue)
        {
            await EnsureCompoundBelongsToProfileAsync(personId, request.CompoundRecordId.Value, cancellationToken);
        }

        var record = new CalculatorResultRecord
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            CompoundRecordId = request.CompoundRecordId,
            CalculatorKind = request.CalculatorKind,
            InputsJson = JsonSerializer.Serialize(request.Inputs, JsonOptions),
            OutputsJson = JsonSerializer.Serialize(request.Outputs, JsonOptions),
            Unit = request.Unit,
            Formula = request.Formula,
            DisplaySummary = request.DisplaySummary,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await _calculatorResultRepository.AddAsync(record, cancellationToken);
        await _calculatorResultRepository.SaveChangesAsync(cancellationToken);

        return MapToResponse(record);
    }

    public async Task<IEnumerable<CalculatorResultRecordResponse>> GetByProfileAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var records = await _calculatorResultRepository.GetByPersonIdAsync(personId, cancellationToken);
        return records.Select(MapToResponse);
    }

    public async Task<CalculatorResultRecordResponse> AttachAsync(Guid personId, Guid resultId, AttachCalculatorResultRequest request, CancellationToken cancellationToken = default)
    {
        var record = await _calculatorResultRepository.GetByIdAsync(resultId, cancellationToken);
        if (record is null || record.PersonId != personId)
            throw new InvalidOperationException($"Calculator result with ID {resultId} not found");

        await EnsureCompoundBelongsToProfileAsync(personId, request.CompoundRecordId, cancellationToken);

        record.CompoundRecordId = request.CompoundRecordId;
        record.UpdatedAtUtc = DateTime.UtcNow;

        await _calculatorResultRepository.UpdateAsync(record, cancellationToken);
        await _calculatorResultRepository.SaveChangesAsync(cancellationToken);

        return MapToResponse(record);
    }

    private async Task EnsureCompoundBelongsToProfileAsync(Guid personId, Guid compoundRecordId, CancellationToken cancellationToken)
    {
        var compound = await _compoundRepository.GetByIdAsync(compoundRecordId, cancellationToken);
        if (compound is null || compound.PersonId != personId)
            throw new InvalidOperationException($"Compound with ID {compoundRecordId} not found");
    }

    private static CalculatorResultRecordResponse MapToResponse(CalculatorResultRecord record)
    {
        return new CalculatorResultRecordResponse(
            record.Id,
            record.PersonId,
            record.CompoundRecordId,
            record.CalculatorKind,
            DeserializeDictionary(record.InputsJson),
            DeserializeDictionary(record.OutputsJson),
            record.Unit,
            record.Formula,
            record.DisplaySummary,
            record.CreatedAtUtc,
            record.UpdatedAtUtc
        );
    }

    private static Dictionary<string, string> DeserializeDictionary(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, string>();
        }

        return JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions)
            ?? new Dictionary<string, string>();
    }
}

public interface ICalculatorResultRecordService
{
    Task<CalculatorResultRecordResponse> SaveAsync(Guid personId, SaveCalculatorResultRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<CalculatorResultRecordResponse>> GetByProfileAsync(Guid personId, CancellationToken cancellationToken = default);
    Task<CalculatorResultRecordResponse> AttachAsync(Guid personId, Guid resultId, AttachCalculatorResultRequest request, CancellationToken cancellationToken = default);
}
