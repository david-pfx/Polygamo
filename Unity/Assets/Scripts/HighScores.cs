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

using UnityEngine;
using PolygamoUnity;

/// <summary>
/// Manage recording and updating high scores
/// </summary>
public class HighScores : MonoBehaviour {

  internal int LastLevelId { get; set; }
  internal int TotalMoves { get; set; }
  internal int TotalStars { get; set; }
  internal int TotalGames { get; set; }
  internal int TotalTime { get; set; }

  GameManager _game { get { return GameManager.Instance; } }
  GameBoardModel _model { get { return _game.Model; } }

  internal void UpdateScore() {
    LoadScore();
    TotalMoves += _model.MoveCount;
    TotalGames += 1;
    TotalTime += _model.TimePlayedSeconds;
  }

  //--- manage persistent score

  // Load or init score
  internal void LoadScore() {
    if (PlayerPrefs.HasKey("games")) {
      LastLevelId = PlayerPrefs.GetInt("gameno");
      TotalGames = PlayerPrefs.GetInt("games");
      TotalMoves = PlayerPrefs.GetInt("moves");
      TotalTime = PlayerPrefs.GetInt("time");
    } else ClearScore();
  }

  internal void SaveScore() {
    PlayerPrefs.SetInt("gameno", LastLevelId);
    PlayerPrefs.SetInt("games", TotalGames);
    PlayerPrefs.SetInt("moves", TotalMoves);
    PlayerPrefs.SetInt("stars", TotalStars);
    PlayerPrefs.SetInt("time", TotalTime);
    PlayerPrefs.Save();
  }

  internal void ClearScore() {
    LastLevelId = 0;
    TotalGames = 0;
    TotalMoves = 0;
    TotalStars = 0;
    TotalTime = 0;
    SaveScore();
  }

}
