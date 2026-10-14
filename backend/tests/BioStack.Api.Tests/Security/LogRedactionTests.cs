namespace BioStack.Api.Tests.Security;

using System.Runtime.CompilerServices;
using BioStack.Api.Security;
using Xunit;

public sealed class LogRedactionTests
{
    private static readonly string[] ProductionLogFiles =
    [
        "backend/src/BioStack.Api/Endpoints/ContactEndpoints.cs",
        "backend/src/BioStack.Api/Auth/MagicLinkDelivery.cs",
        "backend/src/BioStack.Api/Auth/ContactMessageSender.cs",
    ];

    [Theory]
    [InlineData("User@Example.com", "example.com")]
    [InlineData("missing-at", "invalid")]
    [InlineData("", "missing")]
    public void EmailDomain_ReturnsDomainOnly(string email, string expected)
        => Assert.Equal(expected, LogRedaction.EmailDomain(email));

    [Fact]
    public void ProductionLogCalls_DoNotPassExceptionEmailDomainOrMagicLink()
    {
        foreach (var relativePath in ProductionLogFiles)
        {
            var source = ReadRepoFile(relativePath);
            var calls = ExtractLogCalls(source);
            Assert.NotEmpty(calls);
            Assert.All(calls, AssertLogCallIsRedacted);
        }
    }

    [Fact]
    public void DevelopmentInbox_KeepsMagicLinkInMemory()
    {
        var source = ReadRepoFile("backend/src/BioStack.Api/Auth/MagicLinkDelivery.cs");
        Assert.Contains(
            "new DevMagicLinkMessage(contact, magicLink, redirectPath, DateTime.UtcNow, expiresAtUtc)",
            source,
            StringComparison.Ordinal);

        var calls = ExtractLogCalls(source);
        Assert.Equal(3, calls.Count);
        Assert.DoesNotContain(calls, call => call.Contains("magicLink", StringComparison.Ordinal));
    }

    private static void AssertLogCallIsRedacted(string call)
    {
        Assert.DoesNotContain("EmailDomain", call, StringComparison.Ordinal);
        Assert.DoesNotContain("LogRedaction", call, StringComparison.Ordinal);
        Assert.DoesNotContain("magicLink", call, StringComparison.Ordinal);
        Assert.DoesNotContain("@", call, StringComparison.Ordinal);

        var arguments = SplitArguments(call);
        Assert.NotEmpty(arguments);
        var sawTemplate = false;
        foreach (var argument in arguments)
        {
            if (IsMessageTemplate(argument))
            {
                sawTemplate = true;
                AssertTemplateIsSafe(argument);
                continue;
            }

            Assert.True(
                argument is "category" or "message.Category",
                $"Log call passes a non-category argument '{argument}': {call}");
        }

        Assert.True(sawTemplate, $"Log call has no message template: {call}");
    }

    private static void AssertTemplateIsSafe(string literal)
    {
        Assert.DoesNotContain("domain", literal, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("http://", literal, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("https://", literal, StringComparison.OrdinalIgnoreCase);
        var withoutCategory = literal.Replace("{Category}", "", StringComparison.Ordinal);
        Assert.DoesNotContain("{", withoutCategory, StringComparison.Ordinal);
        Assert.DoesNotContain("}", withoutCategory, StringComparison.Ordinal);
    }

    private static bool IsMessageTemplate(string argument)
    {
        if (argument.Contains("$\"", StringComparison.Ordinal) || argument.StartsWith('$'))
        {
            return false;
        }

        return argument.StartsWith('"') || argument.StartsWith("@\"", StringComparison.Ordinal);
    }

    private static List<string> ExtractLogCalls(string source)
    {
        var calls = new List<string>();
        var markers = new[]
        {
            ".LogTrace(",
            ".LogDebug(",
            ".LogInformation(",
            ".LogWarning(",
            ".LogError(",
            ".LogCritical(",
            ".Log(",
        };

        foreach (var marker in markers)
        {
            var index = 0;
            while ((index = source.IndexOf(marker, index, StringComparison.Ordinal)) >= 0)
            {
                var open = index + marker.Length - 1;
                var end = FindCallEnd(source, open);
                calls.Add(source[(open + 1)..(end - 1)]);
                index = end;
            }
        }

        return calls;
    }

    private static int FindCallEnd(string source, int openIndex)
    {
        var depth = 0;
        var inString = false;
        var verbatim = false;
        for (var index = openIndex; index < source.Length; index++)
        {
            var current = source[index];
            if (inString)
            {
                if (verbatim && current == '"' && index + 1 < source.Length && source[index + 1] == '"')
                {
                    index++;
                    continue;
                }

                if (!verbatim && current == '\\')
                {
                    index++;
                    continue;
                }

                if (current == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (current == '"')
            {
                inString = true;
                verbatim = index > 0 && source[index - 1] == '@';
                continue;
            }

            if (current == '(')
            {
                depth++;
            }
            else if (current == ')')
            {
                depth--;
                if (depth == 0)
                {
                    return index + 1;
                }
            }
        }

        throw new InvalidOperationException("Unclosed log call.");
    }

    private static List<string> SplitArguments(string argumentList)
    {
        var arguments = new List<string>();
        var inString = false;
        var verbatim = false;
        var depth = 0;
        var start = 0;
        for (var index = 0; index < argumentList.Length; index++)
        {
            var current = argumentList[index];
            if (inString)
            {
                if (verbatim && current == '"' && index + 1 < argumentList.Length && argumentList[index + 1] == '"')
                {
                    index++;
                    continue;
                }

                if (!verbatim && current == '\\')
                {
                    index++;
                    continue;
                }

                if (current == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (current == '"')
            {
                inString = true;
                verbatim = index > 0 && argumentList[index - 1] == '@';
                continue;
            }

            if (current is '(' or '[' or '{')
            {
                depth++;
            }
            else if (current is ')' or ']' or '}')
            {
                depth--;
            }
            else if (current == ',' && depth == 0)
            {
                arguments.Add(argumentList[start..index].Trim());
                start = index + 1;
            }
        }

        var last = argumentList[start..].Trim();
        if (last.Length > 0)
        {
            arguments.Add(last);
        }

        return arguments;
    }

    private static string ReadRepoFile(string relativePath, [CallerFilePath] string thisFile = "")
    {
        var repoRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "..", ".."));
        var path = Path.Combine(repoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(path), path);
        return File.ReadAllText(path);
    }
}
