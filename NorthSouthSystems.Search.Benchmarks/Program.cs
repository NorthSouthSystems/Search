using BenchmarkDotNet.Running;

internal class Program
{
    internal const string StackExchangeDirectory = @"C:\StackExchange\2023-06-14";
    internal const string StackExchangeSite = "stackoverflow.com";

    private static void Main(string[] args) =>
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
