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
/// Game model
/// 
using System;
using System.Collections.Generic;
using System.Linq;
using Poly.Common;
using Polygamo;

/// <summary>
/// These are the models actually used for playing games.
/// A model contains a code object, which it calls to create another model.
/// 
/// Visibility is internal only. Performance is an issue, especially for
/// building a tree of boards.
/// </summary>
namespace Poly.Engine {

  // possible game states
  internal enum GameStatusKinds {
    Ready, Playing, Finished,
  }

  internal enum GameResultReasons {
    Checkmate, Stalemate, Repetition,
  }

  //---------------------------------------------------------------------------
  /// <summary>
  /// State of game following each move (ply)
  /// </summary>
  internal struct GameState {
    internal BoardModel Board;
    internal ChoiceMaker Chooser;
    public override string ToString() {
      return Board.ToString() + "," + Chooser.ToString();
    }
  }

  ///---------------------------------------------------------------------------
  /// <summary>
  /// Data model for available games
  /// TODO: store history here
  /// </summary>
  internal class MenuModel {
    // Menu def for this model
    internal MenuDef Def { get; private set; }

    // factories
    internal static MenuModel Create(MenuDef def) {
      return new MenuModel {
        Def = def,
      };
    }

    internal GameModel CreateGame(int index = 0) {
      return GameModel.Create(Def.Games[index]);
    }
  }

  ///---------------------------------------------------------------------------
  /// <summary>
  /// Data model for the game being played
  /// TODO: move list
  /// </summary>
  internal class GameModel {
    // game def for this model
    internal GameDef Def { get; private set; }
    // current overall state of game
    internal GameStatusKinds Status { get; private set; }
    // random number generator to be used by all; can be re-seeded
    internal Random Rng { get; private set; }

    // current board position
    internal BoardModel CurrentBoard { get { return _states.Last().Board; } }
    // current move number (0..)
    internal List<MoveModel> MovesPlayed {
      get { return _states.Skip(1).Select(b=>b.Board.LastMove).ToList(); }
    }
    // current move number (0..)
    internal int MoveNumber { get { return _states.Count; } }
    // chooser matched to current board
    internal ChoiceMaker Chooser { get { return _states.Last().Chooser; } }
    // is Undo active?
    internal bool CanUndo { get { return _states.Count > 1; } }

    // chosen move, or random
    internal int ChosenMove {
      get {
        return (Def.PlayerLookup[CurrentBoard.TurnPlayer].IsRandom) ? Rng.Next(CurrentBoard.LegalMoves.Count)
          : (Chooser == null || Chooser.Index < 0) ? 0 : Chooser.Index;
      }
    }

    //--- impl
    List<GameState> _states;     // sequence of game states for moves made
    List<GameState> _redostates; // game states put here after undo
    GameCode _gamecode;
    ChooserKinds _chooserkind;
    internal int _stepcount;    // steps per update (for new board)
    internal int _maxdepth;     // max depth (for new board)

    //-- factories

    // create a game model with board & setup
    internal static GameModel Create(GameDef gamedef, ChooserKinds chooserkind = ChooserKinds.Mcts) {
      var gm = new GameModel {
        Def = gamedef,
        Rng = new Random(),
        _gamecode = gamedef.Code,
        _chooserkind = chooserkind, 
        _stepcount = 10,  // unuseful defaults
        _maxdepth = 2,
      };
      gm.NewBoard();
      return gm;
    }

    // create a new board but not in move list
    internal BoardModel CreateBoard() {
      Def.Board = _gamecode.CreateBoard(this);
      return BoardModel.CreateSetup(this, Def.Board);
    }

    // create a piece for a player
    internal PieceModel CreatePiece(PlayerValue player, PieceValue piece) {
      return PieceModel.Create(player, Def.PieceLookup[piece]);
    }

    // Create all drop moves for some board
    internal IList<MoveModel> CreateDrops(BoardModel board) {
      return _gamecode.CreateDrops(board);
    }

    // Create all non-drop moves for some board
    internal IList<MoveModel> CreateMoves(BoardModel board) {
      return _gamecode.CreateMoves(board);
    }

    //--- enquiries

