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
using System.Windows.Input;
using AvaloniaEdit;
using System;
using Avalonia.Markup.Xaml;

namespace AvaloniaILSpy.Controls
{
	/// <summary>
	/// Interaction logic for ResourceStringTable.xaml
	/// </summary>
	public partial class ResourceStringTable : UserControl
	{
		internal ListBox resourceListView;

		public ResourceStringTable(IEnumerable strings)
		{
			InitializeComponent();
			resourceListView.Items = strings;
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
			resourceListView = this.FindControl<ListBox>("resourceListView");
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			return new Size(availableSize.Width - 45, availableSize.Height);
		}
		
		void ExecuteCopy(object sender, ExecutedRoutedEventArgs args)
		{
			StringBuilder sb = new StringBuilder();
			foreach (var item in resourceListView.SelectedItems)
			{
				sb.AppendLine(item.ToString());
			}
			App.Current.Clipboard.SetTextAsync(sb.ToString());
		}
		
		void CanExecuteCopy(object sender, CanExecuteRoutedEventArgs args)
		{
			args.CanExecute = true;
		}
	}
}