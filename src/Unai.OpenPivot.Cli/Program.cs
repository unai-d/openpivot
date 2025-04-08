using System;
using System.IO;
using Spectre.Console;

namespace Unai.OpenPivot.Cli;

class Program
{
	static void Main(string[] args)
	{
		Logger.EmitMessage += Logger.PrintLogMessageToConsole;

		AnsiConsole.MarkupLine("[bold]OpenPivot[/]");

		if (args.Length < 1)
		{
			AnsiConsole.MarkupLine($"[bold]Usage[/]: Unai.OpenPivot.Cli [yellow]<piv_file_path>[/]");
			AnsiConsole.MarkupLine($"  File path can also be a glob pattern (e.g.: [white on gray]C:\\*.piv[/])");
			return;
		}

		if (args[0].Contains('*')) // FIXME: only works with relative paths
		{
			foreach (var pivFilePath in Directory.GetFileSystemEntries(".", args[0]))
			{
				ProcessPivFile(pivFilePath);
			}
		}
		else if (Directory.Exists(args[0]))
		{
			foreach (var pivFilePath in Directory.GetFileSystemEntries(args[0]))
			{
				ProcessPivFile(pivFilePath);
			}
		}
		else
		{
			ProcessPivFile(args[0]);
		}
	}

	static void ProcessPivFile(string pivFilePath)
	{
		AnsiConsole.Write($"[green]{pivFilePath}[/]");
		try
		{
			var pivProj = new PivFile();
			pivProj.Load(File.OpenRead(pivFilePath));
		}
		catch (Exception ex)
		{
			AnsiConsole.WriteException(ex);
		}
	}
}
