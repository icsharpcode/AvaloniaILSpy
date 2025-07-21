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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.TreeView;
using Microsoft.Win32;
using ICSharpCode.Decompiler.TypeSystem;
using TypeDefinitionHandle = System.Reflection.Metadata.TypeDefinitionHandle;
using System.Text;
using ICSharpCode.ILSpy.Properties;

namespace ICSharpCode.ILSpy.TreeNodes
{
	/// <summary>
	/// Tree node representing an assembly.
	/// This class is responsible for loading both namespace and type nodes.
	/// </summary>
	public sealed class AssemblyTreeNode : ILSpyTreeNode
	{
		readonly Dictionary<string, NamespaceTreeNode> namespaces = new Dictionary<string, NamespaceTreeNode>();
		readonly Dictionary<TypeDefinitionHandle, TypeTreeNode> typeDict = new Dictionary<TypeDefinitionHandle, TypeTreeNode>();
		ICompilation typeSystem;

		public AssemblyTreeNode(LoadedAssembly assembly)
		{
			this.LoadedAssembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
			assembly.ContinueWhenLoaded(OnAssemblyLoaded, TaskScheduler.FromCurrentSynchronizationContext());

			this.LazyLoading = true;
		}

		public AssemblyList AssemblyList {
			get { return LoadedAssembly.AssemblyList; }
		}

		public LoadedAssembly LoadedAssembly { get; }

		public override bool IsAutoLoaded {
			get {
				return LoadedAssembly.IsAutoLoaded;
			}
		}

		public override object Text => LoadedAssembly.Text;

		public override object Icon {
			get {
				if (LoadedAssembly.IsLoaded) {
					return LoadedAssembly.HasLoadError ? Images.AssemblyWarning : Images.Assembly;
				} else {
					return Images.AssemblyLoading;
				}
			}
		}

        //TextBlock tooltip;
        string tooltip;

		public override object ToolTip {
			get {
				if (LoadedAssembly.HasLoadError)
					return "Assembly could not be loaded. Click here for details.";

                // TODO: AVALONIA: add inlines tooltip
                //if (tooltip == null && LoadedAssembly.IsLoaded) {
                //	tooltip = new TextBlock();
                //	var module = LoadedAssembly.GetPEFileOrNull();
                //	var metadata = module?.Metadata;
                //	if (metadata?.IsAssembly == true) {
                //		tooltip.Inlines.Add(new Bold(new Run("Name: ")));
                //		tooltip.Inlines.Add(new Run(metadata.GetFullAssemblyName()));
                //		tooltip.Inlines.Add(new LineBreak());
                //	}
                //	tooltip.Inlines.Add(new Bold(new Run("Location: ")));
                //	tooltip.Inlines.Add(new Run(LoadedAssembly.FileName));
                //	tooltip.Inlines.Add(new LineBreak());
                //	tooltip.Inlines.Add(new Bold(new Run("Architecture: ")));
                //	tooltip.Inlines.Add(new Run(Language.GetPlatformDisplayName(module)));
                //	string runtimeName = Language.GetRuntimeDisplayName(module);
                //	if (runtimeName != null) {
                //		tooltip.Inlines.Add(new LineBreak());
                //		tooltip.Inlines.Add(new Bold(new Run("Runtime: ")));
                //		tooltip.Inlines.Add(new Run(runtimeName));
                //	}
                //	var debugInfo = LoadedAssembly.GetDebugInfoOrNull();
                //	tooltip.Inlines.Add(new LineBreak());
                //	tooltip.Inlines.Add(new Bold(new Run("Debug info: ")));
                //	tooltip.Inlines.Add(new Run(debugInfo?.Description ?? "none"));
                //}

                if (tooltip == null && LoadedAssembly.IsLoaded) {
                  var sb = new StringBuilder();
                  var module = LoadedAssembly.GetPEFileOrNull();
                  var metadata = module?.Metadata;
                  if (metadata?.IsAssembly == true) {
                        sb.Append("Name: ");
                        sb.Append(metadata.GetFullAssemblyName());
                        sb.AppendLine();
                    }
                    sb.Append("Location: ");
                    sb.Append(LoadedAssembly.FileName);
                    sb.AppendLine();

                    sb.Append("Architecture: ");
                    sb.Append(Language.GetPlatformDisplayName(module));
                    string runtimeName = Language.GetRuntimeDisplayName(module);
                    if (runtimeName != null)
                    {
                        sb.AppendLine();
                        sb.Append("Runtime: ");
                        sb.Append(runtimeName);
                    }
                    sb.AppendLine();

                    var debugInfo = LoadedAssembly.GetDebugInfoOrNull();
                    sb.AppendLine();
                    sb.Append("Debug info: ");
                    sb.Append(debugInfo?.Description ?? "none");

                    tooltip = sb.ToString();
                }

                return tooltip;
			}
		}

