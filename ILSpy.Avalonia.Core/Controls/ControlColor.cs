// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using Avalonia;
using Avalonia.Markup;
using Avalonia.Media;
using Portable.Xaml.Markup;

namespace ICSharpCode.ILSpy.Controls
{
	[MarkupExtensionReturnType(typeof(Color))]
	public class ControlColor : MarkupExtension
	{
		[ConstructorArgument("color")]
		public float Color { get; set; }
		
		/// <summary>
		/// Amount of highlight (0..1)
		/// </summary>
		public float Highlight { get; set; }

		public ControlColor()
		{

		}

		/// <summary>
		/// val: Color value in the range 105..255.
		/// </summary>
		public ControlColor(float color)
		{
			if (!(color >= 105 && color <= 255))
				throw new ArgumentOutOfRangeException(nameof(color));
			this.Color = color;
		}
		
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (Color > 227) {
				return Interpolate(227, SystemColors.ControlLightColor, 255, SystemColors.ControlLightLightColor);
			} else if (Color > 160) {
				return Interpolate(160, SystemColors.ControlDarkColor, 227, SystemColors.ControlLightColor);
			} else {
				return Interpolate(105, SystemColors.ControlDarkDarkColor, 160, SystemColors.ControlDarkColor);
			}
		}
		
		Color Interpolate(float v1, Color c1, float v2, Color c2)
		{
			float v = (Color - v1) / (v2 - v1);
			Color c = Add(Multiple(c1, (1 - v)), Multiple(c2, v));
			return Add(Multiple(c, (1 - Highlight)), Multiple(SystemColors.HighlightColor, Highlight));
		}

		static Color Multiple(Color c, float multiplier)
		{
			return new Color((byte)((float)c.A * multiplier), (byte)((float)c.R * multiplier), (byte)((float)c.G * multiplier), (byte)((float)c.B * multiplier));
		}

		static Color Add(Color c1, Color c2)
		{
			return new Color((byte)(c1.A + c2.A), (byte)(c1.A + c2.A), (byte)(c1.A + c2.A), (byte)(c1.A + c2.A));
		}
	}
}