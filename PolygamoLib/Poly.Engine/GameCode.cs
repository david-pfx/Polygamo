﻿/// Polygamo is a general player for abstract games and puzzles. See http://www.polyomino.com/polygamo.
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
/// Builtin functions
/// 
using System;
using System.Collections.Generic;
using System.Linq;
using Poly.Common;
using Polygamo;

/// <summary>
/// These are the built in functions that execute during the game.
/// 
/// The GameCode classes:
/// 1. During execution, create and populate GameDef instances.
/// 2. On request, act as factory classes to create and link GameModel instances.
/// 
/// GameDef classes hold static data in a neutral format
/// GameModel classes hold game state, with mutable data and access to GameDef.
/// </summary>
namespace Poly.Engine {

  ///---------------------------------------------------------------------------
  /// <summary>
  /// Abstract base class for all built-ins
  /// Created with code and an evaluator
  /// </summary>
  internal abstract class CodeBase : Builtin {
    // evaluator is used to launch new executable
    internal Evaluator Evaluator { get; set; }
    // code stream
    internal IList<object> Code { get; set; }
    // storage for counter during method call
    internal int Counter { get; set; }

    // break execution on next method return
    internal void Break() {
      Counter = Code.Count;
    }

    // execute goto on method return -- NOT USED
    internal void GoTo(int counter) {
      Counter = counter;
    }

    public override string ToString() {
      return String.Format("code[{0}] {1}", Code.Count, this.GetType());
    }

    // begin execution
    protected object EvalExec() {
      return Evaluator.Exec(this);
    }
  }

  ///---------------------------------------------------------------------------
  /// <summary>
  /// Dummy code as produced by compiler, turned into a code object for execution
  /// </summary>
  internal class StartCode : CodeBase {
    internal static StartCode Create(Evaluator evaluator, IList<object> code) {
      return new StartCode { Evaluator = evaluator, Code = code };
    }

    internal MenuDef CreateMenu() {
      var menucode = Evaluator.Exec(Code) as MenuCode;
      return menucode.Exec();
    }
  }

  ///---------------------------------------------------------------------------
  /// <summary>
  /// Execute this to create a list of games
  /// </summary>
  internal class MenuCode : CodeBase {
    List<GameCode> _gamecodes = new List<GameCode>();

    internal MenuDef Exec() {
      EvalExec();
      var gamedefs = new List<GameDef>();
      var stripdividers = true; // TODO: make this an option
      foreach (var gamecode in _gamecodes) {
        var def = gamecode.Exec(gamedefs.Count > 0);
        if (!(stripdividers && def.GetStringProperty("title") == "-"))
          gamedefs.Add(def);
        //gamedefs.Add(gamecode.Exec(gamedefs.Count > 0));
      }
      return MenuDef.Create(gamedefs);
    }

    //--- sexpr api
    void s_Game(GameCode game) {
      _gamecodes.Insert(0, game);
    }
    void s_Variant(GameCode game) {
      _gamecodes.Add(game);
    }
  }

  internal struct GoalCodeItem {
    internal ResultKinds Result;
    internal PlayerValue Player;
    internal GoalCode Goal;
  }

  ///---------------------------------------------------------------------------
  /// <summary>
  /// Executable for game object, returns a game def
  /// Factories for board, piece and moves
  /// </summary>
  internal class GameCode : CodeBase {
    internal GameDef Exec(bool isvariant) {
      _gamedef = GameDef.Create(this, isvariant);
      EvalExec();
      return _gamedef;
    }

    GameDef _gamedef;
    BoardCode _boardcode; // created by Opcodes.NEW
    Dictionary<PieceValue, PieceCode> _piececodes = new Dictionary<PieceValue, PieceCode>();

    //--- factories

    // Create a new empty board
    internal BoardDef CreateBoard(GameModel game) {
      if (_boardcode == null) throw Error.Fatal("board does not exist");
      return _boardcode.Exec();
    }

    // check goal conditions for current player to see if game has ended and how
    internal ResultKinds CheckCondition(GoalDef goal, BoardModel board, bool phase2) {
      return goal.Code.Exec(board, phase2) ? goal.Kind : ResultKinds.None;
    }

