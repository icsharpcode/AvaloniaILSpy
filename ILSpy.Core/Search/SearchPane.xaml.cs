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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Collections;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.ILSpy.TreeNodes;

namespace ICSharpCode.ILSpy.Search
{
	/// <summary>
	/// Search pane
	/// </summary>
	public partial class SearchPane : UserControl, IPane
	{
		const int MAX_RESULTS = 1000;
		const int MAX_REFRESH_TIME_MS = 10; // More means quicker forward of data, less means better responsibility
		static SearchPane instance;
		RunningSearch currentSearch;
		bool runSearchOnNextShow;
		DispatcherTimer updateResultTimer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(2D) };

		public ObservableCollection<SearchResult> Results { get; } = new ObservableCollection<SearchResult>();

		public static SearchPane Instance {
			get {
				if (instance == null) {
					Dispatcher.UIThread.VerifyAccess();
					instance = new SearchPane();
				}
				return instance;
			}
		}
		
		// was private, workaround for https://github.com/AvaloniaUI/Avalonia/issues/2593
		public SearchPane()
		{
			InitializeComponent();
			searchModeComboBox.ItemsSource = new []{
				new { Image = Images.Library, Name = "Types and Members" },
				new { Image = Images.Class, Name = "Type" },
				new { Image = Images.Property, Name = "Member" },
				new { Image = Images.Method, Name = "Method" },
				new { Image = Images.Field, Name = "Field" },
				new { Image = Images.Property, Name = "Property" },
				new { Image = Images.Event, Name = "Event" },
				new { Image = Images.Literal, Name = "Constant" },
				new { Image = Images.Library, Name = "Metadata Token" }
			};

			ContextMenuProvider.Add(listBox);
			MainWindow.Instance.CurrentAssemblyListChanged += MainWindow_Instance_CurrentAssemblyListChanged;
			MainWindow.Instance.SessionSettings.FilterSettings.PropertyChanged += FilterSettings_PropertyChanged;

			// This starts empty search right away, so do at the end (we're still in ctor)
			searchModeComboBox.SelectedIndex = (int)MainWindow.Instance.SessionSettings.SelectedSearchMode;
			searchModeComboBox.SelectionChanged += (sender, e) => MainWindow.Instance.SessionSettings.SelectedSearchMode = (Search.SearchMode)searchModeComboBox.SelectedIndex;
			updateResultTimer.Tick += UpdateResults;

			this.DataContext = new DataGridCollectionView(Results);
		}

