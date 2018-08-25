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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using System.Windows.Input;
using Avalonia.Media;
using Avalonia.Threading;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Documentation;
using AvaloniaILSpy.TextView;
using AvaloniaILSpy.TreeNodes;
using AvaloniaILSpy.Controls;
using Mono.Cecil;
using Avalonia.Interactivity;
using AvaloniaEdit;
using System.Collections;
using Avalonia.Controls.Presenters;
using Avalonia.Markup.Xaml;

namespace AvaloniaILSpy
{
    /// <summary>
    /// The main window of the application.
    /// </summary>
    partial class MainWindow : Window, IRoutedCommandBindable
    {
        readonly NavigationHistory<NavigationState> history = new NavigationHistory<NavigationState>();
        ILSpySettings spySettings;
        internal SessionSettings sessionSettings;
        
        internal AssemblyListManager assemblyListManager;
        AssemblyList assemblyList;
        AssemblyListTreeNode assemblyListTreeNode;
        
        readonly DecompilerTextView decompilerTextView;
        
        static MainWindow instance;

        internal Menu mainMenu;
        internal ItemsControl toolBar;
        internal DropDown languageComboBox;
        internal DropDown languageVersionComboBox;
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

        public MainWindow()
        {
            instance = this;
            spySettings = ILSpySettings.Load();
            this.sessionSettings = new SessionSettings(spySettings);
            this.assemblyListManager = new AssemblyListManager(spySettings);
            
            //this.Icon = new WindowIcon(new Bitmap("pack://application:,,,/ILSpy;component/images/ILSpy.ico"));
            
            this.DataContext = sessionSettings;

            InitializeComponent();

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
            languageComboBox = this.FindControl<DropDown>("languageComboBox");
            languageVersionComboBox = this.FindControl<DropDown>("languageVersionComboBox");
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

            CommandBindings.Add(new RoutedCommandBinding(ApplicationCommands.Open, OpenCommandExecuted));
            CommandBindings.Add(new RoutedCommandBinding(ApplicationCommands.Refresh, RefreshCommandExecuted));
            CommandBindings.Add(new RoutedCommandBinding(ApplicationCommands.Save, SaveCommandExecuted));
            CommandBindings.Add(new RoutedCommandBinding(NavigationCommands.BrowseBack, BackCommandExecuted, BackCommandCanExecute));
            CommandBindings.Add(new RoutedCommandBinding(NavigationCommands.BrowseForward, ForwardCommandExecuted, ForwardCommandCanExecute));
            CommandBindings.Add(new RoutedCommandBinding(NavigationCommands.Search, SearchCommandExecuted));

            TemplateApplied += MainWindow_Loaded;
        }

        void SetWindowBounds(Rect bounds)
        {
            this.Position = bounds.Position;
            //this.Left = bounds.Left;
            //this.Top = bounds.Top;
            this.Width = bounds.Width;
            this.Height = bounds.Height;
        }
        
        #region Toolbar extensibility
        
