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

public class ScriptSelectionView : MonoBehaviour {

  public Text TitleText;
  public GameObject ContentPanel;
  public Text ReadmeText;
  public Text StatusBarText;
  public GameObject ScriptItemPrefab;
  public GameObject SelectionPanelOther;
  public Scrollbar VerticalScrollbar;

  GameManager _game { get { return GameManager.Instance; } }
  GameBoardModel _model { get { return _game.Model; } }

  UiManager _uimx;
  //string _clicked = null;

  void Awake() {
    _uimx = FindObjectOfType<UiManager>();
    TitleText.text = "Games Script Selection";
    gameObject.SetActive(false);
  }

  void Start() {
    LoadScriptList();
  }

  private void OnEnable() {
    ReadmeText.text = _game.Items.RootFolderReadme ?? "No game selected.";
    StatusBarText.text = "Please select a game";
  }

  void Update() {
    if (Input.GetKeyDown(KeyCode.S)) ScriptButtonHandler();
  }

  public void ScriptButtonHandler(string input = "click") {
    _uimx.DoButtonAction(input, "Select game variants", StatusBarText, () => _uimx.OpenPanel(SelectionPanelOther));
  }

  public void CloseButtonHandler(string input = "click") {
    _uimx.DoButtonAction(input, "Close", StatusBarText, () => _uimx.CloseCurrent());
  }

  internal void ItemButtonHandler(string name, string input, string prompt) {
    if (input == "enter") {
      ReadmeText.text = _game.Items.LoadReadme(name);
      VerticalScrollbar.value = 1;
    }
    _uimx.DoButtonAction(input, prompt, StatusBarText, () => SelectGame(name));
  }

  // respond to a click
  void SelectGame(string name) {
    Util.Trace(2, "Click! {0}", name);
    var game = _game.Items.ScriptList.Find(g => g.Filename == name);
    StatusBarText.text = String.Format("Loading {0}...", name);
    StartCoroutine(LoadScript(game));
  }

  IEnumerator LoadScript(ScriptInfo script) {
    yield return null;
    StatusBarText.text = (_game.LoadGame(script)) ? "OK" : GameBoardModel.LastError;
    yield return null;
  }

  void LoadScriptList() {
    var games = _game.Items.ScriptList;
    foreach (var game in games) {
      var newitem = Instantiate(ScriptItemPrefab);
      var item = newitem.GetComponent<ScriptItemView>();
      newitem.transform.SetParent(ContentPanel.transform, false);
      item.NameText.text = game.Filename;
      newitem.transform.localScale = Vector3.one;
    }
  }

}


