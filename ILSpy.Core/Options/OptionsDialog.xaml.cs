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
using System.ComponentModel.Composition;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using System.Xml.Linq;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Collections.Generic;
using ICSharpCode.ILSpy.Controls;
using ICSharpCode.ILSpy.Properties;

namespace ICSharpCode.ILSpy.Options
{
	/// <summary>
	/// Interaction logic for OptionsDialog.xaml
	/// </summary>
	public partial class OptionsDialog : DialogWindow
	{
		
		readonly Lazy<IControl, IOptionsMetadata>[] optionPages;

		internal TabControl tabControl;

		public OptionsDialog()
		{
			InitializeComponent();
#if DEBUG
			this.AttachDevTools();
#endif
			// These used to have [ImportMany(..., RequiredCreationPolicy = CreationPolicy.NonShared)], so they use their own
			// ExportProvider instance.
			// FIXME: Ideally, the export provider should be disposed when it's no longer needed.
			var ep = App.ExportProviderFactory.CreateExportProvider();
			this.optionPages = ep.GetExports<IControl, IOptionsMetadata>("OptionPages").ToArray();
			ILSpySettings settings = ILSpySettings.Load();
			var tabItems = new List<TabItem>();
			foreach (var optionPage in optionPages.OrderBy(p => p.Metadata.Order)) {
				TabItem tabItem = new TabItem();
                tabItem.Header = MainWindow.GetResourceString(optionPage.Metadata.Title);
				tabItem.Content = optionPage.Value;
				tabItems.Add(tabItem);
				
				IOptionPage page = optionPage.Value as IOptionPage;
				if (page != null)
					page.Load(settings);
			}
			tabControl.Items = tabItems;
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
			tabControl = this.FindControl<TabControl>("tabControl");
			this.FindControl<Button>("okButton").Click += OKButton_Click;
			this.FindControl<Button>("cancelButton").Click += CancelButton_Click;;
		}

        void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close(false);
        }

		void OKButton_Click(object sender, RoutedEventArgs e)
		{
			ILSpySettings.Update(
				delegate (XElement root) {
					foreach (var optionPage in optionPages) {
						IOptionPage page = optionPage.Value as IOptionPage;
						if (page != null)
							page.Save(root);
					}
				});
			//this.DialogResult = true;
			Close(true);
		}
	}
	
	public interface IOptionsMetadata
	{
		string Title { get; }
		int Order { get; }
	}
	
	public interface IOptionPage
	{
		void Load(ILSpySettings settings);
		void Save(XElement root);
	}
	
	[MetadataAttribute]
	[AttributeUsage(AttributeTargets.Class, AllowMultiple=false)]
	public class ExportOptionPageAttribute : ExportAttribute
	{
		public ExportOptionPageAttribute() : base("OptionPages", typeof(IControl))
		{ }
		
		public string Title { get; set; }
		
		public int Order { get; set; }
	}

    [ExportMainMenuCommand(Menu = nameof(Resources._View), Header = nameof(Resources._Options), MenuCategory = nameof(Resources.Options), MenuOrder = 999)]
    sealed class ShowOptionsCommand : SimpleCommand
	{
		public override async void Execute(object parameter)
		{
			OptionsDialog dlg = new OptionsDialog();
			dlg.Title = "Options";
			if (await dlg.ShowDialog<bool>(MainWindow.Instance) == true) {
				new RefreshCommand().Execute(parameter);
			}
		}
	}
}