    // get game result for board and its current player (as set)
    // interpret result kind Count
    internal ResultKinds CheckGameResult(GoalDef goal, BoardModel board, bool phase2) {
      var result = _gamecode.CheckCondition(goal, board, phase2);
      return result;
    }

    //-- state change

    // Set up a new board (start a new game), optionally specify chooser
    internal void NewBoard() {
      NewBoard(_chooserkind, _stepcount, _maxdepth);
    }

    internal void NewBoard(ChooserKinds chooserkind, int stepcount, int maxdepth) {
      _states = new List<GameState>();
      _redostates = new List<GameState>();
      var board = CreateBoard();
      // always start with default values from game def, but should change before update
      var chooser = ChoiceMaker.Create(board, chooserkind, stepcount, maxdepth);
      AddState(chooser);
      Status = GameStatusKinds.Ready;
    }

    internal void Reseed(int seed) {
      Rng = (seed < 0) ? new Random() : new Random(seed);
    }

    // Make the move by index of legal moves for board
    internal bool MakeMove(int index) {
      if (Chooser == null) throw Error.NullArg("chooser");
      var newstate = Chooser.MakeMove(index);
      if (newstate == null) return false;
      AddState(newstate);
      return true;
    }

    // Board is added after move generation and result checking
    void AddState(ChoiceMaker chooser) {
      if (chooser == null) throw Error.NullArg("chooser");
      if (chooser.Choice == null) throw Error.NullArg("chooser.Choice");
      _redostates.Clear();
      var state = new GameState {
        Board = chooser.Choice.Board,
        Chooser = chooser,
      };
      _states.Add(state);
      Status = state.Board.HasResult ? GameStatusKinds.Finished : GameStatusKinds.Playing;
    }

    // update chooser, but not for random players
    internal bool UpdateChooser() {
      if (Def.PlayerLookup[CurrentBoard.TurnPlayer].IsRandom) return true;
      Chooser.Update();
      return Chooser.Choice.IsDone;
    }

    // Undo last move by moving last board to redo list
    // note: first board must not be moved
    internal void Undo() {
      if (_states.Count > 1) {
        _redostates.Add(_states.Last());
        _states.RemoveAt(_states.Count - 1);
      }
    }

    // Redo previously undone move
    internal void Redo() {
      if (_redostates.Count > 0) {
        _states.Add(_redostates.Last());
        _redostates.RemoveAt(_redostates.Count - 1);
      }
    }

    // Restart from initial position
    internal void Restart() {
      _redostates.Clear();
      while (_states.Count > 1)
        _states.RemoveAt(_states.Count - 1);
    }

  }

  ///---------------------------------------------------------------------------
  /// <summary>
  /// Data model for the board being played
  /// Records info about pieces played, not empty positions
  /// </summary>
  internal class BoardModel {
    // board definition for static info
    internal BoardDef Def { get; private set; }
    // piece stores off board by player
    internal Dictionary<Pair<PlayerValue,PieceValue>, int> OffStoreLookup { get { return _offstores; } }
    //internal Dictionary<PlayerValue, Dictionary<PieceValue, int>> OffStoreLookup { get { return _offstores; } }
    // lookup on positions occupied
    internal Dictionary<PositionValue, PieceModel> PlayedPieceLookup { get { return _playedpieces; } }
    // generated drops and moves for player if no result yet
    internal List<MoveModel> LegalMoves { get; private set; }
    // the previous board
    internal BoardModel LastBoard { get; private set; }
    // the move that led to this board (shows previous player)
    internal MoveModel LastMove { get; private set; }
    // the turn for the previous move
    internal TurnDef LastTurn { get; private set; }

    // current player for friend/enemy and for result
    // volatile, but will match turn player if no result and available moves
    internal PlayerValue CurrentPlayer { get; private set; }
    // game result for this board and result player, or none
    internal ResultKinds Result { get { return _result; } }
    // result player, if result
    internal PlayerValue ResultPlayer { get { return _resultplayer; } }

    // current turn in turn order (shows next player)
    internal TurnDef Turn { get { return _gamedef.GetTurn(_turnindex); } }

