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
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Polygamo;
using Poly.Common;
using System.IO;

namespace PolygamoTest {
  [TestClass]
  public class TestMoveApply {

    [TestMethod]
    public void DropApply() {
      Logger.Open(1);
      var testprog =
        @"(include ""testincl.poly"")" +
        @"(game (gamestub1)" +
        @" (board (boardgrid45))" +
        @" (board-setup (X (man C-1)))" +
        @" (piece (name man) (drops {0}))" +
        @" (piece (name chip))" +
        @")";

      var matrix = new string[,] {
        { "(add)                          ", "A-1,X,man;C-1,X,man" },
        { "((add chip))                   ", "A-1,X,chip;C-1,X,man" },
        { "((add man chip))               ", "A-1,X,man;C-1,X,man" },

        { "(s s (add chip))               ", "C-1,X,chip" },
        { "(C-1 capture e (add chip))     ", "C-2,X,chip" },

        { "(C-1 flip add)                 ", "C-1,O,man" },
        { "(C-1 flip e (add chip))        ", "C-1,O,man;C-2,X,chip" },

        { "(C-1 change-owner add)         ", "C-1,X,man" },
        { "(C-1 change-owner e (add chip))", "C-1,X,man;C-2,X,chip" },

        { "(C-1 (change-type chip) add)   ", "C-1,X,chip" },
        { "(C-1 (change-type chip) e add) ", "C-1,X,chip;C-2,X,man" },
      };
      for (int i = 0; i < matrix.GetLength(0); i++) {
        var tp = String.Format(testprog, matrix[i, 0]);
        var game = PolyGame.Create("moveapply", new StringReader(tp));
        if (game.LegalMoves.Count > 0) game.MakeMove(0);
        var result = game.PlayedPieces
          .Select(p => Util.Join(",", p.Position, p.Player, p.Piece))
          .OrderBy(s => s).Join(";");
        Assert.AreEqual(matrix[i, 1], result, matrix[i, 0]);
      }
    }

    // generate at least one move of each kind and test that it gets applied correctly
    [TestMethod]
    public void MoveApply() {
      Logger.Open(1);
      var testprog =
        @"(include ""testincl.poly"")" +
        @"(game (gamestub1)" +
        @" (board (boardgrid45))" +
        @" (board-setup (X (man C-1)) (O (man B-1)))" +
        @" (piece (name man) (moves {0}))" +
        @" (piece (name chip))" +
        @")";

      var matrix = new string[,] {
        { "(add)                          ", "B-1,O,man;C-1,X,man" },
        { "((add chip))                   ", "B-1,O,man;C-1,X,chip" },
        { "(e add)                        ", "B-1,O,man;C-2,X,man" },
        { "(e (add chip))                 ", "B-1,O,man;C-2,X,chip" },
        { "(e (add man chip))             ", "B-1,O,man;C-2,X,man" },

        { "(e add-copy)                   ", "B-1,O,man;C-1,X,man;C-2,X,man" },
        { "(e (add-copy chip))            ", "B-1,O,man;C-1,X,man;C-2,X,chip" },
        { "(e (add-copy man chip))        ", "B-1,O,man;C-1,X,man;C-2,X,man" },

        { "(n add)                        ", "B-1,X,man" },
        { "(n capture add)                ", "" },  // capture after add!
        { "(n capture e add)              ", "B-2,X,man" },
        { "((capture n) e add)            ", "C-2,X,man" },

        { "(n change-owner add)           ", "B-1,X,man" },
        { "(n change-owner e add)         ", "B-1,X,man;B-2,X,man" },
        { "((change-owner n) e add)       ", "B-1,X,man;C-2,X,man" },
        { "((change-owner B-1) e add)     ", "B-1,X,man;C-2,X,man" },

        { "(n flip add)                   ", "B-1,X,man" },
        { "(n flip e add)                 ", "B-1,X,man;B-2,X,man" },
        { "((flip n) e add)               ", "B-1,X,man;C-2,X,man" },
        { "((flip B-1) e add)             ", "B-1,X,man;C-2,X,man" },

        { "(n (change-type chip) add)     ", "B-1,X,chip" },    // add then change it
        { "(n (change-type chip) e add)   ", "B-1,O,chip;B-2,X,man" },
        { "((change-type chip n) e add)   ", "B-1,O,chip;C-2,X,man" },
        { "((change-type chip B-1) e add) ", "B-1,O,chip;C-2,X,man" },

      };
      for (int i = 0; i < matrix.GetLength(0); i++) {
        var tp = String.Format(testprog, matrix[i, 0]);
        var game = PolyGame.Create("moveapply", new StringReader(tp));
        if (game.LegalMoves.Count > 0) game.MakeMove(0);
        var result = game.PlayedPieces
          .Select(p => Util.Join(",", p.Position, p.Player, p.Piece))
          .OrderBy(s => s).Join(";");
        Assert.AreEqual(matrix[i, 1], result, matrix[i, 0]);
      }
    }

