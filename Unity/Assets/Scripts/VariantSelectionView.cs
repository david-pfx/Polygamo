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
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using PolygamoUnity;

public class VariantSelectionView : MonoBehaviour {

  public Text TitleText;
  public Text StatusbarText;
  public RectTransform ItemGrid;
  public GameObject SelectItemPrefab;
  public SelectModes SelectMode;
  public GameObject SelectionPanelOther;
  public int GridWidth = 5;
  public int GridHeight = 4;

  GameManager _game { get { return GameManager.Instance; } }
  GameBoardModel _model { get { return _game.Model; } }

  int _pageno;
  RectTransform _holder;
  private float _gridsize;
  private float _itemsize;
  private Vector3 _origin;
  private Vector3 _scale;
  UiManager _uimx;

  void Awake() {
    _uimx = FindObjectOfType<UiManager>();
    TitleText.text = "Polygamo";
    gameObject.SetActive(false);
  }

  void Start() {
  }

  private void OnEnable() {
    ShowGameGrid(0);
  }

  void Update() {
    if (Input.GetKeyDown(KeyCode.LeftArrow)) LeftButtonHandler();
    if (Input.GetKeyDown(KeyCode.RightArrow)) RightButtonHandler();
    if (Input.GetKeyDown(KeyCode.S)) ScriptButtonHandler();
  }

  public void LeftButtonHandler(string input = "click") {
    _uimx.DoButtonAction(input, "Scroll left", StatusbarText, () => ShowGameGrid(_pageno - 1));
  }

  public void RightButtonHandler(string input = "click") {
    _uimx.DoButtonAction(input, "Scroll right", StatusbarText, () => ShowGameGrid(_pageno + 1));
  }

  public void ScriptButtonHandler(string input = "click") {
    _uimx.DoButtonAction(input, "Select game scripts", StatusbarText, () => _uimx.OpenPanel(SelectionPanelOther));
  }

  public void CloseButtonHandler(string input = "click") {
    _uimx.DoButtonAction(input, "Close", StatusbarText, () => _uimx.CloseCurrent());
  }

  // build and show the selection grid
  void ShowGameGrid(int page) {
    Util.Trace(2, "Show grid mode={0} page={1}", SelectMode, page);
    var itemcount = GridWidth * GridHeight;
    var offset = page * itemcount;
    var tcount = (SelectMode == SelectModes.Variant) ? _game.Model.GameList.Count : _game.Items.ScriptList.Count;
    var count = Math.Min(itemcount, tcount - offset);
    if (offset < 0 || count <- 0) return;
    _pageno = page;

    _gridsize = ItemGrid.rect.height / GridHeight;
    _itemsize = SelectItemPrefab.GetComponent<RectTransform>().rect.height * 1.1f;

    DestroyGrid();
    _holder = Instantiate(ItemGrid, ItemGrid.parent, false);
    _origin = new Vector3(-0.5f * (GridWidth - 1) * _gridsize, 0.5f * (GridHeight - 1) * _gridsize, 0);
    _scale = new Vector3(_gridsize / _itemsize, _gridsize / _itemsize, 0);
    Util.Trace(3, "gridsize={0} itemsize={1} page={2} xbase={3}", _gridsize, _itemsize, page, offset);

    TitleText.text = (SelectMode == SelectModes.Script) ? "Game Scripts"
      : _model.Script.Filename + " variants";
    StatusbarText.text = "";
    //ItemTitleText.text = Application.dataPath;
    var objs = CreateGrid();
    StartCoroutine(FillGrid(objs, SelectMode, offset, count));
  }

  // create grid inactive with nothing in it
  GameObject[] CreateGrid() {
    var objs = new GameObject[GridWidth * GridHeight];
    for (int i = 0; i < objs.Length; i++) {
      var pos = _origin + new Vector3(_gridsize * (i % GridWidth), -_gridsize * (i / GridWidth), 0);
      objs[i] = Instantiate(SelectItemPrefab, _holder.transform) as GameObject;
      objs[i].transform.localPosition = pos;
      objs[i].transform.localScale = _scale;
      objs[i].SetActive(false);
    }
    return objs;
  }

  // kill the root, children follow
  void DestroyGrid() {
    if (_holder != null)
      DestroyObject(_holder.gameObject);
    _holder = null;
  }

  // fill at leisure (it can take some time)
  IEnumerator FillGrid(GameObject[] objs, SelectModes mode, int xbase, int xcount) {
    Util.Trace(3, "fill grid {0} {1} {2} {3}", objs.Length, mode, xbase, xcount);
    for (int i = 0; i < objs.Length; i++) {
      objs[i].GetComponent<VariantItemView>().Setup(SelectAction, StatusbarText, mode, xbase + i);
      objs[i].SetActive(true);
      yield return null;
    }
  }

  // respond to a click
  void SelectAction(int index) {
    Util.Trace(2, "Click! {0}", index);
    if (SelectMode == SelectModes.Variant)
      _game.LoadGame(index);
    else {
      StatusbarText.text = "Loading...";
      StartCoroutine(LoadScript(_game.Items.ScriptList[index]));
    }
  }

  IEnumerator LoadScript(ScriptInfo script) {
    yield return null;
    StatusbarText.text = (_game.LoadGame(script)) ? "OK" : GameBoardModel.LastError;
    yield return null;
  }
}


