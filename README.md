# NBATerminal

Terminal.Gui MVC TUI for NBA games and player stats.

## Features

- Two-column TUI layout
  - Left: games today
  - Right: selected game stats and home/away teams
- Manual refresh only (no polling)
- Paging for games and stats lists
- Vim-friendly keybindings

## Requirements

- .NET 10 SDK
- Python 3.10+
- `nba_api` Python package

Install Python dependency:

```bash
python3 -m pip install nba_api
```

## Run

```bash
dotnet run
```

## Controls

- `h/l` or left/right arrows: switch active pane
- `j/k` or up/down arrows: move selection (games pane) / page nav (stats pane)
- `n` or `Ctrl+d`: next page
- `p` or `Ctrl+u`: previous page
- `g` then `g`: first page
- `G`: last page
- `r`: refresh data
- `q`: quit

## Project Structure

- `Controllers/` - app flow and coordination
- `Models/` - domain models and paging DTOs
- `Services/` - Python bridge + data retrieval/cache
- `Views/` - Terminal.Gui rendering components
- `Program.cs` - app composition and startup