    // Generate drops for current player for all pieces
    // use all pieces that have moves defined (check off store later) and all positions on the board
    internal IList<MoveModel> CreateDrops(BoardModel board) {
      return _piececodes.SelectMany(p => board.Def.PositionLookup.Keys
                        .SelectMany(q => p.Value.CreateDrops(q, board, MoveTypeValue.Any))).ToList();
    }

    // Generate moves for all pieces on board for this player
    // use pieces only for this player, only if moves defined, and current position on the board
    internal IList<MoveModel> CreateMoves(BoardModel board) {
      var player = board.Turn.TurnPlayer;
      return board.PlayedPieceLookup
        .Where(p => p.Value.Player == player)
        .Where(p => _piececodes.ContainsKey(p.Value.Piece))
        .SelectMany(p => _piececodes[p.Value.Piece].CreateMoves(p.Key, board, MoveTypeValue.Any)).ToList();
    }

    void AddGoal(ResultKinds result, PlayerValue[] players, GoalCode condition) {
      foreach (var player in players)
        _gamedef.AddGoal(result, player, condition);
    }

    //--- sexpr api

    void s_Default() { _gamedef.IsDefault = true; }
    void s_Players(List<PlayerValue> players) {
      _gamedef.AddPlayers(players);
    }
    void s_Option(TextValue option, TypedValue value) { _gamedef.SetProperty(option.Value, value); }

    void s_Description(TextValue text) { _gamedef.SetProperty("description", text); }
    void s_History(TextValue text) { _gamedef.SetProperty("history", text); }
    void s_Strategy(TextValue text) { _gamedef.SetProperty("strategy", text); }
    void s_Thumbnail(TextValue text) { _gamedef.SetProperty("thumbnail", text); } // image for menu
    void s_Title(TextValue text) { _gamedef.SetProperty("title", text); }

    void s_CaptureSound(TextValue path) { _gamedef.SetProperty("capture", path); }
    void s_ChangeSound(TextValue path) { _gamedef.SetProperty("change", path); }
    void s_ClickSound(TextValue path) { _gamedef.SetProperty("click", path); }
    void s_DrawSound(TextValue path) { _gamedef.SetProperty("draw", path); }
    void s_DropSound(TextValue path) { _gamedef.SetProperty("drop", path); }
    void s_LossSound(TextValue path) { _gamedef.SetProperty("loss", path); }
    void s_MoveSound(TextValue path) { _gamedef.SetProperty("move", path); }
    void s_OpeningSound(TextValue path) { _gamedef.SetProperty("opening", path); }
    void s_ReleaseSound(TextValue path) { _gamedef.SetProperty("release", path); }
    void s_WinSound(TextValue path) { _gamedef.SetProperty("win", path); }
    void s_Music(TextValue path) { _gamedef.SetProperty("music", path); }
    void s_Solution(TextValue path) { _gamedef.SetProperty("solution", path); }

    void s_MovePriorities(List<MoveTypeValue> movetypes) { _gamedef.MovePriorities = movetypes; }
    void s_TurnOrder(List<TurnDef> turndefs) { _gamedef.SetTurnOrder(turndefs); }

    void s_DrawCondition(PlayerValue[] players, GoalCode condition) {
      AddGoal(ResultKinds.Draw, players, condition);
    }
    void s_LossCondition(PlayerValue[] players, GoalCode condition) {
      AddGoal(ResultKinds.Loss, players, condition);
    }
    void s_WinCondition(PlayerValue[] players, GoalCode condition) {
      AddGoal(ResultKinds.Win, players, condition);
    }
    void s_CountCondition(Maybe<PlayerValue[]> players, GoalCode condition) {
      var pp = (players.IsNull ? new PlayerValue[] { PlayerValue.None } : players.Value);
      AddGoal(ResultKinds.Count, pp, condition);
    }

    void s_Board(BoardCode board) { _boardcode = board; }
    void s_BoardSetup(List<Pair<PlayerValue, List<PlacementDef>>> setups) {
      foreach (var setup in setups) {
        foreach (var item in setup.Item2) {
          _gamedef.AddSetup(setup.Item1, item.Piece, item.OffQuantity, item.Positions);
        }
      }
    }
    void s_Piece(PieceCode piece) {
      var piecedef = piece.Exec();
      if (piecedef.Piece == null) throw Error.Evaluation("piece has no name");
      _piececodes[piecedef.Piece] = piece;
      _gamedef.AddPiece(piecedef);
    }

