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
/// Choose best move
/// 
using System;
using System.Collections.Generic;
using System.Linq;
using Poly.Common;
using Polygamo;

/// <summary>
/// Chooser is the main class responsible for choosing the best move to play
/// It accepts a board as input and places a weighting on each legal move.
/// The highest weighted move is chosen.
/// </summary>
namespace Poly.Engine {
  internal struct MoveWeight {
    internal const double ResultWin = 99;
    internal const double RatioWin = 50;

    internal int Weight;
    //internal int Index;
  }

  ///===========================================================================
  /// <summary>
  /// Manage a tree of choices corresponding to boards and moves made
  /// </summary>
  internal class ChoiceMaker {

    // translate player result into corresponding weight
    static Dictionary<ResultKinds, double> _weightlookup = new Dictionary<ResultKinds, double> {
      { ResultKinds.None, 0 },
      { ResultKinds.Loss, -MoveWeight.ResultWin },
      { ResultKinds.Draw, 0 },
      { ResultKinds.Win, MoveWeight.ResultWin },
    };

    static Dictionary<ChooserKinds, Action<Choice,int,int>> _methodlookup = new Dictionary<ChooserKinds, Action<Choice,int,int>> {
      {ChooserKinds.None, (c,a1,a2) => new List<MoveWeight>() },
      {ChooserKinds.First, (c,a1,a2) => c.ChooseFirst() },
      {ChooserKinds.Mcts, (c,a1,a2) => c.ChooseMcts(a1, a2) },
      {ChooserKinds.Breadth, (c,a1,a2) => c.ChooseBreadth(a1) },
      {ChooserKinds.Depth, (c,a1,a2) => c.ChooseByDepth(a1) },
      {ChooserKinds.Full, (c,a1,a2) => c.ChooseByDepth(99) },
    };

    //-- props
    // steps per update
    internal int StepCount { get; set; }
    // max depth (implemented for MCTS)
    internal int MaxDepth { get; set; }

    // chosen best move as index into legal moves
    internal int Index { get { return _choice.BestIndex; } }
    // value attached to this move
    internal double Weight { get { return _choice.Weight; } }
    // no of visits
    internal int VisitCount { get { return _choice.VisitCount; } }
    // tree of choices, root has current best move
    internal Choice Choice { get { return _choice; } }

    public override string ToString() {
      return String.Format("Chooser<{0},{1},{2},{3}>", Index, Weight, VisitCount, Choice);
    }

    //-- privates
    Choice _choice;
    ChooserKinds _kind;

    //-- factories
    internal static ChoiceMaker Create(BoardModel board, ChooserKinds kind, int steps, int depth) {
      return new ChoiceMaker {
        _choice = Choice.Create(board),
        _kind = kind,
        StepCount = steps,
        MaxDepth = depth,
      };
    }

    // return chooser for move using existing choices
    internal ChoiceMaker MakeMove(int index) {
      if (_choice == null) return null;
      if (index < 0 || index >= _choice.Children.Count) return null;
      if (_choice.Children[index] == null)
        _choice.Extend(index);
      var choice = _choice.Children[index];
      //if (choice.Children.Count > 0) choice.PickBest();
      return new ChoiceMaker {
        _choice = _choice.Children[index],
        _kind = _kind,
        StepCount = StepCount,
        MaxDepth = MaxDepth,
      };
    }

    //-- methods
    // get weight for board resulting from last move
    internal static double GetWeight(BoardModel board) {
      return -_weightlookup[board.MoveResult];
    }

    // update the choice tree using arg if not yet done
    internal void Update() {
      if (_choice.IsDone) return;
      _methodlookup[_kind](_choice, StepCount, MaxDepth);
    }
  }

  ///===========================================================================
  /// <summary>
  /// A Choice is a possible line of play, meaning a board, move index and calculated weight
  /// May contain move tree, or not.
  /// </summary>
  internal class Choice {
    // relevant board with player, move made and possible result
    internal BoardModel Board { get; private set; }
    // sub-trees (one per legal move)
    internal IList<Choice> Children { get; private set; }
    // chosen best move (index into children/legal moves)
    internal int BestIndex { get; private set; }
    // weight of best move as seen by move player
    internal double Weight { get; private set; }
    // number of times node visited
    internal int VisitCount { get; private set; }
    // number of times node visit was a win (by child, or self if IsDone)
    internal int WinCount { get; private set; }
    // true when (a) Board has result or (b) sub-tree analysis completed
    internal bool IsDone { get; private set; }

