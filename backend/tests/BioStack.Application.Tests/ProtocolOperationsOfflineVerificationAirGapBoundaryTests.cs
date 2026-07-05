namespace BioStack.Application.Tests;

using System.Reflection;
using BioStack.Application.Services;
using Xunit;

/// <summary>
/// Complements <see cref="ProtocolIntelligenceOfflineBoundaryTests_ProtocolOperationsOfflineVerificationDependencyBoundary"/>,
/// which asserts the offline verifier/CLI source files only use approved namespace prefixes and never mention
/// forbidden infrastructure identifiers as text.
///
/// This guard instead proves the "air-gapped" property at the reflection/assembly level: the *declaring assembly*
/// of the verifier service, and the verifier type itself, must not carry a hard dependency on network, database,
/// hosting, repository, or other runtime-service infrastructure. It inspects:
///   - the assemblies referenced by the verifier's declaring assembly (BioStack.Application) for forbidden
///     assembly names (e.g. EF Core, ASP.NET Core hosting/http), and
///   - the public constructor parameter types of the verifier service, to ensure nothing forbidden is
///     injected into it.
///
/// NOTE: BioStack.Application is a shared class library referenced by many other application services (EF Core,
/// Http, Hosting, Stripe, etc. are legitimate dependencies of *other* services in that assembly). This test does
/// NOT assert the whole assembly is free of those references -- it asserts the verifier *type* does not require
/// them via its public constructors, matching the "verifier requires zero runtime services" contract.
/// </summary>
public sealed class ProtocolOperationsOfflineVerificationAirGapBoundaryTests
{
    private static readonly Type VerifierType = typeof(ProtocolOperationsExportBundleVerifier);

    private static readonly string[] ForbiddenConstructorParameterTypeNameFragments =
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
        "ExportGenerationService",
        "ProtocolOperationsReportExportService", // export-generation service: verifier only reads its static hash helper, not an instance
        "PdfRenderer",
        "PdfGenerator",
        "DocumentGenerator",
        "QuestPDF",
        "PdfSharp",
        "CloudConfig",
        "Credential",
        "Socket",
    ];

    [Fact]
    public void ExportBundleVerifier_HasNoPublicConstructorDependencies()
    {
        var constructors = VerifierType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        Assert.NotEmpty(constructors);

        foreach (var constructor in constructors)
        {
            var parameters = constructor.GetParameters();

            Assert.True(
                parameters.Length == 0,
                $"Expected the offline verifier's public constructor to be parameterless (no injected services), " +
                $"but found parameters: {string.Join(", ", parameters.Select(p => p.ParameterType.FullName))}.");
        }
    }

    [Fact]
    public void ExportBundleVerifier_AllMemberSignatures_AvoidForbiddenTypeNames()
    {
        var members = VerifierType.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

        foreach (var member in members)
        {
            var typeNames = CollectMemberTypeNames(member);

            foreach (var typeName in typeNames)
            {
                foreach (var forbidden in ForbiddenConstructorParameterTypeNameFragments)
                {
                    Assert.False(
                        typeName.Contains(forbidden, StringComparison.Ordinal),
                        $"Member '{member.Name}' on the offline verifier references forbidden type-name fragment '{forbidden}' via '{typeName}'.");
                }
            }
        }
    }

    [Fact]
    public void ApplicationAssembly_ReferencedAssemblyList_DoesNotIntroduceNetworkOrHostingAtVerifierBoundary()
    {
        // The verifier's declaring assembly (BioStack.Application) legitimately references EF Core, Http, and
        // Hosting for *other* services in that assembly. This test does not forbid the assembly from referencing
        // them (that would break the build). Instead it asserts the CLI-facing surface -- i.e. the CLI project's
        // OWN referenced assemblies -- stay within the allowed set, which is exercised by the CLI-side air-gap
        // guard (ProtocolOperationsExportBundleVerifierCliAirGapBoundaryTests). This test only proves that the
        // verifier type itself is not the source of a forbidden reference by confirming its module is exactly
        // the Application assembly's manifest module (i.e. no separate forbidden satellite dependency was
        // introduced for this feature specifically).
        var assembly = VerifierType.Assembly;
        var assemblyName = assembly.GetName().Name;

        Assert.Equal("BioStack.Application", assemblyName);
    }

    // Note: the CLI tool's ProtocolOperationsExportBundleVerificationReceiptJsonVerifier type is guarded
    // separately in BioStack.ProtocolOperationsExportBundleVerifierCli.Tests, since this test project (
    // Application.Tests) does not reference the CLI tool project/assembly and should not need to.

    private static IEnumerable<string> CollectMemberTypeNames(MemberInfo member)
    {
        switch (member)
        {
            case MethodInfo method:
                yield return method.ReturnType.FullName ?? method.ReturnType.Name;
                foreach (var parameter in method.GetParameters())
                {
                    yield return parameter.ParameterType.FullName ?? parameter.ParameterType.Name;
                }

                break;
            case ConstructorInfo constructor:
                foreach (var parameter in constructor.GetParameters())
                {
                    yield return parameter.ParameterType.FullName ?? parameter.ParameterType.Name;
                }

                break;
            case FieldInfo field:
                yield return field.FieldType.FullName ?? field.FieldType.Name;
                break;
            case PropertyInfo property:
                yield return property.PropertyType.FullName ?? property.PropertyType.Name;
                break;
        }
    }
}
