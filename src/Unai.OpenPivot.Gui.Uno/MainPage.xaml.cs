using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using SkiaSharp.Views;
using SkiaSharp;
using SkiaSharp.Views.Windows;
using Windows.Storage.Pickers;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Unai.OpenPivot.Gui.Uno;

public sealed partial class MainPage : Page
{
	private Point _currentPosition;

	public MainPage()
	{
		#if !WINDOWS
		SKSwapChainPanel.RaiseOnUnsupported = false;
		#endif

		InitializeComponent();

		#if HAS_UNO_SKIA || WINDOWS
		notSupported.Visibility = Visibility.Visible;
		#endif

		Loaded += OnLoaded;
	}

	private Visibility Not(bool? value) => (!value ?? false) ? Visibility.Visible : Visibility.Collapsed;

	private async void OnLoaded(object sender, RoutedEventArgs e)
	{
		// TODO
	}

	private void OnPaintSwapChain(object sender, SKPaintGLSurfaceEventArgs e)
	{
		var canvas = e.Surface.Canvas;
		var info = e.Info;

		Render(canvas, new Size(info.Width, info.Height));
	}

	private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
	{
		var canvas = e.Surface.Canvas;
		var info = e.Info;

		Render(canvas, new Size(info.Width, info.Height));
	}

	private void OnSurfacePointerMoved(object sender, PointerRoutedEventArgs e)
	{
		_currentPosition = e.GetCurrentPoint(panelGrid).Position;
		
		if (hwAcceleration.IsChecked ?? false)
		{
			#if !WINDOWS
			swapChain.Invalidate();
			#endif
		}
		else
		{
			canvas.Invalidate();
		}
	}

	private void Render(SKCanvas canvas, Size size)
	{
		var scale = (float)(XamlRoot?.RasterizationScale ?? 1);
		var scaledSize = new SKSize((float)size.Width / scale, (float)size.Height / scale);

		canvas.Scale(scale);

		canvas.Clear(new(255, 255, 255));

		var paint = new SKPaint
		{
			Color = SKColors.Black,
			IsAntialias = true,
			Style = SKPaintStyle.Fill,
			TextAlign = SKTextAlign.Center,
			TextSize = 24
		};
		var coord = new SKPoint(scaledSize.Width / 2, (scaledSize.Height + paint.TextSize) / 2);
		canvas.DrawText("This is a Skia canvas", coord, paint);
	}

	public async Task HandleFileOpenClick(object sender, RoutedEventArgs e)
	{
		var fileOpener = new FileOpenPicker();
		fileOpener.FileTypeFilter.Add(".piv");

		var pivFile = await fileOpener.PickSingleFileAsync();
		if (pivFile != null)
		{
			Console.Error.WriteLine($"{pivFile.DisplayName}");
			var fileStream = await pivFile.OpenReadAsync();
			var pivFileC = new Unai.OpenPivot.PivFile();
			pivFileC.Load(fileStream.AsStreamForRead());
		}
	}

	public void HandleExitClick(object sender, RoutedEventArgs e)
	{
		App.Current.Exit();
		Environment.Exit(0);
	}
}
