// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using AvaloniaILSpy;
using AvaloniaILSpy.TreeNodes;
using Mono.Cecil;

namespace TestPlugin
{
	[ExportContextMenuEntryAttribute(Header = "_Save Assembly")]
	public class SaveAssembly : IContextMenuEntry
	{
		public bool IsVisible(TextViewContext context)
		{
			return context.SelectedTreeNodes != null && context.SelectedTreeNodes.All(n => n is AssemblyTreeNode);
		}
		
		public bool IsEnabled(TextViewContext context)
		{
			return context.SelectedTreeNodes != null && context.SelectedTreeNodes.Length == 1;
		}
		
		public async void Execute(TextViewContext context)
		{
			if (context.SelectedTreeNodes == null)
				return;
			AssemblyTreeNode node = (AssemblyTreeNode)context.SelectedTreeNodes[0];
			AssemblyDefinition asm = node.LoadedAssembly.GetAssemblyDefinitionOrNull();
			if (asm != null) {
				SaveFileDialog dlg = new SaveFileDialog();
				dlg.InitialFileName = node.LoadedAssembly.FileName;
				dlg.Filters = new List<FileDialogFilter> { new FileDialogFilter { Name = "Assembly", Extensions = { "dll", "exe" } } };
                var filename = await dlg.ShowAsync(MainWindow.Instance);
                if (filename != null) {
					asm.MainModule.Write(filename);
				}
			}
		}
	}
}
