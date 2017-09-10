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
/// Main program for interactive testing
/// 
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Poly.Common;
using Polygamo;

namespace Poly {
  class Program {
    const string PolyVersion = "Poly 1.0";
    const string Help = "Poly <script.ext> [/options]\n"
      + "\t\tDefault script is test.poly.\n"
      + "\t/n\tn=1 to 4, set tracing level";
    const string DefaultPath = "test.poly";
    static readonly Dictionary<string, Action<string>> _options = new Dictionary<string, Action<string>> {
      { "d", (a) => { AddSource = true; } },
    };
    static Dictionary<string, string> _settings = new Dictionary<string, string>();
    internal static bool AddSource = false;

    internal static string InputPath { get; private set; }
    static TextWriter Out { get { return _output; } }
    static TextWriter _output;


    static void Main(string[] args) {
      Logger.Open(1);
      _output = Console.Out;
      _output.WriteLine(PolyVersion);
      var options = OptionParser.Create(_options, Help);
      if (!options.Parse(args))
        return;
      var Path = options.GetPath(0) ?? DefaultPath;

      try {
        //PlayTTTGames();
        Logger.Level = 0;
        //Logger.Level = 3; // compile logging
        //Play(1, 23, 10, 11, 3); // beer game with logging
        //Play(1, 31, 1, 0, 1); // scan losers TTT with logging
        Play(1, 1, 0, 0, 3); // losers TTT seed 12 with logging
        Console.ReadLine();
      } catch (PolyException ex) {
        _output.WriteLine(ex.Message);
      //} catch (Exception ex) {
      //  _output.WriteLine($"Unexpected exception: {ex.ToString()}");
      //  return;
      }
    }

    static void Play(int gameid, int playid, int variant = -1, int seed = 0, int level = -1) {
      var game = (gameid == 0) ? CreateGameTTT("simple TTT")
        : (gameid == 1) ? PolyGame.Create("TicTacToe.txt")
        : PolyGame.Create(InputPath);
      var player = ConsolePlayer.Create(game, variant, level);
      if (playid == 0) player.Show();
      if (playid == 1) player.PlayTurns(seed, 1, 300, -1, 0);        // me first
      if (playid == 2) player.PlayTurns(seed, 1, 300, 0, -1);        // me second

      if (playid == 10) player.PlayTurns(seed, 3, 0, 4, 4);        // 3 random games, no mcts
      if (playid == 11) player.PlayTurns(seed, 3, 10, 4, 4);        // 3 random games, mcts 10
      if (playid == 12) player.PlayTurns(seed, 3, 100, 4, 4);        // 3 random games, mcts 100
      if (playid == 13) player.PlayTurns(seed, 3, 300, 4, 4);        // 3 random games, mcts 300

      if (playid == 20) player.PlayTurns(seed, 1, 5, 1, 0);       // 1 games, depth=1
      if (playid == 21) player.PlayTurns(seed, 1, 10, 1, 0);       // 1 games limit steps
      if (playid == 22) player.PlayTurns(seed, 1, 100, 1, 0);       // 1 games limit steps
      if (playid == 23) player.PlayTurns(seed, 1, 200, 1, 0);       // 1 games limit steps
      if (playid == 24) player.PlayTurns(seed, 1, 300, 1, 0);       // 1 games limit steps
      if (playid == 25) player.PlayTurns(seed, 1, 1000, 1, 0);       // 1 games limit steps

      if (playid == 30) player.PlayTurns(seed, 10, 200, 1, 0);     // 10 games * 200 steps
      if (playid == 31) player.PlayTurns(seed, 30, 200, 1, 0);     // 30 games * 200 steps
      if (playid == 32) player.PlayTurns(seed, 100, 200, 1, 0);
      if (playid == 33) player.PlayTurns(seed, 500, 500, 3, 3);
      if (playid == 34) player.PlayTurns(seed, 100, 600, 1, 1);

      if (playid == 51) player.PlayTurns(10, 1, 200, 1, 0); // single game, specified seed, 200 steps
      if (playid == 200) player.PlayChoosers();
      if (playid == 300) player.PlayChooser(3, 500, new int[] { -1 });
    }

