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

///
/// Test Suite
/// 
using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Polygamo;
using Poly.Common;
using System.IO;

namespace PolygamoTest {
  [TestClass]
  public class TestChooser {
    static Dictionary<char, Action<PolyGame>> actionlookup = new Dictionary<char, Action<PolyGame>> {
        { 'I', g=> { g.NewBoard(ChooserKinds.First); g.UpdateChooser(); } },
        //{ '1', g=> { g.NewBoard(ChooserKinds.Depth); g.UpdateChooser(1); } },
        //{ '2', g=> { g.NewBoard(ChooserKinds.Depth); g.UpdateChooser(2); } },
        //{ 'F',       g=>g.NewBoard(ChooserKinds.Full) },
        //{ 'S', g=> { g.NewBoard(ChooserKinds.Breadth); g.UpdateChooser(2000); } },
        { 'T', g=> { g.NewBoard(ChooserKinds.Mcts); g.UpdateChooser(2000); } },
        { 'M', g=> { g.MakeMove(g.ChosenMove.Index); g.UpdateChooser(); } },
        { '0', g=> { g.MakeMove(0); g.UpdateChooser(); } },
      };

    // test AI choosing moves
    [TestMethod]
    public void Choose1() {
      Logger.Open(1);
      var testprog =
        @"(include ""testincl.poly"")" +
        @"(game (gamestub1)" +
        @" (board (boardgrid33))" +
        @" (draw-condition (X O) stalemated)" +
        @" (win-condition (relcondttt))" +
        @" (board-setup (X (man B-1 B-2)) (O (man C-1 C-2)))" +
        @" (piece (name man) (drops ((verify empty?) add)))" +
        @")";

      var matrix = new string[,] {
        { "I",         "X;X,man,A-1" },
        //{ "R",         "X;X,man,A-1" },
        //{ "S",         "X;X,man,B-3" },
        //{ "F",         "X;X,man,B-3" },
        { "T",         "X;X,man,B-3" },

        { "IM",        "O;O,man,A-2" },
        //{ "SM",        "O;O,man,C-3" },
        //{ "MI",        "O;O,man,A-2" },
        //{ "MS",        "O;O,man,C-3" },
        //{ "MF",        "O;O,man,C-3" },
      };
      for (int i = 0; i < matrix.GetLength(0); i++) {
        var game = PolyGame.Create("played", new StringReader(testprog));
        foreach (var ch in matrix[i, 0])
          actionlookup[ch](game);
        //Assert.IsTrue(game.ChoicesIter().First().IsDone, matrix[i, 0]);
        var result = game.TurnPlayer + ";" + 
          Util.Join(",", game.ChosenMove.Player, game.ChosenMove.Piece1, game.ChosenMove.Position1);
        Assert.AreEqual(matrix[i, 1], result, matrix[i, 0]);
      }
    }

    // test AI choosing moves
    [TestMethod]
    public void Choose2() {
      Logger.Open(1);
      var testprog =
        @"(include ""testincl.poly"")" +
        @"(game (gamestub1)" +
        @" (board (boardgrid33))" +
        @" (draw-condition (X O) stalemated)" +
        @" (win-condition (relcondttt))" +
        @" (board-setup (X (man B-1)) (O (man C-1 C-2)))" +
        @" (piece (name man) (drops ((verify empty?) add)))" +
        @")";

      var matrix = new string[,] {
        { "I;",         "X,man,A-1;" },
        { "T;",         "X,man,C-3;" },
        //{ "S;",         "X,man,C-3;" },
        //{ "1;",         "X,man,A-1;" },
        //{ "2;",         "X,man,C-3;" },
        //{ "F;",         "X,man,C-3;" },
      };

      for (int i = 0; i < matrix.GetLength(0); i++) {
        var game = PolyGame.Create("choice2", new StringReader(testprog));
        var result = "";
        foreach (var ch in matrix[i, 0]) {
          if (ch == ';')
            result += Util.Join(",", game.ChosenMove.Player, game.ChosenMove.Piece1, game.ChosenMove.Position1) + ";";
          else actionlookup[ch](game);
        }
        //Assert.IsTrue(game.ChoicesIter().First().IsDone, matrix[i, 0]);
        Assert.AreEqual(matrix[i, 1], result, matrix[i, 0]);
      }
    }

    // test AI choosing moves
    [TestMethod]
    public void Choose3() {
      Logger.Open(1);
      var testprog =
        @"(include ""testincl.poly"")" +
        @"(game (gamestub1)" +
        @" (board (boardgrid33))" +
        @" (draw-condition (X O) stalemated)" +
        @" (win-condition (relcondttt))" +
        @" (piece (name man) (drops ((verify empty?) add)))" +
        @")";

      var matrix = new string[,] {
        { "I;",         "X,man,A-1;" },
        { "T;",         "X,man,B-2;" },
        //{ "1;",         "X,man,A-1;" },
        //{ "2;",         "X,man,C-3;" },
        //{ "F;",         "X,man,C-3;" }, // too slow
      };

      for (int i = 0; i < matrix.GetLength(0); i++) {
        var game = PolyGame.Create("choice2", new StringReader(testprog));
        var result = "";
        foreach (var ch in matrix[i, 0]) {
          if (ch == ';')
            result += Util.Join(",", game.ChosenMove.Player, game.ChosenMove.Piece1, game.ChosenMove.Position1) + ";";
          else actionlookup[ch](game);
        }
        Assert.AreEqual(matrix[i, 1], result, matrix[i, 0]);
      }
    }
  }
}
