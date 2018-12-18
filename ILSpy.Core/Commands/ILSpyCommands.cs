// Copyright (c) 2018 Siegfried Pammer
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Input;
using AvaloniaEdit;
using ICSharpCode.ILSpy.Analyzers;

namespace ICSharpCode.ILSpy
{
	static class ILSpyCommands
	{
		public static readonly AnalyzeCommand Analyze = new AnalyzeCommand();
	}

    public static class NavigationCommands
    {
        public static RoutedCommand BrowseBack { get; } = new NavigationCommand(nameof(BrowseBack), new KeyGesture { Modifiers = InputModifiers.Control, Key = Key.BrowserBack });
        public static RoutedCommand BrowseForward { get; } = new NavigationCommand(nameof(BrowseForward), new KeyGesture { Modifiers = InputModifiers.Control, Key = Key.BrowserForward });
        public static RoutedCommand Search { get; } = new RoutedCommand(nameof(Search), new KeyGesture { Modifiers = InputModifiers.Control | InputModifiers.Shift, Key = Key.F });
    }

    public static class ApplicationCommands
    {
        public static RoutedCommand Open { get; } = new RoutedCommand(nameof(Open), new KeyGesture { Modifiers = InputModifiers.Control, Key = Key.O });
        public static RoutedCommand Save { get; } = new RoutedCommand(nameof(Save), new KeyGesture { Modifiers = InputModifiers.Control, Key = Key.S });
        public static RoutedCommand Refresh { get; } = new RoutedCommand(nameof(Refresh), new KeyGesture { Modifiers = InputModifiers.Control, Key = Key.R });
    }
}