    // test apply move for piece flags
    [TestMethod]
    public void MoveSetApply() {
      Logger.Open(1);
      var testprog =
        @"(include ""testincl.poly"")" +
        @"(game (gamestub1)" +
        @" (board (boardgrid45))" +
        @" (board-setup (X (man C-1)) (O (man B-1)))" +
        @" (piece (name man) {0})" +
        @")";

      var matrix = new string[,] {
        { "(moves (add))                          ", "B-1,O,man,;C-1,X,man," },
        { "(attribute H1 false) (moves (add))    ", "B-1,O,man,;C-1,X,man," },
        { "(attribute H1 true) (moves (add))     ", "B-1,O,man,H1;C-1,X,man,H1" },
        { "(attribute H1 true) (attribute H2 true) (moves (add))     ", "B-1,O,man,H1 H2;C-1,X,man,H1 H2" },

        { "(attribute H1 false) (moves ((set-attribute H1 true) add)) ", "B-1,O,man,;C-1,X,man,H1" },
        { "(attribute H1 true) (moves ((set-attribute H1 false) add)) ", "B-1,O,man,H1;C-1,X,man," },

        { "(attribute H1 false) (attribute H2 true) (moves ((set-attribute H1 true) add)) ", "B-1,O,man,H2;C-1,X,man,H1 H2" },
        { "(attribute H1 true) (attribute H2 true) (moves ((set-attribute H1 false) add)) ", "B-1,O,man,H1 H2;C-1,X,man,H2" },
      };
      for (int i = 0; i < matrix.GetLength(0); i++) {
        var tp = String.Format(testprog, matrix[i, 0]);
        var game = PolyGame.Create("moveapply", new StringReader(tp));
        if (game.LegalMoves.Count > 0) game.MakeMove(0);
        var result = game.PlayedPieces
          .Select(p => Util.Join(",", p.Position, p.Player, p.Piece, p.Flags))
          .OrderBy(s => s).Join(";");
        Assert.AreEqual(matrix[i, 1], result, matrix[i, 0]);
      }
    }