    // convenience functions
    internal bool CanMove { get { return LegalMoves.Count != 0; } }
    internal bool HasResult { get { return Result != ResultKinds.None; } }
    internal PlayerValue LastTurnPlayer { get { return LastTurn == null ? PlayerValue.None : LastTurn.TurnPlayer; } }
    internal PlayerValue LastMovePlayer { get { return LastTurn == null ? PlayerValue.None : LastTurn.MovePlayer; } }
    internal PlayerValue TurnPlayer { get { return Turn.TurnPlayer; } }
    internal PlayerValue MovePlayer { get { return Turn.MovePlayer; } }

    // set result, and remember current player if win or loss
    void SetMoveResult(ResultKinds result) {
      if (result == ResultKinds.Count) {
        // count means that player with highest count wins
        var ap = _gamedef.ActivePlayers.ToArray();
        if (ap.Length != 2) throw Error.Assert("player count");
        var counts = ap.Select(p => PiecesCount(p)).ToArray();
        _result = (counts[0] == counts[1]) ? ResultKinds.Draw : ResultKinds.Win;
        _resultplayer = (_result == ResultKinds.Draw) ? PlayerValue.None : (counts[0] > counts[1]) ? ap[0] : ap[1];
      } else {
        _result = result;
        _resultplayer = (result == ResultKinds.None || result == ResultKinds.Draw) ? PlayerValue.None : CurrentPlayer;
      }
      // credit non-active result to previous active player; or else to current
      if (_resultplayer == LastMovePlayer) _resultplayer = LastTurnPlayer;
      else if (_resultplayer == MovePlayer) _resultplayer = TurnPlayer;
    }

    // Result as seen by player of last move
    internal ResultKinds MoveResult {
      get {
        return (ResultPlayer == LastTurnPlayer || ResultPlayer == LastMovePlayer) ? Result
          : Result == ResultKinds.Win ? ResultKinds.Loss
          : Result == ResultKinds.Loss ? ResultKinds.Win
          : Result;
      }
    }
    // winner if any of this board
    internal PlayerValue Winner {
      get {
        return Result == ResultKinds.None || Result == ResultKinds.Draw ? PlayerValue.None
          : MoveResult == ResultKinds.Win ? LastTurnPlayer : TurnPlayer;
      }
    }

    //--- locals
    GameModel _game;
    GameDef _gamedef { get { return _game.Def; } }
    int _turnindex = 0;
    Dictionary<PositionValue, PieceModel> _playedpieces = new Dictionary<PositionValue, PieceModel>();
    Dictionary<Pair<PlayerValue,PieceValue>, int> _offstores = new Dictionary<Pair<PlayerValue, PieceValue>, int>();
    // pieces captured on last move
    HashSet<PieceValue> _captured = new HashSet<PieceValue>();
    // move generation done
    bool _donemovegen = false;

    internal int NextRandom(int range) {
      return _game.Rng.Next(range);
    }

    ResultKinds _result = ResultKinds.None;
    PlayerValue _resultplayer = PlayerValue.None;

    public override string ToString() {
      return String.Format("Board<{0},{1},{2}:({3})>", _game.MoveNumber, CurrentPlayer, Result, 
        PlayedPieceLookup.Select(kv=>String.Format("{0}:{1}:{2}", kv.Key, kv.Value.Player, kv.Value.Piece)).Join());
    }

    // check friend/enemy based on CurrentPlayer
    internal PlayerKinds GetPlayerKind(PlayerValue player) {
      if (player == CurrentPlayer) return PlayerKinds.Friend;
      if (_gamedef.PlayerLookup[player].IsNeutral) return PlayerKinds.Neutral;
      return PlayerKinds.Enemy;
    }

    static BoardModel Create(GameModel game, BoardDef def) {
      return new BoardModel {
        _game = game,
        Def = def,
      };
    }

    // create new initial board
    internal static BoardModel CreateSetup(GameModel game, BoardDef def) {
      return new BoardModel {
        _game = game,
        Def = def,
      }.SetupPieces().GenerateNewState();
    }

