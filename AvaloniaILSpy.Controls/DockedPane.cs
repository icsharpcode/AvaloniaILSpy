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
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Controls.Primitives;

namespace AvaloniaILSpy.Controls
{
	public class DockedPane : TemplatedControl
	{
		static DockedPane()
		{
			//DefaultStyleKeyProperty.OverrideMetadata(typeof(DockedPane), new FrameworkPropertyMetadata(typeof(DockedPane)));

			TemplateAppliedEvent.AddClassHandler<DockedPane>(x=>x.DockedPane_TemplateApplied);
		}
		
		public static readonly StyledProperty<string> TitleProperty =
			AvaloniaProperty.Register<DockedPane,string>("Title");
		
		public string Title {
			get { return (string)GetValue(TitleProperty); }
			set { SetValue(TitleProperty, value); }
		}
		
		public static readonly StyledProperty<object> ContentProperty =
			AvaloniaProperty.Register<DockedPane, object>("Content");
		
		public object Content {
			get { return GetValue(ContentProperty); }
			set { SetValue(ContentProperty, value); }
		}

		void DockedPane_TemplateApplied(TemplateAppliedEventArgs args)
		{
			Button closeButton = (Button)args.NameScope.Find<Button>("PART_Close");
			if (closeButton != null) {
				closeButton.Click += closeButton_Click;
			}
		}
		
		void closeButton_Click(object sender, RoutedEventArgs e)
		{
			if (CloseButtonClicked != null)
				CloseButtonClicked(this, e);
		}
		
		public event EventHandler CloseButtonClicked;
		
		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.Key == Key.F4 && e.Modifiers == InputModifiers.Control || e.Key == Key.Escape) {
				if (CloseButtonClicked != null)
					CloseButtonClicked(this, e);
				e.Handled = true;
			}
		}
	}
}
