// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
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
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
using ICSharpCode.ILSpy.Controls;

namespace ICSharpCode.ILSpy.Controls
{
	public sealed class CustomDialog : DialogWindow
	{
        TextBlock label;
		ListBox buttons;
		int acceptButton;
		int cancelButton;

		/// <summary>
		/// Gets the index of the button pressed.
		/// </summary>
		public int Result
		{
			get {
				return (int)DialogResult;
			}
		}

		public CustomDialog(string caption, string message, int acceptButton = -1, int cancelButton = -1, params string[] buttonLabels)
		{
			this.InitializeComponent();
#if DEBUG
			this.AttachDevTools();
#endif
			this.acceptButton = acceptButton;
			this.cancelButton = cancelButton;
			this.Title = caption;
			this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
			this.Width = buttonLabels.Length * (100+ 10);

			buttons.Items = buttonLabels;

			
			label.Text = message;
		}

		void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
			this.buttons = this.FindControl<ListBox>("buttons");
			this.label = this.FindControl<TextBlock>("content");
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (cancelButton != -1 && e.Key == Key.Escape) {
				this.Close(cancelButton);
			}
		}

		void ButtonClick(object sender, RoutedEventArgs e)
		{
            Button button = sender as Button;
            int index = buttons.ItemContainerGenerator.IndexFromContainer(button.Parent);
            this.Close(index);
			e.Handled = true;
		}
	}
}
