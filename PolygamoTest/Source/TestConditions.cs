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
/// Test suite
/// 
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Polygamo;
using Poly.Common;
using System.IO;

namespace PolygamoTest {
  [TestClass]
  public class TestConditions {
    [TestMethod]
    public void CondWinTtt() {
      Logger.Open(1);
      var testprog =
        @"(include ""testincl.poly"")" +
        @" (game (gamestub1)" +
        @" (option ""pass turn"" true)" +
        @" (board (boardgrid33))" +
        @" (board-setup (X(man A-1 B-1 off 5 C-1)) )" +
        @" (piece (piece1))" +
        @" (win-condition (relcondttt))" +
        @")";
      var game = PolyGame.CreateInner("bs1", new StringReader(testprog));

      //Assert.AreEqual("Win", game.GameResult.ToString());
      game.MakeMove(0);
      Assert.AreEqual("Win", game.GameResult.ToString());
    }

    [TestMethod]
    public void CondRelConfig() {
      Logger.Open(1);
      var testprog =
        @"(include ""testincl.poly"")" +
        @"(game (gamestub1)" +
        @" (option ""pass turn"" true)" +
        @" (board (boardgrid33))" +
        @" (board-setup (X (man {0})))" +
        @" (piece (piece1))" +
        @" (win-condition (relcondttt))" +
        @")";
      var matrix = new string[,] {
        { "           ", "-,None" },
        { "A-1        ", "-,None" },
        { "A-1 B-1    ", "-,None" },
        { "A-1 B-1 C-1", "X,Win" },
        { "A-2 B-2 C-2", "X,Win" },
        { "A-3 B-3 C-3", "X,Win" },
        { "A-1 B-2 C-1", "-,None" },
        { "A-1 A-2 A-3", "X,Win" },
        { "B-1 B-2 B-3", "X,Win" },
        { "C-1 C-2 C-3", "X,Win" },
        { "C-1 C-2 C-1", "-,None" },
        { "A-1 B-2 C-3", "X,Win" },
        { "A-3 B-2 C-1", "X,Win" },
        { "    B-2 C-1", "-,None" },
        { "        C-1", "-,None" },
      };
      for (int i = 0; i < matrix.GetLength(0); i++) {
        var tp = String.Format(testprog, matrix[i, 0]);
        var game = PolyGame.CreateInner("bs1", new StringReader(tp));
        game.MakeMove(0);
        var expecteds = matrix[i, 1].SplitXY();
        Assert.AreEqual(expecteds[0], game.ResultPlayer.ToString(), matrix[i, 0]);
        Assert.AreEqual(expecteds[1], game.GameResult.ToString(), matrix[i, 0]);
        //var expected = game.LastPlayer + "," + game.GameResult;
        //Assert.AreEqual(matrix[i, 1], expected, matrix[i, 0]);
        //Assert.AreEqual(matrix[i, 1], game.GameResult, matrix[i, 0]);
      }
    }

    [TestMethod]
    public void CondAbsConfigPosition() {
      Logger.Open(1);
      var testprog =
        @"(include ""testincl.poly"")" +
        @"(game (gamestub1)" +
        @" (option ""pass turn"" true)" +
        @" (board (boardgrid33))" +
        @" (board-setup (X (man {0})) )" +
        @" (piece (piece1))" +
        @" (win-condition (abscondttt)) )";
      var matrix = new string[,] {
        //{ "           ", "-,None" },
        //{ "A-1        ", "-,None" },
        //{ "A-1 B-1    ", "-,None" },
        { "A-1 B-1 C-1", "X,Win" },
        { "A-2 B-2 C-2", "X,Win" },
        { "A-3 B-3 C-3", "X,Win" },
        { "A-1 B-2 C-1", "-,None" },
        { "A-1 A-2 A-3", "X,Win" },
        { "B-1 B-2 B-3", "X,Win" },
        { "C-1 C-2 C-3", "X,Win" },
        { "C-1 C-2 C-1", "-,None" },
        { "A-1 B-2 C-3", "X,Win" },
        { "A-3 B-2 C-1", "X,Win" },
        { "    B-2 C-1", "-,None" },
        { "        C-1", "-,None" },
      };
      for (int i = 0; i < matrix.GetLength(0); i++) {
        var tp = String.Format(testprog, matrix[i, 0]);
        var game = PolyGame.CreateInner("bs1", new StringReader(tp));
        game.MakeMove(0);
        var expecteds = matrix[i, 1].SplitXY();
        Assert.AreEqual(expecteds[0], game.ResultPlayer.ToString(), matrix[i, 0]);
        Assert.AreEqual(expecteds[1], game.GameResult.ToString(), matrix[i, 0]);
      }
    }

