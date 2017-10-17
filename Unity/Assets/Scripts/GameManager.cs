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
  public float ThinkTime = 3.0f;
  public int StepCount = 100;
  public int MaxDepth = 9;

  // statics
  internal static GameManager Instance { get; private set; }

  // publics
  internal event Action<GameManager, GameState> StateChangeEvent;
  internal GameBoardModel Model { get { return _model; } }
  internal GameState State { get; private set; }
  internal HighScores HighScores { get { return _highscores; } }

  internal bool IsPlaying { get { return State == GameState.Playing; } }
  public bool IsPaused { get { return State == GameState.Pause; } }
  public bool IsGameOver { get { return State == GameState.GameOver; } }

  // private
  GameBoardModel _model;
  HighScores _highscores;
  GameObject _boardobject;
  GameState _savestate;
  UiManager _uimx;

  // System wide initialisation only happens here
  void Awake() {
    Util.Trace(1, "GameManager game={0} var={1}\ndir={2}", InitialScript, InitialVariant, Application.dataPath);
    if (Instance == null) Instance = this;
    else if (Instance != this) DestroyObject(gameObject);
    DontDestroyOnLoad(gameObject);

    // if only this would trap into VS
    Assert.raiseExceptions = true;
    //Assert.IsTrue(false);

    State = GameState.Startup;
    _highscores = GetComponent<HighScores>();
    _uimx = FindObjectOfType<UiManager>();
    _highscores.LoadScore();

    // change setup here for convenient testing
#if UNITY_EDITOR
    _highscores.ClearScore();
    FindObjectOfType<AudioSource>().mute = true;  // set directly -- options panel not started yet
    Items.UserGamesFolder = "test games";
    InitialScript = "Kono";
    InitialVariant = 0;
#endif
  }

  void Start() { 
    Items.FindAllGames();
    var initial = Items.ScriptList.Find(s => s.Filename.ToLower() == InitialScript.ToLower()) ?? Items.ScriptList[0];
    var model = GameBoardModel.Create(this, initial);
    if (model != null && InitialVariant > 0 && InitialVariant < model.GameList.Count)
      model = model.Create(InitialVariant);
    if (model == null) Quit();
    Launch(model);
  }

  // Check for game over each cycle
  void Update() {
    if (State == GameState.Startup) return;
    if (_model.IsGameOver)
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

  // Load a new script
  internal bool LoadGame(ScriptInfo script) {
    var model = GameBoardModel.Create(this, script);
    if (model != null) Launch(model);
    return model != null;
  }

  // Select a different variant
  internal void LoadGame(int index) {
    var model = _model.Create(index);
    Launch(model);
  }

  // Called to start a new game (or could be same again)
  internal void NewGame() {
    SetState(GameState.GameOver);
    _model.NewGame();
    StartCoroutine(FadeAndGo());
  }

  // Called to start a new game (or could be same again)
  internal void RestartGame() {
    SetState(GameState.GameOver);
    _model.Restart();
    StartCoroutine(FadeAndGo());
  }

  internal void Quit() {
    Util.Trace(1, "GameManager quit");
    Application.Quit();
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#endif
  }

  //----- implementation -----

  // Called to start a game previously selected
  void Launch(GameBoardModel model) {
    if (model == null || model.IsGameOver) return;
    SetState(GameState.GameOver);
    _model = model;
    // reset AI params
    _model.ThinkTime = ThinkTime;
    _model.StepCount = StepCount;
    _model.MaxDepth = MaxDepth;
    _uimx.CloseCurrent();
    StartCoroutine(FadeAndGo());
  }

  // transitions to new game, assumes model already created
  IEnumerator FadeAndGo() {
    _highscores.UpdateScore();
    _highscores.SaveScore();
    var camera = FindObjectOfType<Camera>();
    var slideoffset = 2f * camera.orthographicSize * camera.aspect;
    // destroy old board if it exists
    if (_boardobject != null) {
      var bv1 = _boardobject.GetComponent<BoardView>();
      yield return StartCoroutine(bv1.FadeOut(slideoffset));
      Destroy(_boardobject);
    }
    SetState(GameState.NewGame);
    _highscores.LoadScore();
    CreateBoard();
    _model.UpdatePieces();
    var bv2 = _boardobject.GetComponent<BoardView>();
    yield return StartCoroutine(bv2.FadeIn(slideoffset));
    SetState(GameState.Playing);
  }

  // Create a new game given the game number
  // Layout happens in the board view
  void CreateBoard() {
    // instantiate board at the back; set parent local
    _boardobject = Instantiate(BoardPrefab);
    _boardobject.transform.SetParent(_uimx.MainPanel.transform, false);
    _boardobject.transform.SetSiblingIndex(0);

    // set scale to resize to required height
    var uiheight = (_uimx.MainPanel.transform as RectTransform).rect.height;
    var uiheightex = uiheight - 2f * _uimx.VerticalBorderPx;
    var bdheight = (_boardobject.transform as RectTransform).rect.height;
    var scale = uiheightex / bdheight;
    _boardobject.transform.localScale = new Vector3(scale, scale, 0);
    Util.Trace(2, "GM start uih={0} uihx={1} bdh={2} scale={3}", uiheight, uiheightex, bdheight, scale);
  }

}
