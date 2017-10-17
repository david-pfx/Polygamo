/// Polygamo is a general player for abstract games and puzzles. See http://www.polyomino.com/polygamo.
///
/// Copyright © Polyomino Games 2017. All rights reserved.
/// 
/// This is free software. You are free to use it, modify it and/or 
/// distribute it as set out in the licence at http://www.polyomino.com/licence.
/// You should have received a copy of the licence with the software.
/// 
/// This software is distributed in the hope that it will be useful, but with
/// absolutely no warranty, express or implied. See the licence for details.

///
/// Polygamo Library API
/// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Poly.Common;
using Poly.Compiler;
using Poly.Engine;

///===========================================================================
/// <summary>
/// This is the public access for playing a game.
/// There is a strong separation between this API and the underlying details.
/// </summary>

namespace Polygamo {
  // possible game results 
  // also used for condition checking (Count gets converted)
  public enum ResultKinds {
    None, Win, Draw, Loss, Count,
  }

  public enum MoveKinds {
    None,
    Drop, Move, Copy, Take,
    Owner, Piece, Attrib,
  }

  public enum ChooserKinds {
    None, First,
    Depth, Full,
    Breadth,
    Mcts,
  }

  ///===========================================================================
  /// <summary>
  /// Static info about a position (square) on board
  /// </summary>
  public struct PolyPosition {
    public string Name;
    public Rect Location;
    public int[] Coords;
    public bool IsDummy;
    internal static PolyPosition Create(PositionDef position) {
      return new PolyPosition {
        Name = position.Position.Value,
        Location = position.Location,
        Coords = position.Coords,
        IsDummy = position.IsDummy,
      };
    }
  }

  ///===========================================================================
  /// <summary>
  /// Static info about links between positions
  /// </summary>
  public struct PolyLink {
    public string Direction;
    public string From;
    public string To;
    internal static PolyLink Create(LinkDef link) {
      return new PolyLink {
        Direction = link.Direction.Value,
        From = link.From.Value,
        To = link.To.Value,
      };
    }
  }

  ///===========================================================================
  /// <summary>
  /// Static info about zones
  /// </summary>
  public struct PolyZone {
    public string Name;
    public string Player;
    public string Positions;
    internal static PolyZone Create(ZoneValue name, PlayerValue player, IEnumerable<PositionValue> positions) {
      return new PolyZone {
        Name = name.Value,
        Player = player.Value,
        Positions = positions.Select(p => p.Value).OrderBy(p => p).Join(" "),
      };
    }
  }

  ///===========================================================================
  /// <summary>
  /// Static info about a piece
  /// </summary>
  public struct PolyPiece {
    public string Name;
    public string Notation;
    public string Description;
    public string Help;
    public bool IsDummy;
    internal static PolyPiece Create(PieceDef piece) {
      return new PolyPiece {
        Name = piece.Name,
        Notation = piece.HelpLookup.SafeLookup(HelpKinds.Notation)?.Value ?? "",
        Description = piece.HelpLookup.SafeLookup(HelpKinds.Description)?.Value ?? "",
        Help = piece.HelpLookup.SafeLookup(HelpKinds.Help)?.Value ?? "",
        IsDummy = piece.IsDummy,
      };
    }
  }

  ///===========================================================================
  /// <summary>
  /// Static info about images available for a piece
  /// </summary>
  public struct PolyPieceImage {
    public string Piece;
    public string Player;
    public IList<string> Images;
    internal static PolyPieceImage Create(PieceValue piece, PlayerValue player, IList<TextValue> image) {
      return new PolyPieceImage {
        Piece = piece.Value,
        Player = player.Value,
        Images = image.Select(i=>i.Value).ToList(),
      };
    }
  }

  ///===========================================================================
  /// <summary>
  /// Current info about an off-board store
  /// </summary>
  public struct PolyOffStore {
    public string Player;
    public string Piece;
    public int Quantity;
    internal static PolyOffStore Create(PlayerValue player, PieceValue piece, int quantity) {
      return new PolyOffStore {
        Player = player.Value,
        Piece = piece.Value,
        Quantity = quantity,
      };
    }
  }