    // deprecated TODO:
    void s_AllowFlipping(TypedValue value) { }
    void s_AnimateCaptures(TypedValue value) { }
    void s_AnimateDrops(TypedValue value) { }
    void s_IncludeOffPieces(TypedValue value) { }
    void s_MaximalCaptures(TypedValue value) { }
    void s_PassPartial(TypedValue value) { }
    void s_PassTurn(TypedValue value) { }
    void s_RecycleCaptures(TypedValue value) { }
    void s_RecyclePromotions(TypedValue value) { }
  }

  ///---------------------------------------------------------------------------
  /// <summary>
  /// Executable for goal object, determine whether game ended and how
  /// Phase 1 is before move generation, phase 2 is after
  /// </summary>
  internal class GoalCode : CodeBase {
    BoardModel _board;
    bool _phase1;
    bool _phase2;

    internal bool Exec(BoardModel board, bool phase2) {
      _board = board;
      _phase1 = !phase2;
      _phase2 = phase2;
      return (EvalExec() as BoolValue).Value;
    }

    // return true if all positions/zones are satisfied by this occupier
    bool IsAbsolute(OccupierDef occupier, PositionOrZone[] posorzones) {
      return posorzones.All(pz => {
        var occ = (pz.IsPosition)
          ? _board.IsOccupied(occupier.PlayerKind, occupier.Piece, pz.Position)
          : _board.IsOccupied(occupier.PlayerKind, occupier.Piece, pz.Zone);
        return occupier.Not ? !occ : occ;
      });
    }

    // Return true if list of occupiers satisfied for this position
    private bool IsRelative(PositionValue position, List<OccupierDef> occupiers) {
      var curpos = position;
      foreach (var def in occupiers) {
        if (def.IsDirection) {
          curpos = _board.Def.GetPosition(curpos, def.Direction);
          if (curpos == null) return false; // off board
        } else if (!_board.IsOccupied(def.PlayerKind, def.Piece, curpos))
          return false; // piece mismatch
      }
      return true;
    }

    // sexpr api for goal evaluators
    // note: and/or/not in outer scope

    BoolValue s_AbsoluteConfig(List<OccupierDef> occupiers, PositionOrZone[] posorzones) {
      return BoolValue.Create(_phase1 && occupiers.Any(o => IsAbsolute(o, posorzones)));
    }
    BoolValue s_RelativeConfig(List<OccupierDef> occupiers) {
      return BoolValue.Create(_phase1 && _board.PositionIter().Any(p => IsRelative(p, occupiers)));
    }
    BoolValue s_PiecesRemaining(NumberValue quantity, PieceValue piece = null) {
      return BoolValue.Create(_phase1 && _board.PiecesCount(PlayerKinds.Friend, piece) == (int)quantity.Value);
    }
    BoolValue s_TotalPieceCount(NumberValue quantity, PieceValue piece = null) {
      return BoolValue.Create(_phase1 && _board.PiecesCount(PlayerKinds.Any, piece) == (int)quantity.Value);
    }
    // specials for board testing
    BoolValue s_Captured(PieceValue[] pieces) { return BoolValue.Create(_phase2 && _board.Captured(pieces)); }
    BoolValue s_Checkmated(PieceValue[] pieces) { return BoolValue.Create(_phase2 && _board.Checkmated(pieces)); }
    BoolValue s_Repetition() { return BoolValue.Create(_phase2 && _board.Repetition()); }
    TypedValue s_Stalemated() { return BoolValue.Create(_phase2 && _board.Stalemated()); }
  }

  ///---------------------------------------------------------------------------
  /// <summary>
  /// 
  /// </summary>
  internal class OccupierCode : CodeBase {
    OccupierDef _def = new OccupierDef();

    void s__Value(PieceValue piece) {
      _def.Piece = piece;
    }
    void s_Opponent(PieceValue piece) {
      _def.PlayerKind = PlayerKinds.Enemy;
      _def.Piece = piece;
    }
    void s_AnyOwner(PieceValue piece) {
      _def.PlayerKind = PlayerKinds.Any;
      _def.Piece = piece;
    }
    void s_Not(OccupierCode occupied) { _def.Not = !_def.Not; }
  }

