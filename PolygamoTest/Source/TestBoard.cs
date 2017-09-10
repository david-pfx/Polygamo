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
/// Part of the test suite
///
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Polygamo;
using Poly.Common;

namespace PolygamoTest {
  /// <summary>
  /// Test static board, positions, grid, zones, pieces
  /// </summary>
  [TestClass]
  public class TestBoard {
    [TestMethod]
    public void BoardTest1() {
      Logger.Open(1);
      var input = new StringReader(
        @"(include ""testincl.poly"")" +
        @"(gameb1)");
      var game = PolyGame.Create("bt1", input);
      Assert.AreEqual(1, game.Menu.Count());
      Assert.AreEqual("board1", game.Title);
      Assert.AreEqual("board1.png", game.BoardImages[0]);

      var posnames = "ABCD".SelectMany(c => "12345".Select(r => String.Format("{0}-{1}", c, r))).ToArray();
      CollectionAssert.AreEqual(posnames, game.Positions.Select(p=>p.Name).ToArray());
      var polypos = game.Positions.ToDictionary(p => p.Name);
      Assert.AreEqual(Rect.Create(16, 16, 112, 112), polypos["A-1"].Location);
      Assert.AreEqual(Rect.Create(16+112, 16, 112+112, 112), polypos["A-2"].Location);
      Assert.AreEqual(Rect.Create(16, 16+112, 112, 112+112), polypos["B-1"].Location);
      Assert.AreEqual(Rect.Create(16+112, 16+112, 112+112, 112+112), polypos["B-2"].Location);
    }

    [TestMethod]
    public void BoardLink1() {
      Logger.Open(1);
      var input = new StringReader(
        @"(include ""testincl.poly"")" +
        @"(game (gamestub1)" +
        @"(board (boardlink1)) )");
      var game = PolyGame.Create("bt2", input);
      Assert.AreEqual(1, game.Menu.Count());
      Assert.AreEqual("stub1", game.Title);
      Assert.AreEqual("board1.png", game.BoardImages[0]);

      var positions = new PolyPosition[] {
        new PolyPosition { Name = "a1", Location = Rect.Create(11, 12, 13, 14), IsDummy = false, },
        new PolyPosition { Name = "a2", Location = Rect.Create(21, 22, 23, 24), IsDummy = false, },
        new PolyPosition { Name = "b1", Location = Rect.Create(31, 32, 33, 34), IsDummy = false, },
        new PolyPosition { Name = "b2", Location = Rect.Create(41, 42, 43, 44), IsDummy = false, },
      };
      CollectionAssert.AreEquivalent(positions, game.Positions.ToArray());

      var links = new PolyLink[] {
        new PolyLink { Direction = "n", From = "a2", To = "a1" },
        new PolyLink { Direction = "e", From = "a1", To = "b1"},
      };
      CollectionAssert.AreEquivalent(links, game.Links.ToArray());

      var zones = new PolyZone[] {
        new PolyZone { Name = "zone1", Player = "X", Positions = "a1 a2" },
      };
      CollectionAssert.AreEquivalent(zones, game.Zones.ToArray());
    }

    [TestMethod]
    public void BoardGridPositions() {
      Logger.Open(1);
      var input = new StringReader(
        @"(include ""testincl.poly"")" +
        @"(game (gamestub1)" +
        @"(board (boardgrid45)) )"
      );
      var game = PolyGame.Create("bgp", input);

      for(int i = 0; i < game.Positions.Count; ++i) {
        Assert.AreEqual(String.Format("{0}-{1}", "ABCD"[i / 5], "12345"[i % 5]), game.Positions[i].Name, "name");
        Assert.AreEqual(i / 5, game.Positions[i].Coords[0], "coord");
        Assert.AreEqual(i % 5, game.Positions[i].Coords[1], "coord");
      }
    }

    [TestMethod]
    public void BoardGrid45() {
      Logger.Open(1);
      var input = new StringReader(
        @"(include ""testincl.poly"")" +
        @"(game (gamestub1)" +
        @"(board (boardgrid45)) )");
      var game = PolyGame.Create("bt2", input);
      Assert.AreEqual(1, game.Menu.Count());
      Assert.AreEqual("stub1", game.Title);
      Assert.AreEqual("board45a.png", game.BoardImages[0]);
      Assert.AreEqual("board45b.png", game.BoardImages[1]);
      Assert.AreEqual("board45c.png", game.BoardImages[2]);

      var posnames = "ABCD".SelectMany(c => "12345".Select(r => String.Format("{0}-{1}", c, r))).ToArray();
      CollectionAssert.AreEqual(posnames, game.Positions.Select(p => p.Name).ToArray());
    }