  ///===========================================================================
  /// <summary>
  /// Current info about a piece played to the board
  /// </summary>
  public struct PolyPlayedPiece {
    public string Player;
    public string Piece;
    public string Position;
    public string Image;
    public string Flags;

    internal static PolyPlayedPiece Create(PositionValue position, PieceModel piece, int index = 0) {
      var images = piece.Def.GetImages(piece.Player);
      return new PolyPlayedPiece {
        Player = piece.Player.Value,
        Piece = piece.Piece.Value,
        Position = position.Value,
        Flags = piece.AttributeLookup == null ? null
          : piece.AttributeLookup.Where(a => a.Value == BoolValue.True).Select(a => a.Key).Join(" "),
        Image = images[index % images.Count].Value,
      };
    }

    internal static IList<PolyPlayedPiece> Create(BoardModel board) {
      return board.PlayedPieceLookup
        .Select(p => PolyPlayedPiece.Create(p.Key, p.Value)).ToList();
    }

    public override string ToString() {
      return String.Format("{0}:{1}:{2}", Position, Player, Piece);
    }
    public string Format() {
      return String.Format("at {0} {1} {2}", Position, Player, Piece);
    }
  }

  ///===========================================================================
  /// <summary>
  /// Current info for a part of a move
  /// </summary>
  public struct PolyMovePart {
    public MoveKinds Kind;
    public string Position;  // final position, always set
    public string Final;      // original position, for Move only
    public string Player;    // current or new owner, always set
    public string Piece;     // current or new piece type, always set
    public string Attribute; // flag name, only for set attrib
    public bool Value;       // true if flag is set, else if cleared

    static Dictionary<MoveKinds, string> _kindlookup = new Dictionary<MoveKinds, string> {
      { MoveKinds.None,         "{0},{1}" },
      { MoveKinds.Drop,         "{0} drop {1} at {2}" },
      { MoveKinds.Move,         "{0} move {1} to {2}" },
      { MoveKinds.Copy,         "{0} copy {1} to {2}" },
      { MoveKinds.Take,         "{0} capture {1} at {2}" },
      { MoveKinds.Owner,        "{0} change owner of {1} at {2}" },
      { MoveKinds.Piece,        "{0} change piece at {2} to {1}" },
      { MoveKinds.Attrib,       "{0} for piece {1} at {2}" },
    };

    public string Format() {
      var s = String.Format(_kindlookup[Kind], Player, Piece, Position);
      return (Kind == MoveKinds.Move || Kind == MoveKinds.Copy) ? String.Format("{0} from {1}", s, Final)
        : (Kind == MoveKinds.Owner) ? String.Format("{0} to {1}", s, Player)
        : (Kind == MoveKinds.Attrib) ? String.Format("{0} set {1} to {2}", s, Attribute, Value)
        : s;
    }

    internal static PolyMovePart Create(int index, MovePartModel move) {
      return new PolyMovePart {
        Kind = move.Kind,
        Position = move.Position.Value,
        Final = move.Final?.Value,
        Player = move.Player.Value,
        Piece = move.Piece.Value,
        Attribute = (move.Attribute == null) ? null : move.Attribute.Value,
        Value = (move.Attribute != null && move.Value == BoolValue.True),
      };
    }
  }

  ///===========================================================================
  /// <summary>
  /// Current info for an available move
  /// Enough to positively identify and display the move, but not enough to execute it
  /// </summary>
  public class PolyMove {
    public int Index;             // index for base move, used by make move
    public string Player;         // player on turn
    public string Position1;      // first position
    public string Piece1;         // (opt) piece left at first position
    public string Position2;      // (opt) second position
    public string Piece2;         // (opt) piece left at second position
    public bool IsPass = false;   // true if this move is a pass
    private MoveModel _move;


