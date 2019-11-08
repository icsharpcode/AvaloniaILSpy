using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;

namespace ICSharpCode.ILSpy.Controls
{
    public class PlatformDependentWindow : Window
    {
        Action<RawInputEventArgs> originalInputEventHanlder;

        public PlatformDependentWindow()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                originalInputEventHanlder = PlatformImpl.Input;
                PlatformImpl.Input = HandleInput;
            }
        }

        void HandleInput(RawInputEventArgs args)
        {
            if (args is RawKeyEventArgs rawKeyEventArgs)
            {
                // cmd + back = delete
                if (rawKeyEventArgs.Modifiers.HasFlag(RawInputModifiers.Meta) && rawKeyEventArgs.Key == Key.Back)
                {
                    rawKeyEventArgs.Modifiers = RawInputModifiers.None;
                    rawKeyEventArgs.Key = Key.Delete;
                }

                // swap cmd and ctrl
                var modifier = rawKeyEventArgs.Modifiers & ~RawInputModifiers.Control & ~RawInputModifiers.Meta;
                if (rawKeyEventArgs.Modifiers.HasFlag(RawInputModifiers.Meta))
                {
                    modifier |= RawInputModifiers.Control;
                }
                if (rawKeyEventArgs.Modifiers.HasFlag(RawInputModifiers.Control))
                {
                    modifier |= RawInputModifiers.Meta;
                }

                rawKeyEventArgs.Modifiers = modifier;
            }

            originalInputEventHanlder(args);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // Close shortcut
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Cmd + W
                if (!e.Handled && e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.W)
                {
                    Close();
                    e.Handled = true;
                }

                // Cmd + Q
                if (!e.Handled && e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.Q)
                {
                    Application.Current.Exit();
                    e.Handled = true;
                }
            }
        }
    }
}
