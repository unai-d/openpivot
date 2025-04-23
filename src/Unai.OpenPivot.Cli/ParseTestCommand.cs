using System;
using System.Collections.Generic;
using System.IO;
using Spectre.Console;
using Spectre.Console.Cli;
using Unai.OpenPivot;

public class ParseTestCommand : Command<ParseTestCommand.Settings>
{
	public class Settings : CommandSettings
	{
		[CommandArgument(0, "[searchPath]")]
		public string SearchPath { get; init; }

		[CommandOption("-p|--pattern")]
		public string FilePattern { get; init; }
	}

	public record ParsingResult(string FileName)
	{
		public Exception Exception { get; set; }
	}

	public override int Execute(CommandContext context, Settings settings)
	{
		var filePattern = settings.FilePattern ?? "*";
		var searchPath = settings.SearchPath ?? Environment.CurrentDirectory;

		var files = new DirectoryInfo(searchPath).GetFiles(filePattern);

		var parseStats = new List<ParsingResult>();

		foreach (var file in files)
		{
			AnsiConsole.MarkupLine($"[green]{file}[/]");
			var pivProj = new PivFile();
			var pivFileStream = File.OpenRead(file.FullName);
			var parseStat = new ParsingResult(file.Name);
			parseStats.Add(parseStat);

			try
			{
				pivProj.Load(pivFileStream);
			}
			catch (Exception ex)
			{
				AnsiConsole.WriteException(ex);
				parseStat.Exception = ex;
			}

			pivFileStream.Dispose();
			AnsiConsole.WriteLine();
		}

		foreach (var parseStat in parseStats)
		{
			AnsiConsole.Markup($"[{(parseStat.Exception != null ? "red" : "green")}]{parseStat.FileName,-32}[/] ");
			if (parseStat.Exception != null)
			{
				AnsiConsole.WriteLine(parseStat.Exception.Message);
			}
			else
			{
				AnsiConsole.WriteLine("OK");
			}
		}

		return 0;
	}
}
