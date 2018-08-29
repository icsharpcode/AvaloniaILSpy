// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System.ComponentModel.Composition;
using Avalonia;
using Avalonia.Media;

using AvaloniaEdit.Highlighting;
using AvaloniaILSpy;

namespace TestPlugin
{
	[Export(typeof(IAboutPageAddition))]
	public class AboutPageAddition : IAboutPageAddition
	{
		public void Write(ISmartTextOutput textOutput)
		{
			textOutput.WriteLine();
			textOutput.Write("This is a test.");
			textOutput.WriteLine();
			textOutput.WriteLine();
			textOutput.BeginSpan(new HighlightingColor {
				Background = new SimpleHighlightingBrush(Colors.Black),
				FontStyle = FontStyle.Italic,
				Foreground = new SimpleHighlightingBrush(Colors.Aquamarine)
			});
			textOutput.Write("DO NOT PRESS THIS BUTTON --> ");
			textOutput.AddButton(null, "Test!", (sender, args) => MessageBox.Show("Naughty Naughty!", "Naughty!", MessageBoxButton.OK, MessageBoxImage.Exclamation));
			textOutput.Write(" <--");
			textOutput.WriteLine();
			textOutput.EndSpan();
			textOutput.WriteLine();
		}
	}
}
