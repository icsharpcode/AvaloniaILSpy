using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Styling;

namespace ICSharpCode.ILSpy.Controls
{
    public class DialogWindow: Window, IStyleable
	{
        static readonly System.Reflection.FieldInfo _dialogResultField = typeof(Window).GetField("_dialogResult", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

		public object DialogResult { get { return _dialogResultField.GetValue(this); } }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.Escape && e.Modifiers == InputModifiers.None)
            {
                Close();
            }
        }

        Type IStyleable.StyleKey { get; } = typeof(Window);
    }
}
