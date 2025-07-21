﻿// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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
using System.ComponentModel.Composition;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaEdit;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.TreeView;
using System.ComponentModel;
using Avalonia;
using ICSharpCode.ILSpy.Search;
using Avalonia.Controls.Primitives;

namespace ICSharpCode.ILSpy
{
	public interface IContextMenuEntry
	{
		bool IsVisible(TextViewContext context);
		bool IsEnabled(TextViewContext context);
		void Execute(TextViewContext context);
	}
	
	public class TextViewContext
	{
		/// <summary>
		/// Returns the selected nodes in the tree view.
		/// Returns null, if context menu does not belong to a tree view.
		/// </summary>
		public SharpTreeNode[] SelectedTreeNodes { get; private set; }
		
		/// <summary>
		/// Returns the tree view the context menu is assigned to.
		/// Returns null, if context menu is not assigned to a tree view.
		/// </summary>
		public SharpTreeView TreeView { get; private set; }
		
		/// <summary>
		/// Returns the text view the context menu is assigned to.
		/// Returns null, if context menu is not assigned to a text view.
		/// </summary>
		public DecompilerTextView TextView { get; private set; }
		
		/// <summary>
		/// Returns the list box the context menu is assigned to.
		/// Returns null, if context menu is not assigned to a list box.
		/// </summary>
		public ListBox ListBox { get; private set; }
		
		/// <summary>
		/// Returns the reference the mouse cursor is currently hovering above.
		/// Returns null, if there was no reference found.
		/// </summary>
		public ReferenceSegment Reference { get; private set; }
		
		/// <summary>
		/// Returns the position in TextView the mouse cursor is currently hovering above.
		/// Returns null, if TextView returns null;
		/// </summary>
		public TextViewPosition? Position { get; private set; }
		
		public static TextViewContext Create(SharpTreeView treeView = null, DecompilerTextView textView = null, DataGrid listBox = null)
		{
			ReferenceSegment reference;
			if (textView != null)
				reference = textView.GetReferenceSegmentAtMousePosition();
			else if (listBox?.SelectedItem is SearchResult result)
				reference = new ReferenceSegment { Reference = result.Member };
			else
				reference = null;
			var position = textView != null ? textView.GetPositionFromMousePosition() : null;
			var selectedTreeNodes = treeView != null ? treeView.GetTopLevelSelection().ToArray() : null;
			return new TextViewContext {
				TreeView = treeView,
				SelectedTreeNodes = selectedTreeNodes,
				TextView = textView,
				Reference = reference,
				Position = position
			};
		}
	}
	
	public interface IContextMenuEntryMetadata
	{
		string Icon { get; }
		string Header { get; }
		string Category { get; }
		
		double Order { get; }
	}
	
	[MetadataAttribute]
	[AttributeUsage(AttributeTargets.Class, AllowMultiple=false)]
	public class ExportContextMenuEntryAttribute : ExportAttribute, IContextMenuEntryMetadata
	{
		public ExportContextMenuEntryAttribute()
			: base(typeof(IContextMenuEntry))
		{
			// entries default to end of menu unless given specific order position
			Order = double.MaxValue;
		}
		
		public string Icon { get; set; }
		public string Header { get; set; }
		public string Category { get; set; }
		public double Order { get; set; }
		public string InputGestureText { get; set; }
	}
	
	internal class ContextMenuProvider
	{
		/// <summary>
		/// Enables extensible context menu support for the specified tree view.
		/// </summary>
		public static void Add(SharpTreeView treeView, DecompilerTextView textView = null)
		{
			var provider = new ContextMenuProvider(treeView, textView);
			// Context menu is shown only when the ContextMenu property is not null before the
			// ContextMenuOpening event handler is called.
			treeView.ContextMenu = new ContextMenu();
			treeView.ContextMenu.ContextMenuOpening += provider.treeView_ContextMenuOpening;

			if (textView != null) {
				// Context menu is shown only when the ContextMenu property is not null before the
				// ContextMenuOpening event handler is called.
				textView.ContextMenu = new ContextMenu();
				textView.ContextMenu.ContextMenuOpening += provider.textView_ContextMenuOpening;
			}
		}
		
		public static void Add(DataGrid listBox)
		{
			var provider = new ContextMenuProvider(listBox);
			listBox.ContextMenu = new ContextMenu();
			listBox.ContextMenu.ContextMenuOpening += provider.listBox_ContextMenuOpening;
		}
		
		readonly SharpTreeView treeView;
		readonly DecompilerTextView textView;
		readonly DataGrid listBox;
		readonly Lazy<IContextMenuEntry, IContextMenuEntryMetadata>[] entries;
		
		private ContextMenuProvider()
		{
			entries = App.ExportProvider.GetExports<IContextMenuEntry, IContextMenuEntryMetadata>().ToArray();
		}
		
		ContextMenuProvider(SharpTreeView treeView, DecompilerTextView textView = null) : this()
		{
			this.treeView = treeView;
			this.textView = textView;
		}
		
		ContextMenuProvider(DataGrid listBox) : this()
		{
			this.listBox = listBox;
		}

		void treeView_ContextMenuOpening(object sender, CancelEventArgs e)
		{
			TextViewContext context = TextViewContext.Create(treeView);
			if (context.SelectedTreeNodes.Length == 0) {
				e.Cancel = true; // don't show the menu
				return;
			}

			ContextMenu menu = (ContextMenu)sender;
			if (ShowContextMenu(context, out IEnumerable<Control> items))
				menu.ItemsSource = items;
			else
				// hide the context menu.
				e.Cancel = true;
		}
		
		void textView_ContextMenuOpening(object sender, CancelEventArgs e)
		{
			TextViewContext context = TextViewContext.Create(textView: textView);
			ContextMenu menu = (ContextMenu)sender;
			if (ShowContextMenu(context, out IEnumerable<Control> items))
				menu.ItemsSource = items;
			else
				// hide the context menu.
				e.Cancel = true;
		}

		void listBox_ContextMenuOpening(object sender, CancelEventArgs e)
		{
			TextViewContext context = TextViewContext.Create(listBox: listBox);
			ContextMenu menu = (ContextMenu)sender;
			if (ShowContextMenu(context, out IEnumerable<Control> items))
				menu.ItemsSource = items;
			else
				// hide the context menu.
				e.Cancel = true;
		}
		
		bool ShowContextMenu(TextViewContext context, out IEnumerable<Control> menuItems)
		{
			List<Control> items = new List<Control>();
			foreach (var category in entries.OrderBy(c => c.Metadata.Order).GroupBy(c => c.Metadata.Category)) {
				bool needSeparatorForCategory = items.Count > 0;
				foreach (var entryPair in category) {
					IContextMenuEntry entry = entryPair.Value;
					if (entry.IsVisible(context)) {
						if (needSeparatorForCategory) {
							items.Add(new Separator());
							needSeparatorForCategory = false;
						}

						MenuItem menuItem = new MenuItem();
						menuItem.Header = MainWindow.GetResourceString(entryPair.Metadata.Header);
						if (!string.IsNullOrEmpty(entryPair.Metadata.Icon)) {
							menuItem.Icon = new Image {
								Width = 16,
								Height = 16,
								Source = Images.LoadImage(entry, entryPair.Metadata.Icon)
							};
						}

						if (entryPair.Value.IsEnabled(context)) {
							menuItem.Click += delegate { entry.Execute(context); };
						} else
							menuItem.IsEnabled = false;
						items.Add(menuItem);
					}
				}
			}

			menuItems = items;
			return items.Count > 0;
		}
	}
}