    // make the chosen move, update board, game result and moves
    internal BoardModel MakeMove(int index) {
      if (!(index >= 0 && index < LegalMoves.Count())) throw Error.Argument("index out of range");
      return CreateMoved(this, LegalMoves[index]);
    }

    // create new board by applying move to old one
    static BoardModel CreateMoved(BoardModel board, MoveModel move) {
      return new BoardModel {
        _game = board._game,
        LastBoard = board,
        Def = board.Def,
        LastMove = move,
        LastTurn = board.Turn,
        _turnindex = board._turnindex + 1,
        _playedpieces = new Dictionary<PositionValue, PieceModel>(board._playedpieces),
        _offstores = new Dictionary<Pair<PlayerValue, PieceValue>, int>(board._offstores),
      }.ApplyMove(move).GenerateNewState();
    }

    private void CheckCapture(PositionValue position) {
      if (_playedpieces.ContainsKey(position))
        _captured.Add(_playedpieces[position].Piece);
    }

    //--- enquiry functions

    // true if valid static position
    internal bool IsValid(PositionValue position) {
      return position != null && Def.PositionLookup.ContainsKey(position);
    }

    // true if valid played position
    internal bool IsPlayed(PositionValue position) {
      return position != null && _playedpieces.ContainsKey(position);
    }

    internal bool AdjEnemy(PositionValue position) {
      return IsValid(position) 
        && Def.AdjacentIter(position, CurrentPlayer).Any(p => IsEnemy(p));
    }

    // true if no piece played here (also true if off board!?)
    internal bool IsEmpty(PositionValue position) {
      return !IsPlayed(position);
    }

    internal bool IsEnemy(PositionValue position) {
      return IsPlayed(position)
        && GetPlayerKind(_playedpieces[position].Player) == PlayerKinds.Enemy;
    }

    internal bool IsFriend(PositionValue position) {
      return IsPlayed(position)
        && GetPlayerKind(_playedpieces[position].Player) == PlayerKinds.Friend;
    }

    internal bool IsNeutral(PositionValue position) {
      return IsPlayed(position)
        && GetPlayerKind(_playedpieces[position].Player) == PlayerKinds.Neutral;
    }

    internal bool IsPiece(PieceValue piece, PositionValue position) {
      return IsPlayed(position)
        && _playedpieces[position].Piece == piece;
    }

    internal bool InZone(ZoneValue zone, PositionValue position) {
      return IsValid(position) 
        && Def.InZone(zone, CurrentPlayer, position);
    }

    // test for piece occupying position
    internal bool IsOccupied(PlayerKinds kind, PieceValue piece, PositionValue position) {
      return IsPiece(piece, position)
        && (kind == PlayerKinds.Friend ? IsFriend(position)
          : kind == PlayerKinds.Enemy ? IsEnemy(position)
          : true);
    }

    // true if any such piece in zone
    internal bool IsOccupied(PlayerKinds kind, PieceValue piece, ZoneValue zone) {
      if (zone == null) return PositionIter().Any(p => IsOccupied(kind, piece, p));
      return Def.PositionIter(zone, CurrentPlayer).Any(p => IsOccupied(kind, piece, p));
    }

    internal bool IsAttributeSet(IdentValue attribute, PositionValue position) {
      return IsPlayed(position) && _playedpieces[position].IsSet(attribute);
    }

    IEnumerable<PositionValue> PositionIter(PlayerValue player, ZoneValue zone) {
      return Def.ZoneLookup.SafeLookup(Pair.Create(zone, player));
    }

    internal IEnumerable<PositionValue> PositionIter() {
      return Def.PositionLookup.Keys;
    }

    // count pieces remaining for player and type
    internal int PiecesCount(PlayerKinds kind, PieceValue piece) {
      return _playedpieces.Where(p => piece == null || p.Value.Piece == piece)
        .Where(p => kind == PlayerKinds.Any
          || (kind == PlayerKinds.Friend && p.Value.Player == CurrentPlayer)
          || (kind == PlayerKinds.Enemy && p.Value.Player != CurrentPlayer))
        .Count();
    }

    // count pieces remaining for player 
    internal int PiecesCount(PlayerValue player, PieceValue piece = null) {
      return _playedpieces.Where(p => piece == null || p.Value.Piece == piece)
        .Where(p => p.Value.Player == player)
        .Count();
    }

