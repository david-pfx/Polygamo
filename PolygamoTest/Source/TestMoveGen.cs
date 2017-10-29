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
  [TestClass]
  public class TestMoveGen {

    /// <summary>
    /// Most basic smoke test
    /// </summary>
    [TestMethod]
    public void DropsBasic() {
      Logger.Open(1);
      var input = new StringReader(
        @"(include ""testincl.poly"")" +
        @"(game (gamestub1)" +
        @" (board (boardgrid33))" +
        @" (piece (piecedrops1))" +
        @" (board-setup (X(man A-1 B-1 off 5 C-1)) )" +
        @")");
      var game = PolyGame.CreateInner("bs1", input);

      var expstring = "Drop,X,man,A-1;Drop,X,man,A-2;Drop,X,man,A-3;Drop,X,man,B-1;Drop,X,man,B-2;Drop,X,man,B-3";
      var actstring = game.LegalMoveParts
        .Select(m => Util.Join(",", m.Kind, m.Player, m.Piece, m.Position)).Join(";");
      Assert.AreEqual(expstring, actstring);
    }

    /// <summary>
    /// Test all kinds of (if...)
    /// </summary>
    [TestMethod]
    public void DropIf() {
      Logger.Open(1);
      var testprog =
        @"(include ""testincl.poly"")" +
        @"(game (gamestub1)" +
        @"(board (boardgrid45))" +
        @"(piece (piece1))" +
        @"(piece (piece2))" +
        @"(board-setup (X (man C-1) (xx off 1)) (O (man A-3)) (N (chip D-1)) )" +
        @"(piece (name xx) (drops {0}))" +
        @")";

      var matrix = new string[,] {
        // position conditions
        // no args
        { "((if adjacent-to-enemy? add))", "A-2 A-4 B-2 B-3 B-4" },
        //{ "((if (attacked?) e add))", "" },
        //{ "((if (defended?) e add))", "" },
        { "((if not-empty? e add))      ", "A-4 C-2 D-2" },
        { "((if friend? e add))         ", "C-2" },
        //{ "((if goal-position? e add))  ", "C-2" },
        { "((if enemy? e add))          ", "A-4" },
        //{ "((if (last-from?) e add))", "" },
        //{ "((if (last-to?) e add))", "" },
        //{ "((if marked? e add))        ", "D-2" },
        { "((if neutral? e add))        ", "D-2" },

        // with args
        { "((if (adjacent-to-enemy? w) add))", "A-3 A-5 B-3 B-4 B-5" },
        { "((if (piece? chip) e add))   ", "D-2" },
        { "((if (position? B-4) e add)) ", "B-5" },
        { "((if (not-empty? w) add))    ", "A-4 C-2 D-2" }, // assume off board is empty
        { "((if (friend? w) add))       ", "C-2" },
        { "((if (enemy? w) add))        ", "A-4" },
        { "((if (neutral? w) add))      ", "D-2" },
        { "((if (not-on-board? e) add)) ", "A-5 B-5 C-5 D-5" },
        { "((if (piece? chip w) add))   ", "D-2" },
        { "((if (position? B-4 w) add)) ", "B-5" },
        { "((if (in-zone? endz) e add)) ", "D-2 D-3 D-4 D-5" },

        // flags
        { "((set-flag          flg (position? B-4)) (if (flag?           flg) w add))", "B-3" },
        { "((set-flag          A-1 (position? B-4)) (if (flag?           A-1) w add))", "B-3" }, // clash?
        { "((set-flag          flg (position? B-4)) w (if (flag?         flg)   add))", "B-3" },
        { "((set-flag          flg (not-position? B-4)) (if (not-flag?   flg) w add))", "B-3" },
        { "((set-flag          flg (not-position? B-4)) w (if (not-flag? flg)   add))", "B-3" },

        { "((set-position-flag flg (position? B-4))   (if (position-flag? flg)   w add))", "B-3" },
        { "((set-position-flag A-1 (position? B-4))   (if (position-flag? A-1)   w add))", "B-3" }, // clash?
        { "((set-position-flag flg (position? B-4)) w (if (position-flag? flg e)   add))", "B-3" },
        { "((set-position-flag flg true B-4       )   (if (position-flag? flg)   w add))", "B-3" },
        { "((set-position-flag flg true B-4       )   (if (position-flag? flg e)   add))", "B-3" },
        { "((set-position-flag flg true e         )   (if (position-flag? flg B-4) add))", "B-3" },
        { "((set-position-flag flg (not-position? B-4))   (if (not-position-flag? flg)   w add))", "B-3" },
        { "((set-position-flag flg (not-position? B-4)) s (if (not-position-flag? flg n) w n add))", "B-3" },
        { "((set-position-flag flg (not-position? B-4)) w (if (not-position-flag? flg e)   add))", "B-3" },
      };
      for (int i = 0; i < matrix.GetLength(0); i++) {
        var tp = String.Format(testprog, matrix[i, 0]);
        var game = PolyGame.CreateInner("bs1", new StringReader(tp));

        Assert.IsTrue(game.LegalMoveParts.All(m => m.Kind == MoveKinds.Drop));
        Assert.IsTrue(game.LegalMoveParts.All(m => m.Piece == "xx"));
        Assert.IsTrue(game.LegalMoveParts.All(m => m.Player == "X"));
        var movestring = game.LegalMoveParts.Select(m => m.Position).OrderBy(m => m).Join(" ");
        Assert.AreEqual(matrix[i, 1], movestring, matrix[i, 0]);
      }

    }

    /// <summary>
    /// Test if, else, and, or
    /// </summary>
    [TestMethod]
    public void DropControl() {
      Logger.Open(1);
      var testprog =
        @"(include ""testincl.poly"")" +
        @"(game (gamestub1)" +
        @"(board (boardgrid45))" +
        @"(piece (piece1))" +
        @"(piece (piece2))" +
        @"(board-setup (X (man C-1) (xx off 1)) (O (man A-3) (xx off 1)) (N (chip D-1) (xx off 1)) )" +
        @"(piece (name xx) (drops {0}))" +
        @")";

      var matrix = new string[,] {
        { "((if empty? else e add))                     ", "A-4 C-2 D-2" },
        { "((if friend? (while true e add)))            ", "C-2 C-3 C-4 C-5" },
        // or
        { "((if (or (piece? man) (piece? chip)) e add)) ", "A-4 C-2 D-2" },
        { "((if (or (piece? man w) (piece? chip w)) add))", "A-4 C-2 D-2" },
        // and
        { "((if (and not-empty? (not-empty? s)) e add)) ", "C-2" },
        { "((if (and true  (position? B-4)) add))       ", "B-4" },
        { "((if (and false (position? B-4)) add))       ", "" },
        { "((if (and (not true)  (position? B-4)) add)) ", "" },
        { "((if (and (not false) (position? B-4)) add)) ", "B-4" },
        // move flag
        { "((set-flag flg true)  (if (and (flag? flg) (position? B-4)) add))    ", "B-4" },
        { "((set-flag flg false) (if (and (flag? flg) (position? B-4)) add))    ", "" },
        { "((set-flag flg true)  (if (and (not-flag? flg) (position? B-4)) add))", "" },
        { "((set-flag flg false) (if (and (not-flag? flg) (position? B-4)) add))", "B-4" },
        // verify
        { "((verify friend?) e add)                     ", "C-2" },
        { "((verify false) e add)                       ", "" },
        { "((verify (not-on-board? e)) add)             ", "A-5 B-5 C-5 D-5" },

      };
      for (int i = 0; i < matrix.GetLength(0); i++) {
        var tp = String.Format(testprog, matrix[i, 0]);
        var game = PolyGame.CreateInner("bs1", new StringReader(tp));

        Assert.IsTrue(game.LegalMoveParts.All(m => m.Kind == MoveKinds.Drop));
        Assert.IsTrue(game.LegalMoveParts.All(m => m.Piece == "xx"));
        Assert.IsTrue(game.LegalMoveParts.All(m => m.Player == "X"));
        var movestring = game.LegalMoveParts.Select(m => m.Position).OrderBy(m => m).Join(" ");
        Assert.AreEqual(matrix[i, 1], movestring, matrix[i, 0]);
      }
    }

    [TestMethod]
    public void DropPosOrZone() {
      Logger.Open(1);
      var testprog =
        @"(include ""testincl.poly"")" +
        @"(game (gamestub1)" +
        @"(board (boardgrid45))" +
        @"(piece (piece1))" +
        @"(piece (piece2))" +
        @"(board-setup (X (man C-1) (xx off 1)) (O (man A-3)) (N (chip D-1)) )" +
        @"(piece (name xx) (drops {0}))" +
        @")";

      var matrix = new string[,] {
        { "(B-4 add)",                "B-4" },
        { "(B-4 add e add)",          "B-4,B-5" },
        { "(B-4 add B-5 add)",        "B-4,B-5" },
        { "(B-4 add e e add)",        "B-4" },
        { "(B-4 s add)",              "C-4" },
        { "(B-4 C-4 add)",            "C-4" },
        { "(B-4 add)(C-4 add)",       "B-4,C-4" },
        { "(C-4 add)(B-4 add)",       "B-4,C-4" },

        { "(B-2 add e e (opposite e) add)", "B-2,B-3" },
        { "(B-2 add e (opposite n) add)",   "B-2,C-3" },

        { "(endz add)",               "D-1,D-2,D-3,D-4,D-5" },
        { "(endz e add)",             "D-2,D-3,D-4,D-5" },
        { "(endz add)(B-4 add)",      "B-4,D-1,D-2,D-3,D-4,D-5" },
        { "(B-4 add)(endz add)",      "B-4,D-1,D-2,D-3,D-4,D-5" },
      };
      for (int i = 0; i < matrix.GetLength(0); i++) {
        var tp = String.Format(testprog, matrix[i, 0]);
        var game = PolyGame.CreateInner("dporz", new StringReader(tp));

        Assert.IsTrue(game.LegalMoveParts.All(m => m.Kind == MoveKinds.Drop));
        Assert.IsTrue(game.LegalMoveParts.All(m => m.Piece == "xx"));
        Assert.IsTrue(game.LegalMoveParts.All(m => m.Player == "X"));
        var movestring = game.LegalMoveParts.Select(m => m.Position).OrderBy(m => m).Join(",");
        Assert.AreEqual(matrix[i, 1], movestring, matrix[i, 0]);
      }
    }

    /// <summary>
    /// Test move state cursor manipulation: goto, mark, directions, etc
    /// </summary>
    [TestMethod]
    public void MoveSetState() {
      Logger.Open(1);
      var testprog =
        @"(include ""testincl.poly"")" +
        @"(game (gamestub1)" +
        @" (board (boardgrid45))" +
        @" (board-setup (X (man C-1)) (O (man A-3)) (N (chip D-1)) )" +
        @" (piece (name man) (moves {0}))" +
        @" (piece (name chip))" +
        @")";

      var matrix = new string[,] {
        { "(e add)                        ", "Move,man,X,C-1,C-2" },
        { "(e n e s w add)                ", "Move,man,X,C-1,C-2" },
        // mark back
        { "(e mark n n back add)          ", "Move,man,X,C-1,C-2" },
        { "(e mark n n (go mark) add)     ", "Move,man,X,C-1,C-2" },
        { "(e (mark n) e back s add)      ", "Move,man,X,C-1,C-2" },
        { "(e (mark n) e (go mark) s add) ", "Move,man,X,C-1,C-2" },
        { "(e (mark C-1) e back e add)    ", "Move,man,X,C-1,C-2" },
        { "(e (mark C-1) e (go mark) e add)","Move,man,X,C-1,C-2" },
        // from
        { "(e e (go from) e add)          ", "Move,man,X,C-1,C-2" },
        { "(e from to s from w n from add)", "Move,man,X,C-1,C-2" },
        { "(n from to s add)              ", "" },
        // to
        { "(e to e e add)                 ", "Move,man,X,C-1,C-2" },
        { "((go to) e add)                ", "Move,man,X,C-1,C-2" },  // to not set defaults to current
        { "(e (go to) add)                ", "Move,man,X,C-1,C-2" },  // to not set defaults to current
        { "(e to n (go to) add)           ", "Move,man,X,C-1,C-2" },
        { "(e e to n (go to) w to add)    ", "Move,man,X,C-1,C-2" },
      };
      for (int i = 0; i < matrix.GetLength(0); i++) {
        var tp = String.Format(testprog, matrix[i, 0]);
        var game = PolyGame.CreateInner("dporz", new StringReader(tp));

        var movestring = game.LegalMoveParts
          .Select(m => Util.Join(",", m.Kind, m.Piece, m.Player, m.Position, m.Final ?? "")).Join(";");
        Assert.AreEqual(matrix[i, 1], movestring, matrix[i, 0]);
      }
    }

    /// <summary>
    /// Test add and variations
    /// </summary>
    [TestMethod]
    public void MoveAdd() {
      Logger.Open(1);
      var testprog =
        @"(include ""testincl.poly"")" +
        @"(game (gamestub1)" +
        @" (board (boardgrid45))" +
        @" (board-setup (X (man off 1 C-1) (chip off 1)) (O (man off 1 C-3) (chip off 1)) (N (chip D-1)) )" +
        @" (piece (name man) (moves {0}))" +
        @" (piece (name chip))" +
        @")";

      var matrix = new string[,] {
        { "(e add)                      ", "Move,man,X,C-1,C-2" },
        { "(e (add chip))               ", "Move,chip,X,C-1,C-2" },
        { "(e (add man chip))           ", "Move,man,X,C-1,C-2;Move,chip,X,C-1,C-2" },
        { "(B-1 add)                    ", "Move,man,X,C-1,B-1" },
                                        
        { "(e add-copy)                 ", "Copy,man,X,C-1,C-2" },
        { "(e (add-copy chip))          ", "Copy,chip,X,C-1,C-2" },
        { "(e (add-copy man chip))      ", "Copy,man,X,C-1,C-2;Copy,chip,X,C-1,C-2" },
        { "(B-1 add-copy)               ", "Copy,man,X,C-1,B-1" },

        { "(s create n e add)           ", "Move,man,X,C-1,C-2;Drop,man,X,D-1," },
        { "(s (create O) n e add)       ", "Move,man,X,C-1,C-2;Drop,man,O,D-1," },
        { "(s (create chip) n e add)    ", "Move,man,X,C-1,C-2;Drop,chip,X,D-1," },
        { "(s (create O chip) n e add)  ", "Move,man,X,C-1,C-2;Drop,chip,O,D-1," },
        { "(s (create D-2) n e add)     ", "Move,man,X,C-1,C-2;Drop,man,X,D-2," },
        { "(s (create e) n e add)       ", "Move,man,X,C-1,C-2;Drop,man,X,D-2," },
        { "(s (create chip e) n e add)  ", "Move,man,X,C-1,C-2;Drop,chip,X,D-2," },
        { "(s (create O e) n e add)     ", "Move,man,X,C-1,C-2;Drop,man,O,D-2," },
        { "(s (create O chip e) n e add)", "Move,man,X,C-1,C-2;Drop,chip,O,D-2," },
        { "(B-1 create n e add)         ", "Move,man,X,C-1,A-2;Drop,man,X,B-1," },

        { "(e capture add)            ", "Move,man,X,C-1,C-2;Take,man,X,C-2," },
        { "(e e capture w add)        ", "Move,man,X,C-1,C-2;Take,man,X,C-3," },
        { "(e e e capture w w add)    ", "Move,man,X,C-1,C-2;Take,man,X,C-4," },
        { "(e (capture n) add)        ", "Move,man,X,C-1,C-2;Take,man,X,B-2," },
        { "(e (capture e) add)        ", "Move,man,X,C-1,C-2;Take,man,X,C-3," },
        { "((capture C-2) e add)      ", "Move,man,X,C-1,C-2;Take,man,X,C-2," },
        { "((capture C-3) e add)      ", "Move,man,X,C-1,C-2;Take,man,X,C-3," },
        { "(B-1 capture add)          ", "Move,man,X,C-1,B-1;Take,man,X,B-1," },
      };
      for (int i = 0; i < matrix.GetLength(0); i++) {
        var tp = String.Format(testprog, matrix[i, 0]);
        var game = PolyGame.CreateInner("movadd", new StringReader(tp));

        var result = game.LegalMoveParts
          .Select(m => Util.Join(",", m.Kind, m.Piece, m.Player, m.Position, m.Final ?? "")).Join(";");
        Assert.AreEqual(matrix[i, 1], result, matrix[i, 0]);
      }
    }

    /// <summary>
    /// Test change owner and flip
    /// </summary>
    [TestMethod]
    public void MoveChange() {
      Logger.Open(1);
      var testprog =
        @"(include ""testincl.poly"")" +
        @"(game (gamestub1)" +
        @" (board (boardgrid45))" +
        @" (board-setup (X (man C-1)) (O (man C-3)) (N (chip D-1)) )" +
        @" (piece (name man) (moves {0}))" +
        @" (piece (name chip))" +
        @")";

      var matrix = new string[,] {
        { "(e change-owner add)       ", "Move,man,X,C-1,C-2;Owner,man,X,C-2," },
        { "(s change-owner n e add)   ", "Move,man,X,C-1,C-2;Owner,man,X,D-1," },
        { "((change-owner n) e add)   ", "Move,man,X,C-1,C-2;Owner,man,X,B-1," },
        { "((change-owner s) e add)   ", "Move,man,X,C-1,C-2;Owner,man,X,D-1," },
        { "((change-owner D-2) e add) ", "Move,man,X,C-1,C-2;Owner,man,X,D-2," },
        { "((change-owner D-1) e add) ", "Move,man,X,C-1,C-2;Owner,man,X,D-1," },

        { "(e e change-owner e add)   ", "Move,man,X,C-1,C-4;Owner,man,X,C-3," },
        { "(s change-owner e add)     ", "Move,man,X,C-1,D-2;Owner,man,X,D-1," },

        { "(e flip add)               ", "Move,man,X,C-1,C-2;Owner,man,O,C-2," },
        { "(flip add)                 ", "Owner,man,O,C-1," },
        { "(flip e add)               ", "Move,man,X,C-1,C-2;Owner,man,O,C-1," }, // CHECK:???
        { "(s flip n e add)           ", "Move,man,X,C-1,C-2;Owner,man,O,D-1," },
        { "((flip n) e add)           ", "Move,man,X,C-1,C-2;Owner,man,O,B-1," },
        { "((flip s) e add)           ", "Move,man,X,C-1,C-2;Owner,man,O,D-1," },
        { "((flip D-2) e add)         ", "Move,man,X,C-1,C-2;Owner,man,O,D-2," },
        { "((flip D-1) e add)         ", "Move,man,X,C-1,C-2;Owner,man,O,D-1," },
        { "(s flip n e add)           ", "Move,man,X,C-1,C-2;Owner,man,O,D-1," },

        { "(e e flip e add)           ", "Move,man,X,C-1,C-4;Owner,man,O,C-3," },
        { "(s flip e add)             ", "Move,man,X,C-1,D-2;Owner,man,O,D-1," },
      };
      for (int i = 0; i < matrix.GetLength(0); i++) {
        var tp = String.Format(testprog, matrix[i, 0]);
        var game = PolyGame.CreateInner("movadd", new StringReader(tp));

        var result = game.LegalMoveParts
          .Select(m => Util.Join(",", m.Kind, m.Piece, m.Player, m.Position, m.Final ?? "")).Join(";");
        Assert.AreEqual(matrix[i, 1], result, matrix[i, 0]);
      }
    }

    /// <summary>
    /// Test setting attribute
    /// </summary>
    [TestMethod]
    public void MoveAttrib() {
      Logger.Open(1);
      var testprog =
        @"(include ""testincl.poly"")" +
        @"(game (gamestub1)" +
        @" (board (boardgrid45))" +
        @" (board-setup (X (man C-1)) (O (man C-3)) )" +
        @" (piece (name man) {0})" +
        @" (piece (name chip))" +
        @")";

      var matrix = new string[,] {
        { "(attribute H1 true)  (moves (add))",                                 "" },
        { "(attribute H1 true)  (moves (e add))",                               "Move,man,X,C-1,C-2," },
        { "(attribute H1 true)  (moves ((set-attribute H1 false) add))",        "Attrib,man,X,C-1,null,H1 false" },

        { "(attribute H1 false) (moves ((verify H1) e add))",                   "" },
        { "(attribute H1 false) (moves ((verify (or H1 (empty? e))) e add))",   "Move,man,X,C-1,C-2," },
        { "(attribute H1 true)  (moves ((verify H1) e add))",                   "Move,man,X,C-1,C-2," },
        { "(attribute H1 true)  (moves ((verify (and H1 empty?)) e add))",      "" },

        { "(attribute H1 false) (moves ((if H1 e add)))",                       "" },
        { "(attribute H1 true)  (moves ((if H1 e add)))",                       "Move,man,X,C-1,C-2," },

        { "(attribute H1 false) (moves (e (if (H1 w) add)))",                   "" },
        { "(attribute H1 true)  (moves (e (if (H1 w) add)))",                   "Move,man,X,C-1,C-2," },
        { "(attribute H1 false) (moves (e (if (H1 C-1) add)))",                 "" },
        { "(attribute H1 true)  (moves (e (if (H1 C-1) add)))",                 "Move,man,X,C-1,C-2," },
      };
      for (int i = 0; i < matrix.GetLength(0); i++) {
        var tp = String.Format(testprog, matrix[i, 0]);
        var game = PolyGame.CreateInner("movadd", new StringReader(tp));

        var result = game.LegalMoveParts
          .Select(m => Util.Join(",", m.Kind, m.Piece, m.Player, m.Position, m.Final, 
            m.Attribute == null ? "" : m.Attribute + " " + m.Value.ToString().ToLower())).Join(";");
        Assert.AreEqual(matrix[i, 1], result, matrix[i, 0]);
      }
    }

    /// <summary>
    /// Test move turn-order
    /// </summary>
    [TestMethod]
    public void MoveTurnOrder() {
      Logger.Open(1);
      var testprog =
        @"(include ""testincl.poly"")" +
        @"(game (title ""moveto"")" +
        @" (players X O N)" + 
        @" (turn-order (X N) (O N))" +
        @" (board (boardgrid33))" +
        @" (board-setup (X (man C-1)) (O (man C-3)) (N (man off 1)))" +
        @" (piece (name man) {0})" +
        @")";

      var matrix = new string[,] {
        { "(drops ((verify empty?) add))", "A-1,A-2,A-3,B-1,B-2,B-3,C-2" },
      };
      for (int i = 0; i < matrix.GetLength(0); i++) {
        var tp = String.Format(testprog, matrix[i, 0]);
        var game = PolyGame.CreateInner("movto", new StringReader(tp));

        Assert.IsTrue(game.LegalMoveParts.All(m => m.Kind == MoveKinds.Drop));
        Assert.IsTrue(game.LegalMoveParts.All(m => m.Piece == "man"));
        Assert.IsTrue(game.LegalMoveParts.All(m => m.Player == "N"));

        Assert.IsTrue(game.LegalMoves.All(m => m.Piece1 == "man"));
        Assert.IsTrue(game.LegalMoves.All(m => m.Piece2 == ""));
        Assert.IsTrue(game.LegalMoves.All(m => m.Position2 == ""));
        Assert.IsTrue(game.LegalMoves.All(m => m.Player == "N"));

        var positions = matrix[i, 1].Split(',');
        for (int j = 0; j < positions.Length; j++) {
          Assert.AreEqual(positions[j], game.LegalMoveParts[j].Position);
          Assert.AreEqual(j, game.LegalMoves[j].Index);
          Assert.AreEqual(positions[j], game.LegalMoves[j].Position1);
        }
        Assert.IsTrue(game.LegalMoves.All(m => positions[m.Index] == m.Position1));
      }
    }

    /// <summary>
    /// Test move move-type
    /// </summary>
    [TestMethod]
    public void MoveMoveType() {
      Logger.Open(1);
      var testprog =
        @"(include ""testincl.poly"")" +
        @"(game (title ""movetype"")" +
        @" (players X O)" +
        @" (turn-order {0})" +
        @" (board (boardgrid33))" +
        @" (board-setup (X (man A-1)) )" +
        @" (piece (name man) (moves {1}) )" +
        @")";

      var matrix = new string[,] {
        { "X; (s add)",                                         "X A-1 B-1" },
        { "X; (s add) (se add)",                                "X A-1 B-1; X A-1 B-2" },
        { "X;      (s add) (move-type mt) (se add)",            "X A-1 B-1; X A-1 B-2" },
        { "X;      (s add) (s s add) (move-type mt) (se add)",  "X A-1 B-1; X A-1 B-2; X A-1 C-1" },
        { "X;      (s add) (move-type mt) (s s add) (se add)",  "X A-1 B-1; X A-1 B-2; X A-1 C-1" },
        // these test correct if move-type Any includes all others
        { "(X mt); (s add) (move-type mt) (se add)",            "X A-1 B-2" },
        { "(X mt); (s add) (s s add) (move-type mt) (se add)",  "X A-1 B-2" },
        { "(X mt); (s add) (move-type mt) (s s add) (se add)",  "X A-1 B-2; X A-1 C-1" },
        // these test correct if move-type Any does NOT include all others
        //{ "X;      (s add) (move-type mt) (se add)",            "X A-1 B-1" },
        //{ "X;      (s add) (s s add) (move-type mt) (se add)",  "X A-1 B-1; X A-1 C-1" },
        //{ "X;      (s add) (move-type mt) (s s add) (se add)",  "X A-1 B-1" },
      };
      for (int i = 0; i < matrix.GetLength(0); i++) {
        var frags = matrix[i, 0].Split(";");
        var script = String.Format(testprog, frags[0], frags[1]);
        var game = PolyGame.CreateInner("movty", new StringReader(script));

        Assert.IsTrue(game.LegalMoveParts.All(m => m.Kind == MoveKinds.Move));
        Assert.IsTrue(game.LegalMoveParts.All(m => m.Piece == "man"));
        var actuals = game.LegalMoves
          .Select(m => String.Join(" ", m.Player, m.Position1, m.Position2))
          .OrderBy(s => s)
          .Join("; ");
        Assert.AreEqual(matrix[i, 1], actuals, matrix[i, 0]);
        //var splits = matrix[i, 1].Split(";");
        //Assert.AreEqual(splits.Length, game.LegalMoves.Count, matrix[i,0]);
        //for (int j = 0; j < splits.Length; j++) {
        //  Assert.IsTrue(j < game.LegalMoves.Count, splits[j]);
        //  var move = game.LegalMoves[j];
        //  Assert.AreEqual(splits[j], String.Join(" ", move.Player, move.Position1, move.Position2), matrix[i, 0]);
        //}
      }
    }

    [TestMethod]
    public void DropOffStore() {
      Logger.Open(1);
      var testprog =
        @"(include ""testincl.poly"")" +
        @"(game (title ""dropos"")" +
        @" (players X)" +
        @" (turn-order X)" +
        @" (board (boardgrid33))" +
        @" (board-setup (X (man off 2)) )" +
        @" (piece (name man) {0})" +
        @")";

      var matrix = new string[,] {
        { "(drops (e e (verify empty?) add))", "A-3,B-3,C-3; B-3,C-3; " },
      };
      for (int i = 0; i < matrix.GetLength(0); i++) {
        var tp = String.Format(testprog, matrix[i, 0]);
        var game = PolyGame.CreateInner("dropoff", new StringReader(tp));

        var expects = matrix[i, 1].Split(";");
        for (int j = 0; j < expects.Length; j++) {
          foreach (var m in game.LegalMoveParts)
            Assert.AreEqual("Drop,man,X", Util.Join(",", m.Kind, m.Piece, m.Player), matrix[i, 0]);
          foreach (var m in game.LegalMoves)
            Assert.AreEqual("man,,,X", Util.Join(",", m.Piece1, m.Piece2, m.Position2, m.Player), matrix[i, 0]);
          Assert.AreEqual(expects[j], game.LegalMoves.Select(m => m.Position1).Join(","), matrix[i, 0]);

          game.MakeMove(0);
        }
      }
    }

  }
}
