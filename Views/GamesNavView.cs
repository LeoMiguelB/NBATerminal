using NBATerminal.Models;
using System.Collections.ObjectModel;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace NBATerminal.Views;

public sealed class GamesNavView : FrameView
{
    private ListView _gamesListView = null!;
    private Label _pageLabel = null!;
    private bool _isProgrammaticSelection;

    public event Action<int>? GameSelected;

    public GamesNavView()
    {
        Title = "Games Today";
        CreateControls();
        BuildLayout();
        WireEvents();
    }

    public void CreateControls()
    {
        _gamesListView = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(1)
        };
        _gamesListView.CanFocus = false;
        _gamesListView.SetSource(new ObservableCollection<string>());

        _pageLabel = new Label
        {
            Text = string.Empty,
            X = 0,
            Y = Pos.Bottom(_gamesListView),
            Width = Dim.Fill(),
            Height = 1
        };
    }

    public void BuildLayout()
    {
        Add(_gamesListView);
        Add(_pageLabel);
    }

    public void WireEvents()
    {
        _gamesListView.ValueChanged += (_, _) =>
        {
            if (_isProgrammaticSelection)
            {
                return;
            }

            var selected = _gamesListView.SelectedItem ?? -1;
            if (selected >= 0)
            {
                GameSelected?.Invoke(selected);
            }
        };
    }

    public void SetGamesPage(
        IReadOnlyList<GameSummary> games,
        int pageIndex,
        int totalPages,
        int selectedIndexOnPage
    )
    {
        var lines = games.Select(g => g.Display).ToList();
        _gamesListView.SetSource(new ObservableCollection<string>(lines));

        if (lines.Count == 0)
        {
            _gamesListView.SelectedItem = -1;
            _pageLabel.Text = "No games found";
            return;
        }

        _isProgrammaticSelection = true;
        _gamesListView.SelectedItem = Math.Clamp(selectedIndexOnPage, 0, lines.Count - 1);
        _isProgrammaticSelection = false;
        _pageLabel.Text = $"Games Page {pageIndex + 1}/{Math.Max(totalPages, 1)}";
    }
}
