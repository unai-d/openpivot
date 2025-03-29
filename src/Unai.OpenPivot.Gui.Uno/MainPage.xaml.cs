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
using System.Threading.Tasks;
using System.Numerics;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Unai.OpenPivot.Gui.Uno;

public sealed partial class MainPage : Page
{
	private Point _currentPosition;
	private int _currentFrame = 0;

	private PivFile _pivFile = new();

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
		_pivFile.Frames.Add(new());
		_pivFile.Frames[0].FigureInstances.Add(new() { FigureIndex = 1, Position = new(320, 180) });
	}

	private void OnSurfacePointerMoved(object sender, PointerRoutedEventArgs e)
	{
		_currentPosition = e.GetCurrentPoint(panelGrid).Position;
		RedrawCanvas();
	}

	private void RedrawCanvas()
	{
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

	private void Render(SKCanvas canvas, Size size)
	{
		var scale = (float)(XamlRoot?.RasterizationScale ?? 1);
		var scaledSize = new SKSize((float)size.Width / scale, (float)size.Height / scale);

		canvas.Scale(scale);

		canvas.Clear(new(255, 255, 255));

		if (_pivFile == null)
		{
			var paint = new SKPaint
			{
				Color = SKColors.DimGray,
				IsAntialias = true,
				Style = SKPaintStyle.Fill,
				TextAlign = SKTextAlign.Center,
				TextSize = 24
			};
			var coord = new SKPoint(scaledSize.Width / 2, (scaledSize.Height + paint.TextSize) / 2);
			canvas.DrawText("Load a Pivot project file", coord, paint);

			return;
		}

		void RenderFigureSegment(PivFigure figure, int segmentIndex, SKPoint origin, List<PivSegmentOverrides> segmentOverrides = null)
		{
			var segment = figure.GetSegment(segmentIndex);
			var relEndPt = segment.EndPoint;
			if (segmentOverrides != null && segmentOverrides.Count > segmentIndex)
			{
				var arrSegIdx = figure.GetArrayIndexOfSegmentIndex(segmentIndex);
				if (arrSegIdx >= 0)
				{
					var segOvr = segmentOverrides[arrSegIdx];
					if (segOvr != null && segOvr.Angle.HasValue)
					{
						relEndPt = Utils.VectorFromLengthAngle(segment.Length, segOvr.Angle.Value);
					}
				}
			}
			
			var skEndPoint = origin + new SKPoint(relEndPt.X, relEndPt.Y);
			var skMidPoint = new SKPoint((origin.X + skEndPoint.X) / 2, (origin.Y + skEndPoint.Y) / 2);
			var skPaint = new SKPaint
			{
				Color = new((byte)(segment.Color.X * 256), (byte)(segment.Color.Y * 256), (byte)(segment.Color.Z * 256)),
				Style = SKPaintStyle.Stroke,
				IsAntialias = true,
				StrokeWidth = (float)segment.Thickness,
				StrokeCap = SKStrokeCap.Round,
			};

			switch (segment.SegmentType)
			{
				case PivSegmentType.Line:
					canvas.DrawLine(origin, skEndPoint, skPaint);
					break;

				case PivSegmentType.Circle:
				case PivSegmentType.CircleFill:
				case PivSegmentType.CircleWhiteFill:
					var circleRadius = (float)segment.Length / 2;

					// Draw Fill
					if (segment.SegmentType != PivSegmentType.Circle)
					{
						skPaint.Style = SKPaintStyle.Fill;
						if (segment.SegmentType == PivSegmentType.CircleWhiteFill)
						{
							skPaint.Color = SKColors.White;
						}
						canvas.DrawCircle(skMidPoint, circleRadius, skPaint);
					}

					// Draw Stroke
					skPaint.Style = SKPaintStyle.Stroke;
					skPaint.Color = SKColors.Black;
					canvas.DrawCircle(skMidPoint, circleRadius, skPaint);
					break;
			}

			for (int branchIdx = 1; branchIdx < figure.Segments.Count; branchIdx++)
			{
				if (figure.GetSegment(branchIdx).ParentIndex == segmentIndex)
				{
					RenderFigureSegment(figure, branchIdx, skEndPoint, segmentOverrides);
				}
			}
		}

		if (_pivFile.Frames.Count == 0) return;

		foreach (var figInst in _pivFile.Frames[_currentFrame].FigureInstances)
		{
			var figure = _pivFile.Figures[figInst.FigureIndex];
			if (figure != null)
			{
				RenderFigureSegment(figure, 0, new SKPoint(figInst.Position.X, figInst.Position.Y), figInst.SegmentOverrides);
			}
		}
	}

	private void OnFrameNumberBoxChange(object sender, NumberBoxValueChangedEventArgs e)
	{
		if (_pivFile == null) return;

		_currentFrame = (int)_frameNumBox.Value;
		if (_currentFrame < 0) _currentFrame = 0;
		else if (_currentFrame >= _pivFile.Frames.Count) _currentFrame = _pivFile.Frames.Count - 1;
		_frameNumBox.Value = _currentFrame;
		RedrawCanvas();
	}

	public async Task HandleFileOpenClick(object sender, RoutedEventArgs e)
	{
		var fileOpener = new FileOpenPicker();
		fileOpener.SuggestedStartLocation = PickerLocationId.Unspecified;
		fileOpener.FileTypeFilter.Add(".piv");

		var pivFile = await fileOpener.PickSingleFileAsync();
		if (pivFile != null)
		{
			Console.Error.WriteLine($"Selected file: {pivFile.Path}");
			try
			{
				var fileStream = await pivFile.OpenReadAsync();
				_pivFile = new PivFile();
				_pivFile.Load(fileStream.AsStreamForRead());
			}
			catch (Exception ex)
			{
				var errMsg = new ContentDialog()
				{
					Title = "Error Loading Pivot Project File",
					Content = ex,
					XamlRoot = XamlRoot,
					CloseButtonText = "OK",
				};
				await errMsg.ShowAsync();
			}
			RedrawCanvas();
		}
	}

	public void HandleExitClick(object sender, RoutedEventArgs e)
	{
		App.Current.Exit();
		Environment.Exit(0);
	}
}