		public override bool ShowExpander {
			get { return !LoadedAssembly.HasLoadError; }
		}

		void OnAssemblyLoaded(Task<PEFile> moduleTask)
		{
			// change from "Loading" icon to final icon
			RaisePropertyChanged("Icon");
			RaisePropertyChanged("ExpandedIcon");
			RaisePropertyChanged("Tooltip");
			if (moduleTask.IsFaulted) {
				RaisePropertyChanged("ShowExpander"); // cannot expand assemblies with load error
													  // observe the exception so that the Task's finalizer doesn't re-throw it
 				try { moduleTask.Wait(); } catch (AggregateException) { }
			} else {
				RaisePropertyChanged("Text"); // shortname might have changed
			}
		}

		protected override void LoadChildren()
		{
			var module = LoadedAssembly.GetPEFileOrNull();
			if (module == null) {
				// if we crashed on loading, then we don't have any children
				return;
			}
			typeSystem = LoadedAssembly.GetTypeSystemOrNull();
			var assembly = (MetadataModule)typeSystem.MainModule;
			var metadata = module.Metadata;

			this.Children.Add(new ReferenceFolderTreeNode(module, this));
			if (module.Resources.Any())
				this.Children.Add(new ResourceListTreeNode(module));
			foreach (NamespaceTreeNode ns in namespaces.Values) {
				ns.Children.Clear();
			}
			foreach (var type in assembly.TopLevelTypeDefinitions.OrderBy(t => t.ReflectionName, NaturalStringComparer.Instance)) {
                var escapedNamespace = Language.EscapeName(type.Namespace);
                if (!namespaces.TryGetValue(type.Namespace, out NamespaceTreeNode ns)) {
                    ns = new NamespaceTreeNode(escapedNamespace);
                    namespaces.Add(type.Namespace, ns);
                }
                TypeTreeNode node = new TypeTreeNode(type, this);
				typeDict[(TypeDefinitionHandle)type.MetadataToken] = node;
				ns.Children.Add(node);
			}
			foreach (NamespaceTreeNode ns in namespaces.Values.OrderBy(n => n.Name, NaturalStringComparer.Instance)) {
				if (ns.Children.Count > 0)
					this.Children.Add(ns);
			}
		}

		public override bool CanExpandRecursively => true;

		/// <summary>
		/// Finds the node for a top-level type.
		/// </summary>
		public TypeTreeNode FindTypeNode(ITypeDefinition type)
		{
			if (type == null)
				return null;
			EnsureLazyChildren();
			TypeTreeNode node;
			if (typeDict.TryGetValue((TypeDefinitionHandle)type.MetadataToken, out node))
				return node;
			else
				return null;
		}

		/// <summary>
		/// Finds the node for a namespace.
		/// </summary>
		public NamespaceTreeNode FindNamespaceNode(string namespaceName)
		{
			if (string.IsNullOrEmpty(namespaceName))
				return null;
			EnsureLazyChildren();
			NamespaceTreeNode node;
			if (namespaces.TryGetValue(namespaceName, out node))
				return node;
			else
				return null;
		}
		
