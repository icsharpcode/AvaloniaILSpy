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
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Animation;
using Avalonia.Threading;
using AvaloniaEdit.Editing;
using Avalonia.Styling;
using Avalonia.Controls.Shapes;

namespace ICSharpCode.ILSpy.TextView
{
	/// <summary>
	/// Animated rectangle around the caret.
	/// This is used after clicking links that lead to another location within the text view.
	/// </summary>
	sealed class CaretHighlightAdorner : Control
	{
        readonly Pen pen;

        static readonly StyledProperty<double> RectOpacityProperty = AvaloniaProperty.Register<CaretHighlightAdorner, double>(nameof(RectOpacity));
        public double RectOpacity => GetValue(RectOpacityProperty);

        static readonly StyledProperty<Rect> RectProperty = AvaloniaProperty.Register<CaretHighlightAdorner, Rect>(nameof(Rect));
        public Rect Rect => GetValue(RectProperty);

        static CaretHighlightAdorner()
        {
            AffectsRender<CaretHighlightAdorner>(
                RectProperty,
                RectOpacityProperty
            );
        }

        public CaretHighlightAdorner(TextArea textArea)
		{
            Rect min = textArea.Caret.CalculateCaretRectangle();
            min = min.Translate(-textArea.TextView.ScrollOffset);

            double size = Math.Max(min.Width, min.Height) * 0.25;
            Rect max = min.Inflate(size);

            pen = new Pen(TextBlock.GetForeground(textArea.TextView).ToImmutable());

            //geometry.BeginAnimation(RectangleGeometry.RectProperty, new RectAnimation(min, max, new Duration(TimeSpan.FromMilliseconds(300))) { AutoReverse = true });
            //pen.Brush.BeginAnimation(Brush.OpacityProperty, new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(200))) { BeginTime = TimeSpan.FromMilliseconds(450) });

            // HACK: one animation at a time
            var caretAnimation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(1000),
                IterationCount = new IterationCount(1UL),
                PlaybackDirection = PlaybackDirection.Normal,
                Children =
                {
                    new KeyFrame{ Setters = {new Setter(RectProperty, min) }, Cue = new Cue(0) },
                    new KeyFrame{ Setters = {new Setter(RectProperty, max) }, Cue = new Cue(0.3) },
                    new KeyFrame{ Setters = {new Setter(RectProperty, min) }, Cue = new Cue(0.6) },
                    new KeyFrame{ Setters = {new Setter(RectProperty, max) }, Cue = new Cue(1) },

                    new KeyFrame { Setters = { new Setter(RectOpacityProperty, 1.0) },  Cue = new Cue(0), },
                    new KeyFrame { Setters = { new Setter(RectOpacityProperty, 1.0) },  Cue = new Cue(0.45), },
                    new KeyFrame { Setters = { new Setter(RectOpacityProperty, 0.0) },  Cue = new Cue(0.65), },
                    new KeyFrame { Setters = { new Setter(RectOpacityProperty, 0.0) },  Cue = new Cue(1), }
                }
            };

            caretAnimation.RunAsync(this, null, default);
        }

        public static void DisplayCaretHighlightAnimation(TextArea textArea)
		{
			AdornerLayer layer = AdornerLayer.GetAdornerLayer(textArea.TextView);
            CaretHighlightAdorner adorner = new CaretHighlightAdorner(textArea)
            {
                [AdornerLayer.AdornedElementProperty] = textArea
            };
            layer.Children.Add(adorner);

			DispatcherTimer timer = new DispatcherTimer();
			timer.Interval = TimeSpan.FromSeconds(1);
			timer.Tick += delegate {
				timer.Stop();
				layer.Children.Remove(adorner);
			};
			timer.Start();
		}
		
		public override void Render(DrawingContext context)
		{
            using (context.PushOpacity(RectOpacity))
                context.DrawRectangle(pen, Rect, 2f);
		}
	}
}
