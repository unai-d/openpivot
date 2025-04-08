using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Uno.Resizetizer;
using Uno.UI;

namespace Unai.OpenPivot.Gui.Uno;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected Window _mainWin;

	protected override void OnLaunched(LaunchActivatedEventArgs args)
	{
		#if HAS_UNO
		global::Uno.CompositionConfiguration.Configuration |= global::Uno.CompositionConfiguration.Options.UseBrushAntialiasing;
		#endif

		_mainWin = new Window();
		#if DEBUG
		_mainWin.UseStudio();
		#endif

		// Ensure that the window is active.
		if (_mainWin.Content is not Frame rootFrame)
		{
			rootFrame = new Frame();

			rootFrame.NavigationFailed += OnNavigationFailed;

			_mainWin.Content = rootFrame;
		}

		if (rootFrame.Content == null)
		{
			rootFrame.Navigate(typeof(MainPage), args.Arguments);
		}

		_mainWin.SetWindowIcon();
		_mainWin.Activate();
	}

	void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
	{
		throw new InvalidOperationException($"Failed to load {e.SourcePageType.FullName}: {e.Exception}");
	}

	public static void InitializeLogging()
	{
		#if DEBUG
		var factory = LoggerFactory.Create(builder =>
		{
			#if __WASM__
			builder.AddProvider(new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider());
			#elif __IOS__ || __MACCATALYST__
			builder.AddProvider(new global::Uno.Extensions.Logging.OSLogLoggerProvider());
			#else
			builder.AddConsole();
			#endif

			builder.SetMinimumLevel(LogLevel.Information);

			builder.AddFilter("Uno", LogLevel.Warning);
			builder.AddFilter("Windows", LogLevel.Warning);
			builder.AddFilter("Microsoft", LogLevel.Warning);
		});

		global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;

		#if HAS_UNO
		global::Uno.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();
		#endif
		#endif
	}
}
