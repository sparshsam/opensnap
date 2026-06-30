using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace OpenSnap;

/// <summary>
/// Search dialog for screenshot history — live filters as you type.
/// </summary>
public partial class SearchHistoryDialog : Window
{
    private readonly List<string> _allPaths;
    private readonly List<string> _pinnedPaths;

    /// <summary>Fired when the user picks a file to open.</summary>
    public event Action<string>? FileSelected;

    public SearchHistoryDialog(List<string> history, List<string>? pinned = null)
    {
        _allPaths = history;
        _pinnedPaths = pinned ?? new();
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SearchBox.Focus();
        UpdateResults();
    }

    private void OnSearchTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        UpdateResults();
    }

    private void UpdateResults()
    {
        var query = SearchBox.Text.Trim().ToLowerInvariant();

        var matches = string.IsNullOrEmpty(query)
            ? _allPaths.AsEnumerable()
            : _allPaths.Where(p =>
                Path.GetFileName(p).ToLowerInvariant().Contains(query) ||
                p.ToLowerInvariant().Contains(query));

        // Pinned items first, then by recency
        var ordered = matches
            .OrderByDescending(p => _pinnedPaths.Contains(p))
            .ThenByDescending(p => _allPaths.IndexOf(p))
            .Select(p => new SearchResult
            {
                FileName = Path.GetFileName(p),
                FullPath = p,
                IsPinned = _pinnedPaths.Contains(p),
            })
            .ToList();

        ResultsList.ItemsSource = ordered;

        StatusText.Text = ordered.Count switch
        {
            0 => "No matches.",
            1 => "1 result.",
            _ => $"{ordered.Count} results.",
        };
    }

    private void OnResultDoubleClick(object sender, MouseButtonEventArgs e)
    {
        OpenSelected();
    }

    private void OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter && ResultsList.SelectedItem is SearchResult result)
        {
            OpenItem(result.FullPath);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            Close();
        }
        else if (e.Key == Key.Down && ResultsList.Items.Count > 0)
        {
            ResultsList.SelectedIndex = ResultsList.SelectedIndex < ResultsList.Items.Count - 1
                ? ResultsList.SelectedIndex + 1 : 0;
            ResultsList.ScrollIntoView(ResultsList.SelectedItem);
            e.Handled = true;
        }
        else if (e.Key == Key.Up)
        {
            ResultsList.SelectedIndex = ResultsList.SelectedIndex > 0
                ? ResultsList.SelectedIndex - 1 : ResultsList.Items.Count - 1;
            ResultsList.ScrollIntoView(ResultsList.SelectedItem);
            e.Handled = true;
        }
    }

    private void OpenSelected()
    {
        if (ResultsList.SelectedItem is SearchResult result)
            OpenItem(result.FullPath);
    }

    private void OpenItem(string path)
    {
        if (File.Exists(path))
        {
            FileSelected?.Invoke(path);
            Close();
        }
    }

    private sealed class SearchResult
    {
        public string FileName { get; set; } = "";
        public string FullPath { get; set; } = "";
        public bool IsPinned { get; set; }
    }
}
