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
using System;

public class OptionsView : MonoBehaviour {

  // objects
  public Text TitleText;
  public Text HelpText;
  public Text SavedDataText;
  public Toggle SoundToggle;
  // these are used to initialise input field
  public InputField ThinkTimeInput;
  public InputField StepCountInput;
  public InputField MaxDepthInput;

  GameManager _game { get { return GameManager.Instance; } }
  GameBoardModel _model { get { return _game.Model; } }
  HighScores _highscores { get { return _game.HighScores; } }

  float _clearclick = 0;

  public void ThinkTimeEdit() {
    _model.ThinkTime = Math.Max(0.1f, Math.Min(9999f, Util.SafeFloatParse(ThinkTimeInput.text) ?? 0));
    ThinkTimeInput.text = _model.ThinkTime.ToString();
  }

  public void StepCountEdit() {
    _model.StepCount = Math.Max(10, Math.Min(9999, Util.SafeIntParse(StepCountInput.text) ?? 0));
    StepCountInput.text = _model.StepCount.ToString();
  }

  public void MaxDepthEdit() {
    _model.MaxDepth = Math.Max(1, Math.Min(999, Util.SafeIntParse(MaxDepthInput.text) ?? 0));
    MaxDepthInput.text = _model.MaxDepth.ToString();
  }

  // Suspend panel until requested
  void Awake() {
    gameObject.SetActive(false);
  }

  private void OnEnable() {
    SoundToggle.isOn = FindObjectOfType<AudioSource>().mute;
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
    StringBuilder sb = new StringBuilder();

    var template = "<size={0}>{1}</size>\n{3}\n";
    const int HeadingSize = 48;
    const int TextSize = 36;
    if (_model.Description != null)
      sb.AppendFormat(template, HeadingSize,"Description", TextSize,  _model.Description);
    if (_model.History != null)
      sb.AppendFormat(template, HeadingSize, "\nHistory", TextSize, _model.History);
    if (_model.Strategy != null)
      sb.AppendFormat(template, HeadingSize, "\nStrategy", TextSize, _model.Strategy);
    HelpText.text = sb.ToString().Trim();
    ThinkTimeInput.text = _model.ThinkTime.ToString();
    StepCountInput.text = _model.StepCount.ToString();
    MaxDepthInput.text = _model.MaxDepth.ToString();
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


