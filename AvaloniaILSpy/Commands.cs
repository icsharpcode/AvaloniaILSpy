using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Input;
using AvaloniaEdit;

namespace AvaloniaILSpy
{
    public static class NavigationCommands
    {
		public static RoutedCommand BrowseBack { get; }
		public static RoutedCommand BrowseForward { get; }
		public static RoutedCommand BrowseHome { get; }
		public static RoutedCommand BrowseStop { get; }
		public static RoutedCommand Refresh { get; }
		public static RoutedCommand Favorites { get; }
		public static RoutedCommand Search { get; }
		public static RoutedCommand IncreaseZoom { get; }
		public static RoutedCommand DecreaseZoom { get; }
		public static RoutedCommand Zoom { get; }
		public static RoutedCommand NextPage { get; }
		public static RoutedCommand PreviousPage { get; }
		public static RoutedCommand FirstPage { get; }
		public static RoutedCommand LastPage { get; }
		public static RoutedCommand GoToPage { get; }
		public static RoutedCommand NavigateJournal { get; }
	}

    public static class ApplicationCommands
    {
        public static RoutedCommand Open { get; } = new RoutedCommand(nameof(Open), new KeyGesture { Modifiers = InputModifiers.Control, Key = Key.O });
        public static RoutedCommand Save { get; } = new RoutedCommand(nameof(Save), new KeyGesture { Modifiers = InputModifiers.Control, Key = Key.S });
        public static RoutedCommand Refresh { get; } = new RoutedCommand(nameof(Refresh), new KeyGesture { Modifiers = InputModifiers.Control, Key = Key.R });
    }
}
