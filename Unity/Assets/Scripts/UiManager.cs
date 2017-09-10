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
using UnityEngine;
using UnityEngine.UI;
using PolygamoUnity;
using System.Collections;

/// <summary>
/// Handle all aspects of the UI
/// </summary>
public class UiManager : MonoBehaviour {

  public GameObject MainPanel;
  public GameObject OptionsPanel;
  public GameObject SelectionPanel;
  public GameObject ResultPanel;
  public Animator InitiallyOpen;
  public int VerticalBorderPx;
  public Text GameTitleText;
  public Text GameText;
  public Text ResultTitleText;
  public Text ResultText;
  public Text TitleText1;
  public Text TitleText2;
  public Text TitleText3;
  public Text ScoreText1;
  public Text ScoreText2;
  public Text ScoreText3;
  public Text PlayerTitle;
  public Text PlayerText;

  const string OpenPropertyName = "isopen";
  const string ClosedStateName = "closed";

  GameManager _game { get { return GameManager.Instance; } }
  BoardModel _model { get { return _game.Model; } }
  Animator _nowopen = null;  
  bool _closing;

  Dictionary<ResultKinds, string> resultlookup = new Dictionary<ResultKinds, string> {
    { ResultKinds.None, "No Result" },
    { ResultKinds.Win, "{0} has Won" },
    { ResultKinds.Lose, "{0} has Lost" },
    { ResultKinds.Draw, "Draw" },
  };

  void Update() {
    if (_model == null || _game.State == GameState.Startup) _game.Quit();
    else {
      GameText.text = String.Format("{0}", _model.Title);
      ScoreText1.text = String.Format("{0}:{1:00}", _model.TimePlayed.Minutes, _model.TimePlayed.Seconds);
      ScoreText2.text = String.Format("{0}", _model.MoveCount);
      PlayerText.text = _game.IsGameOver ? "Game Over" 
                      : _game.IsPaused ? "Game Paused" 
                      : String.Format("{0} to play", _model.Player);
      ResultText.text = String.Format(resultlookup[_model.GameResult], _model.Player);

      ResultPanel.SetActive(_game.IsGameOver);
      if (_nowopen != null) {
        if (Input.GetKeyDown(KeyCode.Escape)) CloseCurrent();
      } else {
        if (Input.GetKeyDown(KeyCode.Backspace)) BackButtonHandler();
        else if (Input.GetKeyDown(KeyCode.Escape)) QuitButtonHandler();
        else if (Input.GetKeyDown(KeyCode.R)) RestartButtonHandler();
        else if (Input.GetKeyDown(KeyCode.N)) NewGameButtonHandler();
        else if (Input.GetKeyDown(KeyCode.Space) && _game.State == GameState.GameOver) NewGameButtonHandler();
        else if (Input.GetKeyDown(KeyCode.O)) OptionsButtonHandler();
        else if (Input.GetKeyDown(KeyCode.S)) SelectionButtonHandler();
      }
    }
  }

  public void NewGameButtonHandler() {
    _game.NewGame(true);
  }

  public void RestartButtonHandler() {
    _game.NewGame();
  }

  public void BackButtonHandler() {
    _model.Undo();
  }

  public void OptionsButtonHandler() {
    OpenPanel(OptionsPanel);
  }

  public void SelectionButtonHandler() {
    OpenPanel(SelectionPanel);
  }

  public void QuitButtonHandler() {
    _game.HighScores.UpdateScore();
    _game.HighScores.SaveScore();
    Application.Quit();
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#endif
  }

  //****************************************************************************
  // manage currently open screen panel
  // based on Unity HOWTO but no selection maintained
  //****************************************************************************

  internal void OpenPanel(GameObject obj) {
    OpenPanel(obj.GetComponent<Animator>());
  }

  void OpenPanel(Animator toopen) {
    Util.Trace(2, "Open panel {0} closing={1}", toopen.name, _closing);
    if (_nowopen == toopen) return;
    if (_closing) return;
    toopen.gameObject.SetActive(true);
    toopen.transform.SetAsLastSibling();
    CloseCurrent();
    toopen.SetBool(OpenPropertyName, true);
    _nowopen = toopen;
    MainPanel.SetActive(_nowopen == null);
    _game.Pause(_nowopen != null);
  }

  // close current panel and disable when animation finished
  // public -- called from buttons elsewhere
  public void CloseCurrent() {
    if (_closing) return;
    if (_nowopen != null) ClosePanel(_nowopen);
    _nowopen = null;
    MainPanel.SetActive(true);
  }

  void ClosePanel(Animator toclose) {
    toclose.SetBool(OpenPropertyName, false);
    StartCoroutine(DelayedDisable(toclose, ClosedStateName));
  }

  // disable specified obj via anim when state reached
  IEnumerator DelayedDisable(Animator anim, string state) {
    _closing = true;
    while (true) { 
      // skip a frame
      yield return null;
      if (!anim.IsInTransition(0) && anim.GetCurrentAnimatorStateInfo(0).IsName(state)) break;
    }
    anim.gameObject.SetActive(false);
    MainPanel.SetActive(_nowopen == null);
    _game.Pause(_nowopen != null);
    _closing = false;
  }
}
