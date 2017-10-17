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
      var game = PolyGame.CreateInner("testgame.poly");
      Assert.AreEqual(1, game.Menu.Count());
      Assert.AreEqual("testgame title", game.Title);
      CollectionAssert.AreEquivalent(new string[] { "X", "O" }, game.Players.ToArray());
      Assert.AreEqual("X", game.TurnPlayer);
    }

    [TestMethod]
    public void GameTest2() {
      Logger.Open(1);
      var input = new StringReader(@"(include ""testgame.poly"")");
      var game = PolyGame.CreateInner("test game", input);
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
      var game = PolyGame.CreateInner("testincl1", input);
      Assert.AreEqual(1, game.Menu.Count());
      Assert.AreEqual("testincl game1", game.Title);
      CollectionAssert.AreEquivalent(new string[] { "X", "O" }, game.Players.ToArray());
      Assert.AreEqual("X", game.TurnPlayer);
    }

    // test reverse macro order
    [TestMethod]
    public void GameIncl1R() {
      Logger.Open(1);
      var input = new StringReader(
        @"(game1)" +
        @"(include ""testincl.poly"")");
      var game = PolyGame.CreateInner("testincl1", input);
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
      var game = PolyGame.CreateInner("testincl2", input);
      Assert.AreEqual(1, game.Menu.Count());
      Assert.AreEqual("testincl game2", game.Title);
      CollectionAssert.AreEquivalent(new string[] { "X", "O" }, game.Players.ToArray());
      Assert.AreEqual("X", game.TurnPlayer);
    }

    // test (translate)
    // Just for compile; does nothing yet
    [TestMethod]
    public void GameTranslate() {
      Logger.Open(1);
      var testprog =
        @"(translate" +
        @" (""White"" ""Blanc"")" +
        @" (""Black"" ""Noir"")" +
        @" (""Day"" ""Night"") )" +
        @"(game" +
        @" (title ""trans1"")" +
        @" (players X O)" +
        @" (turn-order X O)" +
        @" (board)" +
        @")";
      var matrix = new string[,] {
        { "", "" }
      };

      for (int i = 0; i < matrix.GetLength(0); i++) {
        var tp = String.Format(testprog, matrix[i, 0]);
        var game = PolyGame.CreateInner("tr1", new StringReader(tp));
        Assert.IsNotNull(game, PolyGame.LastError);
        Assert.AreEqual("trans1", game.Title);
        Assert.AreEqual("O,X", game.Players.OrderBy(s => s).Join());
      }

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
        var game = PolyGame.CreateInner("bs1", new StringReader(tp));

        Assert.AreEqual(1, game.Menu.Count());
        Assert.AreEqual("turn1", game.Title);
        Assert.AreEqual("N,O,X", game.Players.Join());
        Assert.AreEqual("O,X", game.ActivePlayers.Join());
        Assert.AreEqual("X", game.FirstPlayer);
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
        @" (history ""game history"")" +
        @" (piece (name man) (help ""man help"") (moves (n (create man) add)) )" +
        @")(variant (title ""var1"")" +
        @" {0}" +
        @")";
      var matrix = new string[,] {
        { @"(description ""xxx"")",
          @"var1,game history;man,man help" },
        { @"(default)",
          @"var1,game history;man,man help" },
        { @"(history ""var1 history"") (piece (name man) (help ""varman help"") (moves (n (create man) add)) )",
          @"var1,var1 history;man,varman help" },
        { @"(history ""var1 history"") (piece (name man2) (help ""varman2 help"") (moves (n (create man) add)) )",
          @"var1,var1 history;man,man help;man2,varman2 help" },
        { @"(history ""var1 history"") (piece (name man2) (help ""varman2 help"") (moves (n (create man) add)) )" +
                                    @" (piece (name man) (help ""varman help"") (moves (n (create man2) add)) )",
          @"var1,var1 history;man,varman help;man2,varman2 help" },
        { @"(history ""var1 history"") (piece (name man) (help ""varman help"") (moves (n (create man2) add)) )" +
                                    @" (piece (name man2) (help ""varman2 help"") (moves (n (create man) add)) )",
          @"var1,var1 history;man,varman help;man2,varman2 help" },
      };

      for (int i = 0; i < matrix.GetLength(0); i++) {
        var tp = String.Format(testprog, matrix[i, 0]);
        var game = PolyGame.CreateInner("cwld", new StringReader(tp), 1);
        var expected0 = matrix[i, 1].SplitXY(0);
        Assert.AreEqual(expected0[0], game.Title, matrix[i, 0]);
        Assert.AreEqual(expected0[1], game.GetOption("history"), matrix[i, 0]);
        for (var j = 0; ; ++j) {
          var expecteds = matrix[i, 1].SplitXY(j + 1);
          if (expecteds == null) break;
          Assert.AreEqual(expecteds[0], game.Pieces[j].Name, matrix[i, 0]);
          Assert.AreEqual(expecteds[1], game.Pieces[j].Help, matrix[i, 0]);
        }
      }
    }

    [TestMethod]
    public void GameMacro() {
      Logger.Open(1);
      var testprog =
        @"(include ""testincl.poly"")" +
        @"(game (gamestub1) (board) {0}" +
        @"(define mac0 sub1)" +
        @"(define mac01 name sub1)" +
        @"(define macx $1)" +
        @"(define macax sub$1)" +
        @"(define macxa $1sub)" +
        @"(define macaxa sub$1sub)" +
        @"(define macb (piece (name sub1)) )" +
        @"(define macbx (piece (name sub$1)) )" +
        @"(define macxb (piece (name $1sub)) )" +
        @"(define macbxb (piece (name sub$1sub)) )" +
        @"(define macabcd (piece (name sub$1)) (piece (name sub$2)) (piece (name sub$3)) (piece (name sub$4)) )" +
        @"(define macs ""sub$1sub"")" +
        @")";
      var matrix = new string[,] {
        { @"(piece (name sub1))",               @"P,sub1" },
        { @"(piece ((mac01)))",                 @"P,sub1" },
        { @"(piece (name (mac0)))",             @"P,sub1" },
        { @"(piece (name (macx xxx)))",         @"P,xxx" },
        { @"(piece (name (macax xxx)))",        @"P,subxxx" },
        { @"(piece (name (macxa xxx)))",        @"P,xxxsub" },
        { @"(piece (name (macaxa xxx)))",       @"P,subxxxsub" },
        { @"(macb)",                            @"P,sub1" },
        { @"(macbx xxx)",                       @"P,subxxx" },
        { @"(macxb xxx)",                       @"P,xxxsub" },
        { @"(macbxb xxx)",                      @"P,subxxxsub" },
        { @"(piece (name x)(help (macs xxx)))", @"H,subxxxsub" },
        { @"(macabcd x y z w)",                 @"P,subw subx suby subz" },
      };

      for (int i = 0; i < matrix.GetLength(0); i++) {
        var tp = String.Format(testprog, matrix[i, 0]);
        var game = PolyGame.CreateInner("cwld", new StringReader(tp), 0);
        var testkind = matrix[i, 1].SplitXY()[0];
        var expected = matrix[i, 1].SplitXY()[1];
        if (testkind == "P") Assert.AreEqual(expected, game.Pieces.Select(p => p.Name).Join(" "), matrix[i, 0]);
        if (testkind == "H") Assert.AreEqual(expected, game.Pieces.Select(p=>p.Help).Join(" "), matrix[i, 0]);
      }
    }

  }
}
