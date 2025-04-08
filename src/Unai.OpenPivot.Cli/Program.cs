using System;
using System.Diagnostics;
using System.IO;
using Spectre.Console;
using static Unai.OpenPivot.Logger;

namespace Unai.OpenPivot.Cli;

class Program
{
	public static void PrintLogMessageToConsole(string message, LogLevel logLevel, StackFrame sf = null)
	{
		var callerClassName = sf.GetMethod().DeclaringType.Name;
		var callerName = sf.GetMethod().Name;

		var msgStyle = logLevel switch
		{
			LogLevel.Error => new Style(Color.Red),
			LogLevel.Warning => new Style(Color.Yellow),
			LogLevel.Debug => new Style(Color.Aqua),
			LogLevel.Trace => new Style(Color.Aqua, null, Decoration.Dim),
			_ => Style.Plain
		};
		var textMarkup = new Text($"{message}\n", msgStyle);
		AnsiConsole.MarkupInterpolated($"[dim][green]{callerClassName}[/].[yellow]{callerName}[/][/] ");
		AnsiConsole.Write(textMarkup);
	}

	static void Main(string[] args)
	{
		EmitMessage += PrintLogMessageToConsole;

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
		AnsiConsole.MarkupLine($"[green]{pivFilePath}[/]");
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
