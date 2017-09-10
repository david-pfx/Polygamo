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

using System.Text;
using UnityEngine;
using UnityEngine.UI;
using PolygamoUnity;

public class OptionsView : MonoBehaviour {

  public Text TitleText;
  public Text HelpText;
  public Text SavedDataText;
  public Toggle SoundToggle;
  public bool SoundDefault;

  GameManager _game { get { return GameManager.Instance; } }
  BoardModel _model { get { return _game.Model; } }
  HighScores _highscores { get { return _game.HighScores; } }

  float _clearclick = 0;

  // Use this for initialization
  void Awake() {
    SoundToggle.isOn = !SoundDefault;
    gameObject.SetActive(false);
  }

  private void OnEnable() {
    UpdateScores();
    UpdateText();
  }

  // Note: tried to use time click, but time stands still while the mouse is down!
  // double click to clear
  internal void ClearScores() {
    Util.Trace(2, "clear time {0}:{1}", _clearclick, Time.time);
    if (Time.time < _clearclick + 1.0f) {
      _highscores.ClearScore();
      UpdateScores();
    } else _clearclick = Time.time;
  }

  void UpdateText() {
    TitleText.text = _model.Title;
    HelpText.text = _model.Help;
  }

  void UpdateScores() {
    _highscores.UpdateScore();
    var sb = new StringBuilder();
    sb.Append(string.Format("Last game {0}\n", _highscores.LastLevelId));
    sb.Append(string.Format("Total moves {0}\n", _highscores.TotalMoves));
    sb.Append(string.Format("Total games {0}\n", _highscores.TotalGames));
    sb.Append(string.Format("Total time {0}", _highscores.TotalTime));
    SavedDataText.text = sb.ToString();
  }

}