    // player choosing a move for board
    internal PlayerValue TurnPlayer { get { return Board.TurnPlayer; } }
    // player whose move led to this board (can be None)
    internal PlayerValue LastTurnPlayer { get { return Board.LastTurnPlayer; } }

    internal MoveModel BestMove { get { return BestIndex >= 0 ? Board.LegalMoves[BestIndex] : new MoveModel(); } }

    public int ResultDelta {
      get {
        return Board.MoveResult == ResultKinds.Win ? 1
             : Board.MoveResult == ResultKinds.Loss ? -1 : 0;
      }
    }

    public override string ToString() {
      return String.Format("Choice<{0}:{1},{2},{3:G4},w={4}/{5} ch={6} bx={7}>", 
        TurnPlayer, IsDone ? "D" : "N", Board.LastMove, Weight, WinCount, VisitCount, 
        Children.Where(c=>c!=null).Count(), BestIndex);
    }

    bool Logging(int level) { return level <= Logger.Level; }

    //-- privates
    int _next = 0;
    static int _stepper;

    //-- factories
    static internal Choice Create(BoardModel board) {
      return new Choice {
        Board = board,
        IsDone = board.HasResult,
        Weight = 0,
        BestIndex = -1, // invalid until set
        VisitCount = 0,
        WinCount = 0,
        Children = new Choice[board.LegalMoves.Count],
      };
    }

    //-- actions
    internal void ChooseFirst() {
      if (IsDone) return;
      BestIndex = 0;
      Extend(0);
      VisitCount = 1;
      Weight = -Children[0].Weight;
      IsDone = true;
    }

    // breadth-wise tree search, one step at a time
    // sets Done at top level when complete
    internal void ChooseBreadth(int steps) {
      Logger.WriteLine(3, ">Breadth={0}", steps);
      while (!IsDone && steps-- > 0)
        ChooseBreadth();
      Logger.WriteLine(3, ">Index={0} Weight={1}", BestIndex, Weight);
    }

    // on each step first extend children (one per step)
    // then invoke each child to do the same, until all done
    internal void ChooseBreadth() {
      if (IsDone) return;
      VisitCount++;
      var child = Children[_next];
      if (child == null) { // extend
        child = Extend(_next);
      } else { // select
        for (int i = 0; i < Children.Count; i++) {
          _next = (_next + 1) % Children.Count;
          //_next = (_next + i) % Children.Count;
          child = Children[_next];
          if (child.IsDone) {
            if (child.Weight < 0) break; // we won!
          } else break;
        }
        if (!child.IsDone) 
          child.ChooseBreadth();
      }
      // import weight into current level, if higher
      if (child.IsDone) {
        if (child.Weight < 0) {
          IsDone = true;
          Weight = -child.Weight * 0.99;
          BestIndex = _next;
        }  else if (-child.Weight > Weight) {
          Weight = -child.Weight * 0.99;
          BestIndex = _next;
        }
      }
      _next = (_next + 1) % Children.Count;
    }

    /////////////////////////////////////////////////////////////////////////////
    ///
    // TODO: code no longer works, fix or drop ===
    internal void ChooseByDepth(int depth) {
      if (Board.HasResult) return;
      //Logger.WriteLine(2, "{0}depth={1} move={2}", " . ".Repeat(5-depth), depth, Board.LastMove);
      var weight = -MoveWeight.ResultWin;
      // set up moves and check for win (loss for other side)
      for (int i = 0; i < Board.LegalMoves.Count; i++) {
        Extend(i);
        if (Children[i].Weight > weight) {  // *** FIX ***
          weight = Children[i].Weight;
          BestIndex = i;
        }
      }
      // no win yet and within allowed depth
      if (depth > 1 && weight != MoveWeight.ResultWin) {
        weight = -MoveWeight.ResultWin;
        for (int i = 0; i < Board.LegalMoves.Count && weight != MoveWeight.ResultWin; i++) {
          Children[i].ChooseByDepth(depth - 1);
          if (Children[i].Weight > weight) {
            weight = Children[i].Weight;
            BestIndex = i;
          }
        }
      }
      Weight = -weight + (weight > 0 ? 1 : -1);
    }