		public override bool CanDrag(SharpTreeNode[] nodes)
		{
			return nodes.All(n => n is AssemblyTreeNode);
		}

		public override void StartDrag(PointerEventArgs e, AvaloniaObject dragSource, SharpTreeNode[] nodes)
		{
			DragDrop.DoDragDrop(e, Copy(nodes), DragDropEffects.Copy | DragDropEffects.Link | DragDropEffects.Move);
		}

		public override bool CanDelete()
		{
			return true;
		}

		public override void Delete()
		{
			DeleteCore();
		}

		public override void DeleteCore()
		{
			LoadedAssembly.AssemblyList.Unload(LoadedAssembly);
		}

		internal const string DataFormat = "ILSpyAssemblies";

		public override IDataObject Copy(SharpTreeNode[] nodes)
		{
			DataObject dataObject = new DataObject();
			dataObject.Set(DataFormat, nodes.OfType<AssemblyTreeNode>().Select(n => n.LoadedAssembly.FileName).ToArray());
			return dataObject;
		}

		public override FilterResult Filter(FilterSettings settings)
		{
			if (settings.SearchTermMatches(LoadedAssembly.ShortName))
				return FilterResult.Match;
			else
				return FilterResult.Recurse;
		}

		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options)
		{
			void HandleException(Exception ex, string message)
			{
				language.WriteCommentLine(output, message);

				output.WriteLine();
				output.MarkFoldStart("Exception details", true);
				output.Write(ex.ToString());
				output.MarkFoldEnd();
			}

			try {
				LoadedAssembly.WaitUntilLoaded(); // necessary so that load errors are passed on to the caller
			} catch (AggregateException ex) {
				language.WriteCommentLine(output, LoadedAssembly.FileName);
				switch (ex.InnerException) {
					case BadImageFormatException badImage:
						HandleException(badImage, "This file does not contain a managed assembly.");
						return;
					case FileNotFoundException fileNotFound:
						HandleException(fileNotFound, "The file was not found.");
						return;
					case DirectoryNotFoundException dirNotFound:
						HandleException(dirNotFound, "The directory was not found.");
						return;
					case PEFileNotSupportedException notSupported:
						HandleException(notSupported, notSupported.Message);
						return;
					default:
						throw;
				}
			}
			language.DecompileAssembly(LoadedAssembly, output, options);
		}

		public override async Task<bool> Save(DecompilerTextView textView)
		{
			Language language = this.Language;
			if (string.IsNullOrEmpty(language.ProjectFileExtension))
				return false;
			SaveFileDialog dlg = new SaveFileDialog();
			dlg.Title = "Save file";
            dlg.InitialFileName = DecompilerTextView.CleanUpName(LoadedAssembly.ShortName);
			dlg.DefaultExtension = language.ProjectFileExtension;
			dlg.Filters = new List<FileDialogFilter>() 
			{
                new FileDialogFilter() { Name = language.Name + " project", Extensions = { language.ProjectFileExtension.TrimStart('.') } },
                new FileDialogFilter() { Name = language.Name + "  single file", Extensions = { language.FileExtension.TrimStart('.') }},
				new FileDialogFilter() { Name = "All files", Extensions = { "*" }}
			};

			var filename = await dlg.ShowAsync(App.Current.GetMainWindow());
			if (!string.IsNullOrEmpty(filename)) {
				DecompilationOptions options = new DecompilationOptions();
				options.FullDecompilation = true;
				if (filename.Contains(language.ProjectFileExtension)) {
					options.SaveAsProjectDirectory = Path.GetDirectoryName(filename);
					foreach (string entry in Directory.GetFileSystemEntries(options.SaveAsProjectDirectory)) {
						if (!string.Equals(entry, filename, StringComparison.OrdinalIgnoreCase)) {
							var result = await MessageBox.Show(
                                Resources.AssemblySaveCodeDirectoryNotEmpty,
                                Resources.AssemblySaveCodeDirectoryNotEmptyTitle,
                                MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
							if (result == MessageBoxResult.No)
								return true; // don't save, but mark the Save operation as handled
							break;
						}
					}
				}
				textView.SaveToDisk(language, new[] { this }, options, filename);
			}
			return true;
		}

		public override string ToString()
		{
			// ToString is used by FindNodeByPath/GetPathForNode
			// Fixes #821 - Reload All Assemblies Should Point to the Correct Assembly
			return LoadedAssembly.FileName;
		}
	}

