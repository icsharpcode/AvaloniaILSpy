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

using System.Collections;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaEdit;
using System;
using Avalonia.Markup.Xaml;
using Avalonia.Controls.Presenters;
using System.Collections.Generic;
using Avalonia.Layout;

namespace ICSharpCode.ILSpy.Controls
{
	/// <summary>
	/// Interaction logic for ResourceObjectTable.xaml
	/// </summary>
	public partial class ResourceObjectTable : UserControl, IRoutedCommandBindable
    {
		internal DataGrid resourceListView;

        public IList<RoutedCommandBinding> CommandBindings { get; } = new List<RoutedCommandBinding>();

        public ResourceObjectTable(IEnumerable resources, ContentPresenter contentPresenter)
        {
            InitializeComponent();
            resourceListView.ItemsSource = resources;
        }

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
			resourceListView = this.FindControl<DataGrid>("resourceListView");
            CommandBindings.Add(new RoutedCommandBinding(global::AvaloniaEdit.ApplicationCommands.Copy, ExecuteCopy, CanExecuteCopy));
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            var size = e.Parent.Bounds;
            Width = size.Width - 45;
            MaxHeight = size.Height;
        }

		void ExecuteCopy(object sender, ExecutedRoutedEventArgs args)
		{
			StringBuilder sb = new StringBuilder();
			foreach (var item in resourceListView.SelectedItems)
			{
				sb.AppendLine(item.ToString());
			}

			//// App.Current.Clipboard.SetTextAsync(sb.ToString());
			var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
			clipboard.SetTextAsync(sb.ToString());
		}
		
		void CanExecuteCopy(object sender, CanExecuteRoutedEventArgs args)
		{
			args.CanExecute = true;
		}
	}
}