    public override string ToString() {
      return (Position1 == null) ? String.Format("Move<{0}>", Index)
        : (Position2 == null) ? String.Format("Move<{0},{1},{2},{3}>", Index, Player, Position1, Piece1)
        : String.Format("Move<{0},{1},{2},{3},{4},{5}>", Index, Player, Position1, Piece1, Position2, Piece2);
    }

    public string ToString(string arg) {
      return (arg == "M" || arg == "P") ? _move.ToString(arg)
        : ToString();
    }

    internal static PolyMove Create(int index, MoveModel move) {
      if (move == null) return new PolyMove {
        Index = index,
      };
      if (move.IsPass) return new PolyMove {
        Index = index, IsPass = true,
        Player = move.Player.Value,
        Position1 = "", Piece1 = "", Position2 = "", Piece2 = "",
        _move = move,
      };
      var mp = move.MoveParts[0];
      var occup1 = mp.Kind == MoveKinds.Drop || mp.Kind == MoveKinds.Copy;
      var occup2 = mp.Final != null;
      return new PolyMove {
        Index = index,
        Player = mp.Player.Value,
        Position1 = mp.Position.Value,
        Piece1 = occup1 ? mp.Piece.Value : "",
        Position2 = occup2 ? mp.Final.Value : "",
        Piece2 = occup2 ? mp.Piece.Value : "",
        _move = move,
      };
    }
  }

  ///===========================================================================
  /// <summary>
  /// Current info for an available turn
  /// </summary>
  public struct PolyTurn {
    public string TurnPlayer;   // player's turn
    public string MovePlayer;   // playing as this
    public string MoveType;
    internal static PolyTurn Create(TurnDef turn) {
      return new PolyTurn {
        TurnPlayer = turn.TurnPlayer.Value,
        MovePlayer = (turn.MovePlayer == PlayerValue.None) ? turn.TurnPlayer.Value : turn.MovePlayer.Value,
        MoveType = turn.MoveType.Value,
      };
    }
  }

  ///===========================================================================
  public struct PolyChoice {
    public string Player;
    public string LastPlayer;
    public bool IsDone;
    public int Depth;
    public double Weight;
    public IList<PolyPlayedPiece> PlayedPieces;
    public PolyMove PlayedMove;
    public PolyMove ChosenMove;
    public IList<PolyPlayedPiece> Played;
    public ResultKinds Result;
    Choice _choice;

    internal static PolyChoice Create(Choice choice, int depth) {
      return new PolyChoice {
        _choice = choice,
        Depth = depth,
        Player = choice.TurnPlayer.Value,
        LastPlayer = choice.LastTurnPlayer.Value,
        IsDone = choice.IsDone,
        Weight = choice.Weight,
        Result = choice.Board.Result,
        PlayedPieces = PolyPlayedPiece.Create(choice.Board),
        PlayedMove = choice.Board.LastMove == null ? null
          : PolyMove.Create(0, choice.Board.LastMove),
        ChosenMove = choice.Board.LegalMoves.Count == 0 ? null
          : PolyMove.Create(choice.BestIndex, choice.Board.LegalMoves[choice.BestIndex]),
        Played = choice.Board.PlayedPieceLookup
          .Select(p => PolyPlayedPiece.Create(p.Key, p.Value)).ToList(),
      };
    }

    public string Format() { return _choice.ToString(); }
  }

  //////////////////////////////////////////////////////////////////////////////
  /// <summary>
  /// Implements a game based on loading a script.
  /// Provides the main access to the game engine, all as simple method calls on this object.
  /// Behind it are models for game, board, piece, move, etc.
  /// </summary>
  //////////////////////////////////////////////////////////////////////////////

  public class PolyGame {
    public static string LastError { get; private set; }
    // -- internals
    internal TextReader _input { get; private set; }
    internal MenuModel _menumodel;
    internal GameModel _gamemodel;
    internal ChoiceMaker _chooser { get { return _gamemodel.Chooser; } }
    BoardModel _boardmodel { get { return _gamemodel.CurrentBoard; } }
    MenuDef _menudef { get { return _menumodel.Def; } }
    GameDef _gamedef { get { return _gamemodel.Def; } }
    BoardDef _boarddef { get { return _boardmodel.Def; } }
    IList<GameDef> _gamedefs { get { return _menudef.Games; } }
    IList<string> _images;
    int _variant = 0;