  ///---------------------------------------------------------------------------
  /// <summary>
  /// Executable for board object, positions and links
  /// </summary>
  internal class BoardCode : CodeBase {
    BoardDef _boarddef;
    internal BoardDef Exec() {
      _boarddef = BoardDef.Create(this);
      EvalExec();
      return _boarddef;
    }

    //--- sexpr api and data

    void s_Dummy(List<PositionValue> positions) {
      foreach (var pos in positions) {
        _boarddef.SetPosition(pos, Rect.Empty);
      }
    }
    void s_Grid(GridCode grid) {
      grid.Exec(_boarddef);
    }
    void s_Image(List<TextValue> paths) {
      _boarddef.Images = paths;
    }
    void s_KillPositions(List<PositionValue> positions) {
      foreach (var pos in positions)
        _boarddef.RemovePosition(pos);
    }
    void s_Links(DirectionValue direction, List<Pair<PositionValue, PositionValue>> positionpairs) {
      foreach (var pospair in positionpairs)
        _boarddef.AddLink(direction, pospair.Item1, pospair.Item2);
    }
    void s_Positions(List<Pair<PositionValue, Rect>> positionsdefs) {
      foreach (var posdef in positionsdefs)
        _boarddef.SetPosition(posdef.Item1, posdef.Item2);
    }
    void s_Symmetry(PlayerValue player, List<Pair<DirectionValue, DirectionValue>> directionpairs) { }
    void s_Unlink(List<TypedValue[]> unlinks) { // TODO: parse
      foreach (var ulk in unlinks) {
        if (!(ulk.Length >= 1 && ulk[0] is PositionValue)) throw Error.Evaluation("bad unlink argument: {0}", ulk.Join());
        var pos = ulk[0] as PositionValue;
        if (ulk.Length == 1)
          _boarddef.RemoveLink(pos);
        else if (ulk.Length == 2 && (ulk[1] is PositionValue || ulk[1] is DirectionValue)) {
          if (ulk[1] is PositionValue)
            _boarddef.RemoveLink(pos, ulk[1] as PositionValue);
          else _boarddef.RemoveLink(pos, ulk[1] as DirectionValue);
        } else throw Error.Evaluation("bad unlink argument: {0}", ulk.Join());
      }
    }
    void s_Zone(ZoneCode zone) {
      var zonedef = zone.Exec();
      _boarddef.AddZone(zonedef);
    }
  }

  ///---------------------------------------------------------------------------
  /// <summary>
  /// Executable for grid object, shorthands in defining board layout
  /// </summary>
  internal class GridCode : CodeBase {
    Rect _startrect;
    List<IList<string>> _names = new List<IList<string>>();
    List<Coord> _coords = new List<Coord>();
    List<DirectionValue> _directions = new List<DirectionValue>();
    List<IList<int>> _diroffs = new List<IList<int>>();

    internal void Exec(BoardDef board) {
      // TODO: do we need to init data items?
      EvalExec();
      board.Dimensions = _names.Count;
      AddPositions("", _startrect, new List<int>(), 0, board);
      AddLinks(board);
    }

    // Recursive walk to add links
    void AddLinks(BoardDef board) {
      for (int i = 0; i < _directions.Count; i++) {
        if (_diroffs[i].Count != _names.Count) throw Error.Evaluation("grid: direction size mismatch");
        AddLinks(i, "", "", 0, board);
      }
    }

    void AddLinks(int dirindex, string fromname, string toname, int level, BoardDef board) {
      for (int i = 0; i < _names[level].Count; i++) {
        var j = i + _diroffs[dirindex][level];
        if (j >= 0 && j < _names[level].Count) {
          var fname = fromname + _names[level][i];
          var tname = toname + _names[level][j];
          if (level == _names.Count - 1) {
            board.AddLink(_directions[dirindex], board.GetPosition(fname), board.GetPosition(tname));
          } else
            AddLinks(dirindex, fname, tname, level + 1, board);
        }
      }
    }

    // Recursive walk to add positions
    void AddPositions(string basename, Rect baserect, List<int> basecoords, int level, BoardDef board) {
      for (int i = 0; i < _names[level].Count; i++) {
        var name = basename + _names[level][i];
        var rect = baserect.Offset(_coords[level].Times(i));
        var coords = new List<int>(basecoords);
        coords.Add(i);
        //var coords_ = coords.AsEnumerable().Concat(new int[] { i }).ToList();
        if (level == _names.Count - 1) {
          board.SetPosition(board.GetPosition(name), rect, coords);
        } else
          AddPositions(name, rect, coords, level + 1, board);
      }
    }

