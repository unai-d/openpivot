using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.IO;
using Windows.Foundation;
using SkiaSharp;
using SkiaSharp.Views.Windows;
using Windows.Storage.Pickers;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Documents;
using Uno.Toolkit.UI;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Uno.Extensions;
using System.Threading;
using System.Numerics;

namespace Unai.OpenPivot.Gui.Uno;

public sealed partial class MainPage : Page
{
	private Point _currentPosition;
	private int _currentFrame = 0;
	private MainPageSectionId _currentSection = MainPageSectionId.AnimationManager;
	private byte[] _uiCheckerboardPng = null;
	private SKBitmap _uiCheckerboardSkBmp = null;
	private float _uiCanvasZoom = 1f;
	private Vector2 _uiCanvasTranslation = Vector2.Zero;
	private bool _uiCanvasIsTranslating = false;

	private PivFile _pivFile = new();

	private ObservableCollection<BackgroundGridItem> _backgroundThumbnails = [];

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
		try
		{
			var chkbrdPng = await StorageFile.GetFileFromApplicationUriAsync(new Uri(BaseUri, "Assets/Checkerboard16.png"));
			var chkbrdStream = await chkbrdPng.OpenReadAsync();			
			_uiCheckerboardPng = await chkbrdStream.AsStreamForRead().ReadBytesAsync(CancellationToken.None);
		}
		catch {}

