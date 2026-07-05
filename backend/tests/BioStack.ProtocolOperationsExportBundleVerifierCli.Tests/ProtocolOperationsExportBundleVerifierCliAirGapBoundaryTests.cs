namespace BioStack.ProtocolOperationsExportBundleVerifierCli.Tests;

using System.Reflection;
using Xunit;

/// <summary>
/// Air-gapped execution boundary guard for the offline export-bundle verifier CLI.
///
/// This complements <c>ProtocolIntelligenceOfflineBoundaryTests_ProtocolOperationsOfflineVerificationDependencyBoundary</c>
/// in BioStack.Application.Tests (which text-scans the CLI/verifier source files for forbidden identifiers and
/// approved <c>using</c> prefixes). That guard proves the source *text* stays clean. This guard proves the same
/// thing at the compiled-assembly level, so a violation can't slip in indirectly (e.g. through a fully-qualified
/// type reference that never appears as a `using`, or a project reference added to the .csproj without any
/// corresponding source change).
///
/// It asserts:
///   1. The CLI's own referenced assemblies (from its .csproj / compiled manifest) are limited to the allowed
///      set: BCL/System.*, BioStack.Contracts, and BioStack.Application (the verifier's home assembly).
///      No EF Core, ASP.NET Core hosting/http, or cloud-SDK assemblies are referenced.
///   2. The CLI entry point / orchestrator type's public constructors take no injected service dependencies
///      (it is designed to run as a static Main with explicit TextWriter args only -- no DI container).
///   3. None of the CLI's public method signatures (Program.Main, ProtocolOperationsExportBundleVerifierCli.Run)
///      mention forbidden infrastructure types.
/// </summary>
public sealed class ProtocolOperationsExportBundleVerifierCliAirGapBoundaryTests
{
    private static readonly string[] AllowedReferencedAssemblyPrefixes =
    [
        "System",
        "netstandard",
        "mscorlib",
        "BioStack.Contracts",
        "BioStack.Application",
    ];

    private static readonly string[] ForbiddenReferencedAssemblyNameFragments =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "Microsoft.Extensions.Http",
        "Microsoft.Extensions.Hosting",
        "Microsoft.Extensions.DependencyInjection",
        "Stripe",
        "IdentityModel",
        "Azure.",
        "AWSSDK",
        "Npgsql",
        "Microsoft.Data.SqlClient",
    ];

    private static readonly string[] ForbiddenMemberTypeNameFragments =
    [
        "HttpClient",
        "IHttpClientFactory",
        "DbContext",
        "Repository",
        "IHostedService",
        "BackgroundService",
        "ProtocolIntelligenceService",
        "TranscriptIntakeService",
        "TranscriptService",
        "IntakeService",
        "PdfRenderer",
        "PdfGenerator",
        "DocumentGenerator",
        "QuestPDF",
        "PdfSharp",
        "CloudConfig",
        "Credential",
    ];

    [Fact]
    public void CliAssembly_ReferencedAssemblies_StayWithinAllowedSet()
    {
        var cliAssembly = typeof(ProtocolOperationsExportBundleVerifierCli).Assembly;
        var referenced = cliAssembly.GetReferencedAssemblies();

        foreach (var reference in referenced)
        {
            var name = reference.Name ?? string.Empty;

            Assert.True(
                AllowedReferencedAssemblyPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)),
                $"CLI assembly references '{name}', which is not in the allowed set (System.*, BioStack.Contracts, BioStack.Application).");

            foreach (var forbiddenFragment in ForbiddenReferencedAssemblyNameFragments)
            {
                Assert.False(
                    name.Contains(forbiddenFragment, StringComparison.OrdinalIgnoreCase),
                    $"CLI assembly references '{name}', which matches forbidden fragment '{forbiddenFragment}'.");
            }
        }
    }

    [Fact]
    public void CliOrchestratorType_HasNoInstanceConstructors_AndNoInjectedServiceDependencies()
    {
        var cliType = typeof(ProtocolOperationsExportBundleVerifierCli);

        // The CLI orchestrator is a static class: no constructors, no DI container, nothing to instantiate.
        Assert.True(cliType.IsAbstract && cliType.IsSealed, "Expected the CLI orchestrator to be a static class.");

        var programType = typeof(Program);
        Assert.True(programType.IsAbstract && programType.IsSealed, "Expected Program to be a static class.");
    }

    [Fact]
    public void CliOrchestratorType_PublicMethodSignatures_AvoidForbiddenInfrastructureTypes()
    {
        var typesToScan = new[]
        {
            typeof(Program),
            typeof(ProtocolOperationsExportBundleVerifierCli),
        };

        foreach (var type in typesToScan)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            foreach (var method in methods)
            {
                var typeNames = new List<string> { method.ReturnType.FullName ?? method.ReturnType.Name };
                typeNames.AddRange(method.GetParameters().Select(p => p.ParameterType.FullName ?? p.ParameterType.Name));

                foreach (var typeName in typeNames)
                {
                    foreach (var forbidden in ForbiddenMemberTypeNameFragments)
                    {
                        Assert.False(
                            typeName.Contains(forbidden, StringComparison.Ordinal),
                            $"Method '{type.Name}.{method.Name}' references forbidden type-name fragment '{forbidden}' via '{typeName}'.");
                    }
                }
            }
        }
    }

    [Fact]
    public void ReceiptJsonVerifierType_IsStatic_WithNoInstanceStateOrInjectedDependencies()
    {
        // ProtocolOperationsExportBundleVerificationReceiptJsonVerifier is an internal static class; reflect via
        // Assembly.GetType (works across assembly boundaries for internal types, unlike typeof) to confirm it
        // has no instance constructors and therefore cannot carry an injected forbidden dependency.
        var cliAssembly = typeof(ProtocolOperationsExportBundleVerifierCli).Assembly;
        var receiptVerifierType = cliAssembly.GetType(
            "BioStack.ProtocolOperationsExportBundleVerifierCli.ProtocolOperationsExportBundleVerificationReceiptJsonVerifier");

        Assert.NotNull(receiptVerifierType);
        Assert.True(
            receiptVerifierType!.IsAbstract && receiptVerifierType.IsSealed,
            "Expected the receipt JSON verifier to remain a static class (no instance state, no constructor-injected dependencies).");
    }

    [Fact]
    public void CliProjectFile_OnlyReferencesAllowedProjects()
    {
        var root = RepositoryRoot();
        var csprojPath = Path.Combine(
            root,
            "backend",
            "tools",
            "BioStack.ProtocolOperationsExportBundleVerifierCli",
            "BioStack.ProtocolOperationsExportBundleVerifierCli.csproj");

        Assert.True(File.Exists(csprojPath), $"Expected '{csprojPath}' to exist.");

        var text = File.ReadAllText(csprojPath);

        // The CLI must only depend on the Application project (home of the verifier service) and Contracts.
        // It must not reference Infrastructure, Domain-with-persistence-extensions, Cognition, or any web/API project.
        Assert.Contains("BioStack.Application.csproj", text, StringComparison.Ordinal);
        Assert.Contains("BioStack.Contracts.csproj", text, StringComparison.Ordinal);

        var forbiddenProjectReferenceFragments = new[]
        {
            "BioStack.Infrastructure.csproj",
            "BioStack.Api",
            "BioStack.Cognition.csproj",
            "BioStack.Web",
        };

        foreach (var forbidden in forbiddenProjectReferenceFragments)
        {
            Assert.DoesNotContain(forbidden, text, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("PackageReference", text, StringComparison.Ordinal);
    }

    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "backend", "BioStack.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate BioStack repository root.");
    }
}
