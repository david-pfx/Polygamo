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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Polygamo;
using UnityEngine;

/// <summary>
/// The GameModel classes implement the view model for the game as a kind of facade.
/// The actual model for the game is held by PolyGame.
/// </summary>
namespace PolygamoUnity {

  internal enum NotifyKinds {
    None, Check, Execute,
  }

  internal enum ResultKinds {
    None, Win, Lose, Draw,
  }

  ///////////////////////////////////////////////////////////////////////////////////////////////////
  /// <summary>
  /// implements data model for Board
  /// </summary>
  public class BoardModel {
    internal IList<string> GameList { get { return _polygame.Menu; } }
    internal IList<string> ThumbnailList { get { return _polygame.Thumbnails; } }

    internal string ScriptName { get; private set; }
    internal string Title { get { return _polygame.Title; } }
    internal string Help { get { return _polygame.GetOption("description") ?? "No help available"; } }
    internal string DefaultPlayer { get { return _polygame.Players[0]; } }
    internal string Player { get { return HasResult ? _polygame.ResultPlayer : _polygame.TurnPlayer; } }
    internal IList<string> Images { get { return _polygame.BoardImages; } }
    internal IList<PolyPieceImage> PieceImages { get { return _polygame.PieceImages; } }

    internal Rect BoundingRect { get { return _bounding; } }
    internal int MoveCount { get; private set; }
    internal TimeSpan TimePlayed { get; private set; }
    internal bool IsThinking { get; set; }

    internal int TimePlayedSeconds { get { return (int)(TimePlayed.Ticks / 10000000); } }
    internal ResultKinds GameResult {
      get { return _polygame.GameResult == Polygamo.ResultKinds.None ? ResultKinds.None
          : _polygame.GameResult == Polygamo.ResultKinds.Draw ? ResultKinds.Draw
          : _polygame.GameResult == Polygamo.ResultKinds.Win && _polygame.LastPlayer == Player ? ResultKinds.Win
          : ResultKinds.Lose; }  // TODO: handle loss by non-current player
    }
    internal bool HasResult { get { return _polygame.GameResult != Polygamo.ResultKinds.None; } }
    internal bool IsGameOver {
      get { return _polygame.GameResult != Polygamo.ResultKinds.None; }
    }

    // get candidate image names for player and piece
    internal IList<string> GetImageNames(string player, string piece) {
      return PieceImages.FirstOrDefault(i => i.Piece == piece && i.Player == player).Images;
    }

    PolyGame _polygame;
    TileModel[] _tiles;
    Rect _bounding;

    internal static BoardModel Create(GameManager game, string scriptname) {
      var gameprog = game.Items.LoadScript(scriptname, scriptname);
      if (gameprog == null) return null;
      // guaranteed never to raise exception!!!
      var engine = PolyGame.Create(scriptname, new StringReader(gameprog));
      return new BoardModel {
        ScriptName = scriptname,
        _polygame = engine,
      }.Setup();
    }

    internal BoardModel Create(int index) {
      return new BoardModel {
        ScriptName = ScriptName,
        _polygame = this._polygame.Create(index)
      }.Setup();
    }

    BoardModel Setup() {
      _tiles = _polygame.Positions
        .Where(s => !s.IsDummy)
        .Select(s => TileModel.Create(this, s.Name,
          new Rect(s.Location.Left, s.Location.Top, s.Location.Width, s.Location.Height)))
        .ToArray();
      _bounding = Rect.MinMaxRect(_tiles.Min(t => t.Rect.xMin), _tiles.Min(t => t.Rect.yMin),
        _tiles.Max(t => t.Rect.xMax), _tiles.Max(t => t.Rect.yMax));
      return this;
    }

    //==========================================================================
    // start a new game
    internal void Start() {
      _polygame.NewBoard(ChooserKinds.Mcts);
      TimePlayed = TimeSpan.Zero;
      UpdatePieces();
    }

    internal void Restart() {
      if (IsGameOver) return;
      _polygame.Restart();
      TimePlayed = TimeSpan.Zero;
      UpdatePieces();
    }

    internal void Undo() {
      if (IsGameOver) return;
      MoveCount++;
      _polygame.UndoMove();
      UpdatePieces();
    }

    internal void Redo() {
      if (IsGameOver) return;
      MoveCount++;
      _polygame.RedoMove();
      UpdatePieces();
    }

