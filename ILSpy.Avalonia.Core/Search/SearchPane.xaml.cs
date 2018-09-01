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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.ILSpy.Controls;
using ICSharpCode.ILSpy.Search;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.TreeView;

namespace ICSharpCode.ILSpy.Search
{
	/// <summary>
	/// Search pane
	/// </summary>
	public partial class SearchPane : UserControl, IPane
	{
		static SearchPane instance;
		RunningSearch currentSearch;
		
		public static SearchPane Instance {
			get {
				if (instance == null) {
					App.Current.MainWindow.VerifyAccess();
					instance = new SearchPane();
				}
				return instance;
			}
		}

        internal SearchBox searchBox;
        internal DropDown searchModeComboBox;
        internal ListBox listBox;

        private SearchPane()
		{
			InitializeComponent();
            searchModeComboBox.Items = new[] {
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

			searchModeComboBox.SelectedIndex = (int)MainWindow.Instance.SessionSettings.SelectedSearchMode;
			searchModeComboBox.SelectionChanged += (sender, e) => MainWindow.Instance.SessionSettings.SelectedSearchMode = (Search.SearchMode)searchModeComboBox.SelectedIndex;
			ContextMenuProvider.Add(listBox);
			
			MainWindow.Instance.CurrentAssemblyListChanged += MainWindow_Instance_CurrentAssemblyListChanged;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            searchBox = this.FindControl<SearchBox>("searchBox");
            searchModeComboBox = this.FindControl<DropDown>("searchModeComboBox");
            listBox = this.FindControl<ListBox>("listBox");

            searchBox.KeyDown += SearchBox_PreviewKeyDown;
            searchModeComboBox.SelectionChanged += SearchModeComboBox_SelectionChanged;
            listBox.KeyDown += ListBox_KeyDown;
            listBox.DoubleTapped += ListBox_MouseDoubleClick;
        }


        bool runSearchOnNextShow;
		
		void MainWindow_Instance_CurrentAssemblyListChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (VisualRoot != null) {
				StartSearch(this.SearchTerm);
			} else {
				StartSearch(null);
				runSearchOnNextShow = true;
			}
		}
		
		public void Show()
		{
			if (VisualRoot == null) {
				MainWindow.Instance.ShowInTopPane("Search", this);
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
        }

        public static readonly StyledProperty<string> SearchTermProperty =
            AvaloniaProperty.Register<SearchPane, string>("SearchTerm", string.Empty, notifying: OnSearchTermChanged);

        public string SearchTerm {
			get { return (string)GetValue(SearchTermProperty); }
			set { SetValue(SearchTermProperty, value); }
		}
		
		static void OnSearchTermChanged(IAvaloniaObject o, bool changed)
		{
			((SearchPane)o).StartSearch(o.GetValue(SearchTermProperty));
		}
		
		void SearchModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			StartSearch(this.SearchTerm);
		}
		
		void StartSearch(string searchTerm)
		{
			if (currentSearch != null) {
				currentSearch.Cancel();
			}
			if (string.IsNullOrEmpty(searchTerm)) {
				currentSearch = null;
				listBox.Items = null;
			} else {
				MainWindow mainWindow = MainWindow.Instance;
				currentSearch = new RunningSearch(mainWindow.CurrentAssemblyList.GetAssemblies(), searchTerm,
					(SearchMode)searchModeComboBox.SelectedIndex, mainWindow.CurrentLanguage);
				listBox.Items = currentSearch.Results;
				new Thread(currentSearch.Run).Start();
			}
		}
		
		void IPane.Closed()
		{
			this.SearchTerm = string.Empty;
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
			}
		}
		
