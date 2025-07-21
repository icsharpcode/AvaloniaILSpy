﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using Avalonia.Controls;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using System.ComponentModel;
using System.Diagnostics;
using Avalonia.VisualTree;
using Avalonia.Data;
using Avalonia.Controls.Presenters;
using System.Collections.Generic;

namespace ICSharpCode.TreeView
{
	public class SharpTreeNodeView : TemplatedControl
	{
		public static readonly StyledProperty<IBrush> TextBackgroundProperty =
			AvaloniaProperty.Register<SharpTreeNodeView, IBrush>(nameof(TextBackground));

		public IBrush TextBackground
		{
			get { return GetValue(TextBackgroundProperty); }
			set { SetValue(TextBackgroundProperty, value); }
		}

		public static readonly DirectProperty<SharpTreeNodeView, object> IconProperty =
			AvaloniaProperty.RegisterDirect<SharpTreeNodeView, object>(nameof(Icon), (owner) => {
				var expanded = owner.Node?.IsExpanded;
				if (!expanded.HasValue) {
					return null;
				}

				return expanded.Value ? owner.Node?.ExpandedIcon : owner.Node?.Icon;
		});

		public object Icon
		{
			get { return GetValue(IconProperty); }
		}

		public SharpTreeNode Node
		{
			get { return DataContext as SharpTreeNode; }
		}

		public SharpTreeViewItem ParentItem { get; private set; }
		
		public static readonly StyledProperty<Control> CellEditorProperty =
			AvaloniaProperty.Register<SharpTreeNodeView, Control>("CellEditor");
		
		public Control CellEditor {
			get { return (Control)GetValue(CellEditorProperty); }
			set { SetValue(CellEditorProperty, value); }
		}

		public SharpTreeView ParentTreeView
		{
			get { return ParentItem.ParentTreeView; }
		}

		internal LinesRenderer LinesRenderer;
		internal Control spacer;
		internal ToggleButton expander;
		internal ContentPresenter icon;
		internal Border textEditorContainer;
		internal Border checkBoxContainer;
		internal CheckBox checkBox;
		internal Border textContainer;
		internal ContentPresenter textContent;
		List<IDisposable> bindings = new List<IDisposable>();

		protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
		{
			base.OnAttachedToVisualTree(e);
			ParentItem = this.FindAncestor<SharpTreeViewItem>();
			ParentItem.NodeView = this;
		}

		protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
		{
			base.OnApplyTemplate(e);

			LinesRenderer = e.NameScope.Find<LinesRenderer>("linesRenderer");
			spacer = e.NameScope.Find<Control>("spacer");
			expander = e.NameScope.Find<ToggleButton>("expander");
			icon = e.NameScope.Find<ContentPresenter>("icon");
			textEditorContainer = e.NameScope.Find<Border>("textEditorContainer");
			checkBoxContainer = e.NameScope.Find<Border>("checkBoxContainer");
			checkBoxContainer = e.NameScope.Find<Border>("checkBoxContainer");
			checkBox = e.NameScope.Find<CheckBox>("checkBox");
			textContainer = e.NameScope.Find<Border>("textContainer");
			textContent = e.NameScope.Find<ContentPresenter>("textContent");

			UpdateTemplate();
		}

		protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
		{
      base.OnPropertyChanged(change);
      if (change.Property == DataContextProperty)
      {
				var e = (AvaloniaPropertyChangedEventArgs<SharpTreeNode>)change;
				var oldTransitions = e.OldValue.GetValueOrDefault();
				var newTransitions = e.NewValue.GetValueOrDefault();

        UpdateDataContext(oldTransitions, newTransitions);
      }
    }

		void UpdateDataContext(SharpTreeNode oldNode, SharpTreeNode newNode)
		{
			if (oldNode != null)
			{
				oldNode.PropertyChanged -= Node_PropertyChanged;
				bindings.ForEach((obj) => obj.Dispose());
				bindings.Clear();
			}
			if (newNode != null) {
				newNode.PropertyChanged += Node_PropertyChanged;
				if (Template != null) {
					UpdateTemplate();
				}
			}
		}

		void Node_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "IsEditing") {
				OnIsEditingChanged();
			} else if (e.PropertyName == "IsLast") {
				if (ParentTreeView.ShowLines) {
					foreach (var child in Node.VisibleDescendantsAndSelf()) {
						var container = ParentTreeView.ContainerFromItem(child) as SharpTreeViewItem;
						if (container != null && container.NodeView != null) {
							container.NodeView.LinesRenderer.InvalidateVisual();
						}
					}
				}
			} else if (e.PropertyName == "IsExpanded") {
				RaisePropertyChanged(IconProperty, null, Icon);
				if (Node.IsExpanded)
					ParentTreeView.HandleExpanding(Node);
			}
		}

		void OnIsEditingChanged()
		{
			if (Node.IsEditing) {
				if (CellEditor == null)
					textEditorContainer.Child = new EditTextBox() { Item = ParentItem };
				else
					textEditorContainer.Child = CellEditor;
			}
			else {
				textEditorContainer.Child = null;
			}
		}

		void UpdateTemplate()
		{
			if(Node != null)
			{
				bindings.Add(expander.Bind(ToggleButton.IsVisibleProperty, new Binding("ShowExpander") { Source = Node }));
				bindings.Add(expander.Bind(ToggleButton.IsCheckedProperty, new Binding("IsExpanded") { Source = Node }));
				bindings.Add(icon.Bind(ContentPresenter.IsVisibleProperty, new Binding("ShowIcon") { Source = Node }));
				bindings.Add(checkBoxContainer.Bind(Border.IsVisibleProperty, new Binding("IsCheckable") { Source = Node }));
				bindings.Add(checkBox.Bind(CheckBox.IsCheckedProperty, new Binding("IsChecked") { Source = Node }));
				bindings.Add(textContainer.Bind(Border.IsVisibleProperty, new Binding("IsEditing") { Source = Node, Converter = BoolConverters.Inverse }));
				bindings.Add(textContent.Bind(ContentPresenter.ContentProperty, new Binding("Text") { Source = Node }));
				RaisePropertyChanged(IconProperty, null, Icon);
			}

			spacer.Width = CalculateIndent();

			if (ParentTreeView.Root == Node && !ParentTreeView.ShowRootExpander) {
				expander.IsVisible = false;
			}
			else {
				expander.ClearValue(IsVisibleProperty);
			}
		}

		internal double CalculateIndent()
		{
			var result = 19 * Node.Level;
			if (ParentTreeView.ShowRoot) {
				if (!ParentTreeView.ShowRootExpander) {
					if (ParentTreeView.Root != Node) {
						result -= 15;
					}
				}
			}
			else {
				result -= 19;
			}
			if (result < 0) {
				Debug.WriteLine("SharpTreeNodeView.CalculateIndent() on node without correctly-set level");
				return 0;
			}
			return result;
		}
	}
}
