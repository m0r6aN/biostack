namespace BioStack.Api.Endpoints;

using System.Security.Cryptography;
using System.Text;
using BioStack.Application.Services;
using BioStack.Contracts.Requests;
using Microsoft.AspNetCore.Http.Features;

public static class AnalyzeEndpoints
{
    private const long MaxUploadBytes = 12 * 1024 * 1024;

    public static void MapAnalyzeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/analyze")
            .WithTags("Analyze");

        group.MapPost("/protocol", AnalyzeProtocol)
            .WithName("AnalyzeProtocol");
    }

    private static async Task<IResult> AnalyzeProtocol(
        HttpRequest httpRequest,
        IProtocolAnalyzerService analyzerService,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        AnalyzeProtocolRequest request;
        ProtocolIngestionRequest ingestionRequest;

        try
        {
            (request, ingestionRequest) = await ParseRequestAsync(httpRequest, ct);
        }
        catch (ProtocolIngestionException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }

        var logger = loggerFactory.CreateLogger("ProtocolAnalyzer");
        logger.LogInformation(
            "Protocol analyzer input received. InputType={InputType} InputHash={InputHash} CharacterCount={CharacterCount}",
            request.InputType,
            HashInput(ingestionRequest.InputText ?? ingestionRequest.LinkUrl ?? request.SourceName ?? "binary"),
            ingestionRequest.SourceBytes?.Length ?? ingestionRequest.InputText?.Length ?? 0);

        try
        {
            var result = await analyzerService.AnalyzeAsync(request, ingestionRequest, ct);
            return Results.Ok(result);
        }
        catch (ProtocolIngestionException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static string HashInput(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input.Trim()));
        return Convert.ToHexString(bytes)[..16];
    }

    private static async Task<(AnalyzeProtocolRequest Request, ProtocolIngestionRequest Ingestion)> ParseRequestAsync(HttpRequest httpRequest, CancellationToken cancellationToken)
    {
        if (httpRequest.HasFormContentType)
        {
            var form = await httpRequest.ReadFormAsync(cancellationToken);
            var file = form.Files["file"];
            if (file is null)
            {
                throw new ProtocolIngestionException("Select a file or capture an image before analyzing.");
            }

            if (file.Length == 0)
            {
                throw new ProtocolIngestionException("The selected file was empty.");
            }

            if (file.Length > MaxUploadBytes)
            {
                throw new ProtocolIngestionException("That file is too large for the analyzer right now. Keep uploads under 12 MB.");
            }

            await using var stream = file.OpenReadStream();
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, cancellationToken);

            var inputType = ParseInputType(form["inputType"], ProtocolInputType.FileUpload);
            var analyzeRequest = new AnalyzeProtocolRequest(
                inputType,
                null,
                null,
                file.FileName,
                EmptyToNull(form["goal"]),
                EmptyToNull(form["sex"]),
                ParseNullableInt(form["age"]),
                ParseNullableDouble(form["weight"]),
                ParseNullableInt(form["maxCompounds"]),
                null,
                null,
                null);

            var ingestion = new ProtocolIngestionRequest(
                inputType,
                null,
                null,
                file.FileName,
                file.ContentType,
                memory.ToArray());

            return (analyzeRequest, ingestion);
        }

        var request = await httpRequest.ReadFromJsonAsync<AnalyzeProtocolRequest>(cancellationToken: cancellationToken)
            ?? throw new ProtocolIngestionException("Analyzer input is required.");

        if (request.InputType == ProtocolInputType.Link)
        {
            if (string.IsNullOrWhiteSpace(request.LinkUrl))
            {
                throw new ProtocolIngestionException("A shared document link is required for link analysis.");
            }

            return (request, new ProtocolIngestionRequest(request.InputType, null, request.LinkUrl, request.SourceName, null, null));
        }

        if (string.IsNullOrWhiteSpace(request.InputText))
        {
            throw new ProtocolIngestionException("Protocol text is required.");
        }

        return (request, new ProtocolIngestionRequest(request.InputType, request.InputText, null, request.SourceName, "text/plain", null));
    }

    private static ProtocolInputType ParseInputType(string? value, ProtocolInputType fallback)
    {
        return Enum.TryParse<ProtocolInputType>(value, ignoreCase: true, out var parsed) ? parsed : fallback;
    }

    private static int? ParseNullableInt(string? value)
    {
        return int.TryParse(value, out var parsed) ? parsed : null;
    }

    private static double? ParseNullableDouble(string? value)
    {
        return double.TryParse(value, out var parsed) ? parsed : null;
    }

    private static string? EmptyToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
