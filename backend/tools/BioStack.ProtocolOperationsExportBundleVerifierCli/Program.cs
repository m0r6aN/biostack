namespace BioStack.ProtocolOperationsExportBundleVerifierCli;

public static class Program
{
    public static int Main(string[] args) =>
        ProtocolOperationsExportBundleVerifierCli.Run(args, Console.Out, Console.Error);
}
