using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Input;
using AvaloniaEdit;

namespace AvaloniaILSpy
{
    public static class NavigationCommands
    {
        public static RoutedCommand BrowseBack { get; } = new RoutedCommand(nameof(BrowseBack), new KeyGesture { Modifiers = InputModifiers.Control, Key = Key.BrowserBack });
        public static RoutedCommand BrowseForward { get; } = new RoutedCommand(nameof(BrowseForward), new KeyGesture { Modifiers = InputModifiers.Control, Key = Key.BrowserForward });
        public static RoutedCommand Search { get; } = new RoutedCommand(nameof(Search), new KeyGesture { Modifiers = InputModifiers.Control | InputModifiers.Shift, Key = Key.F });
	}

    public static class ApplicationCommands
    {
        public static RoutedCommand Open { get; } = new RoutedCommand(nameof(Open), new KeyGesture { Modifiers = InputModifiers.Control, Key = Key.O });
        public static RoutedCommand Save { get; } = new RoutedCommand(nameof(Save), new KeyGesture { Modifiers = InputModifiers.Control, Key = Key.S });
        public static RoutedCommand Refresh { get; } = new RoutedCommand(nameof(Refresh), new KeyGesture { Modifiers = InputModifiers.Control, Key = Key.R });
    }
}
