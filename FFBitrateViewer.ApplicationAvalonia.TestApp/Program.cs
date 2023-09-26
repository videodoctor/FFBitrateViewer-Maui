// See https://aka.ms/new-console-template for more information
using System.CommandLine;
using System.Text;

public class Program
{
    private static async Task Main(string[] args)
    {
        // enabling unicode code characters for input/output
        Console.OutputEncoding = Encoding.Unicode;
        Console.InputEncoding = Encoding.Unicode;

        // echo command
        var echoCommand = new Command("echo", "Echos the first argument");
        var echoMessageArgument = new Argument<string>(name: "message", description: "Message to echo", getDefaultValue: () => string.Empty);
        echoCommand.AddArgument(echoMessageArgument);
        echoCommand.SetHandler(Console.WriteLine, echoMessageArgument);

        // exit command
        var exitCommand = new Command("exit", "Exits the application");
        var exitCodeArgument = new Argument<int>(name: "exitCode", description: "Application exit code", getDefaultValue: () => 0);
        exitCommand.AddArgument(exitCodeArgument);
        exitCommand.SetHandler(Environment.Exit, exitCodeArgument);

        // sleep command
        var sleepCommand = new Command("sleep", "Sleeps for the specified number of seconds");
        var sleepSecondsArgument = new Argument<int>(name: "seconds", description: "Number of seconds to sleep", getDefaultValue: () => 1);
        sleepCommand.AddArgument(sleepSecondsArgument);
        sleepCommand.SetHandler(async (seconds) => await Task.Delay(TimeSpan.FromSeconds(seconds)), sleepSecondsArgument);

        var rootCommand = new RootCommand {
            echoCommand,
            exitCommand,
            sleepCommand
        };
        await rootCommand.InvokeAsync(args);
    }
}