    // TODO: is repetition !!??
    // only called in phase 2
    internal bool Repetition() {
      throw Error.NotImpl("repetition");
    }

    // stalemated means player to move has no moves
    // only called in phase 2
    internal bool Stalemated() {
      return LegalMoves.Count == 0;
    }

    // were any of these pieces captured?
    internal bool Captured(IList<PieceValue> pieces) {
      return pieces.Any(p => _captured.Contains(p));
    }

    // TODO: is checkmated !!??
    internal bool Checkmated(IList<PieceValue> pieces) {
      throw Error.NotImpl("checkmated");
    }

    //--- impl

    BoardModel SetupPieces() {
      foreach (var setup in _game.Def.SetupItems) {
        if (setup.OffQuantity > 0)
          _offstores[Pair.Create(setup.Player, setup.Piece)] = setup.OffQuantity;
        foreach (var position in setup.Positions) {
          var piece = _game.CreatePiece(setup.Player, setup.Piece);
          _playedpieces[position] = piece;
        }
      }
      return this;
    }

    // privately apply move to copy of board
    BoardModel ApplyMove(MoveModel move) {
      foreach (var m in move.MoveParts) {
        switch (m.Kind) {
        case MoveKinds.Drop:
          CheckCapture(m.Position);
          _playedpieces[m.Position] = _game.CreatePiece(m.Player, m.Piece);
          break;
        case MoveKinds.Copy:
          CheckCapture(m.Final);
          var ppc = _playedpieces[m.Position];
          _playedpieces[m.Final] = (ppc.Piece == m.Piece) ? ppc : ppc.Create(m);
          break;
        case MoveKinds.Move:
          CheckCapture(m.Final);
          var ppm = _playedpieces[m.Position];
          _playedpieces[m.Final] = (ppm.Piece == m.Piece) ? ppm : ppm.Create(m);
          if (m.Final != m.Position) _playedpieces.Remove(m.Position);
          break;
        case MoveKinds.Take:
          CheckCapture(m.Position);
          _playedpieces.Remove(m.Position);
          break;
        case MoveKinds.Owner:
        case MoveKinds.Piece:
        case MoveKinds.Attrib:
          _playedpieces[m.Position] = _playedpieces[m.Position].Create(m);
          break;
        default:
          throw Error.Assert("apply {0}", move);
        }
      }
      return this;
    }

    ResultKinds ResultReverse(ResultKinds result) {
      return result == ResultKinds.Win ? ResultKinds.Loss
        : result == ResultKinds.Loss ? ResultKinds.Win
        : result;
    }

    // Generate moves, game result and who plays next
    // If there is a result, it is for CurrentPlayer
    // Else CurrentPlayer same as TurnPlayer, but MovePlayer actually makes the move
    BoardModel GenerateNewState() {
      LegalMoves = new List<MoveModel>();
      // on setup always no result; otherwise check for all players that have a condition
      if (LastMove != null) {
        foreach (var goal in _game.Def.Goals) {
          CurrentPlayer = goal.Player;
          SetMoveResult(_game.CheckGameResult(goal, this, false));
          if (Result != ResultKinds.None) break;
        }
        CurrentPlayer = TurnPlayer;
        GenerateMoves();
        if (Result == ResultKinds.None) {
          foreach (var goal in _game.Def.Goals.Where(g => g.Player == TurnPlayer)) {
            SetMoveResult(_game.CheckGameResult(goal, this, true));
            if (Result != ResultKinds.None) break;
          }
        }
      }
      // if no result generate moves
      if (Result == ResultKinds.None) {
        CurrentPlayer = Turn.TurnPlayer;
        GenerateMoves();
        // default if cannot move (which means no pass either)
        if (!CanMove) SetMoveResult(ResultKinds.Draw);
      } else LegalMoves.Clear();
      return this;
    }

    internal PlayerValue NextOwner(PositionValue pos) {
      var player = PlayedPieceLookup[pos].Player;
      var found = false;
      foreach (var p in _gamedef.ActivePlayers) {
        if (found) return p;
        found = (p == player);
      }
      return _gamedef.ActivePlayers.First();
    }

