using System;
using System.Diagnostics;

namespace Unai.OpenPivot;

public static class Logger
{
	public enum LogLevel
	{
		Error,
		Warning,
		Info,
		Debug,
		Trace,
	}

	public static event Action<string, LogLevel, StackFrame> EmitMessage;

	public static void Log(string message, LogLevel logLevel = LogLevel.Info)
	{
		var sf = new StackFrame(2);
		EmitMessage?.Invoke(message, logLevel, sf);
	}

	public static void Error(string message)
	{
		Log(message, LogLevel.Error);
	}

	public static void Warning(string message)
	{
		Log(message, LogLevel.Warning);
	}

	public static void Info(string message)
	{
		Log(message, LogLevel.Info);
	}

	[Conditional("DEBUG")]
	public static void Debug(string message)
	{
		Log(message, LogLevel.Debug);
	}

	[Conditional("DEBUG")]
	public static void Trace(string message)
	{
		Log(message, LogLevel.Trace);
	}

	public static void PrintLogMessageToConsole(string message, LogLevel logLevel, StackFrame sf = null)
	{
		Console.WriteLine(message);
	}
}
