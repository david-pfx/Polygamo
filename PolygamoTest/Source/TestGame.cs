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
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Polygamo;
using Poly.Common;
using System.IO;

namespace PolygamoTest {
  /// <summary>
  /// Testing base game, include, define and game properties
  /// </summary>
  [TestClass]
  public class TestGame {
    [TestMethod]
    public void GameTest1() {
      Logger.Open(1);
      var game = PolyGame.Create("testgame.poly");
      Assert.AreEqual(1, game.Menu.Count());
      Assert.AreEqual("testgame title", game.Title);
      CollectionAssert.AreEquivalent(new string[] { "X", "O" }, game.Players.ToArray());
      Assert.AreEqual("X", game.TurnPlayer);
    }

    [TestMethod]
    public void GameTest2() {
      Logger.Open(1);
      var input = new StringReader(@"(include ""testgame.poly"")");
      var game = PolyGame.Create("test game", input);
      Assert.AreEqual(1, game.Menu.Count());
      Assert.AreEqual("testgame title", game.Title);
      CollectionAssert.AreEquivalent(new string[] { "X", "O" }, game.Players.ToArray());
      Assert.AreEqual("X", game.TurnPlayer);
    }

    [TestMethod]
    public void GameIncl1() {
      Logger.Open(1);
      var input = new StringReader(
        @"(include ""testincl.poly"")" +
        @"(game1)");
      var game = PolyGame.Create("testincl1", input);
      Assert.AreEqual(1, game.Menu.Count());
      Assert.AreEqual("testincl game1", game.Title);
      CollectionAssert.AreEquivalent(new string[] { "X", "O" }, game.Players.ToArray());
      Assert.AreEqual("X", game.TurnPlayer);
    }

    [TestMethod]
    public void GameIncl2() {
      Logger.Open(1);
      var input = new StringReader(
        @"(include ""testincl.poly"")" +
        @"(game2)");
      var game = PolyGame.Create("testincl2", input);
      Assert.AreEqual(1, game.Menu.Count());
      Assert.AreEqual("testincl game2", game.Title);
      CollectionAssert.AreEquivalent(new string[] { "X", "O" }, game.Players.ToArray());
      Assert.AreEqual("X", game.TurnPlayer);
    }

    [TestMethod]
    public void GameTurn() {
      Logger.Open(1);
      var testprog = 
        @"(game" +
        @"(title ""turn1"")" +
        @"(option ""pass turn"" true)" +
        @"(players X O N)" +
        @"(turn-order {0} )" +
        @"(board)" +
        @")";
      var matrix = new string[,] {
        { "X O",                          "X,X,any;O,O,any;X,X,any;O,O,any;X,X,any;O,O,any" },
        { "X O X",                        "X,X,any;O,O,any;X,X,any;X,X,any;O,O,any;X,X,any" },
        { "X repeat X O X",               "X,X,any;X,X,any;O,O,any;X,X,any;X,X,any;O,O,any" },
        { "X O (X O) (O X) (X N) (O N)",  "X,X,any;O,O,any;X,O,any;O,X,any;X,N,any;O,N,any" },
        { "X O repeat (X MT1) (O MT2)",   "X,X,any;O,O,any;X,X,MT1;O,O,MT2;X,X,MT1;O,O,MT2" },
        { "X O (X O MT1) (O X MT2) (X N MT1) (O N MT2)", "X,X,any;O,O,any;X,O,MT1;O,X,MT2;X,N,MT1;O,N,MT2" }
      };

      for (int i = 0; i < matrix.GetLength(0); i++) {
        var tp = String.Format(testprog, matrix[i, 0]);
        var game = PolyGame.Create("bs1", new StringReader(tp));

        Assert.AreEqual(1, game.Menu.Count());
        Assert.AreEqual("turn1", game.Title);
        Assert.AreEqual("N,O,X", game.Players.OrderBy(s => s).Join());
        Assert.AreEqual("O,X", game.ActivePlayers.OrderBy(s => s).Join());
        var turns = Enumerable.Range(0, 6).Select(x => {
          var ret = game.NextTurn;
          game.MakeMove(0);
          return ret;
        }).Select(t => Util.Join(",", t.TurnPlayer, t.MovePlayer, t.MoveType)).Join(";");
        Assert.AreEqual(matrix[i, 1], turns);
      }

    }

    [TestMethod]
    public void GameVariant() {
      Logger.Open(1);
      var testprog =
        @"(include ""testincl.poly"")" +
        @"(game (gamestub1)" +
        @" (board (boardgrid33))" +
        @" (piece (name man) (drops ((verify empty?) add)))" +
        @" (board-setup (X (man A-1 A-2 A-3)) (O (man B-1 B-2 B-3)) )" +
        @")(variant (title ""var1"")" +
        @"({0})" +
        @")";
      var matrix = new string[,] {
        { @"description ""asdf""", "var1,asdf" },
      };

      for (int i = 0; i < matrix.GetLength(0); i++) {
        var tp = String.Format(testprog, matrix[i, 0]);
        var game = PolyGame.Create("cwld", new StringReader(tp), 1);
        var expecteds = Splitter(matrix[i, 1], 0);
        Assert.AreEqual(expecteds[0], game.Title, matrix[i, 0]);
        Assert.AreEqual(expecteds[1], game.GetOption("description"), matrix[i, 0]);
      }
    }

    string[] Splitter(string target, int index = 0) {
      var parts = target.Split(';');
      return (index < parts.Length) ? parts[index].Split(',') : null;
    }

  }
}
