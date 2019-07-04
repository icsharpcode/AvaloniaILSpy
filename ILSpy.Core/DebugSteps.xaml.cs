using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.IL.Transforms;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.Markup.Xaml;

namespace ICSharpCode.ILSpy
{

	/// <summary>
	/// Interaktionslogik für DebugSteps.xaml
	/// </summary>
	public partial class DebugSteps : UserControl, IPane
	{
		static readonly ILAstWritingOptions writingOptions = new ILAstWritingOptions {
			UseFieldSugar = true,
			UseLogicOperationSugar = true
		};

		public static ILAstWritingOptions Options => writingOptions;

#if DEBUG
		ILAstLanguage language;
#endif

		internal Avalonia.Controls.TreeView tree;

		DebugSteps()
		{
			InitializeComponent();

#if DEBUG
			MainWindow.Instance.SessionSettings.FilterSettings.PropertyChanged += FilterSettings_PropertyChanged;
			MainWindow.Instance.SelectionChanged += SelectionChanged;
			writingOptions.PropertyChanged += WritingOptions_PropertyChanged;

			if (MainWindow.Instance.CurrentLanguage is ILAstLanguage l) {
				l.StepperUpdated += ILAstStepperUpdated;
				language = l;
				ILAstStepperUpdated(null, null);
			}
#endif
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
			tree = this.FindControl<Avalonia.Controls.TreeView>("tree");
			tree.DoubleTapped +=  ShowStateAfter_Click;
			tree.KeyDown += tree_KeyDown;
			// var items = tree.ContextMenu.Items.Cast<MenuItem>().ToList();
			tree.FindControl<MenuItem>("ShowStateBefore").Click += ShowStateBefore_Click;
			tree.FindControl<MenuItem>("ShowStateAfter").Click += ShowStateAfter_Click;
			tree.FindControl<MenuItem>("DebugStep").Click += DebugStep_Click;
		}

		private void WritingOptions_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			DecompileAsync(lastSelectedStep);
		}

		private void SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Dispatcher.UIThread.InvokeAsync(() => {
				tree.Items = null;
				lastSelectedStep = int.MaxValue;
			});
		}

		private void FilterSettings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
#if DEBUG
			if (e.PropertyName == "Language") {
				if (language != null) {
					language.StepperUpdated -= ILAstStepperUpdated;
				}
				if (MainWindow.Instance.CurrentLanguage is ILAstLanguage l) {
					l.StepperUpdated += ILAstStepperUpdated;
					language = l;
					ILAstStepperUpdated(null, null);
				}
			}
#endif
		}

		private void ILAstStepperUpdated(object sender, EventArgs e)
		{
#if DEBUG
			if (language == null) return;
			Dispatcher.UIThread.InvokeAsync(() => {
				tree.Items = language.Stepper.Steps;
				lastSelectedStep = int.MaxValue;
			});
#endif
		}

		public static void Show()
        {
            MainWindow.Instance.ShowInTopPane(Properties.Resources.DebugSteps, new DebugSteps());
		}

		void IPane.Closed()
		{
#if DEBUG
			MainWindow.Instance.SessionSettings.FilterSettings.PropertyChanged -= FilterSettings_PropertyChanged;
			MainWindow.Instance.SelectionChanged -= SelectionChanged;
			writingOptions.PropertyChanged -= WritingOptions_PropertyChanged;
			if (language != null) {
				language.StepperUpdated -= ILAstStepperUpdated;
			}
#endif
		}

		private void ShowStateAfter_Click(object sender, RoutedEventArgs e)
		{
			Stepper.Node n = (Stepper.Node)tree.SelectedItem;
			if (n == null) return;
			DecompileAsync(n.EndStep);
		}

		private void ShowStateBefore_Click(object sender, RoutedEventArgs e)
		{
			Stepper.Node n = (Stepper.Node)tree.SelectedItem;
			if (n == null) return;
			DecompileAsync(n.BeginStep);
		}

		private void DebugStep_Click(object sender, RoutedEventArgs e)
		{
			Stepper.Node n = (Stepper.Node)tree.SelectedItem;
			if (n == null) return;
			DecompileAsync(n.BeginStep, true);
		}

		int lastSelectedStep = int.MaxValue;

		void DecompileAsync(int step, bool isDebug = false)
		{
			lastSelectedStep = step;
			var window = MainWindow.Instance;
			var state = window.TextView.GetState();
			window.TextView.DecompileAsync(window.CurrentLanguage, window.SelectedNodes,
				new DecompilationOptions(window.CurrentLanguageVersion) {
					StepLimit = step,
					IsDebug = isDebug,
					TextViewState = state
				});
		}

		private void tree_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter || e.Key == Key.Return) {
				if (e.Modifiers == InputModifiers.Shift)
					ShowStateBefore_Click(sender, e);
				else
					ShowStateAfter_Click(sender, e);
				e.Handled = true;
			}
		}
	}
}