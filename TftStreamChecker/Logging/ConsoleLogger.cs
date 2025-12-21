namespace TftStreamChecker.Logging;

public class ConsoleLogger
{
    private readonly bool _verbose;

    public ConsoleLogger(bool verbose)
    {
        _verbose = verbose;
    }

    public void Info(string message) => Console.WriteLine(message);

    public void Error(string message) => Console.Error.WriteLine(message);

    public void Verbose(string message)
    {
        if (_verbose) Console.WriteLine(message);
    }
}