    static void PlayTTTGames() {
      // check we get the same result for setup or playing moves -- should pick C-2
      //PlayTTTGame("play choose O C-2",  "B-2", "A-1", 3, 200, new int[] { 0, -1 });
      //PlayTTTGame("setup choose O C-2", "A-1", "A-2 B-2", 3, 200, new int[] { -1 });

      PlayTTTGame("setup 0        choose X B-2", "", "", 3, 200, new int[] { -1 });
      PlayTTTGame("setup 0 move X choose O B-2", "", "", 3, 200, new int[] { 0, -1 });
      //PlayTTTGame("setup 1        choose X B-2", "", "A-1", 3, 100, new int[] { -1 });
      //PlayTTTGame("setup 2        choose X A-2", "A-1", "B-2", 3, 100, new int[] { -1 });
      //PlayTTTGame("setup 2 move X choose O A-3", "A-1", "B-2", 3, 100, new int[] { 0, -1 });
      //PlayTTTGame("setup 3        choose X A-3", "B-2", "A-1 A-2", 3, 100, new int[] { -1 });
      //PlayTTTGame("setup 4        choose X C-1", "A-1 A-2", "B-2 A-3", 3, 100, new int[] { -1 });
      //PlayTTTGame("setup 4 move X choose O B-1", "A-1 A-2", "B-2 A-3", 3, 100, new int[] { 2, -1 });
      //PlayTTTGame("setup 5        choose X B-1", "B-2 A-3", "A-1 A-2 C-1", 3, 100, new int[] { -1 });

      //PlayTTTGame("A-1 B-2", "B-1 C-3", 1, 300, new int[] { -1 });
      //PlayTTTGame("A-1 B-2", "B-1 C-3", 2, 10, new int[] { -1 });

    }

    static void PlayTTTGame(string title, string xsetup, string osetup, int loopct, int steparg, int[] moves) {
      Logger.WriteLine("\nPlay TTT game:{0} setup: X:{1} O:{2}", title, xsetup, osetup);
      var game = CreateGameTTT(title, xsetup, osetup);
      Logger.Level = 3;
      ConsolePlayer.Create(game).PlayChooser(loopct, steparg, moves);
    }

    static PolyGame CreateGameTTT(string title, string xsetup = "", string osetup = "") {
      var testprog =
        @"(include ""testdef.poly"")" +
        @"(game (gamestub1)" +
        @" (title ""{0}"")" +
        @" (board (boardgrid33))" +
        @" (piece (name man) (drops ((verify empty?) add)))" +
        @" (draw-condition (X O) stalemated)" +
        @" (win-condition (relcondttt))" +
        @" (board-setup {1})" +
        @") (variant" +
        @" (title ""{2}"") (players man woman) (piece (name chip)) (board-setup (woman (chip A-2)))" +
        @")";

      var setup = (xsetup == "" ? "" : String.Format("(X (man {0}))", xsetup))
                + (osetup == "" ? "" : String.Format("(O (man {0}))", osetup));
      var prog = new StringReader(String.Format(testprog, title, setup, title + " variant"));
      return PolyGame.Create("internal", prog);
    }
  }

  /// <summary>
  /// Play game using console UI
  /// </summary>
  class ConsolePlayer {
    PolyGame _pg;
    TextWriter _out = Console.Out;
    TextReader _in = Console.In;

    internal static ConsolePlayer Create(PolyGame game, int variant = -1, int level = -1) {
      var ret = new ConsolePlayer {
        _pg = (variant == -1) ? game : game.Create(variant)
      };
      if (level != -1) Logger.Level = level;
      return ret;
    }

    internal void Show() {
      Logger.WriteLine("Show {0}", _pg.Title);
      ShowPosition();
      ShowMoves();
    }

    //==========================================================================
    internal void PlayChoosers() {
      PlayChooser(3, 100, new int[] { 0, -1, 0, -1 });  // full game
      PlayChooser(3, 100, new int[] { 0, 3, 0, -1 });
      PlayChooser(3, 100, new int[] { 4, 0, 0, -1 });
      PlayChooser(3, 100, new int[] { 4, -1 });
      PlayChooser(3, 100, new int[] { -1 });  // just the chooser
    }