	[ExportContextMenuEntry(Header = nameof(Resources._Remove), Icon = "Images/Delete.png")]
	sealed class RemoveAssembly : IContextMenuEntry
	{
		public bool IsVisible(TextViewContext context)
		{
			if (context.SelectedTreeNodes == null)
				return false;
			return context.SelectedTreeNodes.All(n => n is AssemblyTreeNode);
		}

		public bool IsEnabled(TextViewContext context)
		{
			return true;
		}

		public void Execute(TextViewContext context)
		{
			if (context.SelectedTreeNodes == null)
				return;
			foreach (var node in context.SelectedTreeNodes) {
				node.Delete();
			}
		}
	}

	[ExportContextMenuEntry(Header = nameof(Resources._Reload), Icon = "Images/Refresh.png")]
	sealed class ReloadAssembly : IContextMenuEntry
	{
		public bool IsVisible(TextViewContext context)
		{
			if (context.SelectedTreeNodes == null)
				return false;
			return context.SelectedTreeNodes.All(n => n is AssemblyTreeNode);
		}

		public bool IsEnabled(TextViewContext context)
		{
			return true;
		}

		public void Execute(TextViewContext context)
		{
			if (context.SelectedTreeNodes == null)
				return;
			var paths = new List<string[]>();
			using (context.TreeView.LockUpdates()) {
				foreach (var node in context.SelectedTreeNodes) {
					paths.Add(MainWindow.GetPathForNode(node));
					var la = ((AssemblyTreeNode)node).LoadedAssembly;
					la.AssemblyList.ReloadAssembly(la.FileName);
				}
			}
			MainWindow.Instance.SelectNodes(paths.Select(p => MainWindow.Instance.FindNodeByPath(p, true)).ToArray());
			MainWindow.Instance.RefreshDecompiledView();
		}
	}

	[ExportContextMenuEntry(Header = nameof(Resources._LoadDependencies), Category = nameof(Resources.Dependencies))]
	sealed class LoadDependencies : IContextMenuEntry
	{
		public bool IsVisible(TextViewContext context)
		{
			if (context.SelectedTreeNodes == null)
				return false;
			return context.SelectedTreeNodes.All(n => n is AssemblyTreeNode);
		}

		public bool IsEnabled(TextViewContext context)
		{
			return true;
		}

		public void Execute(TextViewContext context)
		{
			if (context.SelectedTreeNodes == null)
				return;
			foreach (var node in context.SelectedTreeNodes) {
				var la = ((AssemblyTreeNode)node).LoadedAssembly;
				var module = la.GetPEFileOrNull();
				if (module != null) {
					var metadata = module.Metadata;
					foreach (var assyRef in metadata.AssemblyReferences) {
						la.LookupReferencedAssembly(new AssemblyReference(module, assyRef));
					}
				}
			}
			MainWindow.Instance.RefreshDecompiledView();
		}
	}

	[ExportContextMenuEntry(Header = nameof(Resources._AddMainList), Category = nameof(Resources.Dependencies))]
	sealed class AddToMainList : IContextMenuEntry
	{
		public bool IsVisible(TextViewContext context)
		{
			if (context.SelectedTreeNodes == null)
				return false;
			return context.SelectedTreeNodes.Where(n => n is AssemblyTreeNode).Any(n => ((AssemblyTreeNode)n).IsAutoLoaded);
		}

