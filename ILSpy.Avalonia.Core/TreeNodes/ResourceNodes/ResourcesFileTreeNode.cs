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
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Util;
using AvaloniaILSpy.Controls;
using AvaloniaILSpy.TextView;
using Microsoft.Win32;
using Mono.Cecil;

namespace AvaloniaILSpy.TreeNodes
{
	[Export(typeof(IResourceNodeFactory))]
	sealed class ResourcesFileTreeNodeFactory : IResourceNodeFactory
	{
		public ILSpyTreeNode CreateNode(Resource resource)
		{
			EmbeddedResource er = resource as EmbeddedResource;
			if (er != null && er.Name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase)) {
				return new ResourcesFileTreeNode(er);
			}
			return null;
		}

		public ILSpyTreeNode CreateNode(string key, object data)
		{
			return null;
		}
	}

	sealed class ResourcesFileTreeNode : ResourceTreeNode
	{
		readonly ICollection<KeyValuePair<string, string>> stringTableEntries = new ObservableCollection<KeyValuePair<string, string>>();
		readonly ICollection<SerializedObjectRepresentation> otherEntries = new ObservableCollection<SerializedObjectRepresentation>();

		public ResourcesFileTreeNode(EmbeddedResource er)
			: base(er)
		{
			this.LazyLoading = true;
		}

		public override object Icon
		{
			get { return Images.ResourceResourcesFile; }
		}

		protected override void LoadChildren()
		{
			EmbeddedResource er = this.Resource as EmbeddedResource;
			if (er != null) {
				Stream s = er.GetResourceStream();
				s.Position = 0;
				try {
					foreach (var entry in new ResourcesFile(s)) {
						ProcessResourceEntry(entry);
					}
				} catch (BadImageFormatException) {
					// ignore errors
				}
			}
		}

		private void ProcessResourceEntry(KeyValuePair<string, object> entry)
		{
			if (entry.Value is String) {
				stringTableEntries.Add(new KeyValuePair<string, string>(entry.Key, (string)entry.Value));
				return;
			}

			if (entry.Value is byte[]) {
				Children.Add(ResourceEntryNode.Create(entry.Key, new MemoryStream((byte[])entry.Value)));
				return;
			}

			var node = ResourceEntryNode.Create(entry.Key, entry.Value);
			if (node != null) {
				Children.Add(node);
				return;
			}

			if (entry.Value == null) {
				otherEntries.Add(new SerializedObjectRepresentation(entry.Key, "null", ""));
			} else if (entry.Value is ResourceSerializedObject so) {
				otherEntries.Add(new SerializedObjectRepresentation(entry.Key, so.TypeName, "<serialized>"));
			} else {
				otherEntries.Add(new SerializedObjectRepresentation(entry.Key, entry.Value.GetType().FullName, entry.Value.ToString()));
			}
		}
		
		public override async Task<bool> Save(DecompilerTextView textView)
		{
			EmbeddedResource er = this.Resource as EmbeddedResource;
			if (er != null) {
				SaveFileDialog dlg = new SaveFileDialog();
				dlg.InitialFileName = DecompilerTextView.CleanUpName(er.Name);
				dlg.Filters = new List<FileDialogFilter>()
				{
					new FileDialogFilter(){ Name="Resources file(*.resources)", Extensions = { "resources" } },
					new FileDialogFilter(){ Name="Resource XML file(*.resx)", Extensions = { "resx" } }
				};
				var filename = await dlg.ShowAsync(App.Current.MainWindow);
				if (!string.IsNullOrEmpty(filename)) {
					Stream s = er.GetResourceStream();
					s.Position = 0;
					if (filename.Contains("resources")) {
						using (var fs = File.OpenWrite(filename)) {
							s.CopyTo(fs);
						}
					} else { 
							using (var writer = new ResXResourceWriter(File.OpenWrite(filename))) {
								foreach (var entry in new ResourcesFile(s)) {
									writer.AddResource(entry.Key, entry.Value);
								}
							}
					}
				}
				
				return true;
			}
			return false;
		}

		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options)
		{
			EnsureLazyChildren();
			base.Decompile(language, output, options);
			if (stringTableEntries.Count != 0) {
				ISmartTextOutput smartOutput = output as ISmartTextOutput;
				if (null != smartOutput) {
					smartOutput.AddUIElement(
						delegate {
                            return new ResourceStringTable(stringTableEntries, MainWindow.Instance.mainPane);
						}
					);
				}
				output.WriteLine();
				output.WriteLine();
			}
			if (otherEntries.Count != 0) {
				ISmartTextOutput smartOutput = output as ISmartTextOutput;
				if (null != smartOutput) {
					smartOutput.AddUIElement(
						delegate {
                            return new ResourceObjectTable(otherEntries, MainWindow.Instance.mainPane);
						}
					);
				}
				output.WriteLine();
			}
		}

		internal class SerializedObjectRepresentation
		{
			public SerializedObjectRepresentation(string key, string type, string value)
			{
				this.Key = key;
				this.Type = type;
				this.Value = value;
			}

			public string Key { get; private set; }
			public string Type { get; private set; }
			public string Value { get; private set; }
		}
	}
}