        void InitToolbar()
        {
            int navigationPos = 0;
            int openPos = 1;
            var toolbarCommands = App.ExportProvider.GetExports<ICommand, IToolbarCommandMetadata>("ToolbarCommand");
            var toolbarItems = toolBar.Items.Cast<IControl>().ToList();
            foreach (var commandGroup in toolbarCommands.OrderBy(c => c.Metadata.ToolbarOrder).GroupBy(c => c.Metadata.ToolbarCategory)) {
                if (commandGroup.Key == "Navigation") {
                    foreach (var command in commandGroup) {
                        toolbarItems.Insert(navigationPos++, MakeToolbarItem(command));
                        openPos++;
                    }
                } else if (commandGroup.Key == "Open") {
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
            return new Button {
                Command = CommandWrapper.Unwrap(command.Value),
                //ToolTip = command.Metadata.ToolTip,
                Tag = command.Metadata.Tag,
                Content = new Image {
                    Width = 16,
                    Height = 16,
                    Source = Images.LoadImage(command.Value, command.Metadata.ToolbarIcon)
                }
            };
        }
        #endregion
        
        #region Main Menu extensibility
        
        void InitMainMenu()
        {
            var mainMenuCommands = App.ExportProvider.GetExports<ICommand, IMainMenuCommandMetadata>("MainMenuCommand");
            List<IControl> mainMenuItems = mainMenu.Items.Cast<IControl>().ToList();
            foreach (var topLevelMenu in mainMenuCommands.OrderBy(c => c.Metadata.MenuOrder).GroupBy(c => c.Metadata.Menu)) {
                MenuItem topLevelMenuItem = mainMenu.Items.OfType<MenuItem>().FirstOrDefault(m => (m.Header as string) == topLevelMenu.Key);
                List<IControl> topLevelMenuItems = topLevelMenuItem?.Items.Cast<IControl>().ToList() ?? new List<IControl>();
                foreach (var category in topLevelMenu.GroupBy(c => c.Metadata.MenuCategory)) {
                    if (topLevelMenuItem == null) {
                        topLevelMenuItem = new MenuItem();
                        topLevelMenuItem.Header = topLevelMenu.Key;
                        mainMenuItems.Add(topLevelMenuItem);
                    } else if (topLevelMenuItems.Count > 0) {
                        topLevelMenuItems.Add(new Separator());
                    }
                    foreach (var entry in category) {
                        MenuItem menuItem = new MenuItem();
                        menuItem.Command = CommandWrapper.Unwrap(entry.Value);
                        if (!string.IsNullOrEmpty(entry.Metadata.Header))
                            menuItem.Header = entry.Metadata.Header;
                        if (!string.IsNullOrEmpty(entry.Metadata.MenuIcon)) {
                            menuItem.Icon = new Image {
                                Width = 16,
                                Height = 16,
                                Source = Images.LoadImage(entry.Value, entry.Metadata.MenuIcon)
                            };
                        }
                        
                        menuItem.IsEnabled = entry.Metadata.IsEnabled;
                        //menuItem.InputGestureText = entry.Metadata.InputGestureText;
                        topLevelMenuItems.Add(menuItem);
                    }
                }
                topLevelMenuItem.Items = topLevelMenuItems;
            }
            mainMenu.Items = mainMenuItems;
        }
        #endregion

        #region Message Hook
        public override void Show()
        {
            base.Show();

            // Validate and Set Window Bounds
            var boundsRect = sessionSettings.WindowBounds;
            bool boundsOK = false;
            foreach (var screen in this.Screens.All) {
                var intersection = boundsRect.Intersect(screen.WorkingArea);
                if (intersection.Width > 10 && intersection.Height > 10)
                    boundsOK = true;
            }
            if (boundsOK)
                SetWindowBounds(sessionSettings.WindowBounds);
            else
                SetWindowBounds(SessionSettings.DefaultWindowBounds);
            
            this.WindowState = sessionSettings.WindowState;
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
            LoadAssemblies(args.AssembliesToLoad, commandLineLoadedAssemblies, false);
            if (args.Language != null)
                sessionSettings.FilterSettings.Language = Languages.GetLanguage(args.Language);
            return true;
        }
        
        void HandleCommandLineArgumentsAfterShowList(CommandLineArguments args)
        {
            if (nugetPackagesToLoad.Count > 0) {
                LoadAssemblies(nugetPackagesToLoad, commandLineLoadedAssemblies, focusNode: false);
                nugetPackagesToLoad.Clear();
            }
            if (args.NavigateTo != null) {
                bool found = false;
                if (args.NavigateTo.StartsWith("N:", StringComparison.Ordinal)) {
                    string namespaceName = args.NavigateTo.Substring(2);
                    foreach (LoadedAssembly asm in commandLineLoadedAssemblies) {
                        AssemblyTreeNode asmNode = assemblyListTreeNode.FindAssemblyNode(asm);
                        if (asmNode != null) {
                            NamespaceTreeNode nsNode = asmNode.FindNamespaceNode(namespaceName);
                            if (nsNode != null) {
                                found = true;
                                SelectNode(nsNode);
                                break;
                            }
                        }
                    }
                } else {
                    foreach (LoadedAssembly asm in commandLineLoadedAssemblies) {
                        ModuleDefinition def = asm.GetModuleDefinitionOrNull();
                        if (def != null) {
                            MemberReference mr = XmlDocKeyProvider.FindMemberByKey(def, args.NavigateTo);
                            if (mr != null) {
                                found = true;
                                // Defer JumpToReference call to allow an assembly that was loaded while
                                // resolving a type-forwarder in FindMemberByKey to appear in the assembly list.
                                Dispatcher.UIThread.InvokeAsync(new Action(() => JumpToReference(mr)), DispatcherPriority.Loaded);
                                break;
                            }
                        }
                    }
                }
                if (!found) {
                    AvaloniaEditTextOutput output = new AvaloniaEditTextOutput();
                    output.Write(string.Format("Cannot find '{0}' in command line specified assemblies.", args.NavigateTo));
                    decompilerTextView.ShowText(output);
                }
            } else if (commandLineLoadedAssemblies.Count == 1) {
                // NavigateTo == null and an assembly was given on the command-line:
                // Select the newly loaded assembly
                JumpToReference(commandLineLoadedAssemblies[0].GetModuleDefinitionOrNull());
            }
            if (args.Search != null)
            {
                SearchPane.Instance.SearchTerm = args.Search;
                SearchPane.Instance.Show();
            }
            commandLineLoadedAssemblies.Clear(); // clear references once we don't need them anymore
        }

        void MainWindow_Loaded(object sender, EventArgs e)
        {
            Application.Current.FocusManager.Focus(treeView);

            InitMainMenu();
            InitToolbar();

            ILSpySettings spySettings = this.spySettings;
            this.spySettings = null;

            // Load AssemblyList only in Loaded event so that WPF is initialized before we start the CPU-heavy stuff.
            // This makes the UI come up a bit faster.
            this.assemblyList = assemblyListManager.LoadList(spySettings, sessionSettings.ActiveAssemblyList);

            HandleCommandLineArguments(App.CommandLineArguments);

            if (assemblyList.GetAssemblies().Length == 0
                && assemblyList.ListName == AssemblyListManager.DefaultListName) {
                LoadInitialAssemblies();
            }

            ShowAssemblyList(this.assemblyList);

            if (sessionSettings.ActiveAutoLoadedAssembly != null) {
                this.assemblyList.Open(sessionSettings.ActiveAutoLoadedAssembly, true);
            }

            Dispatcher.UIThread.InvokeAsync(new Action(() => OpenAssemblies(spySettings)), DispatcherPriority.Loaded);
#if DEBUG
            this.Title = $"ILSpy {RevisionClass.FullVersion}";
#endif
        }

        void OpenAssemblies(ILSpySettings spySettings)
        {
            HandleCommandLineArgumentsAfterShowList(App.CommandLineArguments);
            if (App.CommandLineArguments.NavigateTo == null && App.CommandLineArguments.AssembliesToLoad.Count != 1) {
                SharpTreeNode node = null;
                if (sessionSettings.ActiveTreeViewPath != null) {
                    node = FindNodeByPath(sessionSettings.ActiveTreeViewPath, true);
                    if (node == this.assemblyListTreeNode && sessionSettings.ActiveAutoLoadedAssembly != null) {
                        node = FindNodeByPath(sessionSettings.ActiveTreeViewPath, true);
                    }
                }
                if (node != null) {
                    SelectNode(node);
                    
                    // only if not showing the about page, perform the update check:
                    ShowMessageIfUpdatesAvailableAsync(spySettings);
                } else {
                    AboutPage.Display(decompilerTextView);
                }
            }
            
            AvaloniaEditTextOutput output = new AvaloniaEditTextOutput();
            if (FormatExceptions(App.StartupExceptions.ToArray(), output))
                decompilerTextView.ShowText(output);
        }
        
        bool FormatExceptions(App.ExceptionData[] exceptions, ITextOutput output)
        {
            if (exceptions.Length == 0) return false;
            bool first = true;
            
            foreach (var item in exceptions) {
                if (first)
                    first = false;
                else
                    output.WriteLine("-------------------------------------------------");
                output.WriteLine("Error(s) loading plugin: " + item.PluginName);
                if (item.Exception is System.Reflection.ReflectionTypeLoadException) {
                    var e = (System.Reflection.ReflectionTypeLoadException)item.Exception;
                    foreach (var ex in e.LoaderExceptions) {
                        output.WriteLine(ex.ToString());
                        output.WriteLine();
                    }
                } else
                    output.WriteLine(item.Exception.ToString());
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
                AboutPage.CheckForUpdatesAsync(spySettings ?? ILSpySettings.Load())
                    .ContinueWith(task => AdjustUpdateUIAfterCheck(task, true), TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        void AdjustUpdateUIAfterCheck(Task<string> task, bool displayMessage)
        {
            updateAvailableDownloadUrl = task.Result;
            updatePanel.IsVisible = displayMessage ? true : false;
            if (task.Result != null) {
                updatePanelMessage.Text = "A new ILSpy version is available.";
                downloadOrCheckUpdateButton.Content = "Download";
            } else {
                updatePanelMessage.Text = "No update for ILSpy found.";
                downloadOrCheckUpdateButton.Content = "Check again";
            }
        }
        #endregion
        
        public void ShowAssemblyList(string name)
        {
            ILSpySettings settings = this.spySettings;
            if (settings == null)
            {
                settings = ILSpySettings.Load();
            }
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
                this.Title = "ILSpy";
            else
                this.Title = "ILSpy - " + assemblyList.ListName;
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
            if (reference is TypeReference)
            {
                return assemblyListTreeNode.FindTypeNode(((TypeReference)reference).Resolve());
            }
            else if (reference is MethodReference)
            {
                return assemblyListTreeNode.FindMethodNode(((MethodReference)reference).Resolve());
            }
            else if (reference is FieldReference)
            {
                return assemblyListTreeNode.FindFieldNode(((FieldReference)reference).Resolve());
            }
            else if (reference is PropertyReference)
            {
                return assemblyListTreeNode.FindPropertyNode(((PropertyReference)reference).Resolve());
            }
            else if (reference is EventReference)
            {
                return assemblyListTreeNode.FindEventNode(((EventReference)reference).Resolve());
            }
            else if (reference is AssemblyDefinition)
            {
                return assemblyListTreeNode.FindAssemblyNode((AssemblyDefinition)reference);
            }
            else if (reference is ModuleDefinition)
            {
                return assemblyListTreeNode.FindAssemblyNode((ModuleDefinition)reference);
            }
            else if (reference is Resource)
            {
                return assemblyListTreeNode.FindResourceNode((Resource)reference);
            }
            else
            {
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
            ILSpyTreeNode treeNode = FindTreeNode(reference);
            if (treeNode != null) {
                SelectNode(treeNode);
            } else if (reference is Mono.Cecil.Cil.OpCode) {
                string link = "http://msdn.microsoft.com/library/system.reflection.emit.opcodes." + ((Mono.Cecil.Cil.OpCode)reference).Code.ToString().ToLowerInvariant() + ".aspx";
                OpenLink(link);
            }
            return decompilationTask;
        }

        public static void OpenLink(string link)
        {
            try {
                Process.Start(link);
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
            dlg.Filters = new List<FileDialogFilter>()
            {
                new FileDialogFilter() { Name = ".NET assemblies", Extensions = {"dll","exe", "winmd" }},
                new FileDialogFilter() { Name = "Nuget Packages (*.nupkg)", Extensions = { "nupkg" }},
                new FileDialogFilter() { Name = "All files", Extensions = { "*" }},
            };
            dlg.AllowMultiple = true;
            //dlg.RestoreDirectory = true;
            var filenames = await dlg.ShowAsync();
            if (filenames!= null && filenames.Length > 0) {
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
                        LoadedNugetPackage package = new LoadedNugetPackage(file);
                        var selectionDialog = new NugetPackageBrowserDialog(package);
                        selectionDialog.Owner = this;
                        if (await selectionDialog.ShowDialog<bool>() != true)
                            break;
                        foreach (var entry in selectionDialog.SelectedItems) {
                            var nugetAsm = assemblyList.OpenAssembly("nupkg://" + file + ";" + entry.Name, entry.Stream, true);
                            if (nugetAsm != null) {
                                if (loadedAssemblies != null)
                                    loadedAssemblies.Add(nugetAsm);
                                else {
                                    var node = assemblyListTreeNode.FindAssemblyNode(nugetAsm);
                                    if (node != null && focusNode) {
                                        treeView.SelectedItems.Add(node);
                                        lastNode = node;
                                    }
                                }
                            }
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
                                    treeView.SelectedItems.Add(node);
                                    lastNode = node;
                                }
                            }
                        }
                        break;
                }

                if (lastNode != null && focusNode)
                    treeView.FocusNode(lastNode);
            }
        }
        
        void RefreshCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var path = GetPathForNode(treeView.SelectedItem as SharpTreeNode);
            ShowAssemblyList(assemblyListManager.LoadList(ILSpySettings.Load(), assemblyList.ListName));
            SelectNode(FindNodeByPath(path, true));
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
        
        async void SaveCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.SelectedNodes.Count() == 1) {
                if (await this.SelectedNodes.Single().Save(this.TextView))
                    return;
            }
            this.TextView.SaveToDisk(this.CurrentLanguage,
                this.SelectedNodes,
                new DecompilationOptions() { FullDecompilation = true });
        }
        
        public void RefreshDecompiledView()
        {
            DecompileSelectedNodes();
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
            if(dtState != null)
                history.UpdateCurrent(new NavigationState(dtState));
            var newState = forward ? history.GoForward() : history.GoBack();
            
            ignoreDecompilationRequests = true;
            treeView.SelectedItems.Clear();
            foreach (var node in newState.TreeNodes)
            {
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
            sessionSettings.WindowBounds = this.Bounds;
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
            if (!pane1Height.IsStar || !pane2Height.IsStar)
            {
                return;
            }

            var totalHeight = pane1Height.Value + pane2Height.Value;
            if (totalHeight == 0)
            {
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
