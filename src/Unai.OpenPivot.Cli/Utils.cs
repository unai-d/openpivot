using System.Diagnostics;
using Spectre.Console;
using static Unai.OpenPivot.Logger;

namespace Unai.OpenPivot.Cli;

public static class Utils
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
		AnsiConsole.MarkupInterpolated($"[green]{callerClassName}[/].[yellow]{callerName}[/] ");
		AnsiConsole.Write(textMarkup);
	}
}