    //--- sexpr api 

    void s_Dimensions(DimPosDef dummy, List<Pair<TextValue, Coord>> dimargs) {
      foreach (var dimarg in dimargs) {
        _names.Add(dimarg.Item1.Value.Split('/'));
        _coords.Add(dimarg.Item2);
      }
    }
    void s_Directions(List<Pair<DirectionValue, List<NumberValue>>> dirargs) {
      foreach (var dirarg in dirargs) {
        _directions.Add(dirarg.Item1);
        _diroffs.Add(dirarg.Item2.Select(i => (int)i.Value).ToList());
      }
    }
    void s_StartRectangle(NumberValue v1, NumberValue v2, NumberValue v3, NumberValue v4) {
      _startrect = Rect.Create((int)v1.Value, (int)v2.Value, (int)v3.Value, (int)v4.Value);
    }
  }

  ///---------------------------------------------------------------------------
  /// <summary>
  /// Executable for zone, a named player-dependent collection of positions
  /// </summary>
  internal class ZoneCode : CodeBase {
    ZoneDef _zone;
    internal ZoneDef Exec() {
      _zone = new ZoneDef();
      EvalExec();
      return _zone;
    }
    void s_Name(ZoneValue name) { _zone.Name = name; }
    void s_Players(List<PlayerValue> players) { _zone.Players = new HashSet<PlayerValue>(players); }
    void s_Positions(List<PositionValue> positions) { _zone.Positions = new HashSet<PositionValue>(positions); }
  }

 ///---------------------------------------------------------------------------
  /// <summary>
  /// Executable for piece object, and factories for move and drop objects
  /// </summary>
  internal class PieceCode : CodeBase {
    PieceDef _piecedef;
    List<MoveCode> _dropcodes = new List<MoveCode>();
    List<MoveCode> _movecodes = new List<MoveCode>();

    internal PieceDef Exec() {
      _piecedef = new PieceDef();
      EvalExec();
      return _piecedef;
    }

    //--- factories

    // create drops for this piece and a position on this board
    internal IList<MoveModel> CreateDrops(PositionValue position, BoardModel board, MoveTypeValue movetype) {
      Logger.WriteLine(5, "Create drops piece:{0} position:{1}", _piecedef.Piece, position);
      return _dropcodes.SelectMany(c => c.CreateMoves(MoveKinds.Drop, _piecedef.Piece, position, board, movetype)).ToList();
    }

    // create moves for this piece and position on this board
    internal IList<MoveModel> CreateMoves(PositionValue position, BoardModel board, MoveTypeValue movetype) {
      Logger.WriteLine(5, "Create moves piece:{0} position:{1}", _piecedef.Piece, position);
      return _movecodes.SelectMany(c => c.CreateMoves(MoveKinds.Move, _piecedef.Piece, position, board, movetype)).ToList();
    }

    //--- sexpr api and data

    void s_Attribute(AttributeValue name, BoolValue value) { _piecedef.AttributeLookup[name] = value; }
    void s_Description(TextValue text) { _piecedef.HelpLookup[HelpKinds.Description] = text; }
    void s_Dummy() { _piecedef.IsDummy = true; }
    void s_Help(TextValue text) { _piecedef.HelpLookup[HelpKinds.Help] = text; }
    void s_Image(List<PieceImages> imagedefs) {
      foreach (var def in imagedefs)
        _piecedef.AddImages(def.Player, def.Images);
    }
    void s_Name(PieceValue piece) { _piecedef.Piece = piece; }
    void s_Notation(TextValue text) { _piecedef.HelpLookup[HelpKinds.Notation] = text; }
    void s_Open(TextValue text) { _piecedef.HelpLookup[HelpKinds.Open] = text; }
    void s_Drops(List<MoveCode> moves) {
      _dropcodes = moves;
      foreach (var code in _dropcodes)
        code.Exec();
    }
    void s_Moves(List<MoveCode> moves) {
      _movecodes = moves;
      foreach (var code in _movecodes)
        code.Exec();
    }
  }

