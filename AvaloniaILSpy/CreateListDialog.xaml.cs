using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AvaloniaILSpy.Controls;

namespace AvaloniaILSpy
{
	/// <summary>
	/// Interaction logic for Create.xaml
	/// </summary>
	public partial class CreateListDialog : DialogWindow
	{
		public new bool DialogResult { get; private set; } = false;

		internal Button okButton;
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
			ListName = this.FindControl<TextBox>("ListName");
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