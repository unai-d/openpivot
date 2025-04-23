using Spectre.Console;
using Spectre.Console.Cli;
using static Unai.OpenPivot.Logger;

namespace Unai.OpenPivot.Cli;

class Program
{
	static int Main(string[] args)
	{
		EmitMessage += Utils.PrintLogMessageToConsole;

		AnsiConsole.MarkupLine("[bold]OpenPivot[/]");

		var cliApp = new CommandApp<ParseTestCommand>();
		return cliApp.Run(args);
	}
}