		public bool IsEnabled(TextViewContext context)
		{
			if (context.SelectedTreeNodes == null)
				return false;
			return context.SelectedTreeNodes.Where(n => n is AssemblyTreeNode).Any(n => !((AssemblyTreeNode)n).LoadedAssembly.FileName.StartsWith("nupkg://"));
		}

		public void Execute(TextViewContext context)
		{
			if (context.SelectedTreeNodes == null)
				return;
			foreach (var node in context.SelectedTreeNodes) {
				var loadedAssm = ((AssemblyTreeNode)node).LoadedAssembly;
				if (!loadedAssm.HasLoadError && !loadedAssm.FileName.StartsWith("nupkg://")) {
					loadedAssm.IsAutoLoaded = false;
					node.RaisePropertyChanged("Foreground");
				}
			}
			MainWindow.Instance.CurrentAssemblyList.RefreshSave();
		}
	}

	[ExportContextMenuEntry(Header = nameof(Resources._OpenContainingFolder), Category = nameof(Resources.Shell))]
	sealed class OpenContainingFolder : IContextMenuEntry
	{
		public bool IsVisible(TextViewContext context)
		{
			if (context.SelectedTreeNodes == null)
				return false;
			return context.SelectedTreeNodes
                .All(n => {
                    var a = GetAssemblyTreeNode(n);
                    return a != null && File.Exists(a.LoadedAssembly.FileName);
                });
        }

        internal static AssemblyTreeNode GetAssemblyTreeNode(SharpTreeNode node)
        {
            while (node != null)
            {
                if (node is AssemblyTreeNode a)
                    return a;
                node = node.Parent;
            }
            return null;
        }


        public bool IsEnabled(TextViewContext context)
		{
			if (context.SelectedTreeNodes == null)
				return false;
			return context.SelectedTreeNodes
                .All(n => {
                    var a = GetAssemblyTreeNode(n);
                    return a != null && File.Exists(a.LoadedAssembly.FileName);
                });
        }

        public void Execute(TextViewContext context)
		{
			if (context.SelectedTreeNodes == null)
				return;
            foreach (var n in context.SelectedTreeNodes) {
                var node = GetAssemblyTreeNode(n);
                var path = Path.GetDirectoryName(node.LoadedAssembly.FileName);
                if (Directory.Exists(path)) {
                    MainWindow.OpenFolder(path);
				}
			}
		}
	}

	[ExportContextMenuEntry(Header = nameof(Resources._OpenCommandLineHere), Category = nameof(Resources.Shell))]
	sealed class OpenCmdHere : IContextMenuEntry
	{
		public bool IsVisible(TextViewContext context)
		{
			if (context.SelectedTreeNodes == null)
				return false;
			return context.SelectedTreeNodes
                .All(n => {
                    var a = OpenContainingFolder.GetAssemblyTreeNode(n);
                    return a != null && File.Exists(a.LoadedAssembly.FileName);
                });
        }

        public bool IsEnabled(TextViewContext context)
		{
			if (context.SelectedTreeNodes == null)
				return false;
			return context.SelectedTreeNodes
                .All(n => {
                    var a = OpenContainingFolder.GetAssemblyTreeNode(n);
                    return a != null && File.Exists(a.LoadedAssembly.FileName);
                });
        }

        public void Execute(TextViewContext context)
		{
			if (context.SelectedTreeNodes == null)
				return;
            foreach (var n in context.SelectedTreeNodes) {
                var node = OpenContainingFolder.GetAssemblyTreeNode(n);
				var path = Path.GetDirectoryName(node.LoadedAssembly.FileName);
				if (Directory.Exists(path)) {
					MainWindow.OpenCommandLine(path);
				}
			}
		}
	}

}
