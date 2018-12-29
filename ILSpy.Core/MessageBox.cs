// Copyright (c) 2018 Jeffrey

using System;
using Avalonia.Controls;
using System.Threading.Tasks;
using System.Diagnostics;
using ICSharpCode.ILSpy.Controls;
using System.Collections.Generic;

namespace ICSharpCode.ILSpy
{
    public static class MessageBox
    {
        static readonly Dictionary<MessageBoxButton, string[]> Buttons = new Dictionary<MessageBoxButton, string[]>()
        {
            [MessageBoxButton.OK] = new string[] { "OK" },
            [MessageBoxButton.OKCancel] = new string[] { "OK", "Cancel" },
            [MessageBoxButton.YesNo] = new string[] { "Yes", "No" },
            [MessageBoxButton.YesNoCancel] = new string[] { "Yes", "No", "Cancel" },
        };

        static readonly Dictionary<MessageBoxButton, int> AcceptButtonID = new Dictionary<MessageBoxButton, int>()
        {
            [MessageBoxButton.OK] = 0,
            [MessageBoxButton.OKCancel] = 0,
            [MessageBoxButton.YesNo] = 1,
            [MessageBoxButton.YesNoCancel] = 1,
        };

        static readonly Dictionary<MessageBoxButton, int> CancelButtonID = new Dictionary<MessageBoxButton, int>()
        {
            [MessageBoxButton.OK] = -1,
            [MessageBoxButton.OKCancel] = 1,
            [MessageBoxButton.YesNo] = 1,
            [MessageBoxButton.YesNoCancel] = 2,
        };


        public static Task<MessageBoxResult> Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult)
        {
            return Show(null, messageBoxText, caption, button, icon, defaultResult, MessageBoxOptions.None);
        }

        public static Task<MessageBoxResult> Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            return Show(null, messageBoxText, caption, button, icon, MessageBoxResult.None, MessageBoxOptions.None);
        }

        public static Task<MessageBoxResult> Show(string messageBoxText, string caption, MessageBoxButton button)
        {
            return Show(null, messageBoxText, caption, button, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.None);
        }

        public static Task<MessageBoxResult> Show(string messageBoxText, string caption)
        {
            return Show(null, messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.None);
        }

        public static Task<MessageBoxResult> Show(string messageBoxText)
        {
            return Show(null, messageBoxText, "Message", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.None);
        }

        public static async Task<MessageBoxResult> Show(Window owner, string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions options)
        {
            if (caption == null)
                throw new ArgumentNullException(nameof(caption));
            if (messageBoxText == null)
                throw new ArgumentNullException(nameof(messageBoxText));

            // Button Strings
            var buttons = Buttons[button];

            // Show Message Window
            var win = new CustomDialog(caption, messageBoxText, AcceptButtonID[button], CancelButtonID[button], buttons);
            var btnIndex = await win.ShowDialog<int?>(owner ?? App.Current.MainWindow);

            if (btnIndex != null && Enum.TryParse(buttons[btnIndex.Value], out MessageBoxResult result))
            {
                return result;
            }

            return defaultResult;
        }
    }

    public enum MessageBoxOptions
    {
        None
    }

    public enum MessageBoxResult
    {
        None = 0,
        OK = 1,
        Cancel = 2,
        Yes = 6,
        No = 7
    }

    public enum MessageBoxImage
    {
        None = 0,

        Hand = 16,
        Stop = 16,
        Error = 16,

        Question = 32,

        Exclamation = 48,
        Warning = 48,

        Asterisk = 64,
        Information = 64
    }

    public enum MessageBoxButton
    {
        OK = 0,
        OKCancel = 1,
        YesNoCancel = 3,
        YesNo = 4
    }
}