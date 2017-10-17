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
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using PolygamoUnity;

/// <summary>
/// Tile component, used as prefab
/// </summary>
public class TileView : MonoBehaviour {
  public enum ActionKinds {
    None, Left, Right, Up,
    Enter, EnterLeft = Enter + Left, EnterRight = Enter + Right,
    Exit
  }

  public GameObject BorderObject;
  public GameObject PieceObject;
  public GameObject PreviewObject;

  public Color BorderNormal;
  public Color BorderHighlight;
  public Color TileNormal;
  public Color TileHighlight;

  public float RotateSpeed;
  public float MoveSpeed;

  // possible light overlay
  public string TileText;

  TileModel _tilemodel;

  GameManager _game { get { return GameManager.Instance; } }
  GameBoardModel _model { get { return _game.Model; } }

  Image _tileimage;       // background
  Image _borderimage;     // border
  TileStateMachine _tsm;
  Vector3 _lastcursor;
  MoveModel _previewmove;
  UiManager _uimx;

  ///============================================================================
  /// Handlers
  /// 

  // fluent ctor
  internal TileView Setup(BoardView boardview, TileModel tilemodel) {
    _tilemodel = tilemodel;
    return this;
  }

  // Use this for initialization
  void Start() {
    _tileimage = GetComponent<Image>();
    _borderimage = BorderObject.GetComponent<Image>();
    _uimx = FindObjectOfType<UiManager>();
    _tsm = new TileStateMachine(this);
    OnClear();
    LoadPieceImage(PieceObject, _tilemodel.PlayerName, _tilemodel.PieceName);
    LoadPieceImage(PreviewObject, _tilemodel.PlayerName, _tilemodel.PreviewPiece);
  }

  // Update is called once per frame
  void Update() {
    // Perhaps someone else changed our piece
    if (_tilemodel.IsChanged) {
      LoadPieceImage(PieceObject, _tilemodel.PlayerName, _tilemodel.PieceName);
      LoadPieceImage(PreviewObject, _tilemodel.PreviewPlayer, _tilemodel.PreviewPiece);
    }
  }

  void OnMouseEnter() {
    if (!AllowInput()) return;
    _tsm.HandleInput(TileInput.Over);
  }

  void OnMouseOver() {
    if (!AllowInput()) return;
    var world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    var cursor = transform.InverseTransformPoint(world);
    var input = Input.GetMouseButton(0) ? TileInput.Left : TileInput.Over;
    if (input != _tsm.LastInput || cursor != _lastcursor) {
      _lastcursor = cursor;
      //Util.Trace(2, "mouse {0} {1} {2} {3}", action, Input.mousePosition, world, _lastcursor);
      _tsm.HandleInput(input);
    }
  }

  void OnMouseExit() {
    if (!AllowInput()) return;
    _tsm.HandleInput(TileInput.Exit);
  }

  //============================================================================
  // Implementation

  // Call this to make the visible sprite match the model
  void LoadPieceImage(GameObject obj, string player, string piece) {
    if (piece == null) _game.Items.LoadImage(null, "", obj);
    else {
      var images = _model.GetImageNames(player, piece);
      var loaded = images != null && images.Any(i => _game.Items.LoadImage(_model.Script, i, obj));
      if (images != null && !loaded)
        Util.Trace(2, "show {0} {1}", player, piece, images.Join());
      if (!loaded) _game.Items.LoadImage(null, "piece:red", obj);
    }
  }

  bool AllowInput() {
    return _game.IsPlaying && _model.IsMyTurn;
  }

  //============================================================================
  //----- state change handlers

  internal bool OnClear() {
    _tileimage.color = TileNormal;
    _borderimage.color = BorderNormal;
    LoadPieceImage(PreviewObject, null, null);
    if (_previewmove.IsDual)
      _tilemodel.Board.SetPreview(null, _previewmove.NewPosition, null);
    _uimx.StatusText.text = "";
    return true;
  }

  internal bool OnCheck() {
    if (_model.IsThinking) return false;
    _borderimage.color = BorderHighlight;
    var moves = _tilemodel.GetMoves().OrderBy(m => m.NewPosition).ToList();
    if (moves.Count == 0) {
      _uimx.StatusText.text = (_tilemodel.PieceName == null) ? String.Format("{0}", _tilemodel.Name)
        : String.Format("{0} {1} on {2}", _tilemodel.PlayerName, _tilemodel.PieceName, _tilemodel.Name);
    } else { 
      // in case we want to make this move
      _previewmove = moves[PickMove(moves.Count)];
      LoadPieceImage(PreviewObject, _previewmove.Player, _previewmove.Piece);
      if (_previewmove.IsDual)
        _tilemodel.Board.SetPreview(_previewmove.Player, _previewmove.NewPosition, _previewmove.NewPiece);
      _tileimage.color = TileHighlight;
      _uimx.StatusText.text = _previewmove.Display;
      return true;
    }
    _tileimage.color = TileNormal;
    return false;
  }

  // choose from available moves based on cursor position
  private int PickMove(int count) {
    var width = (transform as RectTransform).rect.width;
    var frac = (_lastcursor.x + width / 2) / width;
    //Util.Trace(2, "picmove {0} {1} {2} {3}", count, width, frac, frac * count);
    return (int)Math.Max(0, Math.Min(count - 1, frac * count));
  }

  internal bool OnExecute() {
    Util.Trace(2, "exec {0}", _previewmove);
    _tilemodel.Board.MakeMove(_previewmove.Index);
    return true;
  }
}

////////////////////////////////////////////////////////////////////////////////
/// <summary>
/// State machine class and types
/// </summary>
////////////////////////////////////////////////////////////////////////////////

enum TileState { Normal, Highlighted, Thinking }
enum TileInput { Exit, Over, Left, Thought }
class TileTransition {
  internal TileState State;
  internal TileInput Input;
  internal Func<TileView, bool> OnAction;
  internal TileState NewState;
  internal TileTransition(TileState state, TileInput input, Func<TileView, bool> onaction, TileState newstate) {
    State = state; Input = input; OnAction = onaction; NewState = newstate;
  }
}

class TileStateMachine {
  TileState _state;
  TileTransition[] _table = new TileTransition[] {
    // from here with this input: do that and go there
      new TileTransition(TileState.Normal, TileInput.Exit, t => t.OnClear(), TileState.Normal),
      new TileTransition(TileState.Normal, TileInput.Over, t => t.OnCheck(), TileState.Highlighted),
      new TileTransition(TileState.Highlighted, TileInput.Exit, t => t.OnClear(), TileState.Normal),
      new TileTransition(TileState.Highlighted, TileInput.Over, t => t.OnClear() && t.OnCheck(), TileState.Highlighted),
      new TileTransition(TileState.Highlighted, TileInput.Left, t => t.OnClear() && t.OnExecute(), TileState.Normal),
    };
  TileView _tileview;

  internal TileInput LastInput { get; private set;}

  internal TileStateMachine(TileView tileview) {
    _tileview = tileview;
  }

  internal bool Check(TileInput input) {
    return _table.Any(t => t.State == _state && t.Input == input);
  }

  // handle an action and return true
  // if not recognised return false
  internal bool HandleInput(TileInput input) {
    if (!Check(input)) return false;
    var trans = _table.First(t => t.State == _state && t.Input == input);
    if (trans.OnAction(_tileview)) {
      //Util.Trace(2, "tsm {0}: {1} => {2}", action, _state, trans.NewState);
      _state = trans.NewState;
    }
    LastInput = input;
    return true;
  }

}
