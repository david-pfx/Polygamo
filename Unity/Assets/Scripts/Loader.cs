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
/// Simply instantiate the game manager
/// </summary>
public class Loader : MonoBehaviour {
  public GameManager gameManager;

  void Awake() {
//    if (GameManager.Instance == null)
//      Instantiate(gameManager);
  }
}