    [TestMethod]
    public void BoardPiece1() {
      Logger.Open(1);
      var input = new StringReader(
        @"(include ""testincl.poly"")" +
        @"(game (gamestub1)" +
        @"(board (boardgrid45))"+
        @"(piece (piece1)) )");
      var game = PolyGame.Create("bt2", input);

      var piece_exp = new string[] {
        "man;M;man desc;man help;False"
      };
      var piece_act = game.Pieces
        .Select(p => Util.Join(";", p.Name, p.Notation, p.Description, p.Help, p.IsDummy));
      CheckEqual(piece_exp, piece_act);

      var image_exp = new string[] {
        "man;X;MANX.png",
        "man;O;MANO.png",
      };
      var image_act = game.PieceImages
        .Select(p => Util.Join(";", p.Piece, p.Player, p.Images.Join(",")));
      CheckEqual(image_exp, image_act);
    }

    private void CheckEqual(IEnumerable<string> exp, IEnumerable<string> act) {
      var exps = exp.OrderBy(s => s).ToList();
      var acts = act.OrderBy(s => s).ToList();
      for (int i = 0; i < exps.Count; i++) {
        Assert.AreEqual(exps[i], acts[i], i.ToString());
      }
    }

    [TestMethod]
    public void BoardPiece2() {
      Logger.Open(1);
      var input = new StringReader(
        @"(include ""testincl.poly"")" +
        @"(game (gamestub1)" +
        @"(board (boardgrid45))" +
        @"(piece (piece1))" +
        @"(piece (piece2))" +
        @"(piece (piece3))" +
        @")");
      var game = PolyGame.Create("bt2", input);

      var piece_exp = new string[] {
        "man;M;man desc;man help;False",
        "chip;;;;False",
        "mchip;;;;False",
      };
      var piece_act = game.Pieces
        .Select(p => Util.Join(";", p.Name, p.Notation, p.Description, p.Help, p.IsDummy));
      CheckEqual(piece_exp, piece_act);

      var image_exp = new string[] {
        "man;X;MANX.png",
        "man;O;MANO.png",
        "chip;X;CHIPX.png",
        "chip;O;CHIPO.png",
        "mchip;X;CHIPX.png,MANX.png",
        "mchip;O;MANO.png,CHIPO.png",
      };
      var image_act = game.PieceImages
        .Select(p => Util.Join(";", p.Piece, p.Player, p.Images.Join(",")));
      CheckEqual(image_exp, image_act);
    }

    [TestMethod]
    public void PieceSetup1() {
      Logger.Open(1);
      var input = new StringReader(
        @"(include ""testincl.poly"")" +
        @"(game (gamestub1)" +
        @" (board (boardgrid33))" +
        @" (piece (piece1))" +
        @" (board-setup (X (man A-1 B-1 off 5 C-1)))" +
        @")");
      var game = PolyGame.Create("bs1", input);

      var ppexpect = "X,man,A-1,MANX.png;X,man,B-1,MANX.png;X,man,C-1,MANX.png";
      var ppactual = game.PlayedPieces
        .Select(p => Util.Join(",", p.Player, p.Piece, p.Position, p.Image)).Join(";");
      Assert.AreEqual(ppexpect, ppactual);

      var stexpect = "X,man,5";
      var stactual = game.OffStores
        .Select(s => Util.Join(",", s.Player, s.Piece, s.Quantity)).Join(";");
      Assert.AreEqual(stexpect, stactual);
    }

    [TestMethod]
    public void PieceSetup2() {
      Logger.Open(1);
      var input = new StringReader(
        @"(include ""testincl.poly"")" +
        @"(game (gamestub1)" +
        @"(board (boardgrid33))" +
        @"(piece (piece1))" +
        @"(board-setup (setup2)) )");
      var game = PolyGame.Create("bs1", input);

      var ppexpect = "X,man,A-1,MANX.png;X,man,B-1,MANX.png;X,man,C-1,MANX.png;O,man,C-2,MANO.png";
      var ppactual = game.PlayedPieces
        .Select(p => Util.Join(",", p.Player, p.Piece, p.Position, p.Image)).Join(";");
      Assert.AreEqual(ppexpect, ppactual);

      var stexpect = "X,man,5;O,man,8";
      var stactual = game.OffStores
        .Select(s => Util.Join(",", s.Player, s.Piece, s.Quantity)).Join(";");
      Assert.AreEqual(stexpect, stactual);

    }
  }

}