    // create a game from text, wrapped for all exceptions
    public static PolyGame Create(string path, TextReader reader = null, int variant = 0) {
      try {
        return CreateInner(path, reader, variant);
      } catch (Exception ex) {
        LastError = ex.Message;
        return null;
      }
    }

    // create a game from text, no wrapper
    public static PolyGame CreateInner(string path, TextReader reader = null, int variant = 0) {
      LastError = null;
      return new PolyGame {
        SourcePath = path,
        _input = reader,
        _variant = variant,
      }.CreateMenuModel().CreateGameModel();
    }

    // create a variant game
    public PolyGame Create(int variant) {
      if (!(variant >= 0 && variant < _gamedefs.Count)) throw Error.Assert("no such game {0}", variant);
      return new PolyGame {
        SourcePath = this.SourcePath,
        _menumodel = this._menumodel,
        _variant = variant,
      }.CreateGameModel();
    }

    // parse a game file and choose a game (which can be changed later)
    // TODO: refactor back into compiler
    PolyGame CreateMenuModel() {
      if (_input == null) {
        if (!File.Exists(SourcePath)) throw Error.Fatal("file does not exist: " + SourcePath);
        _input = new StreamReader(SourcePath);
      }

      var parser = Parser.Create();
      var nodes = parser.ParseNodes(_input, Console.Out, SourcePath).ToList();
      _input.Close();
      if (parser.Error) throw Error.Fatal("compilation terminated");

      var generator = Generator.Create();
      var compiler = Compiler.Create(SourcePath, generator, parser.Symbols);
      compiler.CompileMenu(nodes);

      var evaluator = Evaluator.Create(Console.Out, Console.In);
      var start = StartCode.Create(evaluator, generator.Code);
      if (Logger.Level >= 4) generator.Decode(Logger.Out);

      var menudef = start.CreateMenu();
      _menumodel = MenuModel.Create(menudef);
      if (_gamedefs.Count == 0) throw Error.Fatal("no games found");
      //if (Logger.Level >= 3) _menudef.Dump(Logger.Out);

      return this;
    }

    // create game model, initially or after changing options
    PolyGame CreateGameModel() {
      _gamemodel = _menumodel.CreateGame(_variant);
      if (Logger.Level >= 3) _gamedef.Dump(Logger.Out);
      _images = _boarddef.Images.Select(i => i.Value).ToList();
      return this;
    }

    // return next values from shared rng
    public int NextRandom(int range) {
      return _gamemodel.Rng.Next(range);
    }

    // reseed rng
    public void Reseed(int seed) {
      _gamemodel.Reseed(seed);
    }

    ///=========================================================================
    /// Static information relevant to the available games and variants
    /// 

    // source for game definition
    public string SourcePath { get; private set; }
    // enumerate available variants
    public IList<string> Menu {
      get { return _gamedefs.Select(g => g.GetStringProperty("title")).ToList(); }
    }
    // enumerate variant thumbnails
    // note: might use board image as default, except board not created yet
    public IList<string> Thumbnails {
      get { return _gamedefs.Select(g => g.GetStringProperty("thumbnail")).ToList(); }
    }

    ///=========================================================================
    /// Static information relevant to the currently selected game
    ///

    public string GetOption(string name) {
      var value = _gamedef.GetProperty(name);
      return (value == null) ? null : value.AsString;
    }
    public void SetOption(string name, string value) {
      _gamedef.SetProperty(name, TextValue.Create(value));
      CreateGameModel();
    }

