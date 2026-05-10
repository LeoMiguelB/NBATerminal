#!/usr/bin/env python3
import argparse
import json
import sys


def parse_int(value, default=0):
    try:
        return int(value)
    except (TypeError, ValueError):
        return default


def parse_minutes(statistics):
    minutes = statistics.get("minutes")
    if isinstance(minutes, str) and minutes:
        return minutes

    # Some payloads use PTMMSS
    return statistics.get("minutesCalculated", "0")


def map_team(team_data):
    return {
        "teamId": str(team_data.get("teamId", "")),
        "name": team_data.get("teamName", "") or team_data.get("name", ""),
        "abbreviation": team_data.get("teamTricode", "") or team_data.get("abbreviation", ""),
    }


def get_games_today():
    from nba_api.live.nba.endpoints import scoreboard

    payload = scoreboard.ScoreBoard().get_dict()
    games = payload.get("scoreboard", {}).get("games", [])

    mapped = []
    for game in games:
        mapped.append(
            {
                "gameId": str(game.get("gameId", "")),
                "status": game.get("gameStatusText", "Unknown"),
                "homeTeam": map_team(game.get("homeTeam", {})),
                "awayTeam": map_team(game.get("awayTeam", {})),
            }
        )

    return {"games": mapped}


def map_players(players):
    mapped = []
    for player in players:
        statistics = player.get("statistics", {})
        mapped.append(
            {
                "playerName": player.get("name", "Unknown"),
                "points": parse_int(statistics.get("points")),
                "rebounds": parse_int(statistics.get("reboundsTotal")),
                "assists": parse_int(statistics.get("assists")),
                "minutes": parse_minutes(statistics),
            }
        )
    return mapped


def get_game_stats(game_id):
    from nba_api.live.nba.endpoints import boxscore
    from nba_api.live.nba.endpoints import scoreboard

    try:
        payload = boxscore.BoxScore(game_id=game_id).get_dict()
        game = payload.get("game", {})

        home_team = game.get("homeTeam", {})
        away_team = game.get("awayTeam", {})

        return {
            "game": {
                "gameId": str(game.get("gameId", game_id)),
                "status": game.get("gameStatusText", "Unknown"),
                "homeTeam": map_team(home_team),
                "awayTeam": map_team(away_team),
            },
            "homePlayers": map_players(home_team.get("players", [])),
            "awayPlayers": map_players(away_team.get("players", [])),
        }
    except Exception:
        board = scoreboard.ScoreBoard().get_dict()
        games = board.get("scoreboard", {}).get("games", [])
        match = next((g for g in games if str(g.get("gameId", "")) == str(game_id)), {})

        home_team = match.get("homeTeam", {})
        away_team = match.get("awayTeam", {})
        status = match.get("gameStatusText", "Unavailable")

        return {
            "game": {
                "gameId": str(game_id),
                "status": status,
                "homeTeam": map_team(home_team),
                "awayTeam": map_team(away_team),
            },
            "homePlayers": [],
            "awayPlayers": [],
        }


def main():
    parser = argparse.ArgumentParser()
    subparsers = parser.add_subparsers(dest="command", required=True)

    subparsers.add_parser("games-today")

    game_stats_parser = subparsers.add_parser("game-stats")
    game_stats_parser.add_argument("--game-id", required=True)

    args = parser.parse_args()

    if args.command == "games-today":
        result = get_games_today()
    else:
        result = get_game_stats(args.game_id)

    print(json.dumps(result))


if __name__ == "__main__":
    try:
        main()
    except Exception as exc:
        print(str(exc), file=sys.stderr)
        sys.exit(1)
