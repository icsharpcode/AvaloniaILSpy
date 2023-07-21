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

using Avalonia;
using Avalonia.Controls;
using Mono.Cecil;
using Avalonia.Input;
using System;
using Avalonia.Interactivity;
using ICSharpCode.TreeView;
using Avalonia.Markup.Xaml;
using ICSharpCode.ILSpy.Controls;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Interaction logic for OpenListDialog.xaml
	/// </summary>
	public partial class OpenListDialog : DialogWindow
	{

		public const string DotNetCoreList = ".NET Core";

		readonly AssemblyListManager manager;

		internal ListBox listView;
		internal Button okButton;
		internal Button cancelButton;
		internal Button deleteButton;
		internal Button createButton;
		internal Button resetButton;

		public OpenListDialog()
		{
			manager = MainWindow.Instance.assemblyListManager;
			InitializeComponent();
#if DEBUG
			this.AttachDevTools();
#endif
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
			listView = this.FindControl<ListBox>("listView");
			okButton = this.FindControl<Button>("okButton");
			cancelButton = this.FindControl<Button>("cancelButton");
			deleteButton = this.FindControl<Button>("deleteButton");
			createButton = this.FindControl<Button>("createButton");
			resetButton = this.FindControl<Button>("resetButton");

			listView.SelectionChanged += ListView_SelectionChanged;
			listView.DoubleTapped += listView_MouseDoubleClick;
			okButton.Click += OKButton_Click;
			cancelButton.Click += CancelButton_Click;
			deleteButton.Click += DeleteButton_Click;
			createButton.Click += CreateButton_Click;
			resetButton.Click += ResetButton_Click;

			TemplateApplied += (sender, e) => Application.Current.FocusManager.Focus(listView);
			listView.TemplateApplied += listView_Loaded;
		}

		private void listView_Loaded(object sender, EventArgs e)
		{
			listView.ItemsSource = manager.AssemblyLists;
			CreateDefaultAssemblyLists();
		}

		void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			okButton.IsEnabled = listView.SelectedItem != null;
			deleteButton.IsEnabled = listView.SelectedItem != null;
		}

		void OKButton_Click(object sender, RoutedEventArgs e)
		{
			//this.DialogResult = true;
			Close(true);
		}

		void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			Close(false);
		}

		public string SelectedListName
		{
			get
			{
				return listView.SelectedItem.ToString();
			}
		}

		private void CreateDefaultAssemblyLists()
		{
			if (!manager.AssemblyLists.Contains(DotNetCoreList))
			{
				AssemblyList netcore = new AssemblyList(DotNetCoreList);
				//AddToList(netcore, "System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e");
				AddToList(netcore, "netstandard.library");
				if (netcore.assemblies.Count > 0)
				{
					manager.CreateList(netcore);
				}
			}
		}

		private void AddToList(AssemblyList list, string name)
		{
			//AssemblyNameReference reference = AssemblyNameReference.Parse(FullName);
			string file = typeof(string).Assembly.Location;
			if (file != null)
				list.OpenAssembly(file);
		}

		private async void CreateButton_Click(object sender, RoutedEventArgs e)
		{
			CreateListDialog dlg = new CreateListDialog();
			dlg.Title = "Create List";
			dlg.Closing += (s, args) =>
			{
				if (dlg.DialogResult == true)
				{
					if (manager.AssemblyLists.Contains(dlg.NewListName))
					{
						args.Cancel = true;
						MessageBox.Show("A list with the same name was found.", "Error!", MessageBoxButton.OK);
					}
				}
			};
			if (await dlg.ShowDialog<bool>(this) == true)
			{
				manager.CreateList(new AssemblyList(dlg.NewListName));
			}
		}

		private async void DeleteButton_Click(object sender, RoutedEventArgs e)
		{
			if (listView.SelectedItem == null)
				return;
			if (await MessageBox.Show(this, "Are you sure that you want to delete the selected assembly list?",
"ILSpy", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No, MessageBoxOptions.None) != MessageBoxResult.Yes)
				return;
			manager.DeleteList(listView.SelectedItem.ToString());
		}

		private async void ResetButton_Click(object sender, RoutedEventArgs e)
		{
			if (await MessageBox.Show(this, "Are you sure that you want to remove all assembly lists and recreate the default assembly lists?",
				"ILSpy", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No, MessageBoxOptions.None) != MessageBoxResult.Yes)
				return;
			manager.ClearAll();
			CreateDefaultAssemblyLists();
		}

		private void listView_MouseDoubleClick(object sender, RoutedEventArgs e)
		{
			if (listView.SelectedItem != null)
				//this.DialogResult = true;
				this.Close(true);
		}

	}
}