    // generate actual moves if not done already during condition checking
    // assumes CurrentPlayer set already
    internal void GenerateMoves() {
      if (!_donemovegen) {
        LegalMoves.AddRange(_game.CreateDrops(this));
        LegalMoves.AddRange(_game.CreateMoves(this));
        var passoption = _game.Def.GetProperty("pass turn") ?? OptionValue.False;
        var allowpass = passoption.Equals(OptionValue.True)
                     || (LegalMoves.Count == 0 && passoption.Equals(OptionValue.Forced));
        if (allowpass) 
          LegalMoves.Insert(0, MoveModel.Create(Turn.TurnPlayer, PositionValue.None, PieceValue.None));
        _donemovegen = true;
      }
    }

  }

  ///---------------------------------------------------------------------------
  /// <summary>
  /// Piece model is created from Def
  /// Initially has value, belongs to player, has flags but these can be updated by move
  /// </summary>
  internal class PieceModel {
    internal PieceDef Def { get; private set; }
    internal PieceValue Piece { get; private set; }
    internal PlayerValue Player { get; private set; }
    internal Dictionary<IdentValue, TypedValue> AttributeLookup { get; set; }
    internal long LongHash { get; private set; }

    public override string ToString() {
      return String.Format("piece[{0},{1}]", Player, Piece);
    }

    // factory: flags default to empty
    internal static PieceModel Create(PlayerValue player, PieceDef piecedef) {
      return new PieceModel {
        Def = piecedef,
        Player = player,
        Piece = piecedef.Piece,
        AttributeLookup = piecedef.AttributeLookup,
        LongHash = HashMaker.GetHash(piecedef.Piece) ^ HashMaker.GetHash(player),
      };
    }
    // move lookup allows creating new piece from old with changes
    Dictionary<MoveKinds, Action<PieceModel, MovePartModel>> _move_lookup = new Dictionary<MoveKinds, Action<PieceModel, MovePartModel>> {
      { MoveKinds.Copy, (p,m)=>p.Piece = m.Piece },   // add-copy piece promotion move
      { MoveKinds.Move, (p,m)=>p.Piece = m.Piece },   // add piece promotion move
      { MoveKinds.Piece, (p,m)=>p.Piece = m.Piece },  // change-type
      { MoveKinds.Owner, (p,m)=>p.Player = m.Player },// change-owner/flip
      { MoveKinds.Attrib, (p,m)=> {                   // can only change copy
          p.AttributeLookup = new Dictionary<IdentValue, TypedValue>(p.AttributeLookup);
          p.AttributeLookup[m.Attribute] = m.Value;
        } },
    };

    // Create piece from existing with changes
    internal PieceModel Create(MovePartModel movepart) {
      var ret = MemberwiseClone() as PieceModel;
      _move_lookup[movepart.Kind](ret, movepart);
      return ret;
    }

    internal bool IsSet(IdentValue attribute) {
      return AttributeLookup.SafeLookup(attribute) == BoolValue.True;
    }
  }

  ///---------------------------------------------------------------------------
  /// <summary>
  /// Defines an individual possible move of one or more partials
  /// </summary>
  internal class MoveModel {
    internal PlayerValue Player;
    internal PieceValue Piece;
    internal PositionValue Position;
    internal bool Partial;    // temporary
    internal List<MovePartModel> MoveParts = new List<MovePartModel>();

    internal bool IsPass { get { return MoveParts.Count == 0; } }

    public override string ToString() {
      return String.Format("({0},{1},{2})", Player, Position, Piece);
    }

    public string ToString(string arg) {
      return (arg == "M") ? (IsPass ? "Pass" : MoveParts[0].ToString("M"))
        : (arg == "P") ? (IsPass ? "Pass" : MoveParts.Select(m => m.ToString("M")).Join(", ")) 
        : ToString();
    }

    // factory: parts default to empty
    internal static MoveModel Create(PlayerValue player, PositionValue position, PieceValue piece) {
      return new MoveModel {
        Player = player, Position = position, Piece = piece,
      };
    }
  }

