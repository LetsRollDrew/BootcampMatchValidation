namespace TftStreamChecker.Logging;

public class ConsoleLogger
{
    public void Info(string message) => Console.WriteLine(message);
    public void Verbose(string message)
    {
        // Verbose logging
        Console.WriteLine(message);
    }
}