  ///---------------------------------------------------------------------------
  /// <summary>
  /// Static executable for a series of move steps of a given move type
  /// Nodes are treated in order, move type applies to following nodes.
  /// Builds a table of movegens, indexed by move type.
  /// A movegen is a sequence of steps containing one or more adds, subject to early termination.
  /// </summary>
  internal class MoveCode : CodeBase {
    Maybe<PositionOrZone> _posorzone;
    MoveTypeValue _movetype = MoveTypeValue.Any;
    Dictionary<MoveTypeValue, List<MoveGenCode>> _movegens = new Dictionary<MoveTypeValue, List<MoveGenCode>>();

    // exec code on sexpr api to set up movegens
    internal void Exec() {
      EvalExec();
    }

    // create all drops and moves for piece, position and board
    // checks position/zone restriction
    internal IEnumerable<MoveModel> CreateMoves(MoveKinds kind, PieceValue piece, PositionValue position, BoardModel board, MoveTypeValue movetype) {
      if (!_posorzone.IsNull) {
        var posorzone = _posorzone.Value as PositionOrZone;
        if (posorzone.IsPosition) {
          if (position != posorzone.Position) yield break;
        } else if (!board.InZone(posorzone.Zone, position)) yield break;
      }
      // TODO: after each Exec if the partial flag is set, add any possible further moves 
      // (perhaps of a specific move type)
      var gencodes = (movetype == MoveTypeValue.Any) 
        ? _movegens.Values.SelectMany(m => m) 
        : _movegens[movetype].AsEnumerable();
      // moves generated for AsPlayer, although result will be owned by TurnPlayer
      foreach (var movegen in gencodes)
        foreach (var move in movegen.Exec(board, kind, board.Turn.TurnPlayer, board.Turn.MovePlayer, piece, position))
          yield return move;
    }

    //--- sexpr api

    void s_MoveType(MoveTypeValue movetype) { _movetype = movetype; }
    void s__List(Maybe<PositionOrZone> posorzone, List<MoveGenCode> steplist) {
      _posorzone = posorzone;
      foreach (var step in steplist) {
        _movegens.AddMulti(_movetype, step);
      }
    }
  }

  ///---------------------------------------------------------------------------
  /// <summary>
  /// Dynamic executable for move generation steps
  /// Should always be executed with a new State object
  /// </summary>
  internal class MoveGenCode : CodeBase {
    MoveGenState _state;
    BoardModel _board;

    // main entry point for generating moves for player, piece and position
    // note that a gencode block can contain multiple sections and generate multiple moves for the position
    // CHECK: the state block persists and must be reinitialised 
    internal IList<MoveModel> Exec(BoardModel board, MoveKinds kind, PlayerValue turnplayer, PlayerValue asplayer, PieceValue piece, PositionValue position) {
      _board = board;
      _state = MoveGenState.Create(kind, turnplayer, asplayer, piece, position);
      EvalExec();
      return _state.MoveList;
    }

    // translate null, position or direction into position
    // return null if off board
    PositionValue ToPosition(PositionOrDirection posordir) {
      var pos = (posordir == null) ? _state.Current
              : (posordir.IsPosition) ? posordir.Position
              : _board.Def.GetPosition(_state.Current, posordir.Direction);
      return pos;
    }

    // translate but break if invalid and return safe
    PositionValue ToPositionSafe(PositionOrDirection posordir) {
      return ToPosition(posordir) ?? BreakSafe();
    }

    PositionValue BreakSafe() {
      Break();
      return _state.Current;
    }

    //--- sexpr api and data

    // state change
    void s__Value(PositionOrDirection posordir) {
      _state.Current = ToPositionSafe(posordir);
    }
    void s_Back() { _state.Current = _state.Mark; }
    void s_To() { _state.To = _state.Current; }
    void s_From() { _state.From = _state.Current; }

    static Dictionary<GoKinds, Func<MoveGenCode, PositionValue>> _golookup = new Dictionary<GoKinds, Func<MoveGenCode, PositionValue>>() {
      { GoKinds.From, m=>m._state.From },
      { GoKinds.To, m=>m._state.To },
      { GoKinds.Mark, m=>m._state.Mark },
      { GoKinds.LastFrom, m=>m._state.LastFrom },
      { GoKinds.LastTo, m=>m._state.LastTo },
    };
    void s_Go(GoKinds gokind) {
      _state.Current = _golookup[gokind](this);
    }
    void s_Mark(PositionOrDirection posordir = null) {
      _state.Mark = ToPositionSafe(posordir);
    }
    void s_Opposite(DirectionValue direction) {
      _state.Current = _board.Def.GetOpposite(_state.Current, direction) ?? BreakSafe();
    }