    [TestMethod]
    public void CondAbsConfOccupier() {
      Logger.Open(1);
      var testprog =
        @"(include ""testincl.poly"")" +
        @"(game (gamestub1)" +
        @" (option ""pass turn"" true)" +
        @" (board (boardgrid45))" +
        @" (piece (piece1))" +
        @" (piece (piece2))" +
        @" (board-setup (X (man A-1 A-2)) (O (chip C-3)) )" +
        @" (win-condition (X O) (absolute-config {0}))" +
        @")";
      var matrix = new string[,] {
        { "man (A-5)                    ", "-,None" },
        //{ "man A-5                      ", "-,None" },  -- TODO
        { "man (A-1)                    ", "X,Win" },
        { "man (A-2)                    ", "X,Win" },
        { "man (A-1 A-2 A-3)            ", "-,None" },
        { "man (A-1 A-2 C-3)            ", "-,None" },

        { "(not man) (A-1)              ", "O,Win" },
        { "(not man) (A-4)              ", "X,Win" },
        { "(not man) (C-3)              ", "X,Win" },

        { "(opponent man) (A-1)         ", "O,Win" },
        { "(opponent man) (A-2)         ", "O,Win" },
        { "(opponent chip) (A-1)        ", "-,None" },
        { "(opponent chip) (C-3)        ", "X,Win" },

        { "(not (opponent man)) (A-1)   ", "X,Win" },
        { "(not (opponent man)) (A-2)   ", "X,Win" },
        { "(not (opponent chip)) (A-1)  ", "X,Win" },
        { "(not (opponent chip)) (C-3)  ", "O,Win" },

        { "(any-owner man) (A-1)        ", "X,Win" },
        { "(any-owner man) (A-2)        ", "X,Win" },
        { "(any-owner chip) (A-1)       ", "-,None" },
        { "(any-owner chip) (C-3)       ", "X,Win" },

        { "(not (any-owner man)) (A-1)  ", "-,None" },
        { "(not (any-owner man)) (A-2)  ", "-,None" },
        { "(not (any-owner chip)) (A-1) ", "X,Win" },
        { "(not (any-owner chip)) (C-3) ", "-,None" },

      };
      for (int i = 0; i < matrix.GetLength(0); i++) {
        var tp = String.Format(testprog, matrix[i, 0]);
        var game = PolyGame.CreateInner("bs1", new StringReader(tp));
        game.MakeMove(0);
        var expecteds = matrix[i, 1].SplitXY();
        Assert.AreEqual(expecteds[0], game.ResultPlayer.ToString(), matrix[i, 0]);
        Assert.AreEqual(expecteds[1], game.GameResult.ToString(), matrix[i, 0]);
      }
    }

    [TestMethod]
    public void CondPiecesRemaining() {
      Logger.Open(1);
      var testprog =
        @"(include ""testincl.poly"")" +
        @"(game (gamestub1)" +
        @" (option ""pass turn"" true)" +
        @" (board (boardgrid33))" +
        @" (piece (piece1))" +
        @" (piece (piece2))" +
        @" (board-setup (X (man {0}) (chip {1})) (O (man B-3)) )" +
        @" (win-condition (tot-pce-cond)) )";
      var matrix = new string[,] {
        { "A-1            ,       ", "-,None" },
        { "A-1 B-1        ,       ", "X,Win" },
        { "A-1 B-1 C-1    ,       ", "-,None" },
        { "A-1 B-1 C-1 C-2,       ", "-,None" },
        { "A-1 B-1        ,C-3    ", "-,None" },
        { "A-1            ,C-3 C-2", "X,Win" },
        { "               ,C-3 C-2", "X,Win" },
        { "A-1 B-1 C-1    ,C-3    ", "-,None" },
      };
      for (int i = 0; i < matrix.GetLength(0); i++) {
        var setups = matrix[i, 0].Split(',');
        var tp = String.Format(testprog, setups[0], setups[1]);
        //var tp = String.Format(testprog, matrix[i, 0], matrix[i, 1]);
        var game = PolyGame.CreateInner("bs1", new StringReader(tp));
        game.MakeMove(0);
        var expecteds = matrix[i, 1].SplitXY();
        Assert.AreEqual(expecteds[0], game.ResultPlayer.ToString(), matrix[i, 0]);
        Assert.AreEqual(expecteds[1], game.GameResult.ToString(), matrix[i, 0]);
      }
    }


