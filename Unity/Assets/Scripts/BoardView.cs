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
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using PolygamoUnity;
using Random = UnityEngine.Random;
using System.Collections;

public class BoardView : MonoBehaviour {
  //public enum TileMoveModes {
  //  None, Rotate, Slide, Swap
  //}

  // layout objects
  //public string BoardImage;
  public GameObject tilePrefab;
  //public TileMoveModes MoveMode;
  public float FadeSpeed = 5f;

  GameManager _game { get { return GameManager.Instance; } }
  BoardModel _model { get { return _game.Model; } }

  Transform _tileholder;    // anchor for tiles, used for fades
  List<GameObject> _tiles = new List<GameObject>();
  Rect _boardrect;
  
  // Init here, assuming game manager complete
  void Start() {
    Util.NotNull(_game);
    Util.NotNull(_model);
    _tileholder = transform; // CHECK: will it change?
    _tileholder.position = new Vector3(1000f, 0, 0);  // way off screen -- animation would be better
    LoadBoard();
  }

  void Update() {
    if (_game.IsPlaying)
      _model.UpdateTime(Time.deltaTime);
  }

  // Coroutine to fade away
  internal IEnumerator FadeOut(float slideoffset) {
    yield return null;
    _tileholder.position = new Vector3(0, 0, 0);
    var endx = _tileholder.position.x - slideoffset;
    while (_tileholder.position.x > endx) {
      var motion = new Vector3(-FadeSpeed * Time.deltaTime, 0, 0);
      _tileholder.Translate(motion);
      yield return null;
    }
  }

  // Coroutine to fade in
  internal IEnumerator FadeIn(float slideoffset) {
    yield return null;
    _tileholder.position = new Vector3(slideoffset, 0, 0);
    Util.Trace(3, "FadeIn pos={0} off={1}", _tileholder.position, slideoffset);
    var endx = 0;
    while (_tileholder.position.x > endx) {
      var motion = new Vector3(-FadeSpeed * Time.deltaTime, 0, 0);
      _tileholder.Translate(motion);
      yield return null;
    }
  }

  //--- impl

  void LoadBoard() {
    Util.Trace(2, "Load board script={0} title={1}", _model.ScriptName, _model.Title);
    _boardrect = (_tileholder as RectTransform).rect;
    var image = transform.GetComponent<Image>();
    var loaded = _model.Images.Any(i => _game.Items.LoadImage(image.gameObject, _model.ScriptName, i));
    if (!loaded) _game.Items.LoadImage(image.gameObject, null, "board:green");
    LayoutTiles();
  }

  // Instantiate a prefab for each tile
  // given vertical window size in units
  internal void LayoutTiles() {
    // Instantiate all the tiles to this scale
    var bounding = _model.BoundingRect;
    var scale = _boardrect.height / (bounding.height + 2 * bounding.y);  // include margins
    var firsttile = _model.AllTiles().First();
    var tiletransform = tilePrefab.transform as RectTransform;
    var tscale = firsttile.Rect.height / tiletransform.rect.height * scale;
    Util.Trace(2, "Layout tiles scale={0} tscale={1}\n bounding={2}", scale, tscale, bounding);
    Util.Trace(3, "model={0}\n prefab={1}", firsttile.Rect, tiletransform.rect);

    foreach (var tilemodel in _model.AllTiles()) {
      var pos = (tilemodel.Rect.center - bounding.center) * scale;
      pos.y = -pos.y;  // convert coords
      // create a tile and position it
      var tile = Instantiate(tilePrefab, pos, Quaternion.identity) as GameObject;
      tile.transform.localScale = new Vector3(tscale, tscale, 0);
      tile.transform.SetParent(_tileholder, false);
      _tiles.Add(tile);
      tile.GetComponent<TileView>().Setup(this, tilemodel);
    }
  }
}
