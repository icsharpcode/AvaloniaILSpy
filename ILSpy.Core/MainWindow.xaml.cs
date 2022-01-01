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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Documentation;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.TypeSystem.Implementation;
using ICSharpCode.ILSpy.Controls;
using ICSharpCode.ILSpy.Search;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.TreeView;
using Microsoft.Win32;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// The main window of the application.
	/// </summary>
	public partial class MainWindow : PlatformDependentWindow, IRoutedCommandBindable
	{
		bool refreshInProgress;
		bool handlingNugetPackageSelection;
		readonly NavigationHistory<NavigationState> history = new NavigationHistory<NavigationState>();
		ILSpySettings spySettingsForMainWindow_Loaded;
		internal SessionSettings sessionSettings;
		
		internal AssemblyListManager assemblyListManager;
		AssemblyList assemblyList;
		AssemblyListTreeNode assemblyListTreeNode;
		
		readonly DecompilerTextView decompilerTextView;
		
		static MainWindow instance;

		internal Menu mainMenu;
		internal ItemsControl toolBar;
		internal ComboBox languageComboBox;
		internal ComboBox languageVersionComboBox;
		internal IControl statusBar;
		internal TextBlock StatusLabel;
		internal Grid mainGrid;
		internal ColumnDefinition leftColumn;
		internal ColumnDefinition rightColumn;
		internal SharpTreeView treeView;
		internal Grid rightPane;
		internal RowDefinition topPaneRow;
		internal RowDefinition textViewRow;
		internal RowDefinition bottomPaneRow;
		internal Border updatePanel;
		internal TextBlock updatePanelMessage;
		internal Button downloadOrCheckUpdateButton;
		internal DockedPane topPane;
		internal ContentPresenter mainPane;
		internal DockedPane bottomPane;

		public static MainWindow Instance {
			get { return instance; }
		}
		
		public SessionSettings SessionSettings {
			get { return sessionSettings; }
		}

		public IList<RoutedCommandBinding> CommandBindings { get; } = new List<RoutedCommandBinding>();

		static MainWindow()
		{
			IsVisibleProperty.Changed.Subscribe(OnShow);
		}

		public MainWindow()
		{
			instance = this;
			var spySettings = ILSpySettings.Load();
			this.spySettingsForMainWindow_Loaded = spySettings;
			this.sessionSettings = new SessionSettings(spySettings);
			this.assemblyListManager = new AssemblyListManager(spySettings);

			InitializeComponent();

			this.DataContext = sessionSettings;

#if DEBUG
			this.AttachDevTools();
#endif

			decompilerTextView = App.ExportProvider.GetExportedValue<DecompilerTextView>();
			mainPane.Content = decompilerTextView;
			
			if (sessionSettings.SplitterPosition > 0 && sessionSettings.SplitterPosition < 1) {
				leftColumn.Width = new GridLength(sessionSettings.SplitterPosition, GridUnitType.Star);
				rightColumn.Width = new GridLength(1 - sessionSettings.SplitterPosition, GridUnitType.Star);
			}
			sessionSettings.FilterSettings.PropertyChanged += filterSettings_PropertyChanged;
			
			ContextMenuProvider.Add(treeView, decompilerTextView);

		}
		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
			mainMenu = this.FindControl<Menu>("mainMenu");
			toolBar = this.FindControl<ItemsControl>("toolBar");
			languageComboBox = this.FindControl<ComboBox>("languageComboBox");
			languageVersionComboBox = this.FindControl<ComboBox>("languageVersionComboBox");
			statusBar = this.FindControl<Border>("statusBar");
			StatusLabel = this.FindControl<TextBlock>("StatusLabel");
			mainGrid = this.FindControl<Grid>("mainGrid");
			leftColumn = mainGrid.ColumnDefinitions[0];
			rightColumn = mainGrid.ColumnDefinitions[2];

			treeView = this.FindControl<SharpTreeView>("treeView");
			treeView.SelectionChanged += TreeView_SelectionChanged;

			rightPane = this.FindControl<Grid>("rightPane");
			topPaneRow = rightPane.RowDefinitions[1];
			textViewRow = rightPane.RowDefinitions[3];
			bottomPaneRow = rightPane.RowDefinitions[5];

			updatePanel = this.FindControl<Border>("updatePanel");
			var updatePanelCloseButton = this.FindControl<Button>("updatePanelCloseButton");
			updatePanelCloseButton.Click += updatePanelCloseButtonClick;
			updatePanelMessage = this.FindControl<TextBlock>("updatePanelMessage");
			downloadOrCheckUpdateButton = this.FindControl<Button>("downloadOrCheckUpdateButton");
			downloadOrCheckUpdateButton.Click += downloadOrCheckUpdateButtonClick;
			topPane = this.FindControl<DockedPane>("topPane");
			topPane.CloseButtonClicked += TopPane_CloseButtonClicked;
			mainPane = this.FindControl<ContentPresenter>("mainPane");
			bottomPane = this.FindControl<DockedPane>("bottomPane");
			bottomPane.CloseButtonClicked += BottomPane_CloseButtonClicked;

			List<string> themeNames = new List<string>();
			List<IStyle> themes = new List<IStyle>();
			foreach(string file in Directory.EnumerateFiles("Themes", "*.xaml"))
			{
				try
				{
					var theme = AvaloniaRuntimeXamlLoader.Parse<Styles>(File.ReadAllText(file));
					themes.Add(theme);
					themeNames.Add(Path.GetFileNameWithoutExtension(file));
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, $"Unable to load theme on {file}");
				}
			}

			if (themes.Count == 0)
			{
				var light = AvaloniaRuntimeXamlLoader.Parse<StyleInclude>(@"<StyleInclude xmlns='https://github.com/avaloniaui' Source='resm:Avalonia.Themes.Default.Accents.BaseLight.xaml?assembly=Avalonia.Themes.Default'/>");
				themes.Add(light);
				themeNames.Add("Light");
			}

			var themesDropDown = this.Find<ComboBox>("Themes");
			themesDropDown.Items = themeNames;
			themesDropDown.SelectionChanged += (sender, e) =>
			{
				Styles[0] = themes[themesDropDown.SelectedIndex];
				sessionSettings.Theme = themeNames[themesDropDown.SelectedIndex];
				ApplyTheme();
			};

			Styles.Add(themes[0]);
			int selectedTheme = themeNames.IndexOf(sessionSettings.Theme);
			themesDropDown.SelectedIndex = selectedTheme < 0? 0: selectedTheme;

			CommandBindings.Add(new RoutedCommandBinding(ApplicationCommands.Open, OpenCommandExecuted));
			CommandBindings.Add(new RoutedCommandBinding(ApplicationCommands.Refresh, RefreshCommandExecuted));
			CommandBindings.Add(new RoutedCommandBinding(ApplicationCommands.Save, SaveCommandExecuted, SaveCommandCanExecute));
			CommandBindings.Add(new RoutedCommandBinding(NavigationCommands.BrowseBack, BackCommandExecuted, BackCommandCanExecute));
			CommandBindings.Add(new RoutedCommandBinding(NavigationCommands.BrowseForward, ForwardCommandExecuted, ForwardCommandCanExecute));
			CommandBindings.Add(new RoutedCommandBinding(NavigationCommands.Search, SearchCommandExecuted));

			TemplateApplied += MainWindow_Loaded;
			KeyDown += MainWindow_KeyDown;
			mainMenu.AttachedToVisualTree += MenuAttached;
		}

		private void MenuAttached(object sender, VisualTreeAttachmentEventArgs e)
		{
			if (NativeMenu.GetIsNativeMenuExported(this) && sender is Menu mainMenu)
			{
				mainMenu.IsVisible = false;
				InitNativeMenu();
			}
			else
			{
				InitMainMenu();
			}
		}

		private void MainWindow_KeyDown(object sender, KeyEventArgs e)
		{
			foreach (var commandBinding in CommandBindings)
			{
				if (commandBinding.Command.Gesture?.Matches(e) == true)
				{
					commandBinding.Command.Execute(null, this);
					e.Handled = true;
					break;
				}
			}
		}

		private void ApplyTheme()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && Styles.TryGetResource("ThemeBackgroundBrush", out object backgroundColor) && backgroundColor is ISolidColorBrush brush)
			{
				// HACK: SetTitleBarColor is a method in Avalonia.Native.WindowImpl
				var setTitleBarColorMethod = PlatformImpl.GetType().GetMethod("SetTitleBarColor");
				setTitleBarColorMethod?.Invoke(PlatformImpl, new object[] { brush.Color });
			}

			if (Styles.TryGetResource("ILAsm-Mode", out object ilasm) && ilasm is string ilmode)
			{
				HighlightingManager.Instance.RegisterHighlighting(
					"ILAsm", new string[] { ".il" },
					delegate
					{
						using (Stream s = File.OpenRead($"Themes/{ilmode}"))
						{
							using (XmlTextReader reader = new XmlTextReader(s))
							{
								return HighlightingLoader.Load(reader, HighlightingManager.Instance);
							}
						}
					});
			}
			else
			{
				HighlightingManager.Instance.RegisterHighlighting(
					"ILAsm", new string[] { ".il" },
					delegate
					{
						using (Stream s = typeof(DecompilerTextView).Assembly.GetManifestResourceStream("ICSharpCode.ILSpy.Themes.ILAsm-Mode.xshd"))
						{
							using (XmlTextReader reader = new XmlTextReader(s))
							{
								return HighlightingLoader.Load(reader, HighlightingManager.Instance);
							}
						}
					});
			}

			if (Styles.TryGetResource("CSharp-Mode", out object csharp) && csharp is string csmode)
			{
				HighlightingManager.Instance.RegisterHighlighting(
				"C#", new string[] { ".cs" },
					delegate
					{
						using (Stream s = File.OpenRead($"Themes/{csmode}"))
						{
							using (XmlTextReader reader = new XmlTextReader(s))
							{
								return HighlightingLoader.Load(reader, HighlightingManager.Instance);
							}
						}
					});
			}
			else
			{
				HighlightingManager.Instance.RegisterHighlighting(
				"C#", new string[] { ".cs" },
					delegate
					{
						using (Stream s = typeof(DecompilerTextView).Assembly.GetManifestResourceStream("ICSharpCode.ILSpy.Themes.CSharp-Mode.xshd"))
						{
							using (XmlTextReader reader = new XmlTextReader(s))
							{
								return HighlightingLoader.Load(reader, HighlightingManager.Instance);
							}
						}
					});
			}

			//Reload text editor
			DecompileSelectedNodes();
		}

		void SetWindowBounds(Rect bounds)
		{
			ClientSize = bounds.Size;
			Position = PixelPoint.FromPoint(bounds.Position, PlatformImpl.DesktopScaling);
		}

		#region Toolbar extensibility

		void InitToolbar()
		{
			int navigationPos = 0;
			int openPos = 1;
			var toolbarCommands = App.ExportProvider.GetExports<ICommand, IToolbarCommandMetadata>("ToolbarCommand");
			var toolbarItems = toolBar.Items as IList<object> ?? new List<object>();
			foreach (var commandGroup in toolbarCommands.OrderBy(c => c.Metadata.ToolbarOrder).GroupBy(c => Properties.Resources.ResourceManager.GetString(c.Metadata.ToolbarCategory))) {
				if (commandGroup.Key == Properties.Resources.ResourceManager.GetString("Navigation")) {
					foreach (var command in commandGroup) {
						toolbarItems.Insert(navigationPos++, MakeToolbarItem(command));
						openPos++;
					}
				} else if (commandGroup.Key == Properties.Resources.ResourceManager.GetString("Open")) {
					foreach (var command in commandGroup) {
						toolbarItems.Insert(openPos++, MakeToolbarItem(command));
					}
				} else {
					toolbarItems.Add(new Separator());
					foreach (var command in commandGroup) {
						toolbarItems.Add(MakeToolbarItem(command));
					}
				}
			}
			toolBar.Items = toolbarItems;
		}

		Button MakeToolbarItem(Lazy<ICommand, IToolbarCommandMetadata> command)
		{
			var toolbarButton = new Button {
				Classes = { "ToolBarItem" },
				Command = CommandWrapper.Unwrap(command.Value),
				Tag = command.Metadata.Tag,
				Content = new Image {
					Width = 16,
					Height = 16,
					Source = Images.LoadImage(command.Value, command.Metadata.ToolbarIcon)
				}
			};
			ToolTip.SetTip(toolbarButton, Properties.Resources.ResourceManager.GetString(command.Metadata.ToolTip));
			return toolbarButton;
		}
		#endregion
		
		#region Main Menu extensibility
		
		void InitMainMenu()
		{
			var mainMenuCommands = App.ExportProvider.GetExports<ICommand, IMainMenuCommandMetadata>("MainMenuCommand");
			var mainMenuItems = mainMenu.Items as IList<object> ?? new List<object>();
			foreach (var topLevelMenu in mainMenuCommands.OrderBy(c => c.Metadata.MenuOrder).GroupBy(c => GetResourceString(c.Metadata.Menu))) {
				MenuItem topLevelMenuItem = mainMenu.Items.OfType<MenuItem>().FirstOrDefault(m => (GetResourceString(m.Header as string)) == topLevelMenu.Key);
				var topLevelMenuItems = topLevelMenuItem?.Items as IList<object> ?? new List<object>();
				foreach (var category in topLevelMenu.GroupBy(c => GetResourceString(c.Metadata.MenuCategory))) {
					if (topLevelMenuItem == null) {
						topLevelMenuItem = new MenuItem();
						topLevelMenuItem.Header = GetResourceString(topLevelMenu.Key);
						mainMenuItems.Add(topLevelMenuItem);
					} else if (topLevelMenuItems.Count > 0) {
						topLevelMenuItems.Add(new Separator());
					}
					foreach (var entry in category) {
						MenuItem menuItem = new MenuItem();
						menuItem.Command = CommandWrapper.Unwrap(entry.Value);
						if (!string.IsNullOrEmpty(GetResourceString(entry.Metadata.Header)))
							menuItem.Header = GetResourceString(entry.Metadata.Header);
						if (!string.IsNullOrEmpty(entry.Metadata.MenuIcon)) {
							menuItem.Icon = new Image {
								Width = 16,
								Height = 16,
								Source = Images.LoadImage(entry.Value, entry.Metadata.MenuIcon)
							};
						}
						
						menuItem.IsEnabled = entry.Metadata.IsEnabled;
						topLevelMenuItems.Add(menuItem);
					}
				}
				topLevelMenuItem.Items = topLevelMenuItems;
			}
			mainMenu.Items = mainMenuItems;
		}

		void InitNativeMenu()
		{
			var mainMenuCommands = App.ExportProvider.GetExports<ICommand, IMainMenuCommandMetadata>("MainMenuCommand");
			var mainMenuItems = NativeMenu.GetMenu(this).Items;
			foreach (var topLevelMenu in mainMenuCommands.OrderBy(c => c.Metadata.MenuOrder).GroupBy(c => GetResourceString(c.Metadata.Menu)))
			{
				NativeMenuItem topLevelMenuItem = mainMenuItems.OfType<NativeMenuItem>().FirstOrDefault(m => (GetResourceString(m.Header as string)) == topLevelMenu.Key);
				if (topLevelMenuItem == null)
				{
					topLevelMenuItem = new NativeMenuItem();
					topLevelMenuItem.Header = GetResourceString(topLevelMenu.Key);
					topLevelMenuItem.Menu = new NativeMenu();
					mainMenuItems.Add(topLevelMenuItem);
				}

				var topLevelMenuItems = topLevelMenuItem.Menu.Items;
				foreach (var category in topLevelMenu.GroupBy(c => GetResourceString(c.Metadata.MenuCategory)))
				{
					if (topLevelMenuItems.Count > 0)
					{
						topLevelMenuItems.Add(new NativeMenuItemSeperator());
					}
					foreach (var entry in category)
					{
						NativeMenuItem menuItem = new NativeMenuItem();
						menuItem.Command = CommandWrapper.Unwrap(entry.Value);
						if (!string.IsNullOrEmpty(GetResourceString(entry.Metadata.Header)))
							menuItem.Header = GetResourceString(entry.Metadata.Header);

						// NOTE: add icon here if Avalonia add icon support for native menu

						menuItem.IsEnabled = entry.Metadata.IsEnabled;
						topLevelMenuItems.Add(menuItem);
					}
				}
			}
			mainMenu.Items = mainMenuItems;
		}

		internal static string GetResourceString(string key)
		{
			var str = !string.IsNullOrEmpty(key)? Properties.Resources.ResourceManager.GetString(key):null;
			return string.IsNullOrEmpty(key) || string.IsNullOrEmpty(str) ? key : str.Replace("_", string.Empty); // avalonia menu doesn't support _ key gesture, thus remove _ 
		}

		#endregion

		#region Message Hook

		static void OnShow(AvaloniaPropertyChangedEventArgs args)
		{
			if (args.Sender == instance && (bool)args.NewValue)
			{
				// Validate and Set Window Bounds
				if (instance.sessionSettings.WindowState == WindowState.Normal)
				{
					var boundsRect = instance.sessionSettings.WindowBounds;
					bool boundsOK = false;
					foreach (var screen in instance.Screens.All)
					{
						var intersection = boundsRect.Intersect(screen.WorkingArea.ToRect(instance.PlatformImpl.DesktopScaling));
						if (intersection.Width > 10 && intersection.Height > 10)
							boundsOK = true;
					}
					if (boundsOK)
						instance.SetWindowBounds(instance.sessionSettings.WindowBounds);
					else
						instance.SetWindowBounds(SessionSettings.DefaultWindowBounds);
				}
				else
				{
					instance.WindowState = instance.sessionSettings.WindowState;
				}
			}
		}
		#endregion
		
		public AssemblyList CurrentAssemblyList {
			get { return assemblyList; }
		}
		
		public event NotifyCollectionChangedEventHandler CurrentAssemblyListChanged;
		
		List<LoadedAssembly> commandLineLoadedAssemblies = new List<LoadedAssembly>();

		List<string> nugetPackagesToLoad = new List<string>();
		
		bool HandleCommandLineArguments(CommandLineArguments args)
		{
			int i = 0;
			while (i < args.AssembliesToLoad.Count) {
				var asm = args.AssembliesToLoad[i];
				if (Path.GetExtension(asm) == ".nupkg") {
					nugetPackagesToLoad.Add(asm);
					args.AssembliesToLoad.RemoveAt(i);
				} else {
					i++;
				}
			}
			LoadAssemblies(args.AssembliesToLoad, commandLineLoadedAssemblies, focusNode: false);
			if (args.Language != null)
				sessionSettings.FilterSettings.Language = Languages.GetLanguage(args.Language);
			return true;
		}

		/// <summary>
		/// Called on startup or when passed arguments via WndProc from a second instance.
		/// In the format case, spySettings is non-null; in the latter it is null.
		/// </summary>
		void HandleCommandLineArgumentsAfterShowList(CommandLineArguments args, ILSpySettings spySettings = null)
		{
			if (nugetPackagesToLoad.Count > 0) {
				var relevantPackages = nugetPackagesToLoad.ToArray();
				nugetPackagesToLoad.Clear();

				// Show the nuget package open dialog after the command line/window message was processed.
				Dispatcher.UIThread.InvokeAsync(new Action(() => LoadAssemblies(relevantPackages, commandLineLoadedAssemblies, focusNode: false)), DispatcherPriority.Normal);

			}
			var relevantAssemblies = commandLineLoadedAssemblies.ToList();
			commandLineLoadedAssemblies.Clear(); // clear references once we don't need them anymore
			NavigateOnLaunch(args.NavigateTo, sessionSettings.ActiveTreeViewPath, spySettings, relevantAssemblies);
			if (args.Search != null)
			{
				SearchPane.Instance.SearchTerm = args.Search;
				SearchPane.Instance.Show();
			}
		}

		async void NavigateOnLaunch(string navigateTo, string[] activeTreeViewPath, ILSpySettings spySettings, List<LoadedAssembly> relevantAssemblies)
		{
			var initialSelection = treeView.SelectedItem;
			if (navigateTo != null) {
				bool found = false;
				if (navigateTo.StartsWith("N:", StringComparison.Ordinal)) {
					string namespaceName = navigateTo.Substring(2);
					foreach (LoadedAssembly asm in relevantAssemblies) {
						AssemblyTreeNode asmNode = assemblyListTreeNode.FindAssemblyNode(asm);
						if (asmNode != null) {
							// FindNamespaceNode() blocks the UI if the assembly is not yet loaded,
							// so use an async wait instead.
							await asm.GetPEFileAsync().Catch<Exception>(ex => { });
							NamespaceTreeNode nsNode = asmNode.FindNamespaceNode(namespaceName);
							if (nsNode != null) {
								found = true;
								if (treeView.SelectedItem == initialSelection) {
									SelectNode(nsNode);
								}
								break;
							}
						}
					}
				}
				else if (navigateTo == "none")
				{
					// Don't navigate anywhere; start empty.
					// Used by ILSpy VS addin, it'll send us the real location to navigate to via IPC.
					found = true;

				} else {
					IEntity mr = await Task.Run(() => FindEntityInRelevantAssemblies(navigateTo, relevantAssemblies));
					if (mr != null && mr.ParentModule.PEFile != null) {
						found = true;
						if (treeView.SelectedItem == initialSelection) {
							JumpToReference(mr);
						}
					}
				}
				if (!found && treeView.SelectedItem == initialSelection) {
					AvaloniaEditTextOutput output = new AvaloniaEditTextOutput();
					output.Write(string.Format("Cannot find '{0}' in command line specified assemblies.", navigateTo));
					decompilerTextView.ShowText(output);
				}
			} else if (relevantAssemblies.Count == 1) {
				// NavigateTo == null and an assembly was given on the command-line:
				// Select the newly loaded assembly
				AssemblyTreeNode asmNode = assemblyListTreeNode.FindAssemblyNode(relevantAssemblies[0]);
				if (asmNode != null && treeView.SelectedItem == initialSelection) {
					SelectNode(asmNode);
				}
			} else if (spySettings != null) {
				SharpTreeNode node = null;
				if (activeTreeViewPath?.Length > 0) {
					foreach (var asm in assemblyList.GetAssemblies()) {
						if (asm.FileName == activeTreeViewPath[0]) {
							// FindNodeByPath() blocks the UI if the assembly is not yet loaded,
							// so use an async wait instead.
							await asm.GetPEFileAsync().Catch<Exception>(ex => { });
						}
					}
					node = FindNodeByPath(activeTreeViewPath, true);
				}
				if (treeView.SelectedItem == initialSelection) {
					if (node != null) {
						SelectNode(node);

						// only if not showing the about page, perform the update check:
						ShowMessageIfUpdatesAvailableAsync(spySettings);
					} else {
						AboutPage.Display(decompilerTextView);
					}
				}
			}
		}

		private IEntity FindEntityInRelevantAssemblies(string navigateTo, IEnumerable<LoadedAssembly> relevantAssemblies)
		{
			ITypeReference typeRef = null;
			IMemberReference memberRef = null;
			if (navigateTo.StartsWith("T:", StringComparison.Ordinal)) {
				typeRef = IdStringProvider.ParseTypeName(navigateTo);
			}
			else {
				memberRef = IdStringProvider.ParseMemberIdString(navigateTo);
				typeRef = memberRef.DeclaringTypeReference;
			}
			foreach (LoadedAssembly asm in relevantAssemblies.ToList()) {
				var module = asm.GetPEFileOrNull();
				if (CanResolveTypeInPEFile(module, typeRef, out var typeHandle)) {
					ICompilation compilation = typeHandle.Kind == HandleKind.ExportedType
						? new DecompilerTypeSystem(module, module.GetAssemblyResolver())
						: new SimpleCompilation(module, MinimalCorlib.Instance);
					return memberRef == null
						? typeRef.Resolve(new SimpleTypeResolveContext(compilation)) as ITypeDefinition
						: (IEntity)memberRef.Resolve(new SimpleTypeResolveContext(compilation));
				}
			}
			return null;
		}

		private bool CanResolveTypeInPEFile(PEFile module, ITypeReference typeRef, out EntityHandle typeHandle)
		{
			switch (typeRef)
			{
				case GetPotentiallyNestedClassTypeReference topLevelType:
					typeHandle = topLevelType.ResolveInPEFile(module);
					return !typeHandle.IsNil;
				case NestedTypeReference nestedType:
					if (!CanResolveTypeInPEFile(module, nestedType.DeclaringTypeReference, out typeHandle))
						return false;
					if (typeHandle.Kind == HandleKind.ExportedType)
						return true;
					var typeDef = module.Metadata.GetTypeDefinition((TypeDefinitionHandle)typeHandle);
					typeHandle = typeDef.GetNestedTypes().FirstOrDefault(t => {
						var td = module.Metadata.GetTypeDefinition(t);
						var typeName = ReflectionHelper.SplitTypeParameterCountFromReflectionName(module.Metadata.GetString(td.Name), out int typeParameterCount);
						return nestedType.AdditionalTypeParameterCount == typeParameterCount && nestedType.Name == typeName;
					});
					return !typeHandle.IsNil;
				default:
					typeHandle = default;
					return false;
			}
		}

		void MainWindow_Loaded(object sender, EventArgs e)
		{
			Application.Current.FocusManager.Focus(this);

			InitToolbar();

			ILSpySettings spySettings = this.spySettingsForMainWindow_Loaded;
			this.spySettingsForMainWindow_Loaded = null;
			var loadPreviousAssemblies = Options.MiscSettingsPanel.CurrentMiscSettings.LoadPreviousAssemblies;

			if (loadPreviousAssemblies) {
				// Load AssemblyList only in Loaded event so that WPF is initialized before we start the CPU-heavy stuff.
				// This makes the UI come up a bit faster.
				this.assemblyList = assemblyListManager.LoadList(spySettings, sessionSettings.ActiveAssemblyList);
			} else {
				this.assemblyList = new AssemblyList(AssemblyListManager.DefaultListName);
				assemblyListManager.ClearAll();
			}

			HandleCommandLineArguments(App.CommandLineArguments);

			if (assemblyList.GetAssemblies().Length == 0
				&& assemblyList.ListName == AssemblyListManager.DefaultListName
				&& loadPreviousAssemblies) {
				LoadInitialAssemblies();
			}

			ShowAssemblyList(this.assemblyList);

			if (sessionSettings.ActiveAutoLoadedAssembly != null) {
				this.assemblyList.Open(sessionSettings.ActiveAutoLoadedAssembly, true);
			}

			Dispatcher.UIThread.InvokeAsync(new Action(() => OpenAssemblies(spySettings)), DispatcherPriority.Loaded);
		}

		void OpenAssemblies(ILSpySettings spySettings)
		{
			HandleCommandLineArgumentsAfterShowList(App.CommandLineArguments, spySettings);

			AvaloniaEditTextOutput output = new AvaloniaEditTextOutput();
			if (FormatExceptions(App.StartupExceptions.ToArray(), output))
				decompilerTextView.ShowText(output);
		}
		
		bool FormatExceptions(App.ExceptionData[] exceptions, ITextOutput output)
		{
			var stringBuilder = new StringBuilder();
			var result = FormatExceptions(exceptions, stringBuilder);
			if (result)
			{
				output.Write(stringBuilder.ToString());
			}
			return result;
		}

		internal static bool FormatExceptions(App.ExceptionData[] exceptions, StringBuilder output)
		{
			if (exceptions.Length == 0) return false;
			bool first = true;
			
			foreach (var item in exceptions) {
				if (first)
					first = false;
				else
					output.AppendLine("-------------------------------------------------");
				output.AppendLine("Error(s) loading plugin: " + item.PluginName);
				if (item.Exception is System.Reflection.ReflectionTypeLoadException) {
					var e = (System.Reflection.ReflectionTypeLoadException)item.Exception;
					foreach (var ex in e.LoaderExceptions) {
						output.AppendLine(ex.ToString());
						output.AppendLine();
					}
				} else
					output.AppendLine(item.Exception.ToString());
			}
			
			return true;
		}
		
		#region Update Check
		string updateAvailableDownloadUrl;
		
		public void ShowMessageIfUpdatesAvailableAsync(ILSpySettings spySettings, bool forceCheck = false)
		{
			Task<string> result;
			if (forceCheck) {
				result = AboutPage.CheckForUpdatesAsync(spySettings);
			} else {
				result = AboutPage.CheckForUpdatesIfEnabledAsync(spySettings);
			}
			result.ContinueWith(task => AdjustUpdateUIAfterCheck(task, forceCheck), TaskScheduler.FromCurrentSynchronizationContext());
		}
		
		void updatePanelCloseButtonClick(object sender, RoutedEventArgs e)
		{
			updatePanel.IsVisible = false;
		}
		
		void downloadOrCheckUpdateButtonClick(object sender, RoutedEventArgs e)
		{
			if (updateAvailableDownloadUrl != null) {
				MainWindow.OpenLink(updateAvailableDownloadUrl);
			} else {
				updatePanel.IsVisible = false;
				AboutPage.CheckForUpdatesAsync(ILSpySettings.Load())
					.ContinueWith(task => AdjustUpdateUIAfterCheck(task, true), TaskScheduler.FromCurrentSynchronizationContext());
			}
		}

		void AdjustUpdateUIAfterCheck(Task<string> task, bool displayMessage)
		{
			updateAvailableDownloadUrl = task.Result;
			updatePanel.IsVisible = displayMessage ? true : false;
			if (task.Result != null) {
				updatePanelMessage.Text = Properties.Resources.ILSpyVersionAvailable;
				downloadOrCheckUpdateButton.Content = Properties.Resources.Download;
			}
			else {
				updatePanelMessage.Text = Properties.Resources.UpdateILSpyFound;
				downloadOrCheckUpdateButton.Content = Properties.Resources.CheckAgain;
			}
		}
		#endregion
		
		public void ShowAssemblyList(string name)
		{
			ILSpySettings settings = ILSpySettings.Load();
			AssemblyList list = this.assemblyListManager.LoadList(settings, name);
			//Only load a new list when it is a different one
			if (list.ListName != CurrentAssemblyList.ListName)
			{
				ShowAssemblyList(list);
			}
		}
		
		void ShowAssemblyList(AssemblyList assemblyList)
		{
			history.Clear();
			this.assemblyList = assemblyList;
			
			assemblyList.assemblies.CollectionChanged += assemblyList_Assemblies_CollectionChanged;
			
			assemblyListTreeNode = new AssemblyListTreeNode(assemblyList);
			assemblyListTreeNode.FilterSettings = sessionSettings.FilterSettings.Clone();
			assemblyListTreeNode.Select = SelectNode;
			treeView.Root = assemblyListTreeNode;
			
			if (assemblyList.ListName == AssemblyListManager.DefaultListName)
#if DEBUG
				this.Title = $"ILSpy {RevisionClass.FullVersion}";
#else
				this.Title = "ILSpy";
#endif
			else
#if DEBUG
				this.Title = $"ILSpy {RevisionClass.FullVersion} - " + assemblyList.ListName;
#else
				this.Title = "ILSpy - " + assemblyList.ListName;
#endif
		}

		void assemblyList_Assemblies_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == NotifyCollectionChangedAction.Reset) {
				history.RemoveAll(_ => true);
			}
			if (e.OldItems != null) {
				var oldAssemblies = new HashSet<LoadedAssembly>(e.OldItems.Cast<LoadedAssembly>());
				history.RemoveAll(n => n.TreeNodes.Any(
					nd => nd.AncestorsAndSelf().OfType<AssemblyTreeNode>().Any(
						a => oldAssemblies.Contains(a.LoadedAssembly))));
			}
			if (CurrentAssemblyListChanged != null)
				CurrentAssemblyListChanged(this, e);
		}

		void LoadInitialAssemblies()
		{
			// Called when loading an empty assembly list; so that
			// the user can see something initially.
			System.Reflection.Assembly[] initialAssemblies = {
				typeof(object).Assembly,
				typeof(Uri).Assembly,
				typeof(System.Linq.Enumerable).Assembly,
				typeof(System.Xml.XmlDocument).Assembly,
			};
			foreach (System.Reflection.Assembly asm in initialAssemblies)
				assemblyList.OpenAssembly(asm.Location);
		}

		void filterSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			RefreshTreeViewFilter();
			if (e.PropertyName == "Language" || e.PropertyName == "LanguageVersion") {
				DecompileSelectedNodes(recordHistory: false);
			}
		}

		public void RefreshTreeViewFilter()
		{
			// filterSettings is mutable; but the ILSpyTreeNode filtering assumes that filter settings are immutable.
			// Thus, the main window will use one mutable instance (for data-binding), and assign a new clone to the ILSpyTreeNodes whenever the main
			// mutable instance changes.
			if (assemblyListTreeNode != null)
				assemblyListTreeNode.FilterSettings = sessionSettings.FilterSettings.Clone();
		}

		internal AssemblyListTreeNode AssemblyListTreeNode {
			get { return assemblyListTreeNode; }
		}

		#region Node Selection

		public void SelectNode(SharpTreeNode obj)
		{
			if (obj != null) {
				if (!obj.AncestorsAndSelf().Any(node => node.IsHidden)) {
					// Set both the selection and focus to ensure that keyboard navigation works as expected.
					treeView.FocusNode(obj);
					treeView.SelectedItem = obj;
				} else {
					MessageBox.Show("Navigation failed because the target is hidden or a compiler-generated class.\n" +
						"Please disable all filters that might hide the item (i.e. activate " +
						"\"View > Show internal types and members\") and try again.",
						"ILSpy", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				}
			}
		}

		public void SelectNodes(IEnumerable<SharpTreeNode> nodes)
		{
			if (nodes.Any() && nodes.All(n => !n.AncestorsAndSelf().Any(a => a.IsHidden))) {
				treeView.FocusNode(nodes.First());
				treeView.SetSelectedNodes(nodes);
			}
		}

		/// <summary>
		/// Retrieves a node using the .ToString() representations of its ancestors.
		/// </summary>
		public SharpTreeNode FindNodeByPath(string[] path, bool returnBestMatch)
		{
			if (path == null)
				return null;
			SharpTreeNode node = treeView.Root;
			SharpTreeNode bestMatch = node;
			foreach (var element in path) {
				if (node == null)
					break;
				bestMatch = node;
				node.EnsureLazyChildren();
				var ilSpyTreeNode = node as ILSpyTreeNode;
				if (ilSpyTreeNode != null)
					ilSpyTreeNode.EnsureChildrenFiltered();
				node = node.Children.FirstOrDefault(c => c.ToString() == element);
			}
			if (returnBestMatch)
				return node ?? bestMatch;
			else
				return node;
		}

		/// <summary>
		/// Gets the .ToString() representation of the node's ancestors.
		/// </summary>
		public static string[] GetPathForNode(SharpTreeNode node)
		{
			if (node == null)
				return null;
			List<string> path = new List<string>();
			while (node.Parent != null) {
				path.Add(node.ToString());
				node = node.Parent;
			}
			path.Reverse();
			return path.ToArray();
		}

		public ILSpyTreeNode FindTreeNode(object reference)
		{
			switch (reference) {
				case PEFile asm:
					return assemblyListTreeNode.FindAssemblyNode(asm);
				case Resource res:
					return assemblyListTreeNode.FindResourceNode(res);
				case ITypeDefinition type:
					return assemblyListTreeNode.FindTypeNode(type);
				case IField fd:
					return assemblyListTreeNode.FindFieldNode(fd);
				case IMethod md:
					return assemblyListTreeNode.FindMethodNode(md);
				case IProperty pd:
					return assemblyListTreeNode.FindPropertyNode(pd);
				case IEvent ed:
					return assemblyListTreeNode.FindEventNode(ed);
				default:
					return null;
			}
		}

		public void JumpToReference(object reference)
		{
			JumpToReferenceAsync(reference).HandleExceptions();
		}

		/// <summary>
		/// Jumps to the specified reference.
		/// </summary>
		/// <returns>
		/// Returns a task that will signal completion when the decompilation of the jump target has finished.
		/// The task will be marked as canceled if the decompilation is canceled.
		/// </returns>
		public Task JumpToReferenceAsync(object reference)
		{
			decompilationTask = TaskHelper.CompletedTask;
			switch (reference) {
				case Decompiler.Disassembler.OpCodeInfo opCode:
					OpenLink(opCode.Link);
					break;
				case ValueTuple<PEFile, System.Reflection.Metadata.EntityHandle> unresolvedEntity:
					var typeSystem = new DecompilerTypeSystem(unresolvedEntity.Item1, unresolvedEntity.Item1.GetAssemblyResolver(),
						TypeSystemOptions.Default | TypeSystemOptions.Uncached);
					reference = typeSystem.MainModule.ResolveEntity(unresolvedEntity.Item2);
					goto default;
				default:
					ILSpyTreeNode treeNode = FindTreeNode(reference);
					if (treeNode != null)
						SelectNode(treeNode);
					break;
			}
			return decompilationTask;
		}

		/// <summary>
		/// TODO: Opens the link in browser.
		/// </summary>
		/// <param name="link">Link.</param>
		public static void OpenLink(string link)
		{
			try {
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					Process.Start(link);
				}
				else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
				{
					Process.Start("open", link);
				}
				else
				{
					Process.Start("xdg-open", link);
				}
#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
			} catch (Exception) {
#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
				// Process.Start can throw several errors (not all of them documented),
				// just ignore all of them.
			}
		}

		/// <summary>
		/// open conatiner folder
		/// </summary>
		/// <param name="path">full path</param>
		public static void OpenFolder(string path)
		{
			try
			{
				if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					Process.Start("explorer.exe", $"/select,\"{path}\"");
				}
				else if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
				{
					Process.Start("open", path);
				}
				else
				{
					Process.Start("xdg-open", path);
				}
#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
			}
			catch (Exception)
			{
#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
				// Process.Start can throw several errors (not all of them documented),
				// just ignore all of them.
			}
		}

		/// <summary>
		/// Open command line interface
		/// </summary>
		/// <param name="path">Full path.</param>
		public static void OpenCommandLine(string path)
		{
			try
			{
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					Process.Start("cmd.exe", $"/k \"cd {path}\"");
				}
				else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
				{
					Process.Start("/Applications/Utilities/Terminal.app/Contents/MacOS/Terminal", path);
				}
				else
				{
					Process.Start("xterm", $"-e \"cd {path}\"; bash");
				}
#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
			} catch (Exception) {
#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
				// Process.Start can throw several errors (not all of them documented),
				// just ignore all of them.
			}
		}
		#endregion

		#region Open/Refresh
		async void OpenCommandExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			e.Handled = true;
			OpenFileDialog dlg = new OpenFileDialog();
			dlg.Title = "Open file";
			dlg.Filters = new List<FileDialogFilter>()
			{
				new FileDialogFilter() { Name = ".NET assemblies", Extensions = {"dll","exe", "winmd" }},
				new FileDialogFilter() { Name = "Nuget Packages (*.nupkg)", Extensions = { "nupkg" }},
				new FileDialogFilter() { Name = "All files", Extensions = { "*" }},
			};
			dlg.AllowMultiple = true;
			//dlg.RestoreDirectory = true;
			var filenames = await dlg.ShowAsync(this);
			if (filenames != null && filenames.Length > 0)
			{
				OpenFiles(filenames);
			}
		}
		
		public void OpenFiles(string[] fileNames, bool focusNode = true)
		{
			if (fileNames == null)
				throw new ArgumentNullException(nameof(fileNames));

			if (focusNode)
				treeView.UnselectAll();

			LoadAssemblies(fileNames, focusNode: focusNode);
		}

		async void LoadAssemblies(IEnumerable<string> fileNames, List<LoadedAssembly> loadedAssemblies = null, bool focusNode = true)
		{
			SharpTreeNode lastNode = null;
			foreach (string file in fileNames) {
				switch (Path.GetExtension(file)) {
					case ".nupkg":
						this.handlingNugetPackageSelection = true;
						try
						{
							LoadedNugetPackage package = new LoadedNugetPackage(file);
							var selectionDialog = new NugetPackageBrowserDialog(package, this);
							if (await selectionDialog.ShowDialog<bool>(this) != true)
								break;
							foreach (var entry in selectionDialog.SelectedItems)
							{
								var nugetAsm = assemblyList.OpenAssembly("nupkg://" + file + ";" + entry.Name, entry.Stream, true);
								if (nugetAsm != null)
								{
									if (loadedAssemblies != null)
										loadedAssemblies.Add(nugetAsm);
									else
									{
										var node = assemblyListTreeNode.FindAssemblyNode(nugetAsm);
										if (node != null && focusNode)
										{
											treeView.SelectedItems.Add(node);
											lastNode = node;
										}
									}
								}
							}
						}
						finally
						{
							this.handlingNugetPackageSelection = false;
						}

						break;
					default:
						var asm = assemblyList.OpenAssembly(file);
						if (asm != null) {
							if (loadedAssemblies != null)
								loadedAssemblies.Add(asm);
							else {
								var node = assemblyListTreeNode.FindAssemblyNode(asm);
								if (node != null && focusNode) {
									lastNode = node;
								}
							}
						}
						break;
				}

				if (lastNode != null && focusNode)
					treeView.FocusNode(lastNode);
			}

			
			// Select only the last node to avoid multi selection
			if(lastNode != null) {
				treeView.SelectedItem = lastNode;
            }
		}
		
		void RefreshCommandExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			try
			{
				refreshInProgress = true;
				var path = GetPathForNode(treeView.SelectedItem as SharpTreeNode);
				ShowAssemblyList(assemblyListManager.LoadList(ILSpySettings.Load(), assemblyList.ListName));
				SelectNode(FindNodeByPath(path, true));
			} finally {
				refreshInProgress = false;
			}

		}
		
		void SearchCommandExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			SearchPane.Instance.Show();
		}
		#endregion
		
		#region Decompile (TreeView_SelectionChanged)
		void TreeView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			DecompileSelectedNodes();

			if (SelectionChanged != null)
				SelectionChanged(sender, e);
		}

		Task decompilationTask;
		bool ignoreDecompilationRequests;

		void DecompileSelectedNodes(DecompilerTextViewState state = null, bool recordHistory = true)
		{
			if (ignoreDecompilationRequests)
				return;

			if (treeView.SelectedItems.Count == 0 && refreshInProgress)
				return;

			if (decompilerTextView == null)
				return;

			if (recordHistory) {
				var dtState = decompilerTextView.GetState();
				if(dtState != null)
					history.UpdateCurrent(new NavigationState(dtState));
				history.Record(new NavigationState(treeView.SelectedItems.OfType<SharpTreeNode>()));
			}
			
			if (treeView.SelectedItems.Count == 1) {
				ILSpyTreeNode node = treeView.SelectedItem as ILSpyTreeNode;
				if (node != null && node.View(decompilerTextView))
					return;
			}
			decompilationTask = decompilerTextView.DecompileAsync(this.CurrentLanguage, this.SelectedNodes, new DecompilationOptions() { TextViewState = state });
		}

		void SaveCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.Handled = true;
			e.CanExecute = SaveCodeContextMenuEntry.CanExecute(SelectedNodes.ToList());
		}

		void SaveCommandExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			SaveCodeContextMenuEntry.Execute(SelectedNodes.ToList());
		}

		public void RefreshDecompiledView()
		{
			try
			{
				refreshInProgress = true;
				DecompileSelectedNodes();
			} finally {
				refreshInProgress = false;
			}

		}
		
		public DecompilerTextView TextView {
			get { return decompilerTextView; }
		}

		public Language CurrentLanguage => sessionSettings.FilterSettings.Language;
		public LanguageVersion CurrentLanguageVersion => sessionSettings.FilterSettings.LanguageVersion;

		public event EventHandler<SelectionChangedEventArgs> SelectionChanged;

		public IEnumerable<ILSpyTreeNode> SelectedNodes {
			get {
				return treeView.GetTopLevelSelection().OfType<ILSpyTreeNode>();
			}
		}
		#endregion

		#region Back/Forward navigation
		void BackCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.Handled = true;
			e.CanExecute = history.CanNavigateBack;
		}

		void BackCommandExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			if (history.CanNavigateBack) {
				e.Handled = true;
				NavigateHistory(false);
			}
		}

		void ForwardCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.Handled = true;
			e.CanExecute = history.CanNavigateForward;
		}

		void ForwardCommandExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			if (history.CanNavigateForward) {
				e.Handled = true;
				NavigateHistory(true);
			}
		}

		void NavigateHistory(bool forward)
		{
			var dtState = decompilerTextView.GetState();
			if (dtState != null)
				history.UpdateCurrent(new NavigationState(dtState));
			var newState = forward ? history.GoForward() : history.GoBack();

			ignoreDecompilationRequests = true;
			treeView.SelectedItems.Clear();
			foreach (var node in newState.TreeNodes) {
				treeView.SelectedItems.Add(node);
			}
			if (newState.TreeNodes.Any())
				treeView.FocusNode(newState.TreeNodes.First());
			ignoreDecompilationRequests = false;
			DecompileSelectedNodes(newState.ViewState, false);
		}

		#endregion

		protected override void HandleWindowStateChanged(WindowState state)
		{
			base.HandleWindowStateChanged(state);
			// store window state in settings only if it's not minimized
			if (this.WindowState != WindowState.Minimized)
				sessionSettings.WindowState = this.WindowState;
		}

		protected override bool HandleClosing()
		{
			sessionSettings.ActiveAssemblyList = assemblyList.ListName;
			sessionSettings.ActiveTreeViewPath = GetPathForNode(treeView.SelectedItem as SharpTreeNode);
			sessionSettings.ActiveAutoLoadedAssembly = GetAutoLoadedAssemblyNode(treeView.SelectedItem as SharpTreeNode);
			sessionSettings.WindowBounds = new Rect(Position.ToPoint(PlatformImpl.DesktopScaling), ClientSize);
			sessionSettings.SplitterPosition = leftColumn.Width.Value / (leftColumn.Width.Value + rightColumn.Width.Value);
			if (topPane.IsVisible == true)
				sessionSettings.TopPaneSplitterPosition = topPaneRow.Height.Value / (topPaneRow.Height.Value + textViewRow.Height.Value);
			if (bottomPane.IsVisible == true)
				sessionSettings.BottomPaneSplitterPosition = bottomPaneRow.Height.Value / (bottomPaneRow.Height.Value + textViewRow.Height.Value);
			sessionSettings.Save();

			return base.HandleClosing();
		}

		private string GetAutoLoadedAssemblyNode(SharpTreeNode node)
		{
			if (node == null)
				return null;
			while (!(node is TreeNodes.AssemblyTreeNode) && node.Parent != null) {
				node = node.Parent;
			}
			//this should be an assembly node
			var assyNode = node as TreeNodes.AssemblyTreeNode;
			var loadedAssy = assyNode.LoadedAssembly;
			if (!(loadedAssy.IsLoaded && loadedAssy.IsAutoLoaded))
				return null;

			return loadedAssy.FileName;
		}

		#region Top/Bottom Pane management

		/// <summary>
		///   When grid is resized using splitter, row height value could become greater than 1.
		///   As result, when a new pane is shown, both textView and pane could become very small.
		///   This method normalizes two rows and ensures that height is less then 1.
		/// </summary>
		void NormalizePaneRowHeightValues(RowDefinition pane1Row, RowDefinition pane2Row)
		{
			var pane1Height = pane1Row.Height;
			var pane2Height = pane2Row.Height;

			//only star height values are normalized.
			if (!pane1Height.IsStar || !pane2Height.IsStar) {
				return;
			}

			var totalHeight = pane1Height.Value + pane2Height.Value;
			if (totalHeight == 0) {
				return;
			}

			pane1Row.Height = new GridLength(pane1Height.Value / totalHeight, GridUnitType.Star);
			pane2Row.Height = new GridLength(pane2Height.Value / totalHeight, GridUnitType.Star);
		}

		public void ShowInTopPane(string title, object content)
		{
			topPaneRow.MinHeight = 100;
			if (sessionSettings.TopPaneSplitterPosition > 0 && sessionSettings.TopPaneSplitterPosition < 1) {
				//Ensure all 3 blocks are in fair conditions
				NormalizePaneRowHeightValues(bottomPaneRow, textViewRow);

				textViewRow.Height = new GridLength(1 - sessionSettings.TopPaneSplitterPosition, GridUnitType.Star);
				topPaneRow.Height = new GridLength(sessionSettings.TopPaneSplitterPosition, GridUnitType.Star);
			}
			topPane.Title = title;
			if (topPane.Content != content) {
				IPane pane = topPane.Content as IPane;
				if (pane != null)
					pane.Closed();
				topPane.Content = content;
			}
			topPane.IsVisible = true;
		}
		
		void TopPane_CloseButtonClicked(object sender, EventArgs e)
		{
			sessionSettings.TopPaneSplitterPosition = topPaneRow.Height.Value / (topPaneRow.Height.Value + textViewRow.Height.Value);
			topPaneRow.MinHeight = 0;
			topPaneRow.Height = new GridLength(0);
			topPane.IsVisible = false;
			
			IPane pane = topPane.Content as IPane;
			topPane.Content = null;
			if (pane != null)
				pane.Closed();
		}
		
		public void ShowInBottomPane(string title, object content)
		{
			bottomPaneRow.MinHeight = 100;
			if (sessionSettings.BottomPaneSplitterPosition > 0 && sessionSettings.BottomPaneSplitterPosition < 1) {
				//Ensure all 3 blocks are in fair conditions
				NormalizePaneRowHeightValues(topPaneRow, textViewRow);

				textViewRow.Height = new GridLength(1 - sessionSettings.BottomPaneSplitterPosition, GridUnitType.Star);
				bottomPaneRow.Height = new GridLength(sessionSettings.BottomPaneSplitterPosition, GridUnitType.Star);
			}
			bottomPane.Title = title;
			if (bottomPane.Content != content) {
				IPane pane = bottomPane.Content as IPane;
				if (pane != null)
					pane.Closed();
				bottomPane.Content = content;
			}
			bottomPane.IsVisible = true;
		}
		
		void BottomPane_CloseButtonClicked(object sender, EventArgs e)
		{
			sessionSettings.BottomPaneSplitterPosition = bottomPaneRow.Height.Value / (bottomPaneRow.Height.Value + textViewRow.Height.Value);
			bottomPaneRow.MinHeight = 0;
			bottomPaneRow.Height = new GridLength(0);
			bottomPane.IsVisible = false;
			
			IPane pane = bottomPane.Content as IPane;
			bottomPane.Content = null;
			if (pane != null)
				pane.Closed();
		}
		#endregion
		
		public void UnselectAll()
		{
			treeView.UnselectAll();
		}
		
		public void SetStatus(string status, IBrush foreground)
		{
			if (this.statusBar.IsVisible == false)
				this.statusBar.IsVisible = true;
			this.StatusLabel.Foreground = foreground;
			this.StatusLabel.Text = status;
		}
		
		public IEnumerable GetMainMenuItems()
		{
			return mainMenu.Items;
		}
		
		public IEnumerable GetToolBarItems()
		{
			return toolBar.Items;
		}
	}
}
