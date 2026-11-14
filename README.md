# Unity Chess

A 2D, two-player chess game built with **Unity** and **C#**, with a full implementation of the standard rules of chess.

<!-- Add a screenshot or GIF: ![Gameplay](docs/gameplay.png) -->

## Features

- Standard 8x8 board, click-to-select pieces with highlighted legal moves (captures shown in red)
- Legal-move enforcement: you cannot leave or put your own king in check, and pinned pieces cannot move away
- **Check, checkmate and stalemate** detection with an on-screen result
- **Castling** (both sides, with all the usual restrictions), **en passant** and **pawn promotion**
- Draws by **insufficient material**, the **50-move rule** and **threefold repetition**
- Main menu, game timer and restart after the game ends

## Getting started

**Requirements:** Unity `<your Unity version>` (see `ProjectSettings/ProjectVersion.txt`).

1. Clone the repository:
   ```bash
   git clone https://github.com/OhadBashani/<repo-name>.git
   ```
2. Open the project folder in Unity Hub (**Add** → select the folder).
3. Open the `MainMenu` scene (or the `Game` scene) from `Assets/Scenes` and press **Play**.

## How to play

- White moves first. Click one of your pieces to see its legal moves.
- Click a highlighted square to move. Red squares are captures.
- When a pawn reaches the last rank it is promoted (to a queen by default, or to a piece of your choice if a promotion panel is set up in the scene).
- When the game ends, click anywhere to start a new game.

## Project structure

```
Assets/
  Scripts/
    GameController.cs   Board state, turns, rules, check/mate/draw detection
    ChessManager.cs     A single piece: type, colour, move generation
    MovePlate.cs        Clickable highlighted square for a legal move
    Move.cs             A destination square and special-move flags
    Timer.cs            Game timer
    MainMenu.cs         Main menu buttons
  Prefabs/              Piece and move-plate prefabs
  Scenes/               Menu and game scenes
  Sprites/              Board and piece art
```

## Roadmap

The game logic currently lives inside Unity components. Planned next steps:

- Separate the chess rules into a Unity-independent core library
- Automated tests (unit tests and perft checks for move generation)
- UI polish: move list, undo, animations, sounds
- Single-player mode against a simple AI (minimax with alpha-beta pruning)
- WebGL build

See [ROADMAP.md](ROADMAP.md) for details.

## Credits

<!-- List the sources of the art and any tutorials or assets you used, with their licenses. -->

## License

Released under the [MIT License](LICENSE).
