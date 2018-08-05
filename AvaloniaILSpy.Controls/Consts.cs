using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Media.Imaging;

namespace Avalonia
{
    public static class SystemParameters
	{
		public const double MinimumHorizontalDragDistance = 2.0;
		public const double MinimumVerticalDragDistance = 2.0;
	}

	public static class SystemColors
	{
		public static IBrush ControlTextBrush { get; } = System.Drawing.SystemBrushes.ControlText.ToAvaloniaBrush();
		public static IBrush ControlDarkBrush { get; } = System.Drawing.SystemBrushes.ControlDark.ToAvaloniaBrush();
		public static IBrush HighlightBrush { get; } = System.Drawing.SystemBrushes.HighlightText.ToAvaloniaBrush();
		public static IBrush HighlightTextBrush { get; } = System.Drawing.SystemBrushes.HighlightText.ToAvaloniaBrush();
		public static IBrush WindowTextBrush { get; } = System.Drawing.SystemBrushes.WindowText.ToAvaloniaBrush();
		public static IBrush WindowBrush { get; } = System.Drawing.SystemBrushes.Window.ToAvaloniaBrush();
		public static IBrush GrayTextBrush { get; } = System.Drawing.SystemBrushes.GrayText.ToAvaloniaBrush();
		public static IBrush InfoTextBrush { get; } = System.Drawing.SystemBrushes.InfoText.ToAvaloniaBrush();
		public static IBrush InfoBrush { get; } = System.Drawing.SystemBrushes.Info.ToAvaloniaBrush();
		public static IBrush InactiveCaptionBrush { get; } = System.Drawing.SystemBrushes.InactiveCaption.ToAvaloniaBrush();
		public static IBrush InactiveCaptionTextBrush { get; } = System.Drawing.SystemBrushes.InactiveCaptionText.ToAvaloniaBrush();


		public static Color ControlLightColor { get; } = System.Drawing.SystemColors.ControlLight.ToAvaloniaColor();
		public static Color ControlLightLightColor { get; } = System.Drawing.SystemColors.ControlLightLight.ToAvaloniaColor();
		public static Color ControlDarkColor { get; } = System.Drawing.SystemColors.ControlDark.ToAvaloniaColor();
		public static Color ControlDarkDarkColor { get; } = System.Drawing.SystemColors.ControlDarkDark.ToAvaloniaColor();
		public static Color HighlightColor { get; } = System.Drawing.SystemColors.Highlight.ToAvaloniaColor();

		public static Color ToAvaloniaColor(this System.Drawing.Color color)
		{
			return new Color(color.A, color.R, color.G, color.B);
		}

		public static IBrush ToAvaloniaBrush(this System.Drawing.Brush brush)
		{
			if (brush is System.Drawing.SolidBrush solidbrush) {
				return new ImmutableSolidColorBrush(solidbrush.Color.ToAvaloniaColor());
			}
			else if(brush is System.Drawing.TextureBrush textureBrush) {
				using (var imageStream = new MemoryStream()) {
					var image = textureBrush.Image;
					image.Save(imageStream, System.Drawing.Imaging.ImageFormat.Bmp);

					var avaloniaBitmap = new Bitmap(imageStream);
					return new ImageBrush(avaloniaBitmap);
				}

			} else {
				throw new NotSupportedException();
			}
		}
	}
}
