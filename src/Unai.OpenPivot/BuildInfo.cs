using System;
using System.Reflection;

namespace Unai.OpenPivot;

public static class BuildInfo
{
	public static string VersionString { get; set; } = "";
	public static string VersionMetadata { get; set; } = "";

	static BuildInfo()
	{
		try
		{
			var iverAttr = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>();
			var iverStr = iverAttr.InformationalVersion.Split('+', 2);
			VersionString = iverStr[0];
			if (iverStr.Length > 1) VersionMetadata = iverStr[1];
		}
		catch {}
	}
}
