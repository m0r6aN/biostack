namespace BioStack.StructuralEvaluationReportCli;

using BioStack.KnowledgeWorker.Pipeline;

public static class Program
{
    public static int Main(string[] args)
    {
        if (!TryReadRequiredOption(args, "--repository-root", out var repositoryRoot) ||
            !TryReadRequiredOption(args, "--output-directory", out var outputDirectory) ||
            args.Length != 4)
        {
            Console.Error.WriteLine(
                "Usage: BioStack.StructuralEvaluationReportCli --repository-root <path> --output-directory <path>");
            return 2;
        }

        try
        {
            var reportPath = new StructuralEvaluationReportBuilder(
                    Path.GetFullPath(repositoryRoot))
                .WriteReport(Path.GetFullPath(outputDirectory));

            Console.WriteLine($"report_path={reportPath}");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Structural evaluation report generation failed: {exception.Message}");
            return 1;
        }
    }

    private static bool TryReadRequiredOption(
        string[] args,
        string option,
        out string value)
    {
        var index = Array.IndexOf(args, option);
        if (index < 0 || index + 1 >= args.Length || string.IsNullOrWhiteSpace(args[index + 1]))
        {
            value = string.Empty;
            return false;
        }

        value = args[index + 1];
        return true;
    }
}
