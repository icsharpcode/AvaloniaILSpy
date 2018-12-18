using System;
using System.Windows.Input;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaloniaEdit;

namespace ICSharpCode.ILSpy
{
    /// <summary>
    /// Navigation command. CanExecuteChanged will get called when focused is changed. 
    /// </summary>
    internal class NavigationCommand: RoutedCommand, ICommand
    {
        static EventHandler interactiveEventHandler;

        static NavigationCommand()
        {
            InputElement.GotFocusEvent.AddClassHandler(typeof(InputElement), HandlePointerEvent);
        }

        public NavigationCommand(string name, KeyGesture keyGesture) : base(name, keyGesture)
        {
        }

        private static void HandlePointerEvent(object sender, RoutedEventArgs args)
        {
            interactiveEventHandler?.Invoke(sender, args);
        }

        event EventHandler ICommand.CanExecuteChanged
        {
            add { interactiveEventHandler += value; }
            remove { interactiveEventHandler -= value; }
        }
    }
}