    [TestMethod]
    public void CondWinLoseDraw() {
      Logger.Open(1);
      var testprog =
        @"(include ""testincl.poly"")" +
        @"(game (gamestub1)" +
        @" (board (boardgrid33))" +
        @" (piece (name man) (drops ((verify empty?) add)))" +
        @" (board-setup (X (man off 2 A-1 A-2 A-3)) (O (man off 1 B-1 B-2 B-3)) )" +
        @"({0})" +
        @")";
      var matrix = new string[,] {
        { "win-condition  (X) (absolute-config man (A-2))", "X,Win" },
        { "loss-condition (X) (absolute-config man (A-2))", "X,Loss" },
        { "draw-condition (X) (absolute-config man (A-2))", "-,Draw" },

        { "win-condition  (O) (absolute-config man (B-2))", "O,Win" },
        { "loss-condition (O) (absolute-config man (B-2))", "O,Loss" },
        { "draw-condition (O) (absolute-config man (B-2))", "-,Draw" },

        { "win-condition  (X) (absolute-config man (C-1))", "X,Win" },
        { "loss-condition (X) (absolute-config man (C-1))", "X,Loss" },
        { "draw-condition (X) (absolute-config man (C-1))", "-,Draw" },

        { "win-condition  (O) (absolute-config man (C-2))", "-,None;O,Win" },
        { "loss-condition (O) (absolute-config man (C-2))", "-,None;O,Loss" },
        { "draw-condition (O) (absolute-config man (C-2))", "-,None;-,Draw" },
        { "win-condition  (X) (absolute-config man (C-3))", "-,None;-,None;X,Win" },
        { "loss-condition (X) (absolute-config man (C-3))", "-,None;-,None;X,Loss" },
        { "draw-condition (X) (absolute-config man (C-3))", "-,None;-,None;-,Draw" },
        { "win-condition  (X) (absolute-config man (B-1))", "-,None;-,None;-,Draw" },
        { "loss-condition (X) (absolute-config man (B-1))", "-,None;-,None;-,Draw" },
        { "draw-condition (X) (absolute-config man (B-1))", "-,None;-,None;-,Draw" },
        { "win-condition  (O) (absolute-config man (A-1))", "-,None;-,None;-,Draw" },
        { "loss-condition (O) (absolute-config man (A-1))", "-,None;-,None;-,Draw" },
        { "draw-condition (O) (absolute-config man (A-1))", "-,None;-,None;-,Draw" },
      };

      for (int i = 0; i < matrix.GetLength(0); i++) {
        var tp = String.Format(testprog, matrix[i, 0]);
        var game = PolyGame.CreateInner("cwld", new StringReader(tp));
        for (int j = 0; game.GameResult == ResultKinds.None; j++) {
          game.MakeMove(0);
          var expecteds = matrix[i, 1].SplitXY(j);
          Assert.AreEqual(expecteds[0], game.ResultPlayer.ToString(), matrix[i, 0]);
          Assert.AreEqual(expecteds[1], game.GameResult.ToString(), matrix[i, 0]);
        }
      }
    }

    [TestMethod]
    public void CondWinLoseDraw45() {
      Logger.Open(1);
      var testprog =
        @"(include ""testincl.poly"")" +
        @"(game (gamestub1)" +
        @" (option ""pass turn"" true)" +
        @" (board (boardgrid45))" +
        @" (piece (piece1))" +
        @" (piece (piece2))" +
        @" (board-setup (X (man off 5 A-1 A-2)) (X (chip off 5 A-3)) (O (chip off 5 C-3)) )" +
        @" ({0})" +
        @")";
      var matrix = new string[,] {
        { "win-condition  (X) (absolute-config man (B-1))", "-,None" },
        { "win-condition  (X) (absolute-config man (A-1))", "X,Win" },
        { "loss-condition (X) (absolute-config man (B-1))", "-,None" },
        { "loss-condition (X) (absolute-config man (A-1))", "X,Loss" },
        { "draw-condition (X) (absolute-config man (B-1))", "-,None" },
        { "draw-condition (X) (absolute-config man (A-1))", "-,Draw" },
        { "win-condition  (X) (absolute-config man (A-1 A-2))", "X,Win" },
        { "win-condition  (X) (absolute-config man (A-1 A-2 A-3))", "-,None" },

        { "win-condition  (X) (pieces-remaining 3 man)", "-,None" },
        { "win-condition  (X) (pieces-remaining 2 man)", "X,Win" },
        { "win-condition  (X) (pieces-remaining 1 man)", "-,None" },

        { "win-condition  (X) (pieces-remaining 3 chip)", "-,None" },
        { "win-condition  (X) (pieces-remaining 2 chip)", "-,None" },
        { "win-condition  (X) (pieces-remaining 1 chip)", "X,Win" },

        { "win-condition  (X) (pieces-remaining 3)", "X,Win" },
        { "win-condition  (X) (pieces-remaining 2)", "-,None" },
        { "win-condition  (X) (pieces-remaining 1)", "-,None" },
        
        { "count-condition (total-piece-count 3)", "-,None" },
        { "count-condition (total-piece-count 4)", "X,Win" },
        { "count-condition (total-piece-count 5)", "-,None" },
        //{ "count-condition stalemated)", "-,Draw" }, // BUG:compile error

      };
      for (int i = 0; i < matrix.GetLength(0); i++) {
        var tp = String.Format(testprog, matrix[i, 0]);
        var game = PolyGame.CreateInner("cwld", new StringReader(tp));
        game.MakeMove(0);
        var expecteds = matrix[i, 1].SplitXY();
        Assert.AreEqual(expecteds[0], game.ResultPlayer.ToString(), matrix[i, 0]);
        Assert.AreEqual(expecteds[1], game.GameResult.ToString(), matrix[i, 0]);
      }
    }

