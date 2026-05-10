using NBATerminal.Models;
using NBATerminal.Services;
using NBATerminal.Views;
using Terminal.Gui.App;

namespace NBATerminal.Controllers;

public sealed class MainController
{
    private const int GamesPageSize = 8;
    private const int StatsPageSize = 16;

    private readonly MainView _mainView;
    private readonly GameService _gameService;
    private readonly StatsService _statsService;
    private readonly IApplication _app;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    private IReadOnlyList<GameSummary> _games = Array.Empty<GameSummary>();
    private GameDetails? _selectedGameDetails;
    private int _selectedGameIndex;
    private int _gamesPageIndex;
    private int _statsPageIndex;
    private ActivePane _activePane = ActivePane.Games;

    public MainController(MainView mainView, GameService gameService, StatsService statsService, IApplication app)
    {
        _mainView = mainView;
        _gameService = gameService;
        _statsService = statsService;
        _app = app;
    }

    public void Initialize()
    {
        WireEvents();
        _ = LoadDataAsync(forceRefresh: true);
    }

    private void WireEvents()
    {
        _mainView.MoveDownRequested += OnMoveDown;
        _mainView.MoveUpRequested += OnMoveUp;
        _mainView.NextPageRequested += OnNextPage;
        _mainView.PreviousPageRequested += OnPreviousPage;
        _mainView.GoFirstRequested += OnFirstPage;
        _mainView.GoLastRequested += OnLastPage;
        _mainView.FocusLeftRequested += () => SetActivePane(ActivePane.Games);
        _mainView.FocusRightRequested += () => SetActivePane(ActivePane.Stats);
        _mainView.RefreshRequested += () => _ = LoadDataAsync(forceRefresh: true);
        _mainView.QuitRequested += () => _app.RequestStop(_mainView);
        _mainView.GamesNav.GameSelected += OnGameSelectedFromView;
    }

