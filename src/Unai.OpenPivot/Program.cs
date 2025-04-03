using System;
using System.IO;

namespace Unai.OpenPivot;

class Program
{
	static void Main(string[] args)
	{
		Logger.EmitMessage += Logger.PrintLogMessageToConsole;

		string pivDirPath = "../../samples";

		foreach (var pivFilePath in Directory.GetFileSystemEntries(pivDirPath, args.Length > 0 ? args[0] : "*.piv"))
		{
			Logger.Info($"\x1b[92m{pivFilePath}\x1b[0m");
			try
			{
				var pivProj = new PivFile();
				pivProj.Load(File.OpenRead(pivFilePath));
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}
	}
}