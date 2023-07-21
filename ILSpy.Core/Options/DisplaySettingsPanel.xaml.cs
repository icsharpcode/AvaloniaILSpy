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
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Document;

namespace ICSharpCode.ILSpy.Options
{
    /// <summary>
    /// Interaction logic for DisplaySettingsPanel.xaml
    /// </summary>
	[ExportOptionPage(Title = nameof(Properties.Resources.Display), Order = 20)]
    public partial class DisplaySettingsPanel : UserControl, IOptionPage
	{
		internal ComboBox fontSelector;

		public DisplaySettingsPanel()
		{
			InitializeComponent();

            Task<FontFamily[]> task = new Task<FontFamily[]>(FontLoader);
			task.Start();
			task.ContinueWith(
				delegate(Task continuation) {
					Dispatcher.UIThread.InvokeAsync(
						(Action)(
							async () => {
								fontSelector.ItemsSource = task.Result;
								if (continuation.Exception != null) {
									foreach (var ex in continuation.Exception.InnerExceptions) {
										await MessageBox.Show(ex.ToString());
									}
								}
							}),
						DispatcherPriority.Normal
					);
				}
			);
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
			fontSelector = this.FindControl<ComboBox>("fontSelector");
            var textEditor = this.FindControl<TextEditor>("textEditor");

            textEditor.Document = new TextDocument("AaBbCcXxYyZz".ToCharArray());
        }

		public void Load(ILSpySettings settings)
		{
			this.DataContext = LoadDisplaySettings(settings);
        }

        static DisplaySettings currentDisplaySettings;
		
		public static DisplaySettings CurrentDisplaySettings {
			get {
				return currentDisplaySettings ?? (currentDisplaySettings = LoadDisplaySettings(ILSpySettings.Load()));
			}
		}
		
		//static bool IsSymbolFont(FontFamily fontFamily)
		//{
		//	foreach (var tf in fontFamily.GetTypefaces()) {
		//		GlyphTypeface glyph;
		//		try {
		//			if (tf.TryGetGlyphTypeface(out glyph))
		//				return glyph.Symbol;
		//		} catch (Exception) {
		//			return true;
		//		}
		//	}
		//	return false;
		//}
		
		static FontFamily[] FontLoader()
		{
			// TODO: filter SymbolFonts
			return FontManager.Current.GetInstalledFontFamilyNames().Select(x => new FontFamily(x)).ToArray();
		}

		public static DisplaySettings LoadDisplaySettings(ILSpySettings settings)
		{
			XElement e = settings["DisplaySettings"];
			var s = new DisplaySettings();
			s.SelectedFont = new FontFamily((string)e.Attribute("Font") ?? FontManager.Current.DefaultFontFamilyName);
			s.SelectedFontSize = (double?)e.Attribute("FontSize") ?? 10.0 * 4 / 3;
			s.ShowLineNumbers = (bool?)e.Attribute("ShowLineNumbers") ?? false;
            s.ShowDebugInfo = (bool?)e.Attribute("ShowDebugInfo") ?? false;
            s.ShowMetadataTokens = (bool?) e.Attribute("ShowMetadataTokens") ?? false;
            s.ShowMetadataTokensInBase10 = (bool?)e.Attribute("ShowMetadataTokensInBase10") ?? false;
            s.EnableWordWrap = (bool?)e.Attribute("EnableWordWrap") ?? false;
			s.SortResults = (bool?)e.Attribute("SortResults") ?? true;
            s.FoldBraces = (bool?)e.Attribute("FoldBraces") ?? false;
            s.ExpandMemberDefinitions = (bool?)e.Attribute("ExpandMemberDefinitions") ?? false;
            s.ExpandUsingDeclarations = (bool?)e.Attribute("ExpandUsingDeclarations") ?? false;
            s.IndentationUseTabs = (bool?)e.Attribute("IndentationUseTabs") ?? true;
            s.IndentationSize = (int?)e.Attribute("IndentationSize") ?? 4;
            s.IndentationTabSize = (int?)e.Attribute("IndentationTabSize") ?? 4;
            s.HighlightMatchingBraces = (bool?)e.Attribute("HighlightMatchingBraces") ?? true;

            return s;
		}
		
		public void Save(XElement root)
		{
			var s = (DisplaySettings)this.DataContext;
			
			var section = new XElement("DisplaySettings");
			section.SetAttributeValue("Font", s.SelectedFont.Name);
			section.SetAttributeValue("FontSize", s.SelectedFontSize);
			section.SetAttributeValue("ShowLineNumbers", s.ShowLineNumbers);
            section.SetAttributeValue("ShowDebugInfo", s.ShowDebugInfo);
            section.SetAttributeValue("ShowMetadataTokens", s.ShowMetadataTokens);
            section.SetAttributeValue("ShowMetadataTokensInBase10", s.ShowMetadataTokensInBase10);
            section.SetAttributeValue("EnableWordWrap", s.EnableWordWrap);
			section.SetAttributeValue("SortResults", s.SortResults);
            section.SetAttributeValue("FoldBraces", s.FoldBraces);
            section.SetAttributeValue("ExpandMemberDefinitions", s.ExpandMemberDefinitions);
            section.SetAttributeValue("ExpandUsingDeclarations", s.ExpandUsingDeclarations);
            section.SetAttributeValue("IndentationUseTabs", s.IndentationUseTabs);
            section.SetAttributeValue("IndentationSize", s.IndentationSize);
            section.SetAttributeValue("IndentationTabSize", s.IndentationTabSize);
            section.SetAttributeValue("HighlightMatchingBraces", s.HighlightMatchingBraces);

            XElement existingElement = root.Element("DisplaySettings");
			if (existingElement != null)
				existingElement.ReplaceWith(section);
			else
				root.Add(section);

			if (currentDisplaySettings != null)
				currentDisplaySettings.CopyValues(s);
		}

        private void TextBox_PreviewTextInput(object sender, TextInputEventArgs e)
        {
            if (!e.Text.All(char.IsDigit))
                e.Handled = true;
        }
    }


    public class FontSizeConverter : IValueConverter
	{
		public static readonly FontSizeConverter Instance = new FontSizeConverter();

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) {
                return 11.0;
            }

            if (value is double d) {
				return Math.Round(d / 4 * 3);
			}
			
			throw new NotImplementedException();
		}
		
		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) {
                return 11.0 * 4 / 3;
            }

            if (value is double dd) {
                return dd * 4 / 3;
            }

            if (value is string s) {
                if (double.TryParse(s, out double d))
                    return d * 4 / 3;
                return 11.0 * 4 / 3;
            }

            throw new NotImplementedException();
        }
	}
}
