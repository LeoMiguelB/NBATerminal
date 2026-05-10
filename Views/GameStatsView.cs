using System.Collections.ObjectModel;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace NBATerminal.Views;

public sealed class GameStatsView : FrameView
{
    private Label _headerLabel = null!;
    private Label _awayTeamLabel = null!;
    private Label _homeTeamLabel = null!;
    private ListView _statsListView = null!;
    private Label _pageLabel = null!;

    public GameStatsView()
    {
        Title = "Game Stats";
        CreateControls();
        BuildLayout();
        WireEvents();
    }

    public void CreateControls()
    {
        _headerLabel = new Label
        {
            Text = "Select a game",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = 1
        };

        _awayTeamLabel = new Label
        {
            Text = string.Empty,
            X = 0,
            Y = Pos.Bottom(_headerLabel),
            Width = Dim.Fill(),
            Height = 1
        };

        _homeTeamLabel = new Label
        {
            Text = string.Empty,
            X = 0,
            Y = Pos.Bottom(_awayTeamLabel),
            Width = Dim.Fill(),
            Height = 1
        };

        _statsListView = new ListView
        {
            X = 0,
            Y = Pos.Bottom(_homeTeamLabel),
            Width = Dim.Fill(),
            Height = Dim.Fill(1)
        };
        _statsListView.CanFocus = false;
        _statsListView.SetSource(new ObservableCollection<string>());

        _pageLabel = new Label
        {
            Text = string.Empty,
            X = 0,
            Y = Pos.Bottom(_statsListView),
            Width = Dim.Fill(),
            Height = 1
        };
    }

    public void BuildLayout()
    {
        Add(_headerLabel);
        Add(_awayTeamLabel);
        Add(_homeTeamLabel);
        Add(_statsListView);
        Add(_pageLabel);
    }

    public void WireEvents()
    {
    }

    public void SetEmpty(string message)
    {
        _headerLabel.Text = message;
        _awayTeamLabel.Text = string.Empty;
        _homeTeamLabel.Text = string.Empty;
        _statsListView.SetSource(new ObservableCollection<string>());
        _pageLabel.Text = string.Empty;
    }

    public void SetGameHeader(string statusText, string awayTeam, string homeTeam)
    {
        _headerLabel.Text = statusText;
        _awayTeamLabel.Text = $"Away: {awayTeam}";
        _homeTeamLabel.Text = $"Home: {homeTeam}";
    }

    public void SetStatsPage(IReadOnlyList<string> rows, int pageIndex, int totalPages)
    {
        _statsListView.SetSource(new ObservableCollection<string>(rows));
        _pageLabel.Text = $"Stats Page {pageIndex + 1}/{Math.Max(totalPages, 1)}";
    }
}