    // setters
    void s_SetAttribute(AttributeValue ident, BoolValue value) {
      _state.AddSetAttribute(ident, value);
    }
    void s_SetPositionFlag(IdentValue ident, BoolValue value, PositionOrDirection posordir = null) {
      _state.SetPositionFlag(ident, ToPositionSafe(posordir), value.Value);
    }
    void s_SetFlag(IdentValue name, BoolValue value) {
      _state.SetMoveFlag(name, value.Value);
    }

    // move output
    void s_Add(List<PieceValue> pieces = null) {
      _state.AddPieceMoves(pieces);
    }
    void s_AddCopy(List<PieceValue> pieces = null) {
      _state.Kind = MoveKinds.Copy;
      _state.AddPieceMoves(pieces);
    }
    void s_AddPartial(List<TypedValue> pieceormovetypes = null) { // TODO: parse
      var pieces = (pieceormovetypes == null) ? null 
        : pieceormovetypes.Where(p => p is PieceValue)
        .Cast<PieceValue>();
      _state.AddPieceMoves(pieces);
      _state.Partial = true;
    }
    void s_AddCopyPartial(List<TypedValue> pieceormovetypes = null) {// TODO: parse
      _state.Kind = MoveKinds.Copy;
      var pieces = (pieceormovetypes == null) ? null
        : pieceormovetypes.Where(p => p is PieceValue)
        .Cast<PieceValue>();
      _state.AddPieceMoves(pieces);
      _state.Partial = true;
    }
    void s_Capture(PositionOrDirection posordir = null) {
      var pos = ToPositionSafe(posordir);
      if (_board.IsPlayed(pos))
        _state.AddCapture(pos);
      else Break();
    }
    void s_Cascade() {
    }
    void s_ChangeOwner(PositionOrDirection posordir = null) {
      var pos = ToPositionSafe(posordir);
      if (_board.IsPlayed(pos))
      _state.AddChangeOwner(pos, _state.Player);
      else Break();
    }
    void s_ChangeType(PieceValue piece, PositionOrDirection posordir = null) {
      var pos = ToPositionSafe(posordir);
      if (_board.IsPlayed(pos))
        _state.AddChangePiece(pos, piece);
      else Break();
    }
    void s_Create(Maybe<PlayerValue> player, Maybe<PieceValue> piece, PositionOrDirection posordir = null) {
      _state.AddDrop(player.IsNull ? _state.Player : player.Value, ToPositionSafe(posordir), piece.IsNull ? _state.Piece : piece.Value);
    }
    void s_Flip(PositionOrDirection posordir = null) {
      var pos = ToPositionSafe(posordir);
      if (_board.IsPlayed(pos))
        _state.AddChangeOwner(pos, _board.NextOwner(pos));
      else Break();
    }

    // control code
    // note: if, else, while, and, or use VM codes; not is in base scope
    void s_Verify(BoolValue test) {
      if (!test.Value) Break();
    }

    // conditions codes
    BoolValue s_Flag_(IdentValue flag) {
      return BoolValue.Create(_state.GetMoveFlag(flag));
    }
    BoolValue s_PositionFlag_(IdentValue flag, PositionOrDirection posordir = null) {
      return BoolValue.Create(_state.GetPositionFlag(flag, ToPosition(posordir)));
    }

    BoolValue s__Attribute(AttributeValue attribute, PositionOrDirection posordir = null) {
      return BoolValue.Create(_board.IsAttributeSet(attribute, ToPosition(posordir)));
    }
    //BoolValue s__Attribute() { return BoolValue.False; }

    // simple state enquiries
    BoolValue s_Marked_(PositionOrDirection posordir = null) { return BoolValue.Create(_state.Mark == ToPosition(posordir)); }
    BoolValue s_Position_(PositionValue position, PositionOrDirection posordir = null) { return BoolValue.Create(position == ToPosition(posordir)); }

