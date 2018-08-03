using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;

namespace Avalonia.Controls
{
    public class DialogWindow: Window
	{
		static System.Reflection.FieldInfo _dialogResultField = typeof(Window).GetField("_dialogResult", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

		public object DialogResult { get { return _dialogResultField.GetValue(this); } }

		public DialogWindow()
		{
			Topmost = true;
			HasSystemDecorations = false; // issue in mac when close
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
	}
}