    internal void PlayChooser(int loopct, int steparg, int[] moves) {
      //Logger.Level = 2;
      Logger.WriteLine("Play chooser {0} loop={1} step={2} moves={3}", 
        _pg.Title, loopct, steparg, moves.Join());

      //ShowPosition();
      //ShowMoves();
      _pg.NewBoard(ChooserKinds.Mcts);
      //Logger.Level = 3;
      var treelog = 0;
      foreach (var move in moves) {
        Logger.WriteLine("> Board: {0}", _pg.PlayedPieces.Join());
        if ((treelog & 1) != 0) ShowTree();
        if (move >= 0) {
          Logger.WriteLine("> Make move {0}", move);
          ShowMove(_pg.GetLegalMove(move));
          _pg.MakeMove(move);
        } else {
          if ((treelog & 2) != 0) ShowTree();
          for (int i = 0; i < loopct; i++) {
            Logger.WriteLine("> UpdateChooser {0} step={1}", i, steparg);
            _pg.UpdateChooser(steparg);
            if (_pg.ChoicesIter().First().IsDone) break;
          }
          Logger.WriteLine("> Updated index={0} count={1} weight={2:G5} move={3}", 
            _pg.ChosenMove.Index, _pg.VisitCount, _pg.Weight, _pg.ChosenMove);
          if ((treelog & 4) != 0) ShowTree();
          ShowMove(_pg.GetLegalMove(_pg.ChosenMove.Index));
          _pg.MakeMove(_pg.ChosenMove.Index);
        }
        if ((treelog & 8) != 0) ShowTree();
      }
    }

    //==========================================================================
    internal void PlayTurns(int initseed, int games, int depth, int xrandom, int orandom) {
     // Logger.Level = 2;
      _out.WriteLine("\nPlay random '{0}' games={1} depth={2} random X={3} O={4}\n", 
        _pg.Title, games, depth, xrandom, orandom);

      var score = new Dictionary<Pair<string, ResultKinds>, int>();
      for (int seed = initseed; seed < initseed + games; seed++) {
        _pg.Reseed(seed);
        _pg.NewBoard(ChooserKinds.Mcts);
        var pick = 0;
        for (int moveno = 0; ; moveno++) {
          //if (moveno > 0) Logger.Level = 5;
          if (_pg.GameResult != ResultKinds.None) break;
          //var xplayer = moveno % 2 == 0;
          //var pickrandom = moveno < (xplayer ? xrandom : orandom);
          //if (pickrandom) pick = _Polygamo.NextRandom(_Polygamo.LegalMoves.Count);
          var random = (moveno % 2 == 0) ? xrandom : orandom;
          if (random == 0) pick = GetUserMove(moveno);
          else if (moveno < random) pick = _pg.NextRandom(_pg.LegalMoves.Count);
          else if (depth == 0) pick = 0;
          else {
            _pg.UpdateChooser(depth);
            //if (moveno == 4) ShowTree(2);
            pick = _pg.ChosenMove.Index;
          }
          if (pick < 0) break;
          Logger.WriteLine(3, "{0} make move {1} {2}", _pg.TurnPlayer, pick, _pg.GetLegalMove(pick));
          //var trigger = (_Polygamo.GetLegalMove(pick).Position1 == "B-2");
          //if (trigger) Logger.Level = 4;
          _pg.MakeMove(pick);
          //if (trigger) Logger.PopLevel();
        }
        var key = Pair.Create(_pg.ResultPlayer, _pg.GameResult);
        if (score.ContainsKey(key)) score[key]++;
        else score[key] = 1;
        _out.WriteLine("Seed {0} result {1} {2}", seed, _pg.ResultPlayer, _pg.GameResult);
        ShowPosition();
      }
      foreach (var kvp in score)
        _out.WriteLine("\nplayer={0} result={1} count={2}", kvp.Key.Item1, kvp.Key.Item2, kvp.Value);
    }