    internal void UpdatePieces() {
      var played = _polygame.PlayedPieces.ToDictionary(p => p.Position);
      foreach (var tile in _tiles)
        tile.SetPiece(played.SafeLookup(tile.Name));
    }

    internal void UpdateTime(float deltaTime) {
      TimePlayed += new TimeSpan((long)(deltaTime * 10000000.0));
    }

    // Enumerate all
    public IEnumerable<TileModel> AllTiles() {
      foreach (var t in _tiles)
        yield return t;
    }

    internal int CheckMoves(string position) {
      return GetLegalMoves(position).Count;
    }

    // make (only) move for given position
    internal bool MakeMove(string position, string piece, string position2) {
      var moves = GetLegalMoves(position).Where(m => m.Piece1 == piece).ToList();
      if (moves.Count == 1) MakeMove(moves[0].Index);
      return moves.Count == 1;
    }

    // make move by index
    internal void MakeMove(int index) {
      _polygame.MakeMove(index);
      MoveCount++;
      UpdatePieces();
    }

    // choose a move in limited steps, return true if complete
    internal bool UpdateChooser(int steps) {
      return _polygame.UpdateChooser(steps);
    }

    // return chosen move, other info
    internal int ChosenMove { get { return _polygame.ChosenMove.Index; } }
    internal int VisitCount { get { return _polygame.VisitCount; } }
    internal double Weight { get { return _polygame.Weight; } }

    internal IList<PolyMove> GetLegalMoves(string position) {
      return _polygame.LegalMoves.Where(m => m.Position1 == position).ToList();
    }

    internal void SetPreview(string player, string position, string piece) {
      foreach (var tile in _tiles)
        if (tile.Name == position)
          tile.SetPreview(player, piece);
    }
  }

  ///////////////////////////////////////////////////////////////////////////////////////////////////
  /// <summary>
  /// Implements data model for Tile
  /// A Tile is a named location on the board. 
  /// It has a background and a border and it may hold a piece and/or a preview
  /// </summary>
  public class TileModel {
    internal string Name { get; private set; }
    internal Rect Rect { get; private set; }
    internal string PlayerName { get; private set; }
    internal string PieceName { get; private set; }
    internal string PreviewPlayer { get; private set; }
    internal string PreviewPiece { get; private set; }
    internal bool IsSelected { get; private set; }
    internal bool IsDisabled { get; private set; }
    internal bool IsChanged {
      get {
        if (!_ischanged) return false;
        _ischanged = false;
        return true;
      }
    }
    internal BoardModel Board { get { return _model; } }

    BoardModel _model;
    bool _ischanged;

    //--- overrides
    public override string ToString() {//..
      return String.Format("[{0}:{1}]", Name, Rect);
    }

    internal static TileModel Create(BoardModel model, string name, Rect rect) {
      return new TileModel() {
        _model = model,
        Name = name,
        Rect = rect,
      }.Setup();
    }

    internal void SetPiece(PolyPlayedPiece piece) {
      if (!(PieceName == piece.Piece && PlayerName == piece.Player)) {
        PieceName = piece.Piece;
        PlayerName = piece.Player;
        _ischanged = true;
      }
    }

    internal void SetPreview(string player, string piece) {
      if (!(PreviewPiece == piece && PreviewPlayer == player)) {
        PreviewPiece = piece;
        PreviewPlayer = player;
        _ischanged = true;
      }
    }

    private TileModel Setup() {
      return this;
    }

    // Enable or disable a tile, fix up ???
    internal void Enable(bool enable) {
      if (IsDisabled != !enable) {
        IsDisabled = !enable;
      }
    }

    internal IList<MoveModel> GetMoves() {
      return _model.GetLegalMoves(Name)
        .Select(m => new MoveModel {
          Index = m.Index, Player = m.Player, Piece = m.Piece1,
          Position = m.Position1, NewPiece = m.Piece2, NewPosition = m.Position2,
        })
        .Distinct()
        .ToList();
    }
  }

  internal struct MoveModel {
    internal int Index;
    internal string Player;
    internal string Position;
    internal string Piece;
    internal string NewPosition;
    internal string NewPiece;

    internal bool IsDual { get { return NewPosition != ""; } }
    public override string ToString() {
      return (!IsDual) ? String.Format("({0},{1},{2})", Player, Position, Piece)
        : String.Format("({0},{1},{2}->{2},{3})", Player, Position, Piece, NewPosition, NewPiece);
    }
  }

}