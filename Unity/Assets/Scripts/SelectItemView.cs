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
using UnityEngine;
using UnityEngine.UI;
using PolygamoUnity;

public enum SelectModes {
  None, Script, Variant
}
public class SelectItemView : MonoBehaviour {
  const int ShortLength = 10;

  public Text TitleText;
  public Text BaseText;
  public GameObject Thumbnail;

  GameManager _game { get { return GameManager.Instance; } }
  BoardModel _model { get { return _game.Model; } }
  SelectModes _mode = SelectModes.None;
  int _itemindex;
  Action<int> _selectaction;
  Text _fulltextobj;
  string _titletext;
  string _basetext;

  // pseudo-ctor
  internal SelectItemView Setup(Action<int> action, Text fulltextobj, SelectModes mode, int index) {
    _selectaction = action;
    _fulltextobj = fulltextobj;
    _mode = mode;
    _itemindex = index;
    return this;
  }

  internal void ShowScript(int index) {
    var script = _game.Items.ScriptList[index];
    _titletext = script;
    _basetext = "";
    TitleText.text = _titletext.Shorten(ShortLength);
    BaseText.text = _basetext.Shorten(ShortLength);
    _game.Items.LoadImage(Thumbnail, script, "thumbnail");
  }

  internal void ShowVariant(int index) {
    var script = _model.ScriptName;
    _titletext = _game.Model.GameList[index];
    _basetext = script;
    TitleText.text = _titletext.Shorten(ShortLength);
    BaseText.text = _basetext.Shorten(ShortLength);
    var b = _game.Model.Create(index);
    _game.Items.LoadImage(Thumbnail, script, b.ThumbnailList[index] ?? b.Images[0]);
  }

  void Start() {
    if (_mode == SelectModes.Script && _itemindex < _game.Items.ScriptList.Count)
      ShowScript(_itemindex);
    else if (_mode == SelectModes.Variant && _itemindex < _game.Model.GameList.Count)
      ShowVariant(_itemindex);
    else gameObject.SetActive(false);
  }

  void OnMouseEnter() {
    _fulltextobj.text = _titletext;
  }

  void OnMouseExit() {
    _fulltextobj.text = "";
  }

  void OnMouseDown() {
    _selectaction(_itemindex);
  }
}