  ///---------------------------------------------------------------------------
  /// <summary>
  /// Defines an individual move part, which may be either
  /// 1. Drop To, Move From-To, or Capture From
  /// 2. Change owner, piece type or piece flag
  /// 
  /// </summary>
  internal class MovePartModel {
    internal MoveKinds Kind;
    internal PositionValue Position;  // original position, always set
    internal PositionValue Final;     // final position, for Move only
    internal PlayerValue Player;      // current or new owner, always set
    internal PieceValue Piece;        // current or new piece type, always set
    internal IdentValue Attribute;    // attribute name
    internal TypedValue Value;        // attribute value (if being set)

    public override string ToString() {
      return String.Format("MovePart[{0},{1},{2},{3},{4}]", Kind, Player, Position, Piece, Final);
    }

    Dictionary<MoveKinds, string> _kindlookup = new Dictionary<MoveKinds, string> {
      { MoveKinds.None,         "{0},{1}" },
      { MoveKinds.Drop,         "Drop {0} at {1}" },
      { MoveKinds.Move,         "Move {0} at {1}" },
      { MoveKinds.Copy,         "Copy {0} at {1}" },
      { MoveKinds.Take,         "Capture {0} at {1}" },
      { MoveKinds.Owner,        "Change owner of {0} at {1}" },
      { MoveKinds.Piece,        "Change piece at {1} to {0}" },
      { MoveKinds.Attrib,       "For piece {0} at {1}" },
    };

    public string ToString(string arg) {
      if (arg == "M") {
        var s = String.Format(_kindlookup[Kind], Piece, Position);
        return (Kind == MoveKinds.Move || Kind == MoveKinds.Copy) ? String.Format("{0} to {1}", s, Final)
          : (Kind == MoveKinds.Owner) ? String.Format("{0} to {1}", s, Player)
          : (Kind == MoveKinds.Attrib) ? String.Format("{0} set {1} to {2}", s, Attribute, Value)
          : s;
      }
      return ToString();
    }

    // Create any kind of move except set attribute
    internal static MovePartModel Create(MoveKinds kind, PlayerValue player, PositionValue position, PieceValue piece, 
      PositionValue final = null) {
      return new MovePartModel {
        Kind = kind, Player = player, Piece = piece, Position = position, Final = final,
      };
    }

    // Create set attribute move
    internal static MovePartModel CreateSetAttribute(PlayerValue player, PositionValue position, PieceValue piece, 
      IdentValue ident, TypedValue value) {
      var mpm = Create(MoveKinds.Attrib, player, position, piece);
      mpm.Attribute = ident;
      mpm.Value = value;
      return mpm;
    }

  }

  ///---------------------------------------------------------------------------
  /// <summary>
  /// State used by move generation steps to build a move and its parts
  /// </summary>
  internal class MoveGenState {
    internal PlayerValue TurnPlayer;  // player owning move (TurnDef.Player)
    internal PlayerValue Player;      // player doing move (TurnDef.AsPlayer)
    internal PieceValue Piece;
    internal MoveKinds Kind;
    internal PositionValue Position;
    internal PositionValue From;      // start position for move, can be updated
    internal PositionValue To;        // to position for move, if null use current
    internal PositionValue Current;   // temporary current during generation logic
    internal PositionValue Mark;      // temporary store during generation logic
    internal PositionValue LastFrom;  // previous From
    internal PositionValue LastTo;    // previous To
    internal List<MoveModel> MoveList = new List<MoveModel>();
    internal bool Partial { get { return _move.Partial; } set { _move.Partial = value; } }

    internal MoveModel _move;         // base move contains original starting values
    List<MovePartModel> _changes = new List<MovePartModel>();
    List<MovePartModel> _captures = new List<MovePartModel>();
    HashSet<Pair<PositionValue, IdentValue>> _position_flag_set = new HashSet<Pair<PositionValue, IdentValue>>();
    HashSet<IdentValue> _move_flag_set = new HashSet<IdentValue>();

    public override string ToString() {
      return String.Format("State[{0},{1},{2}->{3} @{4} #{5}]", Piece, Kind, From, To, Current, Mark);
    }