    int GetUserMove(int moveno) {
      ShowBoard();
      ShowMoves();
      while (true) {
        _out.Write("Please select move {0} [0-{1}]: ", moveno, _pg.LegalMoves.Count - 1);
        var input = _in.ReadLine();
        var exre = new Regex("[XxQq]+");
        if (exre.Match(input).Success) return -1;
        int ret;
        if (Int32.TryParse(input, out ret) && ret >= 0 && ret < _pg.LegalMoves.Count) return ret;
      }
    }

    //--------------------------------------------------------------------------
    // debugging and display routines

    void ShowDepths() {
      var depthcounts = new List<int>();
      foreach (var choice in _pg.ChoicesIter()) {
        if (choice.Depth >= depthcounts.Count)
          depthcounts.Add(1);
        else depthcounts[choice.Depth]++;
      }
      _out.WriteLine("Depths: {0}", depthcounts.Join());
    }

    void ShowTree(int depth = 99, bool showdepths = true) {
      if (showdepths) ShowDepths();
      foreach (var choice in _pg.ChoicesIter()
        .Where(c => c.Depth <= depth)
        .Where(c => c.IsDone)
        //.Where(c => c.Result != ResultKinds.None)
        //.Where(c => c.Weight != 0)
        ) {
        _out.WriteLine("{0}{1}: last={2} chosen={3}", 
          "-".Repeat(choice.Depth), choice.Format(), choice.PlayedMove, choice.ChosenMove);
        if (choice.Result != ResultKinds.None) _out.WriteLine("{0}  {1}:{2}:{3}",
          "-".Repeat(choice.Depth), choice.LastPlayer, choice.Result, choice.PlayedPieces.Join());
      }
    }

    void ShowLayout() {
      _out.WriteLine(">>>Title: {0}", _pg.Title);
      _out.WriteLine("   Images: {0}", _pg.BoardImages.Join());
      _out.WriteLine("   Players: {0}", _pg.Players.Join());
      _out.WriteLine("   Positions: {0}", _pg.Positions.Count());
      foreach (var p in _pg.Positions)
        _out.WriteLine("    {0} {1} {2}", p.Name, p.Location, p.IsDummy ? "dummy" : "");
      _out.WriteLine("Pieces: {0}", _pg.Pieces.Count());
      foreach (var p in _pg.Pieces)
        _out.WriteLine("    {0} {1} {2}", p.Name, p.Notation, p.IsDummy ? "dummy" : "");
    }

    void ShowPosition() {
      _out.WriteLine(">>>Position: {0}", _pg.PlayedPieces.Join(", "));
    }

    void ShowBoard() {
      _out.WriteLine(">>>Board");
      var nrows = _pg.Positions.Max(p => p.Coords[0]) + 1;
      var ncols = _pg.Positions.Max(p => p.Coords[1]) + 1;
      var plen = _pg.Players.Max(p => p.Length) + _pg.Pieces.Max(p => p.Name.Length) + 1;
      for (int r = 0; r < nrows; r++) {
        for (int c = 0; c < ncols; c++) {
          var rc = r * ncols + c;
          var piece = _pg.PlayedPieces.FirstOrDefault(p => p.Position == _pg.Positions[rc].Name);
          var s = String.Format("{0}:{1}", piece.Player, piece.Piece);
          _out.Write("|{0}", (s == ":" ? "" : s).PadRight(plen));
        }
        _out.WriteLine("|");
      }
    }

    void ShowMoves() {
      _out.WriteLine(">>>Moves: {0}", _pg.LegalMoves.Count());
      foreach (var p in _pg.LegalMoves)
        ShowMove(p);
    }

    void ShowMove(PolyMove move) {
      _out.Write("  {0}: {1} {2} {3}", move.Index, move.Player, move.Piece1, move.Position1);
      //foreach (var p in move.Parts)
      //  _out.Write("; {0} {1} {2} {3} {4} {5}", p.Kind, p.Player, p.Piece, p.Position,
      //    p.Attribute, p.Attribute == null ? "" : p.Value.ToString());
      _out.WriteLine();
    }
  }
}
