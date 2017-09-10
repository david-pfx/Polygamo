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

using System.Collections;
using System;
using UnityEngine;
using UnityEngine.Assertions;
using PolygamoUnity;

public enum GameState {
  Startup, Playing, GameOver, NewGame, Pause
}

public class GameManager : MonoBehaviour {

  // settables
  public GameObject BoardPrefab;
  public string InitialScript;
  public int InitialVariant; 
  public ItemManager Items;
  public float ThinkTime = 1.0f;
  public int StepCount = 100;
  public bool IgnoreHighScore;  // for testing

  // statics
  internal static GameManager Instance { get; private set; }

  // publics
  internal event Action<GameManager, GameState> StateChangeEvent;
  internal BoardModel Model { get { return _model; } }
  internal GameState State { get; private set; }
  internal HighScores HighScores { get { return _highscores; } }

  internal bool IsPlaying { get { return State == GameState.Playing; } }
  public bool IsPaused { get { return State == GameState.Pause; } }
  public bool IsGameOver { get { return State == GameState.GameOver; } }

  // private
  BoardModel _model;
  HighScores _highscores;
  GameObject _boardobject;
  GameState _savestate;

  // System wide initialisation only happens here
  void Awake() {
    Util.Trace(1, "GameManager game={0} var={1}", InitialScript, InitialVariant);
    if (Instance == null) Instance = this;
    else if (Instance != this) DestroyObject(gameObject);
    DontDestroyOnLoad(gameObject);

    // if only this would trap into VS
    Assert.raiseExceptions = true;
    //Assert.IsTrue(false);

    State = GameState.Startup;
    _highscores = GetComponent<HighScores>();

    _highscores.LoadScore();
    if (IgnoreHighScore)
      _highscores.ClearScore();
    Items.FindAllGames();
    var model = BoardModel.Create(this, InitialScript);
    if (model != null && InitialVariant > 0 && InitialVariant < model.GameList.Count)
      model = model.Create(InitialVariant);
    if (model == null) Quit();
    Launch(model);
  }

  // Check for game over each cycle
  void Update() {
    if (State == GameState.Startup) return;
    if (Model.IsGameOver)
      SetState(GameState.GameOver);
  }

  // change state, trigger event, return true if changed
  internal bool SetState(GameState state) {
    if (state == State) return false;
    State = state;
    if (state != GameState.Pause) _savestate = state;
    if (StateChangeEvent != null) StateChangeEvent(this, State);
    return true;
  }

  internal bool Pause(bool pause) {
    if (pause) {
      _savestate = State;
      if (!SetState(GameState.Pause)) return false;
    } else if (!SetState(_savestate)) return false;
    return true;
  }

  // Start a new game by loading a new script
  internal void NewGame(string scriptname) {
    var model = BoardModel.Create(this, scriptname);
    Launch(model);
  }

  // Start a new game by selecting a different variant
  internal void NewGame(int index) {
    var model = Model.Create(index);
    Launch(model);
  }

  // Called to start a new game (or could be same again)
  internal void NewGame(bool next = false) {
    SetState(GameState.GameOver);
    if (next) Model.Start();
    else Model.Restart();
    StartCoroutine(FadeAndGo());
  }

  // Called to start a game previously selected
  internal void Launch(BoardModel model) {
    if (model == null || model.IsGameOver) return;
    SetState(GameState.GameOver);
    _model = model;
    FindObjectOfType<UiManager>().CloseCurrent();
    StartCoroutine(FadeAndGo());
  }

  // transitions to new game
  IEnumerator FadeAndGo() {
    _highscores.UpdateScore();
    _highscores.SaveScore();
    var camera = FindObjectOfType<Camera>();
    var slideoffset = 2f * camera.orthographicSize * camera.aspect;
    // destroy old board if it exists
    if (_boardobject != null) {
      var bv1 = _boardobject.GetComponent<BoardView>();
      yield return bv1.FadeOut(slideoffset);
      Destroy(_boardobject);
    }
    SetState(GameState.NewGame);
    StartGame();
    var bv2 = _boardobject.GetComponent<BoardView>();
    yield return bv2.FadeIn(slideoffset);
    SetState(GameState.Playing);
  }

  // Create a new game given the game number
  // Layout happens in the board view
  internal void StartGame() {
    Model.Start();
    _highscores.LoadScore();

    var uimx = FindObjectOfType<UiManager>();
    
    // instantiate board at the back; set parent local
    _boardobject = Instantiate(BoardPrefab);
    _boardobject.transform.SetParent(uimx.MainPanel.transform, false);
    _boardobject.transform.SetSiblingIndex(0);

    // set scale to resize to required height
    var uiheight = (uimx.MainPanel.transform as RectTransform).rect.height;
    var uiheightex = uiheight - 2f * uimx.VerticalBorderPx;
    var bdheight = (_boardobject.transform as RectTransform).rect.height;
    var scale = uiheightex / bdheight;
    _boardobject.transform.localScale = new Vector3(scale, scale, 0);
    Util.Trace(2, "GM start uih={0} uihx={1} bdh={2} scale={3}", uiheight, uiheightex, bdheight, scale);
  }

  internal void Quit() {
    Util.Trace(1, "GameManager quit");
    Application.Quit();
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#endif
  }

}