    ////////////////////////////////////////////////////////////////////////////
    //
    // Monte Carlo tree search, one step at a time
    // Reimplemented according to http://www.cameronius.com/cv/mcts-survey-master.pdf
    // sets Done at top level when complete (not impl)
    internal void ChooseMcts(int steps, int maxdepth) {
      Logger.WriteLine(3, ">Mcts steps={0} depth={1} this:{2}", steps, maxdepth, this);
      for (_stepper = 0; _stepper < steps && !IsDone; ++_stepper)
        ApplyMcts(maxdepth);
      BestIndex = SelectMaxUct(0);
      if (Logging(3)) {
        for (int i = 0; i < Children.Count; ++i)
          if (Children[i] != null) Logger.WriteLine("  {0}: {1} {2}", i, Children[i], Children[i].BestMove);
      }
      Logger.WriteLine(3, ">[Mcts st={0} this={1} {2}]", _stepper, this, BestMove);
    }

    // on each step: first extend children (one per step), play out, back propagate
    // then select child by UCT, play out, back propagate
    // mark and prune completed sub-trees until (maybe) all done
    internal void ApplyMcts(int maxdepth) {
      if (Logging(4)) Logger.WriteLine("ApplyMcts max={0} this={1}", maxdepth, this);
      var stack = new Stack<Choice>();
      var node = this;
      const double explconst = 1.414;

      // ref: apply tree policy
      while (stack.Count < maxdepth && !node.IsDone) {
        stack.Push(node);
        var index = node.SelectExtend();
        if (index != -1) {
          node.BestIndex = index;
          node = node.Extend(index);
          break;
        } else {
          index = node.SelectMaxUct(explconst);
          node.BestIndex = index;
          node = node.Children[index];
        }
      }

      // ref: apply default policy
      while (stack.Count < maxdepth && !node.IsDone) {
        stack.Push(node);
        var index = Board.NextRandom(node.Children.Count);
        node.BestIndex = index;
        node = node.Extend(index);
      }

      var delta = node.ResultDelta;
      while (node != null) {
        node.VisitCount++;
        node.WinCount += delta;
        node.Weight = (double)node.WinCount / (double)node.VisitCount;
        delta = -delta;
        node = (stack.Count == 0) ? null : stack.Pop();
      }
      if (Logging(4)) Logger.WriteLine("[ChooseMcts this:{0}]", this);
    }

    // pick next empty branch to extend, or -1 if none
    int SelectExtend() {
      var index = Board.NextRandom(Children.Count);
      for (int i = 0; i < Children.Count; i++) {
        if (Children[index] == null) return index;
        index = (index + 1) % Children.Count;
      }
      return -1;
    }

    // pick next branch, balancing known vs unknown, or -1 if none
    // if param=0 returns best
    int SelectMaxUct(double explparam) {
      var index = -1;
      var tlnvc = 2 * Math.Log(VisitCount);
      var uct = -9999.0;
      var firstuct = 0.0;
      for (int i = 0; i < Children.Count; i++) {
        var child = Children[i];
        if (child == null && explparam <= 0) continue; // null children allowed in final call
        //if (child == null && explparam == 0) continue; // null children allowed in final call
        if (child == null) throw Error.Assert("null child {0}", i);
        //if (child.IsDone) continue; // may need special?
        var thisuct = (double)child.WinCount / (double)child.VisitCount
          + explparam * Math.Sqrt(tlnvc / child.VisitCount);
        if (i == 0) firstuct = thisuct;
        if (thisuct > uct) {
          uct = thisuct;
          index = i;
        }
      }
      if (explparam == 0 && uct == firstuct) { // all the same, not advantage found, go for random
        var indexes = Enumerable.Range(0, Children.Count)
          .Where(x => Children[x] != null && !Board.LegalMoves[x].IsPass)
          .ToArray();
        index = (indexes.Length == 0) ? 0 : indexes[Board.NextRandom(indexes.Length)];
      }
      if (Logging(4)) Logger.WriteLine("-maxuct ex={0} ix={1} fu={2} mv{3}", 
        explparam, index, firstuct == uct, Board.LegalMoves[index]);
      return index;
    }

    // extend tree by adding specified move and board
    // non-specific -- used by all strategies
    internal Choice Extend(int index) {
      if (index < 0 || index >= Children.Count || Children[index] != null) throw Error.Assert("extend {0}", index);
      if (Logging(4)) Logger.WriteLine("-extend {0}:{1}", index, Board.LegalMoves[index]);
      var board = Board.MakeMove(index);
      Children[index] = Choice.Create(board);
      return Children[index];
    }
  }
}