    private async Task LoadDataAsync(bool forceRefresh)
    {
        await _loadLock.WaitAsync();
        try
        {
            _app.Invoke(() => _mainView.SetStatus("Loading..."));

            var games = await _gameService.GetGamesTodayAsync(forceRefresh);
            _games = games;

            if (_games.Count == 0)
            {
                _selectedGameIndex = 0;
                _gamesPageIndex = 0;
                _statsPageIndex = 0;
                _selectedGameDetails = null;

                _app.Invoke(Render);
                return;
            }

            _selectedGameIndex = Math.Clamp(_selectedGameIndex, 0, _games.Count - 1);
            _gamesPageIndex = _selectedGameIndex / GamesPageSize;

            var selectedGame = _games[_selectedGameIndex];
            _selectedGameDetails = await _statsService.GetGameDetailsAsync(selectedGame, forceRefresh);
            _statsPageIndex = Math.Min(_statsPageIndex, GetStatsTotalPages() - 1);
            _statsPageIndex = Math.Max(_statsPageIndex, 0);

            _app.Invoke(Render);
        }
        catch (Exception ex)
        {
            _app.Invoke(() =>
            {
                _mainView.GameStats.SetEmpty($"Error: {ex.Message}");
                _mainView.SetStatus("Press r to retry");
            });
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private void OnMoveDown()
    {
        if (_activePane == ActivePane.Games)
        {
            if (_games.Count == 0)
            {
                return;
            }

            var nextIndex = Math.Min(_selectedGameIndex + 1, _games.Count - 1);
            if (nextIndex == _selectedGameIndex)
            {
                return;
            }

            _selectedGameIndex = nextIndex;
            _gamesPageIndex = _selectedGameIndex / GamesPageSize;
            _statsPageIndex = 0;
            _ = LoadSelectedGameStatsAsync();
            return;
        }

        OnNextPage();
    }

    private void OnMoveUp()
    {
        if (_activePane == ActivePane.Games)
        {
            if (_games.Count == 0)
            {
                return;
            }

            var nextIndex = Math.Max(_selectedGameIndex - 1, 0);
            if (nextIndex == _selectedGameIndex)
            {
                return;
            }

            _selectedGameIndex = nextIndex;
            _gamesPageIndex = _selectedGameIndex / GamesPageSize;
            _statsPageIndex = 0;
            _ = LoadSelectedGameStatsAsync();
            return;
        }

        OnPreviousPage();
    }

    private void OnNextPage()
    {
        if (_activePane == ActivePane.Games)
        {
            var next = Math.Min(_gamesPageIndex + 1, GetGamesTotalPages() - 1);
            if (next == _gamesPageIndex)
            {
                return;
            }

            _gamesPageIndex = next;
            _selectedGameIndex = _gamesPageIndex * GamesPageSize;
            _statsPageIndex = 0;
            _ = LoadSelectedGameStatsAsync();
            return;
        }

        _statsPageIndex = Math.Min(_statsPageIndex + 1, GetStatsTotalPages() - 1);
        Render();
    }

    private void OnPreviousPage()
    {
        if (_activePane == ActivePane.Games)
        {
            var next = Math.Max(_gamesPageIndex - 1, 0);
            if (next == _gamesPageIndex)
            {
                return;
            }

            _gamesPageIndex = next;
            _selectedGameIndex = _gamesPageIndex * GamesPageSize;
            _statsPageIndex = 0;
            _ = LoadSelectedGameStatsAsync();
            return;
        }

        _statsPageIndex = Math.Max(_statsPageIndex - 1, 0);
        Render();
    }

    private void OnFirstPage()
    {
        if (_activePane == ActivePane.Games)
        {
            _gamesPageIndex = 0;
            _selectedGameIndex = 0;
            _statsPageIndex = 0;
            _ = LoadSelectedGameStatsAsync();
            return;
        }

        _statsPageIndex = 0;
        Render();
    }

    private void OnLastPage()
    {
        if (_activePane == ActivePane.Games)
        {
            _gamesPageIndex = GetGamesTotalPages() - 1;
            _selectedGameIndex = _gamesPageIndex * GamesPageSize;
            _statsPageIndex = 0;
            _ = LoadSelectedGameStatsAsync();
            return;
        }

        _statsPageIndex = GetStatsTotalPages() - 1;
        Render();
    }

    private void SetActivePane(ActivePane pane)
    {
        _activePane = pane;
        Render();
    }

    private async Task LoadSelectedGameStatsAsync()
    {
        if (_games.Count == 0 || _selectedGameIndex >= _games.Count)
        {
            return;
        }

        await _loadLock.WaitAsync();
        try
        {
            _app.Invoke(() => _mainView.SetStatus("Loading game stats..."));
            _selectedGameDetails = await _statsService.GetGameDetailsAsync(_games[_selectedGameIndex], forceRefresh: false);
            _app.Invoke(Render);
        }
        catch (Exception ex)
        {
            _app.Invoke(() =>
            {
                _mainView.GameStats.SetEmpty($"Error: {ex.Message}");
                _mainView.SetStatus("Press r to retry");
            });
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private void OnGameSelectedFromView(int selectedIndexOnPage)
    {
        if (_games.Count == 0)
        {
            return;
        }

        var absoluteIndex = (_gamesPageIndex * GamesPageSize) + selectedIndexOnPage;
        if (absoluteIndex < 0 || absoluteIndex >= _games.Count || absoluteIndex == _selectedGameIndex)
        {
            return;
        }

        _selectedGameIndex = absoluteIndex;
        _statsPageIndex = 0;
        _ = LoadSelectedGameStatsAsync();
    }

    private void Render()
    {
        RenderGamesPane();
        RenderStatsPane();

        var paneText = _activePane == ActivePane.Games ? "Games pane active" : "Stats pane active";
        _mainView.SetStatus($"{paneText} | h/l focus | j/k move | ctrl-u,n/p page | gg/G first/last | r refresh | q/esc/ctrl-d quit");
    }

    private void RenderGamesPane()
    {
        if (_games.Count == 0)
        {
            _mainView.GamesNav.SetGamesPage(Array.Empty<GameSummary>(), 0, 1, 0);
            return;
        }

        var totalPages = GetGamesTotalPages();
        _gamesPageIndex = Math.Clamp(_gamesPageIndex, 0, totalPages - 1);

        var pageItems = _games
            .Skip(_gamesPageIndex * GamesPageSize)
            .Take(GamesPageSize)
            .ToList();

        var selectedOnPage = _selectedGameIndex - (_gamesPageIndex * GamesPageSize);
        _mainView.GamesNav.SetGamesPage(pageItems, _gamesPageIndex, totalPages, selectedOnPage);
    }

    private void RenderStatsPane()
    {
        if (_selectedGameDetails is null)
        {
            _mainView.GameStats.SetEmpty("No game selected");
            return;
        }

        var details = _selectedGameDetails;
        _mainView.GameStats.SetGameHeader(
            $"{details.Game.AwayTeam.Abbreviation} @ {details.Game.HomeTeam.Abbreviation} | {details.Game.Status}",
            details.Game.AwayTeam.Name,
            details.Game.HomeTeam.Name
        );

        var allRows = BuildPlayerRows(details);
        var totalPages = Math.Max(1, (int)Math.Ceiling((double)allRows.Count / StatsPageSize));
        _statsPageIndex = Math.Clamp(_statsPageIndex, 0, totalPages - 1);

        var pageRows = allRows.Skip(_statsPageIndex * StatsPageSize).Take(StatsPageSize).ToList();
        _mainView.GameStats.SetStatsPage(pageRows, _statsPageIndex, totalPages);
    }

    private static List<string> BuildPlayerRows(GameDetails details)
    {
        var rows = new List<string>
        {
            $"Away ({details.Game.AwayTeam.Abbreviation})",
        };

        rows.AddRange(details.AwayPlayers.Select(p => p.ToDisplayString()));
        rows.Add(string.Empty);
        rows.Add($"Home ({details.Game.HomeTeam.Abbreviation})");
        rows.AddRange(details.HomePlayers.Select(p => p.ToDisplayString()));

        return rows;
    }

    private int GetGamesTotalPages()
    {
        if (_games.Count == 0)
        {
            return 1;
        }

        return (int)Math.Ceiling((double)_games.Count / GamesPageSize);
    }

    private int GetStatsTotalPages()
    {
        if (_selectedGameDetails is null)
        {
            return 1;
        }

        var totalRows = _selectedGameDetails.AwayPlayers.Count + _selectedGameDetails.HomePlayers.Count + 3;
        return Math.Max(1, (int)Math.Ceiling((double)totalRows / StatsPageSize));
    }

    private enum ActivePane
    {
        Games,
        Stats
    }
}
