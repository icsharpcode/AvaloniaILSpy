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

namespace Avalonia.Controls
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

        public new Task<TResult> ShowDialog<TResult>()
		{
			var affectedWindows = Application.Current.Windows.Where(w => w.IsEnabled && w != this).ToList();
			var activated = affectedWindows.Where(w => w.IsActive).FirstOrDefault();

			affectedWindows.ForEach(w => w.IsEnabled = false);

			Show();

			var result = new TaskCompletionSource<TResult>();

			Renderer?.Start();

			Observable.FromEventPattern<EventHandler, EventArgs>(
				x => this.Closed += x,
				x => this.Closed -= x)
				.Take(1)
				.Subscribe(_ =>
				{
					affectedWindows.ForEach(w => w.IsEnabled = true);
					activated?.Activate();
					result.TrySetResult((TResult)(DialogResult ?? default(TResult)));
				});

			return result.Task;
		}

        Type IStyleable.StyleKey { get; } = typeof(Window);
    }
}