    // simple board enquiries
    BoolValue s_AdjacentToEnemy_(PositionOrDirection posordir = null) { return BoolValue.Create(_board.AdjEnemy(ToPosition(posordir))); }
    BoolValue s_Empty_(PositionOrDirection posordir = null) { return BoolValue.Create(_board.IsEmpty(ToPosition(posordir))); }
    BoolValue s_Enemy_(PositionOrDirection posordir = null) { return BoolValue.Create(_board.IsEnemy(ToPosition(posordir))); }
    BoolValue s_Friend_(PositionOrDirection posordir = null) { return BoolValue.Create(_board.IsFriend(ToPosition(posordir))); }
    BoolValue s_Neutral_(PositionOrDirection posordir = null) { return BoolValue.Create(_board.IsNeutral(ToPosition(posordir))); }
    BoolValue s_OnBoard_(PositionOrDirection posordir = null) { return BoolValue.Create(_board.IsValid(ToPosition(posordir))); }
    BoolValue s_Piece_(PieceValue piece, PositionOrDirection posordir = null) { return BoolValue.Create(_board.IsPiece(piece, ToPosition(posordir))); }
    BoolValue s_LastFrom_(PositionOrDirection posordir = null) { return BoolValue.Create(_state.LastFrom == ToPosition(posordir)); }
    BoolValue s_LastTo_(PositionOrDirection posordir = null) { return BoolValue.Create(_state.LastTo == ToPosition(posordir)); }
    BoolValue s_InZone_(ZoneValue zone, PositionOrDirection posordir = null) { return BoolValue.Create(_board.InZone(zone, ToPosition(posordir))); }

    BoolValue s_Attacked_(TypedValue movetype = null, PositionOrDirection posordir = null) { return BoolValue.False; } // TODO: attacked
    BoolValue s_Defended_(TypedValue movetype = null, PositionOrDirection posordir = null) { return BoolValue.False; } // TODO: defended
    BoolValue s_GoalPosition_(PositionOrDirection posordir = null) { return BoolValue.False; } // TODO: goal position

    BoolValue s_NotAdjacentToEnemy_(PositionOrDirection posordir = null)                        { return s_Not(s_AdjacentToEnemy_(posordir)); }
    BoolValue s_NotAttacked_(TypedValue movetype = null, PositionOrDirection posordir = null)   { return s_Not(s_Attacked_(movetype, posordir)); }
    BoolValue s_NotDefended_(TypedValue movetype = null, PositionOrDirection posordir = null)   { return s_Not(s_Defended_(movetype, posordir)); }
    BoolValue s_NotEmpty_(PositionOrDirection posordir = null)                                  { return s_Not(s_Empty_(posordir)); }
    BoolValue s_NotEnemy_(PositionOrDirection posordir = null)                                  { return s_Not(s_Enemy_(posordir)); }
    BoolValue s_NotFlag_(IdentValue flag)                                                       { return s_Not(s_Flag_(flag)); }
    BoolValue s_NotFriend_(PositionOrDirection posordir = null)                                 { return s_Not(s_Friend_(posordir)); }
    BoolValue s_NotGoalPosition_(PositionOrDirection posordir = null)                           { return s_Not(s_GoalPosition_(posordir)); }
    BoolValue s_NotInZone_(ZoneValue zone, PositionOrDirection posordir = null)                 { return s_Not(s_InZone_(zone, posordir)); }
    BoolValue s_NotLastFrom_(PositionOrDirection posordir = null)                               { return s_Not(s_LastFrom_(posordir)); }
    BoolValue s_NotLastTo_(PositionOrDirection posordir = null)                                 { return s_Not(s_LastTo_(posordir)); }
    BoolValue s_NotMarked_(PositionOrDirection posordir = null)                                 { return s_Not(s_Marked_(posordir)); }
    BoolValue s_NotNeutral_(PositionOrDirection posordir = null)                                { return s_Not(s_Neutral_(posordir)); }
    BoolValue s_NotOnBoard_(PositionOrDirection posordir = null)                                { return s_Not(s_OnBoard_(posordir)); }
    BoolValue s_NotPiece_(PieceValue piece, PositionOrDirection posordir = null)                { return s_Not(s_Piece_(piece, posordir)); }
    BoolValue s_NotPosition_(PositionValue position, PositionOrDirection posordir = null)       { return s_Not(s_Position_(position, posordir)); }
    BoolValue s_NotPositionFlag_(IdentValue flag, PositionOrDirection posordir = null)          { return s_Not(s_PositionFlag_(flag, posordir)); }
  }
}