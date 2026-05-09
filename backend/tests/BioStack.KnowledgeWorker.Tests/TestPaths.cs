namespace BioStack.KnowledgeWorker.Tests;

internal static class TestPaths
{
    public static string BackendRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "BioStack.KnowledgeWorker", "Schemas");
            if (Directory.Exists(candidate)) return dir.FullName;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate backend root from test output directory.");
    }

    public static string WorkerSchemaDirectory()
        => Path.Combine(BackendRoot(), "src", "BioStack.KnowledgeWorker", "Schemas");

    public static string FixturePath(string fileName)
        => Path.Combine(BackendRoot(), "tests", "BioStack.KnowledgeWorker.Tests", "Fixtures", fileName);
}