    // title of current game
    public string Title { get { return _gamedef.GetStringProperty("title"); } }
    // enumerate players
    public IList<string> Players {
      get { return _gamedef.PlayerLookup.Select(p => p.Key.Value)
          .OrderBy(p=>p)
          .ToList(); }
    }
    // enumerate active players
    public IList<string> ActivePlayers {
      get { return _gamedef.PlayerLookup.Where(p => p.Value.IsActive).Select(p => p.Key.Value)
          .OrderBy(p => p)
          .ToList(); }
    }
    // first player is the first active player to have a turn
    public string FirstPlayer {
      get {
        return _gamedef.TurnOrders
          .Where(t => _gamedef.PlayerLookup[t.TurnPlayer].IsActive)
          .First().TurnPlayer.Value;
      }
    }
    public IList<string> BoardImages { get { return _images; } }

    // list of defined positions
    public IList<PolyPosition> Positions {
      get { return _boarddef.PositionLookup.Select(p => PolyPosition.Create(p.Value)).ToList(); }
    }

    // list of defined links
    public IList<PolyLink> Links {
      get {
        return _boarddef.LinkLookup
          .SelectMany(p => p.Value
          .Select(k => PolyLink.Create(k))).ToList();
      }
    }

    // list of defined zones
    public IList<PolyZone> Zones {
      get {
        return _boarddef.ZoneLookup
          .Select(z => PolyZone.Create(z.Key.Item1, z.Key.Item2, z.Value))
          .OrderBy(z => z.Name)
          .ToList();
      }
    }

    // list of defined pieces
    public IList<PolyPiece> Pieces {
      get {
        return _gamedef.PieceLookup
          .Select(p => PolyPiece.Create(p.Value))
          .OrderBy(p => p.Name)
          .ToList();
      }
    }

    // game piece images (by player)
    public IList<PolyPieceImage> PieceImages {
      get {
        return _gamedef.PieceLookup
      .SelectMany(p => p.Value.ImageLookup
      .Select(i => PolyPieceImage.Create(p.Key, i.Key, i.Value))).ToList();
      }
    }

    // list of off-board piece stores
    public IList<PolyOffStore> OffStores {
      get {
        return _boardmodel.OffStoreLookup
          .Select(kv => PolyOffStore.Create(kv.Key.Item1, kv.Key.Item2, kv.Value)).ToList();
      }
    }

    ///=========================================================================
    /// Dynamic information relevant to the current game state
    ///

    // next player, as player and move type
    public PolyTurn NextTurn { get { return PolyTurn.Create(_boardmodel.Turn); } }
    // current active player on turn for board 
    public string TurnPlayer { get { return _boardmodel.TurnPlayer.Value; } }
    // active player on previous turn
    public string LastPlayer { get { return _boardmodel.LastTurnPlayer.Value; } }
    // player who owns result if any
    public string ResultPlayer {
      get { return (_boardmodel.ResultPlayer == PlayerValue.None ) ? "-" : _boardmodel.ResultPlayer.Value; }
    }
    // get game result for board and current player
    public ResultKinds GameResult { get { return _boardmodel.Result; } }
    // get move from chooser, or possibly random
    public PolyMove ChosenMove { get { return GetLegalMove(_gamemodel.ChosenMove); } }
    // is pass allowed now?
    public bool CanPass { get { return LegalMoves.Count > 0 && LegalMoves[0].IsPass; } }
    // is undo possible now?
    public bool CanUndo { get { return _gamemodel.CanUndo; } }

    // enumerate current played pieces
    public IList<PolyPlayedPiece> PlayedPieces { get { return PolyPlayedPiece.Create(_boardmodel); } }

    public IList<PolyMovePart> LegalMoveParts {
      get {
        return _boardmodel.LegalMoves
          .SelectMany(m => m.MoveParts
          .Select((p, x) => PolyMovePart.Create(x, p)))
          .ToList();
      }
    }

    // enumerate over allowed moves
    public IList<PolyMove> LegalMoves {
      get {
        return _boardmodel.LegalMoves.Select((m, x) => PolyMove.Create(x, m)).ToList();
      }
    }

    // enumerate over moves played to this point
    public IList<PolyMove> MovesPlayed {
      get {
        return _gamemodel.MovesPlayed.Select((m, x) => PolyMove.Create(x, m)).ToList();
      }
    }

