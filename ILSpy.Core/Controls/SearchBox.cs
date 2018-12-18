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
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Data;
using Avalonia.Media.Imaging;
using System.ComponentModel;
using Avalonia.Markup.Xaml.Converters;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;

namespace ICSharpCode.ILSpy.Controls
{
	public class SearchBox : TextBox
	{
		#region Dependency properties

        public static StyledProperty<IBitmap> SearchIconProperty = AvaloniaProperty.Register<SearchBox, IBitmap>(nameof(SearchIcon));

        public static StyledProperty<IBitmap> ClearSearchIconProperty = AvaloniaProperty.Register<SearchBox, IBitmap>(nameof(ClearSearchIcon));

        public static StyledProperty<IBrush> WatermarkColorProperty = AvaloniaProperty.Register<SearchBox, IBrush>(nameof(WatermarkColor));

        public static readonly IValueConverter GridLengthConvert = new FuncValueConverter<double, GridLength>(n => new GridLength(n));

        public static readonly StyledProperty<TimeSpan> UpdateDelayProperty =
			AvaloniaProperty.Register<SearchBox, TimeSpan>(nameof(UpdateDelay), TimeSpan.FromMilliseconds(200));

        #endregion

        public SearchBox()
        {
            PseudoClass<SearchBox, string>(TextProperty, t=>!string.IsNullOrEmpty(t), ":hastext");
        }
		
		#region Public Properties

		public IBrush WatermarkColor {
			get { return (IBrush)GetValue(WatermarkColorProperty); }
			set { SetValue(WatermarkColorProperty, value); }
		}

        public TimeSpan UpdateDelay
        {
            get { return (TimeSpan)GetValue(UpdateDelayProperty); }
            set { SetValue(UpdateDelayProperty, value); }
        }
        
        public IBitmap SearchIcon
        {
            get { return GetValue(SearchIconProperty); }
            set { SetValue(SearchIconProperty, value); }
        }

        public IBitmap ClearSearchIcon
        {
            get { return GetValue(ClearSearchIconProperty); }
            set { SetValue(ClearSearchIconProperty, value); }
        }

        #endregion

        #region Handlers

		private void IconBorder_MouseLeftButtonUp(object obj, PointerReleasedEventArgs e) {
            this.Text = string.Empty;
		}

        #endregion

        #region Overrides

        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            base.OnTemplateApplied(e);
            Border iconBorder = e.NameScope.Find<Border>("PART_IconBorder");
            if (iconBorder != null)
            {
                iconBorder.AddHandler(Border.PointerReleasedEvent, IconBorder_MouseLeftButtonUp, RoutingStrategies.Tunnel);
            }
        }
		
		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.Key == Key.Escape && this.Text.Length > 0) {
				this.Text = string.Empty;
				e.Handled = true;
			} else {
				base.OnKeyDown(e);
			}
		}
        #endregion
    }
}