		void MainWindow_Instance_CurrentAssemblyListChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (VisualRoot != null) {
				StartSearch(this.SearchTerm);
			} else {
				StartSearch(null);
				runSearchOnNextShow = true;
			}
		}

		void FilterSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName != nameof(FilterSettings.ShowApiLevel))
				return;

			if (IsVisible) {
				StartSearch(this.SearchTerm);
			} else {
				StartSearch(null);
				runSearchOnNextShow = true;
			}
		}

		public void Show()
		{
			if (VisualRoot == null) {
				MainWindow.Instance.ShowInTopPane(Properties.Resources.SearchPane_Search, this);
				if (runSearchOnNextShow) {
					runSearchOnNextShow = false;
					StartSearch(this.SearchTerm);
				}
			}
			Dispatcher.UIThread.InvokeAsync(
				new Action(
					delegate {
						searchBox.Focus();
						//searchBox.SelectAll();
						searchBox.SelectionStart = 0;
						searchBox.SelectionEnd = searchBox.Text?.Length ?? 0;
					}),
				DispatcherPriority.Background);
			updateResultTimer.Start();
		}

		public static readonly StyledProperty<string> SearchTermProperty =
			AvaloniaProperty.Register<SearchPane, string>("SearchTerm", string.Empty);

		public string SearchTerm {
			get { return GetValue(SearchTermProperty) ?? string.Empty; }
			set { SetValue(SearchTermProperty, value ?? string.Empty); }
		}

		protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
		{
			if (change.Property == SearchTermProperty)
			{
				((SearchPane)change.Sender).StartSearch(change.Sender.GetValue(SearchTermProperty));
			}

			base.OnPropertyChanged(change);
		}

		void SearchModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			MainWindow.Instance.SessionSettings.SelectedSearchMode = (SearchMode)searchModeComboBox.SelectedIndex;
			StartSearch(this.SearchTerm);
		}

		void IPane.Closed()
		{
			this.SearchTerm = string.Empty;
			updateResultTimer.Stop();
		}
		
		void ListBox_MouseDoubleClick(object sender, RoutedEventArgs e)
		{
			JumpToSelectedItem();
			e.Handled = true;
		}
		
		void ListBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Return) {
				e.Handled = true;
				JumpToSelectedItem();
			} else if(e.Key == Key.Up && listBox.SelectedIndex == 0) {
				e.Handled = true;
				listBox.SelectedIndex = -1;
				searchBox.Focus();
			}
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.Key == Key.T && e.KeyModifiers == KeyModifiers.Control) {
				searchModeComboBox.SelectedIndex = (int)SearchMode.Type;
				e.Handled = true;
			} else if (e.Key == Key.M && e.KeyModifiers == KeyModifiers.Control) {
				searchModeComboBox.SelectedIndex = (int)SearchMode.Member;
				e.Handled = true;
			} else if (e.Key == Key.S && e.KeyModifiers == KeyModifiers.Control) {
				searchModeComboBox.SelectedIndex = (int)SearchMode.Literal;
				e.Handled = true;
			}
		}

		void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Down && Results.Count > 0) {
				e.Handled = true;
				// TODO: movefocus
				//listBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
				listBox.SelectedIndex = 0;
			}
		}

		void UpdateResults(object sender, EventArgs e)
		{
			if (currentSearch == null)
				return;

			var timer = Stopwatch.StartNew();
			int resultsAdded = 0;
			while (Results.Count < MAX_RESULTS && timer.ElapsedMilliseconds < MAX_REFRESH_TIME_MS && currentSearch.resultQueue.TryTake(out var result)) {
				InsertResult(Results, result);
				++resultsAdded;
			}

			if (resultsAdded > 0 && Results.Count == MAX_RESULTS) {
				Results.Add(new SearchResult { Name = Properties.Resources.SearchAbortedMoreThan1000ResultsFound });
				currentSearch.Cancel();
			}
		}

		async void StartSearch(string searchTerm)
		{
			if (currentSearch != null) {
				currentSearch.Cancel();
				currentSearch = null;
			}

			Results.Clear();

			RunningSearch startedSearch = null;
			if (!string.IsNullOrEmpty(searchTerm)) {
				MainWindow mainWindow = MainWindow.Instance;

				searchProgressBar.IsIndeterminate = true;
				startedSearch = new RunningSearch(mainWindow.CurrentAssemblyList.GetAssemblies(), searchTerm,
					(SearchMode)searchModeComboBox.SelectedIndex, mainWindow.CurrentLanguage, 
					mainWindow.SessionSettings.FilterSettings.ShowApiLevel);
				currentSearch = startedSearch;

				await startedSearch.Run();
			}

			if (currentSearch == startedSearch) { //are we still running the same search
				searchProgressBar.IsIndeterminate = false;
			}
		}

		void InsertResult(IList<SearchResult> results, SearchResult result)
		{
			if (results.Count == 0) {
				results.Add(result);
			} else if (Options.DisplaySettingsPanel.CurrentDisplaySettings.SortResults) {
				// Keep results collection sorted by "Fitness" by inserting result into correct place
				// Inserts in the beginning shifts all elements, but there can be no more than 1000 items.
				for (int i = 0; i < results.Count; i++) {
					if (results[i].Fitness < result.Fitness) {
						results.Insert(i, result);
						return;
					}
				}
				results.Insert(results.Count - 1, result);
			} else {
				// Original Code
				int index = results.BinarySearch(result, 0, results.Count - 1, SearchResult.Comparer);
				results.Insert(index < 0 ? ~index : index, result);
			}
		}

		void JumpToSelectedItem()
		{
			if (listBox.SelectedItem is SearchResult result) {
				MainWindow.Instance.JumpToReference(result.Member);
			}
		}
		
		sealed class RunningSearch
		{
			readonly CancellationTokenSource cts = new CancellationTokenSource();
			readonly LoadedAssembly[] assemblies;
			readonly string[] searchTerm;
			readonly SearchMode searchMode;
			readonly Language language;
			readonly ApiVisibility apiVisibility;
			public readonly IProducerConsumerCollection<SearchResult> resultQueue = new ConcurrentQueue<SearchResult>(); 

			public RunningSearch(LoadedAssembly[] assemblies, string searchTerm, SearchMode searchMode, Language language, ApiVisibility apiVisibility)
			{
				this.assemblies = assemblies;
				this.searchTerm = CommandLineToArgumentArray(searchTerm);
				this.language = language;
				this.searchMode = searchMode;
				this.apiVisibility = apiVisibility;
			}

			static IEnumerable<string> Split(string str, Func<char, bool> controller)
			{
				int nextPiece = 0;
				for (int c = 0; c < str.Length; c++)
				{
					if (controller(str[c]))
					{
						yield return str.Substring(nextPiece, c - nextPiece);
						nextPiece = c + 1;
					}
				}

				yield return str.Substring(nextPiece);
			}

			static string TrimMatchingQuotes(string input, char quote)
			{
				input = input.Trim();
				if (input.Length >= 2 && input[0] == quote && input[input.Length-1] == quote)
				{
					return input.Substring(1, input.Length - 2);
				}
				return input;
			}

			string[] CommandLineToArgumentArray(string commandLine)
			{
				bool inQuotes = false;

				return Split(commandLine, c =>
				{
					if (c == '\"')
					{
						inQuotes = !inQuotes;
					}
					return !inQuotes && c == ' ';
				}).Select(arg => TrimMatchingQuotes(arg, '\"'))
				.Where(arg => !string.IsNullOrEmpty(arg))
				.ToArray();
			}
			
			public void Cancel()
			{
				cts.Cancel();
			}
			
			public async Task Run()
			{
				try {
					await Task.Factory.StartNew(() => {
						var searcher = GetSearchStrategy();
						try {
							foreach (var loadedAssembly in assemblies) {
								var module = loadedAssembly.GetPEFileOrNull();
								if (module == null)
									continue;
								searcher.Search(module, cts.Token);
							}
						} catch (OperationCanceledException) {
							// ignore cancellation
						}

					}, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);
				} catch (TaskCanceledException) {
					// ignore cancellation
				}
			}

			AbstractSearchStrategy GetSearchStrategy()
			{
				if (searchTerm.Length == 1) {
					if (searchTerm[0].StartsWith("tm:", StringComparison.Ordinal))
						return new MemberSearchStrategy(language, apiVisibility, searchTerm[0].Substring(3), resultQueue);

					if (searchTerm[0].StartsWith("t:", StringComparison.Ordinal))
						return new MemberSearchStrategy(language, apiVisibility, searchTerm[0].Substring(2), resultQueue, MemberSearchKind.Type);

					if (searchTerm[0].StartsWith("m:", StringComparison.Ordinal))
						return new MemberSearchStrategy(language, apiVisibility, searchTerm[0].Substring(2), resultQueue, MemberSearchKind.Member);

					if (searchTerm[0].StartsWith("md:", StringComparison.Ordinal))
						return new MemberSearchStrategy(language, apiVisibility, searchTerm[0].Substring(3), resultQueue, MemberSearchKind.Method);

					if (searchTerm[0].StartsWith("f:", StringComparison.Ordinal))
						return new MemberSearchStrategy(language, apiVisibility, searchTerm[0].Substring(2), resultQueue, MemberSearchKind.Field);

					if (searchTerm[0].StartsWith("p:", StringComparison.Ordinal))
						return new MemberSearchStrategy(language, apiVisibility, searchTerm[0].Substring(2), resultQueue, MemberSearchKind.Property);

					if (searchTerm[0].StartsWith("e:", StringComparison.Ordinal))
						return new MemberSearchStrategy(language, apiVisibility, searchTerm[0].Substring(2), resultQueue, MemberSearchKind.Event);

					if (searchTerm[0].StartsWith("c:", StringComparison.Ordinal))
						return new LiteralSearchStrategy(language, apiVisibility, resultQueue, searchTerm[0].Substring(2));

					if (searchTerm[0].StartsWith("@", StringComparison.Ordinal))
						return new MetadataTokenSearchStrategy(language, apiVisibility, resultQueue, searchTerm[0].Substring(1));
				}

				switch (searchMode)
				{
					case SearchMode.TypeAndMember:
						return new MemberSearchStrategy(language, apiVisibility, resultQueue, searchTerm);
					case SearchMode.Type:
						return new MemberSearchStrategy(language, apiVisibility, resultQueue, searchTerm, MemberSearchKind.Type);
					case SearchMode.Member:
						return new MemberSearchStrategy(language, apiVisibility, resultQueue, searchTerm, MemberSearchKind.Member);
					case SearchMode.Literal:
						return new LiteralSearchStrategy(language, apiVisibility, resultQueue, searchTerm);
					case SearchMode.Method:
						return new MemberSearchStrategy(language, apiVisibility, resultQueue, searchTerm, MemberSearchKind.Method);
					case SearchMode.Field:
						return new MemberSearchStrategy(language, apiVisibility, resultQueue, searchTerm, MemberSearchKind.Field);
					case SearchMode.Property:
						return new MemberSearchStrategy(language, apiVisibility, resultQueue, searchTerm, MemberSearchKind.Property);
					case SearchMode.Event:
						return new MemberSearchStrategy(language, apiVisibility, resultQueue, searchTerm, MemberSearchKind.Event);
					case SearchMode.Token:
						return new MetadataTokenSearchStrategy(language, apiVisibility, resultQueue, searchTerm);
				}

				return null;
			}
		}
	}

	public sealed class SearchResult : IMemberTreeNode
	{
		public static readonly System.Collections.Generic.IComparer<SearchResult> Comparer = new SearchResultComparer();

		public IEntity Member { get; set; }
		public float Fitness { get; set; }

		public string Location { get; set; }
		public string Name { get; set; }
		public object ToolTip { get; set; }
		public Bitmap Image { get; set; }
		public Bitmap LocationImage { get; set; }

		public override string ToString()
		{
			return Name;
		}

		class SearchResultComparer : System.Collections.Generic.IComparer<SearchResult>
		{
			public int Compare(SearchResult x, SearchResult y)
			{
				return StringComparer.Ordinal.Compare(x?.Name ?? "", y?.Name ?? "");
			}
		}
	}

	[ExportMainMenuCommand(Menu = nameof(Properties.Resources._View), Header =nameof(Properties.Resources.Search), MenuIcon = "Images/Find.png", MenuCategory = nameof(Properties.Resources.View), MenuOrder = 100)]
	[ExportToolbarCommand(ToolTip = nameof(Properties.Resources.SearchCtrlShiftFOrCtrlE), ToolbarIcon = "Images/Find.png", ToolbarCategory = nameof(Properties.Resources.View), ToolbarOrder = 100)]
	sealed class ShowSearchCommand : CommandWrapper
	{
		public ShowSearchCommand()
			: base(NavigationCommands.Search)
		{
			//NavigationCommands.Search.InputGestures.Clear();
			//NavigationCommands.Search.InputGestures.Add(new KeyGesture(Key.F, ModifierKeys.Control | ModifierKeys.Shift));
			//NavigationCommands.Search.InputGestures.Add(new KeyGesture(Key.E, ModifierKeys.Control));
		}
	}

	public enum SearchMode
	{
		TypeAndMember,
		Type,
		Member,
		Method,
		Field,
		Property,
		Event,
		Literal,
		Token
	}
}