    // test undo and redo
    [TestMethod]
    public void MoveUndoRedo() {
      Logger.Open(1);
      var testprog =
        @"(include ""testincl.poly"")" +
        @"(game (gamestub1)" +
        @" (board (boardgrid45))" +
        @" (board-setup (X (man C-1)) (O (man B-1)))" +
        @" (piece (name man) (drops ((verify empty?) add)))" +
        @")";

      var matrix = new string[,] {
        { "",         "X;B-1,O,man,;C-1,X,man," },
        { "M",        "O;A-1,X,man,;B-1,O,man,;C-1,X,man," },
        { "MM",       "X;A-1,X,man,;A-2,O,man,;B-1,O,man,;C-1,X,man," },
        { "MMM",      "O;A-1,X,man,;A-2,O,man,;A-3,X,man,;B-1,O,man,;C-1,X,man," },

        { "MU",       "X;B-1,O,man,;C-1,X,man," },
        { "MMUU",     "X;B-1,O,man,;C-1,X,man," },
        { "MUMU",     "X;B-1,O,man,;C-1,X,man," },

        { "MUMUM",    "O;A-1,X,man,;B-1,O,man,;C-1,X,man," },
        { "MMU",      "O;A-1,X,man,;B-1,O,man,;C-1,X,man," },
        { "MMMUUUM",  "O;A-1,X,man,;B-1,O,man,;C-1,X,man," },

        { "MUR",      "O;A-1,X,man,;B-1,O,man,;C-1,X,man," },
        { "MMUURR",   "X;A-1,X,man,;A-2,O,man,;B-1,O,man,;C-1,X,man," },
        { "MMMUUURRR","O;A-1,X,man,;A-2,O,man,;A-3,X,man,;B-1,O,man,;C-1,X,man," },

        { "MMUMURS",  "X;B-1,O,man,;C-1,X,man," },
        { "MMUMURSM", "O;A-1,X,man,;B-1,O,man,;C-1,X,man," },

        { "MMUUU",    "X;B-1,O,man,;C-1,X,man," },
        { "MMURRUU",  "X;B-1,O,man,;C-1,X,man," },
        { "MMSMUU",   "X;B-1,O,man,;C-1,X,man," },
      };
      var action = new Dictionary<char, Action<PolyGame>> {
        { 'M', g=>g.MakeMove(0) },
        { 'U', g=>g.UndoMove() },
        { 'R', g=>g.RedoMove() },
        { 'S', g=>g.Restart() },
      };
      for (int i = 0; i < matrix.GetLength(0); i++) {
        var game = PolyGame.Create("undoredo", new StringReader(testprog));
        foreach (var ch in matrix[i, 0])
          action[ch](game);
        var result = game.TurnPlayer + ";" + game.PlayedPieces
          .Select(p => Util.Join(",", p.Position, p.Player, p.Piece, p.Flags))
          .OrderBy(s => s).Join(";");
        Assert.AreEqual(matrix[i, 1], result, matrix[i, 0]);
      }
    }

    // test player to move and history
    [TestMethod]
    public void MovesPlayed() {
      Logger.Open(1);
      var testprog =
        @"(include ""testincl.poly"")" +
        @"(game (gamestub1)" +
        @" (board (boardgrid45))" +
        @" (board-setup (X (man C-1)) (O (man B-1)))" +
        @" (piece (name man) (drops ((verify empty?) add)))" +
        @")";

      var matrix = new string[,] {
        { "",          "X;" },
        { "M",         "O;X" },
        { "MM",        "X;X,O" },
        { "MMM",       "O;X,O,X" },
        { "MMMM",      "X;X,O,X,O" },
        { "MMMMU",     "O;X,O,X" },
        { "MMMMUU",    "X;X,O" },
        { "MMMMUUU",   "O;X" },
        { "MMMMUUUU",  "X;" },

        { "MMS",       "X;" },
        { "MMSU",      "X;" },
        { "MMUUU",     "X;" },
        { "MUUSRM",    "O;X" },
      };
      var action = new Dictionary<char, Action<PolyGame>> {
        { 'M', g=>g.MakeMove(0) },
        { 'U', g=>g.UndoMove() },
        { 'R', g=>g.RedoMove() },
        { 'S', g=>g.Restart() },
      };
      for (int i = 0; i < matrix.GetLength(0); i++) {
        var game = PolyGame.Create("played", new StringReader(testprog));
        foreach (var ch in matrix[i, 0])
          action[ch](game);
        var result = game.TurnPlayer + ";" + game.MovesPlayed
          .Select(m => m.Player).Join(",");
        Assert.AreEqual(matrix[i, 1], result, matrix[i, 0]);
      }
    }
  }
}
