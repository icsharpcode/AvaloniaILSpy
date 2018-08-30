using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ICSharpCode.TreeView;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Interaction logic for Create.xaml
	/// </summary>
	public partial class CreateListDialog : DialogWindow
	{
		public new bool DialogResult { get; private set; } = false;

        internal Button okButton;
        internal Button cancelButton;
		internal TextBox ListName;

		public CreateListDialog()
		{
			this.InitializeComponent();
#if DEBUG
			this.AttachDevTools();
#endif
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
            okButton = this.FindControl<Button>("okButton");
            cancelButton = this.FindControl<Button>("cancelButton");
			ListName = this.FindControl<TextBox>("ListName");

            // Work around for TextChanged event
            //ListName.TextInput += TextBox_TextChanged;
            ListName.GetObservable(TextBox.TextProperty).Subscribe(text => TextBox_TextChanged(this, new TextInputEventArgs{Text = text}));
            okButton.Click += OKButton_Click;
            cancelButton.Click += CancelButton_Click;

            TemplateApplied += (sender, e) => Application.Current.FocusManager.Focus(ListName);
		}

		private void TextBox_TextChanged(object sender, TextInputEventArgs e)
		{
			okButton.IsEnabled = !string.IsNullOrWhiteSpace(ListName.Text);
		}

		private void OKButton_Click(object sender, RoutedEventArgs e)
		{
			if (!string.IsNullOrWhiteSpace(ListName.Text))
			{
				this.DialogResult = true;
				this.Close(true);
			}
        }

        void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close(false);
        }

		protected override void OnPointerPressed(PointerPressedEventArgs e)
		{
			this.DialogResult = false;
			base.OnPointerPressed(e);
		}

		public string NewListName
		{
			get
			{
				return ListName.Text;
			}
		}

	}
}