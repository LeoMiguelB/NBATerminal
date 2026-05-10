using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace NBATerminal.Views;

public sealed class MainView : Window
{
    private GamesNavView _gamesNavView = null!;
    private GameStatsView _gameStatsView = null!;
    private Label _statusLabel = null!;
    private bool _pendingG;

    public event Action? MoveUpRequested;
    public event Action? MoveDownRequested;
    public event Action? NextPageRequested;
    public event Action? PreviousPageRequested;
    public event Action? GoFirstRequested;
    public event Action? GoLastRequested;
    public event Action? FocusLeftRequested;
    public event Action? FocusRightRequested;
    public event Action? RefreshRequested;
    public event Action? QuitRequested;

    public MainView()
    {
        Title = "NBATerminal";
        CreateControls();
        BuildLayout();
        WireEvents();
    }

    public GamesNavView GamesNav => _gamesNavView;

    public GameStatsView GameStats => _gameStatsView;

    public void CreateControls()
    {
        _gamesNavView = new GamesNavView
        {
            X = 0,
            Y = 0,
            Width = Dim.Percent(35),
            Height = Dim.Fill(1)
        };

        _gameStatsView = new GameStatsView
        {
            X = Pos.Right(_gamesNavView),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(1)
        };

        _statusLabel = new Label
        {
            Text = "h/l focus | j/k move | ctrl-u,n/p page | gg/G first/last | r refresh | q/esc/ctrl-d quit",
            X = 0,
            Y = Pos.Bottom(_gamesNavView),
            Width = Dim.Fill(),
            Height = 1
        };
    }

    public void BuildLayout()
    {
        Add(_gamesNavView);
        Add(_gameStatsView);
        Add(_statusLabel);
    }

    public void WireEvents()
    {
    }

    public void SetStatus(string message)
    {
        _statusLabel.Text = message;
    }

    protected override bool OnKeyDown(Key key)
    {
        if (IsPlainKey(key, KeyCode.J) || key.KeyCode == KeyCode.CursorDown)
        {
            MoveDownRequested?.Invoke();
            return true;
        }

        if (IsPlainKey(key, KeyCode.K) || key.KeyCode == KeyCode.CursorUp)
        {
            MoveUpRequested?.Invoke();
            return true;
        }

        if (IsPlainKey(key, KeyCode.H) || key.KeyCode == KeyCode.CursorLeft)
        {
            FocusLeftRequested?.Invoke();
            return true;
        }

        if (IsPlainKey(key, KeyCode.L) || key.KeyCode == KeyCode.CursorRight)
        {
            FocusRightRequested?.Invoke();
            return true;
        }

        if (IsPlainKey(key, KeyCode.N))
        {
            NextPageRequested?.Invoke();
            _pendingG = false;
            return true;
        }

        if (IsPlainKey(key, KeyCode.P) || IsCtrlKey(key, KeyCode.U))
        {
            PreviousPageRequested?.Invoke();
            _pendingG = false;
            return true;
        }

        if (IsPlainKey(key, KeyCode.R))
        {
            RefreshRequested?.Invoke();
            _pendingG = false;
            return true;
        }

        if (IsPlainKey(key, KeyCode.Q))
        {
            QuitRequested?.Invoke();
            _pendingG = false;
            return true;
        }

        if (key.KeyCode == KeyCode.Esc || IsCtrlKey(key, KeyCode.D))
        {
            QuitRequested?.Invoke();
            _pendingG = false;
            return true;
        }

        if (IsPlainKey(key, KeyCode.G))
        {
            if (_pendingG)
            {
                GoFirstRequested?.Invoke();
                _pendingG = false;
            }
            else
            {
                _pendingG = true;
            }

            return true;
        }

        if (key.KeyCode == KeyCode.G && key.IsShift && !key.IsCtrl && !key.IsAlt)
        {
            GoLastRequested?.Invoke();
            _pendingG = false;
            return true;
        }

        _pendingG = false;
        return base.OnKeyDown(key);
    }

    private static bool IsPlainKey(Key key, KeyCode keyCode)
    {
        return key.KeyCode == keyCode && !key.IsCtrl && !key.IsAlt && !key.IsShift;
    }

    private static bool IsCtrlKey(Key key, KeyCode keyCode)
    {
        return key.KeyCode == keyCode && key.IsCtrl && !key.IsAlt;
    }
}
