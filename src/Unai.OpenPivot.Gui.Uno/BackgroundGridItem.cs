using System;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Unai.OpenPivot.Gui.Uno;

public class BackgroundGridItem(WeakReference<PivBackground> bgData, BitmapImage thumbnail)
{
	public WeakReference<PivBackground> BackgroundRef { get; set; } = bgData;
	public BitmapImage Thumbnail { get; set; } = thumbnail;
	public PivBackground Background => BackgroundRef != null ? (BackgroundRef.TryGetTarget(out var bg) ? bg : null) : null;
	public string Name => Background?.Name ?? "<null>";
}