    // enquiries
    public PolyMove GetLegalMove(int index) {
      return (index >= 0 && index < LegalMoves.Count) ? LegalMoves[index]
        : PolyMove.Create(index, null);
    }

    // enumerate over allowed move parts (for testing)
    public IList<PolyMovePart> GetMoveParts(int index) {
      if (!(index >= 0 && index < LegalMoves.Count)) throw Error.Assert("no such move {0}", index);
      return _boardmodel.LegalMoves[index].MoveParts
        .Select((p, x) => PolyMovePart.Create(x, p))
        .ToList();
    }

    //==========================================================================
    // Change game state by making or unmaking a move
    //

    // start a new game with default chooser and params
    public void NewBoard() {
      _gamemodel.NewBoard();
    }

    // start a new game with chooser and params
    public void NewBoard(ChooserKinds chooserkind, int stepcount, int maxdepth) {
      _gamemodel.NewBoard(chooserkind, stepcount, maxdepth);
    }

    // make a move
    public bool MakeMove(int index) {
      return _gamemodel.MakeMove(index);
    }

    public void UndoMove() {
      _gamemodel.Undo();
    }

    public void RedoMove() {
      _gamemodel.Redo();
    }

    public void Restart() {
      _gamemodel.Restart();
    }

    // parameters for chooser
    public int StepCount {
      get { return _chooser.StepCount; }
      set { _chooser.StepCount = value; }
    }
    public int MaxDepth {
      get { return _chooser.MaxDepth; }
      set { _chooser.MaxDepth = value; }
    }
    public int VisitCount { get { return _chooser.VisitCount; } }
    public double Weight { get { return _chooser.Weight; } }

    // update chooser, return true if all done
    public bool UpdateChooser() {
      return _gamemodel.UpdateChooser();
    }

    public IEnumerable<PolyChoice> ChoicesIter() {
      return ChoiceHelper(_chooser.Choice, 0);
    }

    IEnumerable<PolyChoice> ChoiceHelper(Choice choice, int depth) {
      yield return PolyChoice.Create(choice, depth);
      foreach (var child in choice.Children)
        if (child != null) foreach (var c in ChoiceHelper(child, depth + 1))
            yield return c;
    }

    //--- implementation

    // entry point for experimental testing
    public void Test() {
      //Logger.Level = 3;
      Console.WriteLine("\n\n====================================================\n\n");
      foreach (var game in _gamedefs) {
        Console.WriteLine("Game {0}: default:{1}", game.GetStringProperty("title"), game.IsDefault);
      }

      //Logger.Level = 3;
      Console.WriteLine("\nDefined positions");
      foreach (var position in _boardmodel.PositionIter())
        Console.WriteLine("Position {0}", position);

      Console.WriteLine("\nPlaced pieces");
      foreach (var piece in _boardmodel.PlayedPieceLookup)
        Console.WriteLine("Position {0}: piece {1}, player {2}", piece.Key, piece.Value.Piece, piece.Value.Player);

      Console.WriteLine("\nOff stores");
      foreach (var kv in _boardmodel.OffStoreLookup) {
        Console.WriteLine("Player:{0} piece:{1} count:{2}", kv.Key.Item1, kv.Key.Item2, kv.Value);
      }

      //Logger.Level = 3;
      Console.WriteLine("\nPlayer {0}", _boardmodel.Turn.TurnPlayer);
      Console.WriteLine("\nGame state: {0} for {1}", _boardmodel.Result, _boardmodel.Turn.TurnPlayer);
      Console.WriteLine("\nMoves {0} Result {1}", _boardmodel.LegalMoves.Count(), _boardmodel.Result);
      foreach (var move in _boardmodel.LegalMoves) {
        Logger.WriteLine(4, "{0}", move);
        for (int i = 0; i < move.MoveParts.Count; i++) {
          Console.WriteLine("{0}{1}", (i == 0 ? "  " : "  --"), move.ToString("P"));
        }
      }
    }

  }

}
