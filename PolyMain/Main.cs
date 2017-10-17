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
  internal class Program {
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
        //Load(@"TicTacToe.txt", 10).Play(23, 11, 3); // beer game with logging
        //Load(@"TicTacToe.txt", 1).Play(31, 0, 1); // scan losers TTT with logging
        //Load(@"TicTacToe.txt", 0).Play(1, 0, 3); // losers TTT with logging
        //Load(@"..\GamesZrf\Breakthrough.zrf", 0, 2).Play(0, 0, 3); // Breakthrough with logging
        //Load(@"TicTacToe.txt", 0, 1).Play(1, 0, 3);
        //CreateGameTTT("ttt", "", "", 4).Play(0);

        // create and run test game
        //CreateTestGame("xxx", 1);

        // compile one and do something
        //CompileAndAct(SearchFolders(@"..\Unity\User Games", 38, 1), 0, 0, pw => pw.Play(1, 0, 5));

        // interactive selection
        //CompileAndAsk(SearchFolders(@"..\Unity\User Games"), 1, pw => pw.Play(1, 0, 3));
        CompileAndAsk(SearchFolders(@"..\GamesPoly"), 1, pw => pw.Play(1, 0, 3));

        // compile all for errors
        //CompileAndAct(SearchFolders(@"..\Unity\User Games"), 0, 0, null);

        Console.ReadLine();

      } catch (PolyException ex) {
        _output.WriteLine(ex.Message);
        //} catch (Exception ex) {
        //  _output.WriteLine($"Unexpected exception: {ex.ToString()}");
        //  return;
      }
    }

    static IList<string> SearchFolders(string root, int skip = 0, int take = 999) {
      _output.WriteLine("Searching root {0}...", root);
      var paths = Directory.GetFiles(root, "*.zrf", SearchOption.AllDirectories)
        .Concat(Directory.GetFiles(root, "*.poly", SearchOption.AllDirectories))
        .Where(p => !p.ToLower().Contains("include"))
        .OrderBy(p => p)
        .Skip(skip)
        .Take(take)
        .ToList();
      _output.WriteLine("Root {0} found {1} files", root, paths.Count);
      return paths;
    }

    static void CompileAndAsk(IList<string> paths, int level, Action<PlayWrapper> action) {
      var variant = 0;
      PlayWrapper pw = null;
      var gno = -1;
      while (true) {
        if (gno >= 0) _output.WriteLine("Game {0} script {1}", gno, paths[gno]);
        var prompt = string.Format("Choose a game [0-{0}], quit or play: ", paths.Count - 1);
        var input = GetInput(prompt, "|p|[0-9]+");
        if (input == "") return;
        if (input == "p" && pw != null) action(pw);
        else {
          gno = input.SafeIntParse() ?? -1;
          if (gno == -1) gno = paths.ToList().FindIndex(p => p.Contains(input));
          if (gno >= 0) {
            _output.WriteLine("Game {0} script {1}", gno, paths[gno]);
            pw = LoadGame(paths[gno], variant, level, true);
          }
        }
      }
    }

    static void CompileAndAct(IList<string> paths, int variant, int level, Action<PlayWrapper> action) {
      for (var i = 0; i < paths.Count; ++i) {
        Logger.WriteLine(1, "=====\nGame {0} script {1}", i, paths[i]);
        var pw = CompileAndAct(paths[i], variant, level, action);
        if (Logger.Level == 0) {
          Logger.WriteLine(">>>{0}: path:{1} {2}", i, paths[i],
            (pw == null) ? "Failed: " + PolyGame.LastError : "Variants: " + pw.Player.Game.Menu.Join());
        }
      }
    }

    static PlayWrapper CompileAndAct(string gamename, int variant, int level, Action<PlayWrapper> action) {
      Logger.WriteLine(1, "Script: {0} variant {1} level {2}", gamename, variant, level);
      var pw = LoadGame(gamename, variant, level);
      if (pw == null) return null;
      if (action != null) action(pw);
      return pw;
    }

    static PlayWrapper LoadGame(string gamename, int variant, int level = -1, bool inner = false) {
      if (level >= 0) Logger.Level = level;
      var game = (inner) ? ((gamename != null) ? PolyGame.CreateInner(gamename) : PolyGame.CreateInner(InputPath))
                         : ((gamename != null) ? PolyGame.Create(gamename) : PolyGame.Create(InputPath));
      if (game == null) {
        Logger.WriteLine(1, "\n*** {0} did not load ***\n{1}", gamename, PolyGame.LastError);
        return null;
      }
      if (Logger.Level >= 1) ShowMenu(gamename, game);
      return new PlayWrapper {
        Player = ConsolePlayer.Create(game, variant)
      };
    }

    internal static string GetInput(string prompt, string pattern) {
      var exre = new Regex(pattern);
      while (true) {
        Console.Write("{0} [{1}]: ", prompt, pattern);
        var input = Console.ReadLine().ToLower();
        if (exre.Match(input).Success) return input;
      }
    }

    static PlayWrapper CreateGameTTT(string title, string xsetup = "", string osetup = "", int level = -1) {
      if (level >= 0) Logger.Level = level;
      var testprog0 =
        @"(game" +
        @"(title ""testincl game1"")" +
        @"(players X O)" +
        @"(turn-order X O)" +
        @"(board)" +
        @")";

      var testprog =
        @"(include ""testdef.poly"")" +
        @"(game (gamestub1)" +
        @" (title ""{0}"")" +
        @" (board (boardgrid33))" +
        @" (piece (name man) (drops (e (opposite e) (verify empty?) add)))" +
        //@" (piece (name man) (drops ((verify empty?) add)))" +
        //@" (draw-condition (X O) stalemated)" +
        //@" (win-condition (relcondttt))" +
        @" (board-setup {1})" +
        @") (variant" +
        @" (title ""{2}"") (board-setup)" +
        //@" (title ""{2}"") (players man woman) (piece (name chip)) (board-setup (woman (chip A-2)))" +
        @")";

      var setup = (xsetup == "" ? "" : String.Format("(X (man {0}))", xsetup))
                + (osetup == "" ? "" : String.Format("(O (man {0}))", osetup));
      var prog = new StringReader(String.Format(testprog0, title, setup, title + " variant"));
      var game = PolyGame.CreateInner("internal", prog);
      return new PlayWrapper {
        Player = ConsolePlayer.Create(game, 0)
      };
    }

    static void CreateTestGame(string text, int level = -1) {
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
      };

      if (level >= 0) Logger.Level = level;
      for (int i = 0; i < matrix.GetLength(0); i++) {
        var tp = String.Format(testprog, matrix[i, 0]);
        var game = PolyGame.CreateInner("cwld", new StringReader(tp), 0);
        var expected = matrix[i, 1];
        //Logger.Assert(expected == game.Pieces.Select(p => p.Name).Join(","), matrix[i, 0]);
      }

    }

    static void ShowMenu(string gamename, PolyGame game) {
      _output.WriteLine("Game: {0}\nVariants: {1}", gamename, game.Menu.Join());
    }
  }

  public class PlayWrapper { 
    public ConsolePlayer Player { get; set; }

    public void Play(int playid, int seed = 0, int level = -1) {
      if (level >= 0) Logger.Level = level;
      if (playid == 0) Player.ShowState();
      if (playid == 1) Player.PlayTurns(seed, 1, 300, -1, 0);        // me first
      if (playid == 2) Player.PlayTurns(seed, 1, 300, 0, -1);        // me second
      if (playid == 3) Player.PlayTurns(seed, 1, 0, -1, -1);        // me both

      if (playid == 10) Player.PlayTurns(seed, 3, 0, 4, 4);        // 3 random games, no mcts
      if (playid == 11) Player.PlayTurns(seed, 3, 10, 4, 4);        // 3 random games, mcts 10
      if (playid == 12) Player.PlayTurns(seed, 3, 100, 4, 4);        // 3 random games, mcts 100
      if (playid == 13) Player.PlayTurns(seed, 3, 300, 4, 4);        // 3 random games, mcts 300

      if (playid == 20) Player.PlayTurns(seed, 1, 5, 1, 0);       // 1 games * 5 steps
      if (playid == 21) Player.PlayTurns(seed, 1, 10, 1, 0);       // 1 games * 10 steps
      if (playid == 22) Player.PlayTurns(seed, 1, 100, 1, 0);  
      if (playid == 23) Player.PlayTurns(seed, 1, 200, 1, 0);  
      if (playid == 24) Player.PlayTurns(seed, 1, 300, 1, 0);  
      if (playid == 25) Player.PlayTurns(seed, 1, 1000, 1, 0); 

      if (playid == 30) Player.PlayTurns(seed, 10, 200, 1, 0);     // 10 games * 200 steps
      if (playid == 31) Player.PlayTurns(seed, 30, 200, 1, 0);     // 30 games * 200 steps
      if (playid == 32) Player.PlayTurns(seed, 100, 200, 1, 0);
      if (playid == 33) Player.PlayTurns(seed, 500, 500, 3, 3);
      if (playid == 34) Player.PlayTurns(seed, 100, 600, 1, 1);

      if (playid == 51) Player.PlayTurns(10, 1, 200, 1, 0); // single game, specified seed, 200 steps
      //if (playid == 200) Player.PlayChoosers();
      //if (playid == 300) Player.PlayChooser(3, 500, new int[] { -1 });
    }

    static void PlayTTTGames() {
      // check we get the same result for setup or playing moves -- should pick C-2
      //PlayTTTGame("play choose O C-2",  "B-2", "A-1", 3, 200, new int[] { 0, -1 });
      //PlayTTTGame("setup choose O C-2", "A-1", "A-2 B-2", 3, 200, new int[] { -1 });

      //PlayTTTGame("setup 0        choose X B-2", "", "", 3, 200, new int[] { -1 });
      //PlayTTTGame("setup 0 move X choose O B-2", "", "", 3, 200, new int[] { 0, -1 });
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

    //static void PlayTTTGame(string title, string xsetup, string osetup, int loopct, int steparg, int[] moves) {
    //  Logger.WriteLine("\nPlay TTT game:{0} setup: X:{1} O:{2}", title, xsetup, osetup);
    //  var game = CreateGameTTT(title, xsetup, osetup);
    //  Logger.Level = 3;
    //  ConsolePlayer.Create(game).PlayChooser(loopct, steparg, 999, moves);
    //}

    //static PolyGame CreateGameTTT(string title, string xsetup = "", string osetup = "") {
    //  var testprog =
    //    @"(include ""testdef.poly"")" +
    //    @"(game (gamestub1)" +
    //    @" (title ""{0}"")" +
    //    @" (board (boardgrid33))" +
    //    @" (piece (name man) (drops ((verify empty?) add)))" +
    //    @" (draw-condition (X O) stalemated)" +
    //    @" (win-condition (relcondttt))" +
    //    @" (board-setup {1})" +
    //    @") (variant" +
    //    @" (title ""{2}"") (players man woman) (piece (name chip)) (board-setup (woman (chip A-2)))" +
    //    @")";

    //  var setup = (xsetup == "" ? "" : String.Format("(X (man {0}))", xsetup))
    //            + (osetup == "" ? "" : String.Format("(O (man {0}))", osetup));
    //  var prog = new StringReader(String.Format(testprog, title, setup, title + " variant"));
    //  return PolyGame.Create("internal", prog);
    //}
  }

  /// <summary>
  /// Play game using console UI
  /// </summary>
  public class ConsolePlayer {
    public PolyGame Game;
    TextWriter _out = Console.Out;
    TextReader _in = Console.In;

    internal static ConsolePlayer Create(PolyGame game, int variant = -1) {
      var ret = new ConsolePlayer {
        Game = (variant == -1) ? game : game.Create(variant)
      };
      return ret;
    }

    internal void ShowState() {
      Logger.WriteLine("Show state for {0}", Game.Title);
      ShowPosition();
      ShowMoves();
    }

    //==========================================================================
    //internal void PlayChoosers() {
    //  PlayChooser(3, 100, new int[] { 0, -1, 0, -1 });  // full game
    //  PlayChooser(3, 100, new int[] { 0, 3, 0, -1 });
    //  PlayChooser(3, 100, new int[] { 4, 0, 0, -1 });
    //  PlayChooser(3, 100, new int[] { 4, -1 });
    //  PlayChooser(3, 100, new int[] { -1 });  // just the chooser
    //}

    internal void PlayChooser(int loopct, int steparg, int maxdepth, int[] moves) {
      //Logger.Level = 2;
      Logger.WriteLine("Play chooser {0} loop={1} step={2} depth={3} moves={4}", 
        Game.Title, loopct, steparg, maxdepth, moves.Join());

      //ShowPosition();
      //ShowMoves();
      Game.NewBoard();
      Game.StepCount = steparg;
      Game.MaxDepth = maxdepth;
      //Game.NewBoard(ChooserKinds.Mcts, steparg, maxdepth);
      //Logger.Level = 3;
      var treelog = 0;
      foreach (var move in moves) {
        Logger.WriteLine("> Board: {0}", Game.PlayedPieces.Join());
        if ((treelog & 1) != 0) ShowTree();
        if (move >= 0) {
          Logger.WriteLine("> Make move {0}", move);
          ShowMove(Game.GetLegalMove(move));
          Game.MakeMove(move);
        } else {
          if ((treelog & 2) != 0) ShowTree();
          for (int i = 0; i < loopct; i++) {
            Logger.WriteLine("> UpdateChooser {0} step={1} depth={2}", i, steparg, maxdepth);
            Game.UpdateChooser();
            if (Game.ChoicesIter().First().IsDone) break;
          }
          Logger.WriteLine("> Updated index={0} count={1} weight={2:G5} move={3}", 
            Game.ChosenMove.Index, Game.VisitCount, Game.Weight, Game.ChosenMove);
          if ((treelog & 4) != 0) ShowTree();
          ShowMove(Game.GetLegalMove(Game.ChosenMove.Index));
          Game.MakeMove(Game.ChosenMove.Index);
        }
        if ((treelog & 8) != 0) ShowTree();
      }
    }

    //==========================================================================
    internal void PlayTurns(int initseed, int ngames, int nsteps, int xrandom, int orandom) {
      var maxdepth = 9;
      _out.WriteLine("\nPlay random '{0}' games={1} steps={2} maxdepth={3} random X={4} O={5}\n", 
        Game.Title, ngames, nsteps, maxdepth, xrandom, orandom);

      var score = new Dictionary<Pair<string, ResultKinds>, int>();
      for (int seed = initseed; seed < initseed + ngames; seed++) {
        Game.Reseed(seed);
        Game.NewBoard();
        //Game.NewBoard(ChooserKinds.Mcts, 1, 1);
        Game.StepCount = nsteps;
        Game.MaxDepth = maxdepth;
        //Game.NewBoard(ChooserKinds.Mcts, nsteps, maxdepth);
        var pick = 0;
        for (int moveno = 0; ; moveno++) {
          ShowBoard();
          ShowMoves();
          if (Game.GameResult != ResultKinds.None) break;
          // which to use?
          var nrandom = (moveno % 2 == 0) ? xrandom : orandom;
          // means get user input
          if (nrandom == -1) {
            do {
              var input = Program.GetInput("Please select a move", "|[0-9]+");
              if (input == "") return;
              pick = input.SafeIntParse() ?? -1;
            } while (!(pick >= 0 && pick < Game.LegalMoves.Count));
          }  
          else if (moveno < nrandom) pick = Game.NextRandom(Game.LegalMoves.Count);
          else if (nsteps == 0) pick = 0;
          else {
            Game.UpdateChooser();
            var done = Game.UpdateChooser();
            Logger.WriteLine(3, "Update returns {0}", done);
            //if (moveno == 4) ShowTree(2);
            pick = Game.ChosenMove.Index;
          }
          if (pick < 0) break;
          Logger.WriteLine(3, "{0} make move {1}: {2}", Game.TurnPlayer, pick, Game.GetLegalMove(pick).ToString("P"));
          //var trigger = (_Polygamo.GetLegalMove(pick).Position1 == "B-2");
          //if (trigger) Logger.Level = 4;
          Game.MakeMove(pick);
          //if (trigger) Logger.PopLevel();
        }
        var key = Pair.Create(Game.ResultPlayer, Game.GameResult);
        if (score.ContainsKey(key)) score[key]++;
        else score[key] = 1;
        _out.WriteLine("Seed {0} result {1} {2}", seed, Game.ResultPlayer, Game.GameResult);
        ShowPosition();
      }
      foreach (var kvp in score)
        _out.WriteLine("\nplayer={0} result={1} count={2}", kvp.Key.Item1, kvp.Key.Item2, kvp.Value);
    }

    //--------------------------------------------------------------------------
    // debugging and display routines

    void ShowDepths() {
      var depthcounts = new List<int>();
      foreach (var choice in Game.ChoicesIter()) {
        if (choice.Depth >= depthcounts.Count)
          depthcounts.Add(1);
        else depthcounts[choice.Depth]++;
      }
      _out.WriteLine("Depths: {0}", depthcounts.Join());
    }

    void ShowTree(int depth = 99, bool showdepths = true) {
      if (showdepths) ShowDepths();
      foreach (var choice in Game.ChoicesIter()
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
      _out.WriteLine(">>>Title: {0}", Game.Title);
      _out.WriteLine("   Images: {0}", Game.BoardImages.Join());
      _out.WriteLine("   Players: {0}", Game.Players.Join());
      _out.WriteLine("   Positions: {0}", Game.Positions.Count());
      foreach (var p in Game.Positions)
        _out.WriteLine("    {0} {1} {2}", p.Name, p.Location, p.IsDummy ? "dummy" : "");
      _out.WriteLine("Pieces: {0}", Game.Pieces.Count());
      foreach (var p in Game.Pieces)
        _out.WriteLine("    {0} {1} {2}", p.Name, p.Notation, p.IsDummy ? "dummy" : "");
    }

    void ShowPosition() {
      _out.WriteLine(">>>Position: {0}", Game.PlayedPieces.Join(", "));
    }

    void ShowBoard() {
      _out.WriteLine(">>>Board");
      var positions = Game.Positions.Where(p => p.Coords != null && p.Coords.Length > 0 && p.Coords.Length <= 2).ToList();
      if (positions.Count == 0) return;
      //if (Game.Positions.Any(p => p.Coords == null || p.Coords.Length == 0 || p.Coords.Length >2)) return;
      var onedim = positions[0].Coords.Length == 1;
      var nrows = (onedim) ? 1 : positions.Max(p => p.Coords[0]) + 1;
      var ncols = positions.Max(p => p.Coords[onedim ? 0 : 1]) + 1;
      var plen = Game.Players.Max(p => p.Length) + Game.Pieces.Max(p => p.Name.Length) + 1;
      for (int c = 0; c < ncols; c++) {
        for (int r = 0; r < nrows; r++) {
          var rc = r * ncols + c;
          var position = (onedim) ? positions.FirstOrDefault(p => p.Coords[0] == c)
            : positions.FirstOrDefault(p => p.Coords[0] == r && p.Coords[1] == c);
          var piece = Game.PlayedPieces.FirstOrDefault(p => p.Position == position.Name);
          var s = String.Format("{0}:{1}", piece.Player, piece.Piece);
          _out.Write("|{0}", (s == ":" ? "" : s).PadRight(plen));
        }
        _out.WriteLine("|");
      }
    }

    void ShowMoves() {
      _out.WriteLine(">>>Moves: {0}", Game.LegalMoves.Count());
      foreach (var p in Game.LegalMoves)
        ShowMove(p);
    }

    void ShowMove(PolyMove move) {
      _out.WriteLine("  {0}: {1} {2}", move.Index, move.Player, move.ToString("P"));
      //_out.Write("  {0}: {1} {2} {3}", move.Index, move.Player, move.Piece1, move.Position1);
      //foreach (var p in move.Parts)
      //  _out.Write("; {0} {1} {2} {3} {4} {5}", p.Kind, p.Player, p.Piece, p.Position,
      //    p.Attribute, p.Attribute == null ? "" : p.Value.ToString());
      //_out.WriteLine();
    }
  }
}
