// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia;
using Avalonia.Interactivity;
using System;

namespace AvaloniaILSpy.Controls
{
	class EditTextBox : TextBox
	{
		static EditTextBox()
		{
			//DefaultStyleKeyProperty.OverrideMetadata(typeof(EditTextBox),
			//	new FrameworkPropertyMetadata(typeof(EditTextBox)));
		}

		public EditTextBox()
		{
			Initialized += delegate { Init(); };
		}

		public SharpTreeViewItem Item { get; set; }

		public SharpTreeNode Node {
			get { return Item.Node; }
		}

		void Init()
		{
			Text = Node.LoadEditText();
			Focus();
			SelectAll();
		}

		void SelectAll()
		{
			SelectionStart = 0;
			SelectionEnd = Text.Length;
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.Key == Key.Enter) {
				Commit();
			} else if (e.Key == Key.Escape) {
				Node.IsEditing = false;
			}
		}

		protected override void OnLostFocus(RoutedEventArgs e)
		{
			if (Node.IsEditing) {
				Commit();
			}
		}

		bool commiting;

		void Commit()
		{
			if (!commiting) {
				commiting = true;

				Node.IsEditing = false;
				if (!Node.SaveEditText(Text)) {
					Item.Focus();
				}
				Node.RaisePropertyChanged("Text");

				//if (Node.SaveEditText(Text)) {
				//    Node.IsEditing = false;
				//    Node.RaisePropertyChanged("Text");
				//}
				//else {
				//    Init();
				//}

				commiting = false;
			}
		}
	}
}