		_pivFile.Frames.Add(new());
		_pivFile.Frames[0].FigureInstances.Add(new() { FigureIndex = 1, Position = new(_pivFile.CanvasWidth / 2, _pivFile.CanvasHeight / 2) });
		UpdateBackgroundData();
		RedrawCanvas();
	}

	private void OnSurfacePointerMoved(object sender, PointerRoutedEventArgs e)
	{
		var pointerPosition = e.GetCurrentPoint(panelGrid).Position;
		var pointerPositionDelta = pointerPosition - _currentPosition;

		if (_uiCanvasIsTranslating)
		{
			_uiCanvasTranslation.X += (float)pointerPositionDelta.X / _uiCanvasZoom;
			_uiCanvasTranslation.Y += (float)pointerPositionDelta.Y / _uiCanvasZoom;

			RedrawCanvas();
		}

		_currentPosition = pointerPosition;
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

		var vpMatrix = SKMatrix.CreateTranslation(_uiCanvasTranslation.X - (_pivFile.CanvasWidth / 2), _uiCanvasTranslation.Y - (_pivFile.CanvasHeight / 2));
		vpMatrix = vpMatrix.PostConcat(SKMatrix.CreateScale(_uiCanvasZoom, _uiCanvasZoom));
		vpMatrix = vpMatrix.PostConcat(SKMatrix.CreateTranslation(scaledSize.Width / 2, scaledSize.Height / 2));
		canvas.SetMatrix(vpMatrix);

		canvas.Clear(new(128, 128, 128));

		if (_pivFile == null)
		{
			return;
		}

		if (_pivFile.Frames.Count == 0) return;

		var frameBgIdx = _pivFile.Frames[_currentFrame].BackgroundIndex;
		var frameBg = _pivFile.Backgrounds[frameBgIdx];

		switch (frameBg.Type)
		{
			case PivBackroundType.SolidColor:
				{
					var skPaint = new SKPaint
					{
						Style = SKPaintStyle.Fill,
						Color = Utils.Vector4ToSKColor(frameBg.Color),
					};
					canvas.DrawRect(new(0, 0, _pivFile.CanvasWidth, _pivFile.CanvasHeight), skPaint);
				}
				break;

			case PivBackroundType.Gradient:
				{
					var skPaint = new SKPaint
					{
						Shader = SKShader.CreateLinearGradient(
							new SKPoint(frameBg.GradientStart.X * _pivFile.CanvasWidth, frameBg.GradientStart.Y * _pivFile.CanvasHeight),
							new SKPoint(frameBg.GradientEnd.X * _pivFile.CanvasWidth, frameBg.GradientEnd.Y * _pivFile.CanvasHeight),
							[
								Utils.Vector4ToSKColor(frameBg.Color),
								Utils.Vector4ToSKColor(frameBg.SecondColor),
							],
							SKShaderTileMode.Clamp
						)
					};
					canvas.DrawRect(new(0, 0, _pivFile.CanvasWidth, _pivFile.CanvasHeight), skPaint);
				}
				break;

			case PivBackroundType.JPEG:
			case PivBackroundType.PNG:
				{
					var skImage = SKImage.FromEncodedData(frameBg.ImageData);
					canvas.DrawImage(skImage, SKPoint.Empty);
				}
				break;
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
						relEndPt = OpenPivot.Utils.VectorFromLengthAngle(segment.Length, segOvr.Angle.Value);
					}
				}
			}
			
			var skEndPoint = origin + new SKPoint(relEndPt.X, relEndPt.Y);
			var skMidPoint = new SKPoint((origin.X + skEndPoint.X) / 2, (origin.Y + skEndPoint.Y) / 2);
			var skPaint = new SKPaint
			{
				Color = Utils.Vector4ToSKColor(segment.Color),
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
					skPaint.Color = Utils.Vector4ToSKColor(segment.Color);
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

		// Render figure instances.

		foreach (var figInst in _pivFile.Frames[_currentFrame].FigureInstances)
		{
			if (figInst.FigureIndex >= _pivFile.Figures.Count)
			{
				continue;
			}

			var figure = _pivFile.Figures[figInst.FigureIndex];
			if (figure != null)
			{
				RenderFigureSegment(figure, 0, new SKPoint(figInst.Position.X, figInst.Position.Y), figInst.SegmentOverrides);
			}
		}

		// Render viewport limits.
		
		_uiCheckerboardSkBmp ??= SKBitmap.Decode(_uiCheckerboardPng);
		var skShader = SKShader.CreateBitmap(_uiCheckerboardSkBmp, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat);

		canvas.DrawRect(new(0, 0, _pivFile.CanvasWidth, _pivFile.CanvasHeight), new SKPaint()
		{
			Shader = skShader,
			Style = SKPaintStyle.Stroke,
			Color = SKColors.White.WithAlpha(0x80)
		});
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
			UpdateBackgroundData();
			RedrawCanvas();
		}
	}

	public void HandleExitClick(object sender, RoutedEventArgs e)
	{
		App.Current.Exit();
		Environment.Exit(0);
	}

	public async Task OnAboutBoxShow(object sender, RoutedEventArgs e)
	{
		var repoLink = new Hyperlink() { NavigateUri = new("https://github.com/unai-d/openpivot") };
		repoLink.Inlines.Add(new Run() { Text = "GitHub Repository" });

		var buildString = new TextBlock() { TextWrapping = TextWrapping.Wrap };
		buildString.Inlines.Add(new Run() { Text = "OpenPivot version 0.1" });
		buildString.Inlines.Add(new LineBreak());
		buildString.Inlines.Add(repoLink);

		var aboutBox = new ContentDialog()
		{
			Title = "About OpenPivot",
			Content = buildString,
			CloseButtonText = "OK",
			XamlRoot = XamlRoot,
		};

		await aboutBox.ShowAsync();
	}

	public void OnSectionChange(object sender, TabBarSelectionChangedEventArgs e)
	{
		_currentSection = (MainPageSectionId)_uiMainTabBar.SelectedIndex;
		panelGrid.Visibility = _currentSection == MainPageSectionId.AnimationManager ? Visibility.Visible : Visibility.Collapsed;
		_uiFigureMgr.Visibility = _currentSection == MainPageSectionId.FigureManager ? Visibility.Visible : Visibility.Collapsed;
		_uiBackgroundMgr.Visibility = _currentSection == MainPageSectionId.BackgroundManager ? Visibility.Visible : Visibility.Collapsed;
	}

	public void UpdateBackgroundData()
	{
		_backgroundThumbnails.Clear();

		if (_pivFile != null)
		{
			foreach (var bg in _pivFile.Backgrounds)
			{
				BitmapImage bitmap = null;
				if (bg.ImageData != null)
				{
					try
					{
						var ms = new MemoryStream(bg.ImageData);
						bitmap = new();
						bitmap.DecodePixelWidth = 256;
						bitmap.SetSource(ms);
					}
					catch (Exception ex)
					{
						Console.WriteLine(ex);
					}
				}
				_backgroundThumbnails.Add(new(new(bg), bitmap));
			}
		}
	}

	public void OnSurfacePointerWheelChanged(object sender, PointerRoutedEventArgs e)
	{
		var wheelDelta = e.GetCurrentPoint(this).Properties.MouseWheelDelta;
		_uiCanvasZoom *= wheelDelta > 0 ? 1.5f : (1 / 1.5f);
		_uiCanvasZoom = Math.Clamp(_uiCanvasZoom, 0.125f, 32f);

		RedrawCanvas();
	}

	public void OnSurfacePointerPressed(object sender, PointerRoutedEventArgs e)
	{
		_uiCanvasIsTranslating = e.GetCurrentPoint(this).Properties.IsLeftButtonPressed;
	}

	public void OnSurfacePointerReleased(object sender, PointerRoutedEventArgs e)
	{
		_uiCanvasIsTranslating = false;
	}
}
