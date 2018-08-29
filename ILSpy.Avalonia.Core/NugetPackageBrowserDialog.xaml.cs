// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

using AvaloniaILSpy.Controls;
using Mono.Cecil;

namespace AvaloniaILSpy
{
	/// <summary>
	/// Interaction logic for NugetPackageBrowserDialog.xaml
	/// </summary>
	public partial class NugetPackageBrowserDialog : DialogWindow, INotifyPropertyChanged
	{
        public LoadedNugetPackage Package { get; }

        internal Button okButton;
        internal Button cancelButton;

		public NugetPackageBrowserDialog()
		{
			InitializeComponent();
#if DEBUG
			this.AttachDevTools();
#endif
		}

		public NugetPackageBrowserDialog(LoadedNugetPackage package)
		{
			InitializeComponent();
#if DEBUG
			this.AttachDevTools();
#endif
			this.Package = package;
			this.Package.PropertyChanged += Package_PropertyChanged;
			DataContext = this;
		}

		private void InitializeComponent()
		{
            AvaloniaXamlLoader.Load(this);
            okButton = this.FindControl<Button>("okButton");
            cancelButton = this.FindControl<Button>("cancelButton");

            okButton.Click += OKButton_Click;
            cancelButton.Click += CancelButton_Click;
		}

		public new event PropertyChangedEventHandler PropertyChanged;

		void Package_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(Package.SelectedEntries)) {
				OnPropertyChanged(new PropertyChangedEventArgs("HasSelection"));
			}
        }

        void OKButton_Click(object sender, RoutedEventArgs e)
        {
            Close(true);
        }

        void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close(false);
        }

		protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			PropertyChanged?.Invoke(this, e);
		}

		public Entry[] SelectedItems {
			get {
				return Package.SelectedEntries.ToArray();
			}
		}

		public bool HasSelection {
			get { return SelectedItems.Length > 0; }
		}
	}
}