		void JumpToSelectedItem()
		{
			SearchResult result = listBox.SelectedItem as SearchResult;
			if (result != null) {
				MainWindow.Instance.JumpToReference(result.Member);
			}
		}
		
		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.Key == Key.T && e.Modifiers == InputModifiers.Control) {
				searchModeComboBox.SelectedIndex = (int)SearchMode.Type;
				e.Handled = true;
			} else if (e.Key == Key.M && e.Modifiers == InputModifiers.Control) {
				searchModeComboBox.SelectedIndex = (int)SearchMode.Member;
				e.Handled = true;
			} else if (e.Key == Key.S && e.Modifiers == InputModifiers.Control) {
				searchModeComboBox.SelectedIndex = (int)SearchMode.Literal;
				e.Handled = true;
			}
		}
		
		void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Down && listBox.ItemCount > 0) {
				e.Handled = true;
                // TODO: move focus
                //listBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
				listBox.SelectedIndex = 0;
			}
		}

		sealed class RunningSearch
		{
			readonly Dispatcher dispatcher;
			readonly CancellationTokenSource cts = new CancellationTokenSource();
			readonly LoadedAssembly[] assemblies;
			readonly string[] searchTerm;
			readonly SearchMode searchMode;
			readonly Language language;
			public readonly ObservableCollection<SearchResult> Results = new ObservableCollection<SearchResult>();
			int resultCount;

			public RunningSearch(LoadedAssembly[] assemblies, string searchTerm, SearchMode searchMode, Language language)
			{
				this.dispatcher = Dispatcher.UIThread;
				this.assemblies = assemblies;
				this.searchTerm = searchTerm.Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);
				this.language = language;
				this.searchMode = searchMode;
				
				this.Results.Add(new SearchResult { Name = "Searching..." });
			}
			
			public void Cancel()
			{
				cts.Cancel();
			}
			
			public void Run()
			{
				try {
					var searcher = GetSearchStrategy(searchMode, searchTerm);
					// TODO : parallelize
					foreach (var loadedAssembly in assemblies) {
						var module = loadedAssembly.GetPEFileOrNull();
						if (module == null)
							continue;
						CancellationToken cancellationToken = cts.Token;
						searcher.Search(module);
					}
				} catch (OperationCanceledException) {
					// ignore cancellation
				}
				// remove the 'Searching...' entry
				dispatcher.InvokeAsync(
					new Action(delegate { this.Results.RemoveAt(this.Results.Count - 1); }),
                    DispatcherPriority.Normal);
			}
			
			void AddResult(SearchResult result)
			{
				if (++resultCount == 1000) {
					result = new SearchResult { Name = "Search aborted, more than 1000 results found." };
					cts.Cancel();
				}
				dispatcher.InvokeAsync(
					new Action(delegate { InsertResult(this.Results, result); }),
                    DispatcherPriority.Normal);
				cts.Token.ThrowIfCancellationRequested();
			}

			void InsertResult(ObservableCollection<SearchResult> results, SearchResult result)
			{
				if (Options.DisplaySettingsPanel.CurrentDisplaySettings.SortResults)
				{
					// Keep results collection sorted by "Fitness" by inserting result into correct place
					// Inserts in the beginning shifts all elements, but there can be no more than 1000 items.
					for (int i = 0; i < results.Count; i++)
					{
						if (results[i].Fitness < result.Fitness)
						{
							results.Insert(i, result);
							return;
						}
					}
					results.Insert(results.Count - 1, result);
				}
				else
				{
					// Original Code
					int index = results.BinarySearch(result, 0, results.Count - 1, SearchResult.Comparer);
					results.Insert(index < 0 ? ~index : index, result);
				}
			}

			AbstractSearchStrategy GetSearchStrategy(SearchMode mode, string[] terms)
			{
				if (terms.Length == 1) {
					if (terms[0].StartsWith("tm:", StringComparison.Ordinal))
						return new MemberSearchStrategy(language, AddResult, terms[0].Substring(3));

					if (terms[0].StartsWith("t:", StringComparison.Ordinal))
						return new MemberSearchStrategy(language, AddResult, terms[0].Substring(2), MemberSearchKind.Type);

					if (terms[0].StartsWith("m:", StringComparison.Ordinal))
						return new MemberSearchStrategy(language, AddResult, terms[0].Substring(2), MemberSearchKind.Member);

					if (terms[0].StartsWith("md:", StringComparison.Ordinal))
						return new MemberSearchStrategy(language, AddResult, terms[0].Substring(3), MemberSearchKind.Method);

					if (terms[0].StartsWith("f:", StringComparison.Ordinal))
						return new MemberSearchStrategy(language, AddResult, terms[0].Substring(2), MemberSearchKind.Field);

					if (terms[0].StartsWith("p:", StringComparison.Ordinal))
						return new MemberSearchStrategy(language, AddResult, terms[0].Substring(2), MemberSearchKind.Property);

					if (terms[0].StartsWith("e:", StringComparison.Ordinal))
						return new MemberSearchStrategy(language, AddResult, terms[0].Substring(2), MemberSearchKind.Event);

					if (terms[0].StartsWith("c:", StringComparison.Ordinal))
						return new LiteralSearchStrategy(language, AddResult, terms[0].Substring(2));

					if (terms[0].StartsWith("@", StringComparison.Ordinal))
						return new MetadataTokenSearchStrategy(language, AddResult, terms[0].Substring(1));
				}

				switch (mode)
				{
					case SearchMode.TypeAndMember:
						return new MemberSearchStrategy(language, AddResult, terms);
					case SearchMode.Type:
						return new MemberSearchStrategy(language, AddResult, terms, MemberSearchKind.Type);
					case SearchMode.Member:
						return new MemberSearchStrategy(language, AddResult, terms, MemberSearchKind.Member);
					case SearchMode.Literal:
						return new LiteralSearchStrategy(language, AddResult, terms);
					case SearchMode.Method:
						return new MemberSearchStrategy(language, AddResult, terms, MemberSearchKind.Method);
					case SearchMode.Field:
						return new MemberSearchStrategy(language, AddResult, terms, MemberSearchKind.Field);
					case SearchMode.Property:
						return new MemberSearchStrategy(language, AddResult, terms, MemberSearchKind.Property);
					case SearchMode.Event:
						return new MemberSearchStrategy(language, AddResult, terms, MemberSearchKind.Event);
					case SearchMode.Token:
						return new MetadataTokenSearchStrategy(language, AddResult, terms);
				}

				return null;
			}
		}
	}

	sealed class SearchResult : IMemberTreeNode
	{
		public static readonly System.Collections.Generic.IComparer<SearchResult> Comparer = new SearchResultComparer();
		
		public IEntity Member { get; set; }
		public float Fitness { get; set; }
		
		public string Location { get; set; }
		public string Name { get; set; }
		public IBitmap Image { get; set; }
		public IBitmap LocationImage { get; set; }
		
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