    [TestMethod]
    public void CondPlayer() {
      Logger.Open(1);
      var testprog =
        @"(include ""testincl.poly"")" +
        @"(game (title ""cpl"") (players X O N) (turn-order (X N) (O N))" +
        @" (board (boardgrid33))" +
        @" (piece (name man) (drops ((verify empty?) add)))" +
        @" (board-setup (N (man off 2 A-1 A-2 A-3)) )" +
        @"({0})" +
        @")";
      var matrix = new string[,] {
        // X plays as N at B-1, and then gets the result
        { "win-condition  (N) (absolute-config man (A-2))", "X,Win" },
        { "loss-condition (N) (absolute-config man (A-2))", "X,Loss" },
        { "draw-condition (N) (absolute-config man (A-2))", "-,Draw" },

        // X plays as N at B-1, and then gets the result
        { "win-condition  (N) (absolute-config man (B-1))", "X,Win" },
        { "loss-condition (N) (absolute-config man (B-1))", "X,Loss" },
        { "draw-condition (N) (absolute-config man (B-1))", "-,Draw" },

        // X plays as N at B-1, then O plays as N at B-2 and gets the result
        { "win-condition  (N) (absolute-config man (B-2))", "-,None;O,Win" },
        { "loss-condition (N) (absolute-config man (B-2))", "-,None;O,Loss" },
        { "draw-condition (N) (absolute-config man (B-2))", "-,None;-,Draw" },
      };

      for (int i = 0; i < matrix.GetLength(0); i++) {
        var tp = String.Format(testprog, matrix[i, 0]);
        var game = PolyGame.CreateInner("cwld", new StringReader(tp));
        for (int j = 0; game.GameResult == ResultKinds.None; j++) {
          game.MakeMove(0);
          var expecteds = matrix[i, 1].SplitXY(j);
          Assert.AreEqual(expecteds[0], game.ResultPlayer.ToString(), matrix[i, 0]);
          Assert.AreEqual(expecteds[1], game.GameResult.ToString(), matrix[i, 0]);
        }
      }
    }

    // strategy: eventually both sides run out of N moves and there is a result
    [TestMethod]
    public void CondPostMoveGen() {
      Logger.Open(1);
      var testprog =
        @"(include ""testincl.poly"")" +
        @"(game (gamestub1)" +
        @" (board (boardgrid33))" +
        @" (piece (name man) (moves (n add)) )" +
        @" (board-setup (X (man B-1)) (O (man A-1)) )" +
        @" {0}" +
        @")";


      var matrix = new string[,] {
        { "",                                                     "-,Draw" },
        { "(pass-turn false)",                                    "-,Draw" },
        { "(pass-turn true)",                                     "-,None;-,None;-,None;-,None" },
        { "(pass-turn forced)",                                   "-,None;-,None;-,None;-,None" },
        { "(win-condition (X O) stalemated)",                     "O,Win" },
        { "(pass-turn false) (win-condition (X O) stalemated)",   "O,Win" },
        { "(pass-turn true) (win-condition (X O) stalemated)",    "-,None;-,None;-,None;-,None" },
        { "(pass-turn forced) (win-condition (X O) stalemated)",  "-,None;-,None;-,None;-,None" },
      };
      for (int i = 0; i < matrix.GetLength(0); i++) {
        var tp = String.Format(testprog, matrix[i, 0]);
        var game = PolyGame.CreateInner("cwld", new StringReader(tp));
        for (int j = 0; game.GameResult == ResultKinds.None && j < 4; j++) {
          game.MakeMove(0);
          var expecteds = matrix[i, 1].SplitXY(j);
          Assert.AreEqual(expecteds[0], game.ResultPlayer.ToString(), matrix[i, 0]);
          Assert.AreEqual(expecteds[1], game.GameResult.ToString(), matrix[i, 0]);
        }
      }
    }
  }
}