    internal static MoveGenState Create(MoveKinds kind, PlayerValue turnplayer, PlayerValue asplayer, PieceValue piece, PositionValue position) {
      return new MoveGenState {
        Kind = kind, Position = position,
        TurnPlayer = turnplayer, Player = asplayer, Piece = piece,
        //Kind = kind, TurnPlayer = turnplayer, Player = asplayer, Piece = piece,
        //Position = position, From = position, To = position,
        Current = position, Mark = position,
      }.Reset();
    }

    // reset after Add
    MoveGenState Reset() {
      LastFrom = From;
      LastTo = To ?? Current;
      From = Position;
      To = null;
      _move = MoveModel.Create(TurnPlayer, Position, Piece);
      _changes.Clear();
      _captures.Clear();
      return this;
    }

    internal void SetMoveFlag(IdentValue name, bool isset) {
      if (isset)
        _move_flag_set.Add(name);
      else _move_flag_set.Remove(name);
    }

    internal bool GetMoveFlag(IdentValue name) {
      return _move_flag_set.Contains(name);
    }

    internal void SetPositionFlag(IdentValue name, PositionValue position, bool isset) {
      if (isset)
        _position_flag_set.Add(Pair.Create(position,name));
      else _position_flag_set.Remove(Pair.Create(position, name));
    }

    internal bool GetPositionFlag(IdentValue name, PositionValue position) {
      return _position_flag_set.Contains(Pair.Create(position, name));
    }

    // Cascade adds the current move without a reset
    void Cascade() {
      _move.MoveParts.Add(CreateMovePart(Piece));
      From = Current;
    }

    internal MovePartModel CreateMovePart(PieceValue piece) {
      return (Kind == MoveKinds.Move || Kind == MoveKinds.Copy)
        ? MovePartModel.Create(Kind, Player, From, piece ?? Piece, To ?? Current)
        : MovePartModel.Create(Kind, Player, To ?? Current, piece ?? Piece, null);
    }

    internal void AddPieceMoves(IEnumerable<PieceValue> pieces) {
      if (pieces == null) AddPieceMove(Piece);
      else foreach (var p in pieces)
          AddPieceMove(p);
    }

    void AddPieceMove(PieceValue piece) {
      Logger.WriteLine(4, "Add moves piece:{0} position:{1} from:{2} changes:{3} captures:{4}",
        piece, To ?? Current, From, _changes.Count, _captures.Count);
      AddMovePart(CreateMovePart(piece));
      foreach (var movepart in _changes) AddMovePart(movepart);
      foreach (var movepart in _captures) AddMovePart(movepart);
      MoveList.Add(_move);
      Reset();
    }

    // add move part but omit those that do nothing
    void AddMovePart(MovePartModel movepart) {
      if (!IsNoop(movepart))
        _move.MoveParts.Add(movepart);
    }

    // move part that does nothing
    bool IsNoop(MovePartModel mp) {
      if (mp.Kind == MoveKinds.Move || mp.Kind == MoveKinds.Copy)
        return mp.Position == mp.Final && mp.Piece == _move.Piece;
      if (Kind == MoveKinds.Owner) return mp.Player == _move.Player;
      if (Kind == MoveKinds.Piece) return mp.Piece == _move.Piece;
      return false;
    }

    internal void AddChangePiece(PositionValue position, PieceValue piece) {
      _changes.Add(MovePartModel.Create(MoveKinds.Piece, Player, position, piece));
    }

    internal void AddChangeOwner(PositionValue position, PlayerValue player) {
      _changes.Add(MovePartModel.Create(MoveKinds.Owner, player, position, Piece));
    }

    internal void AddCapture(PositionValue position) {
      _changes.Add(MovePartModel.Create(MoveKinds.Take, Player, position, Piece));
    }

    internal void AddDrop(PlayerValue player, PositionValue position, PieceValue piece) {
      _changes.Add(MovePartModel.Create(MoveKinds.Drop, player, position, piece));
    }

    internal void AddSetAttribute(IdentValue ident, BoolValue value) {
      _changes.Add(MovePartModel.CreateSetAttribute(Player, Position, Piece, ident, value));
    }
  }
}
