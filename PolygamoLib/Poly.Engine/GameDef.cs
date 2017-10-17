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
/// Game defs
/// 
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Poly.Common;
using Polygamo;

/// <summary>
/// These are game definitions. Data content is built by parsing game definition files, then immutable.
/// </summary>
namespace Poly.Engine {

  internal enum SoundKinds {
    Capture, Click, Change, Draw, Drop, Loss, Move, Opening, Release, Win,
    Music,
  }

  internal enum HelpKinds {
    Title, Description, History, Strategy,
    Help, Notation, Open,
  }

  internal enum ConditionKinds {
    Move, Draw, Loss, Win, Count,
  }

  internal enum GoalKinds {
    And, Or, Not,
    Stalemated, Repetition,
    Captured, Checkmated, Absolute, Relative, Remaining, PieceCount,
  }

  internal enum PlayerKinds {
    Any, Friend, Enemy, Neutral,
  }

  internal enum GoKinds {
    None, From, To, Mark, LastFrom, LastTo,
  }

  // for value that can be either position or zone
  internal class PositionOrZone {
    internal TypedValue Value;
    internal bool IsPosition { get { return Value is PositionValue; } }
    internal PositionValue Position { get { return Value as PositionValue; } }
    internal ZoneValue Zone { get { return Value as ZoneValue; } }
    public override string ToString() {
      return (IsPosition) ? Position.ToString() : Zone.ToString();
    }
  }

  // for value that can be either position or direction
  internal class PositionOrDirection {
    internal TypedValue Value;
    internal bool IsPosition { get { return Value is PositionValue; } }
    //internal bool IsDirection { get { return Value is DirectionValue; } }
    internal PositionValue Position { get { return Value as PositionValue; } }
    internal DirectionValue Direction { get { return Value as DirectionValue; } }
    public override string ToString() {
      return (IsPosition) ? Position.ToString() : Direction.ToString();
    }
  }

  // pairs of player and image
  internal class PieceImages {
    internal PlayerValue Player;
    internal IList<TextValue> Images;
  }

  internal class DefBase { }

  /// <summary>
  /// Information about available games and variants
  /// </summary>
  internal class MenuDef : DefBase {
    internal IList<GameDef> Games;
    internal GameDef Default;

    internal static MenuDef Create(IList<GameDef> gamedefs) {
      return new MenuDef {
        Games = gamedefs,
        Default = gamedefs.FirstOrDefault(g => g.IsDefault) ?? gamedefs[0],
      };
    }
  }

  ///---------------------------------------------------------------------------
  /// <summary>
  /// Information about a game (or variant)
  /// </summary>
  internal class GameDef : DefBase {
    internal bool IsDefault = false;
    internal readonly Dictionary<IdentValue, TypedValue> PropertyLookup = new Dictionary<IdentValue, TypedValue>();
    internal readonly Dictionary<PlayerValue, PlayerDef> PlayerLookup = new Dictionary<PlayerValue, PlayerDef>();
    internal readonly Dictionary<PieceValue, PieceDef> PieceLookup = new Dictionary<PieceValue, PieceDef>();
    internal readonly List<GoalDef> Goals = new List<GoalDef>();
    internal List<TurnDef> TurnOrders; // TODO: check not null
    internal List<SetupDef> SetupItems = new List<SetupDef>();
    // move priorities from high to low
    internal List<MoveTypeValue> MovePriorities = new List<MoveTypeValue>();
    internal BoardDef Board;
    internal GameCode Code;


    internal bool IsVariant { get { return _isvariant; } }

    private int _turn_repeat = 0;
    private bool _isvariant;

    internal void Dump(TextWriter tw) {
      tw.WriteLine("Game: ");
      tw.WriteLine("  Settings={0}", PropertyLookup.Join(","));
      tw.WriteLine("  Players={0}", PlayerLookup.Join(","));
      tw.WriteLine("  MovePriorities={0}", MovePriorities.Join(","));
      tw.WriteLine("  TurnOrders={0}", TurnOrders.Join(","));
      tw.WriteLine("  Setups={0}", SetupItems.Join(","));
      tw.WriteLine("  Goals={0}", Goals.Join(","));
      Board.Dump(tw);
    }

    internal static GameDef Create(GameCode code, bool isvariant) {
      return new GameDef() {
        Code = code,
        _isvariant = isvariant,
      }.Init();
    }

    private GameDef() { }

    GameDef Init() {
      Board = BoardDef.Create(null);  // FIX:???
      SetProperty("title", TextValue.Create("Unknown"));
      return this;
    }

    internal IEnumerable<PlayerValue> ActivePlayers {
      get { return PlayerLookup.Values
          .Where(p => p.IsActive)
          .Select(p=>p.Player);
      }
    }

    internal TurnDef GetTurn(int index) {
      int x = (index < TurnOrders.Count) ? index
        : _turn_repeat + (index - _turn_repeat) % (TurnOrders.Count - _turn_repeat);
      return TurnOrders[x];
    }

    internal void SetProperty(string name, TypedValue value) {
      PropertyLookup[IdentValue.Create(name)] = value;
    }

    internal void SetProperty(IdentValue ident, TypedValue value) {
      PropertyLookup[ident] = value;
    }

    internal TypedValue GetProperty(string name) {
      return GetProperty(IdentValue.Create(name));
    }
    internal TypedValue GetProperty(IdentValue name) {
      var value = PropertyLookup.SafeLookup(name);
      return value;
    }

    // typed getters
    internal bool GetBoolProperty(string name) {
      var ret = GetProperty(IdentValue.Create(name));
      if (ret == null) return false;
      if (ret.DataType == DataTypes.Bool) return ret == BoolValue.True;
      if (ret.DataType == DataTypes.Text) return ret.AsString.ToLower() == "true";
      if (ret.DataType == DataTypes.Number) return (ret as NumberValue).Value != 0;
      return false;
    }

    internal void AddGoal(ResultKinds kind, PlayerValue player, GoalCode code) {
      Goals.Add(new GoalDef {
        Kind = kind, Player = player, Code = code
      });
    }

    internal int GetIntProperty(string name) {
      var ret = GetProperty(IdentValue.Create(name));
      return (ret == null) ? 0 : (int)(ret as NumberValue).Value;
    }
    internal string GetStringProperty(string name) {
      var ret = GetProperty(IdentValue.Create(name));
      return (ret == null) ? null : ret.AsString;
    }


    //--- impl setup

    internal void AddSetup(PlayerValue player, PieceValue piece, int offqty, IList<PositionValue> positions) {
      SetupItems.Add(SetupDef.Create(player, piece, offqty, positions));
    }

    internal void AddPlayers(IList<PlayerValue> players) {
      foreach (var player in players)
        PlayerLookup[player] = new PlayerDef { Player = player };
    }

    internal void SetTurnOrder(List<TurnDef> turndefs) {
      TurnOrders = turndefs.Where(t => t != TurnDef.Repeat).ToList();
      _turn_repeat = turndefs.Select((t, x) => t == TurnDef.Repeat ? x : 0).Max();
      foreach (var player in PlayerLookup.Values)
        player.IsActive = TurnOrders.Any(t => t.TurnPlayer == player.Player);
    }

    internal void AddPiece(PieceDef piece) {
      PieceLookup.Add(piece.Piece, piece);
    }
  }

  ///---------------------------------------------------------------------------
  /// <summary>
  /// Definition for a player
  /// </summary>
  internal class PlayerDef : DefBase {
    internal PlayerValue Player;
    internal bool IsActive;

    internal bool IsNeutral { get { return !IsActive; } }
    internal bool IsRandom { get { return Player.Value.StartsWith("?"); } }

    public override string ToString() {
      return String.Format("{0}:{1}", Player.Value, IsActive ? "active" : "neutral");
    }
  }

  ///---------------------------------------------------------------------------
  /// <summary>
  /// Definition for a particular turn
  /// </summary>
  internal class TurnDef : DefBase {
    internal PlayerValue TurnPlayer;    // player on turn to move
    internal PlayerValue MovePlayer;    // moves made as this player
    internal MoveTypeValue MoveType;    // only moves of this type
    internal bool Skip;                 // only first time, then skip
    internal static TurnDef Repeat = new TurnDef();

    public override string ToString() {
      return String.Format("{0} as {1} type {2}{3}", TurnPlayer, MovePlayer, MoveType, (Skip ? " skip" : ""));
    }
  }

  ///---------------------------------------------------------------------------
  /// <summary>
  /// Define testing for end of game
  /// </summary>
  internal class GoalDef : DefBase {
    internal ResultKinds Kind;
    internal PlayerValue Player;
    internal GoalCode Code;

    public override string ToString() {
      return String.Format("{0}:{1}", Kind, Player);
    }
  }

  /// <summary>
  /// Occupier definition used by goal code
  /// </summary>
  internal class OccupierDef : DefBase {
    internal bool Not = false;
    internal PlayerKinds PlayerKind = PlayerKinds.Friend;
    internal PieceValue Piece;
    internal DirectionValue Direction; // relative only

    internal bool IsDirection { get { return Direction != null; } }

    public override string ToString() {
      return (IsDirection)
        ? String.Format("Occupier[{0}]", Direction)
        : String.Format("Occupier[{0}{1},{2}]", Not ? "not " : "", PlayerKind, Piece);
    }

    internal static OccupierDef Create(PlayerKinds kind, PieceValue piece) {
      return new OccupierDef {
        PlayerKind = kind, Piece = piece,
      };
    }

    internal static OccupierDef Create(DirectionValue direction) {
      return new OccupierDef {
        Direction = direction,
      };
    }

    internal OccupierDef SetNot() {
      Logger.Assert(!IsDirection, "invalid not direction");
      Not = !Not;
      return this;
    }
  }

  /// <summary>
  /// Board definition
  /// </summary>
  internal class BoardDef : DefBase {
    // executable code to create a board
    internal BoardCode Code;
    // images for this board, chosen by user
    internal List<TextValue> Images = new List<TextValue>();
    // known positions for this board
    internal readonly Dictionary<PositionValue, PositionDef> PositionLookup = new Dictionary<PositionValue, PositionDef>();
    // known zones for this board
    internal readonly Dictionary<Pair<ZoneValue, PlayerValue>, HashSet<PositionValue>> ZoneLookup = 
      new Dictionary<Pair<ZoneValue, PlayerValue>, HashSet<PositionValue>>();
    // known links for this board
    internal readonly Dictionary<PositionValue, List<LinkDef>> LinkLookup = new Dictionary<PositionValue, List<LinkDef>>();
    // known symmetries for this board
    internal readonly Dictionary<Pair<PlayerValue,DirectionValue>, DirectionValue> SymmetryLookup =
      new Dictionary<Pair<PlayerValue, DirectionValue>, DirectionValue>();

    internal int Dimensions = 0;

    BoardDef() { }
    internal static BoardDef Create(BoardCode code) {
      return new BoardDef {
        Code = code,
      };
    }

    internal void Dump(TextWriter tw) {
      tw.WriteLine("Board: dimensions={0}", Dimensions);
      tw.WriteLine("  Images={0}", Images.Join());
      tw.WriteLine("  Positions={0}", PositionLookup.Values.Join("; "));
      var sl = LinkLookup.Select(kv => string.Format("{0}:({1})", kv.Key, kv.Value.Join()));
      tw.WriteLine("  Links={0}", sl.Join("; "));
      var ss = ZoneLookup.Select(kv => String.Format("{0},{1},{2}", kv.Key.Item1, kv.Key.Item2, kv.Value.Join()));
      tw.WriteLine("  Zones={0}", ss.Join(";"));
    }

    // enquiry functions
    internal bool InZone(ZoneValue zone, PlayerValue player, PositionValue position) {
      var z = ZoneLookup.SafeLookup(Pair.Create(zone, player));
      return z != null && z.Contains(position);
    }

    // get position from position and direction
    internal PositionValue GetPosition(PositionValue position, PlayerValue player, DirectionValue direction) {
      if (!LinkLookup.ContainsKey(position)) return null;
      var newdir = SymmetryLookup.SafeLookup(Pair.Create(player, direction)) ?? direction;
      var link = LinkLookup[position].FirstOrDefault(k => k.Direction == newdir);
      return (link == null) ? null : link.To;
    }

    // get position following direction in reverse
    internal PositionValue GetOpposite(PositionValue position, DirectionValue direction) {
      // for every key for every value match to and direction, return position
      return LinkLookup.Keys.FirstOrDefault(p => LinkLookup[p]
        .Any(lk => lk.To == position && lk.Direction == direction));
    }

    // iterate positions for zone and player
    internal IEnumerable<PositionValue> PositionIter(ZoneValue zone, PlayerValue player) {
      return ZoneLookup[Pair.Create(zone, player)];
    }

    // iterate all adjacent positions for player
    internal IEnumerable<PositionValue> AdjacentIter(PositionValue position, PlayerValue player) {
      var links = LinkLookup.SafeLookup(position); // might be no links on this position
      if (links != null)
        foreach (var link in links)
          yield return link.To;
    }

    //--- impl board creation

    internal PositionValue GetPosition(string name) {
      var position = PositionValue.Create(name);
      SetPosition(position);
      return position;
    }

    void SetPosition(PositionValue position) {
        if (!PositionLookup.ContainsKey(position)) PositionLookup[position] = new PositionDef {
        Position = position
      };
    }

    internal void SetPosition(PositionValue position, Rect location, IList<int> coords = null) {
      PositionLookup[position] = new PositionDef {
        Position = position, Location = location, Coords = coords == null ? null : coords.ToArray()
      };
    }

    internal void RemovePosition(PositionValue position) {
      RemoveLink(position);
      PositionLookup.Remove(position);
    }

    //--- links
    internal void AddLink(DirectionValue direction, PositionValue frompos, PositionValue topos) {
      LinkLookup.AddMulti(frompos, LinkDef.Create(frompos, direction, topos));
    }

    // Remove all links for position
    internal void RemoveLink(PositionValue frompos) {
      var links = LinkLookup.GetMulti(frompos);
      if (links != null) {
        // remove back link
        foreach (var link in links.ToArray()) // copy collection to insulate from removals
        //foreach (var link in links) -- cannot repro error
            RemoveLink(link.To, link.From);
        // remove key
        LinkLookup.Remove(frompos);
      }
    }

    // Remove all links between two positions
    internal void RemoveLink(PositionValue frompos, PositionValue topos) {
      var links = LinkLookup.GetMulti(frompos);
      if (links != null) {
        links.RemoveAll(l => l.To == topos);
        if (links.Count == 0) LinkLookup.Remove(frompos);
      }
    }

    // remove link by direction
    internal void RemoveLink(PositionValue position, DirectionValue direction) {
      var links = LinkLookup.GetMulti(position);
      if (links != null) {
        links.RemoveAll(l => l.Direction == direction);
        if (links.Count == 0) LinkLookup.Remove(position);
      }
    }

    //--- zones
    internal void AddZone(ZoneDef zone) {
      foreach (var player in zone.Players)
        ZoneLookup[Pair.Create(zone.Name, player)] = zone.Positions;
    }

    //-- symmetries
    internal void AddSymmetry(PlayerValue player, DirectionValue directionValue1, DirectionValue directionValue2) {
      SymmetryLookup[Pair.Create(player, directionValue1)] = directionValue2;
    }
  }

  ///---------------------------------------------------------------------------
  /// <summary>
  /// Defines the links from one position to another by direction
  /// </summary>
  internal class LinkDef : DefBase {
    internal PositionValue From;
    internal DirectionValue Direction;
    internal PositionValue To;

    public override string ToString() {
      return String.Format("Link[{0},{1},{2}]", From, Direction, To);
    }

    internal static LinkDef Create(PositionValue from, DirectionValue direction, PositionValue to) {
      return new LinkDef { From = from, Direction = direction, To = to };
    }

  }

  ///---------------------------------------------------------------------------
  /// <summary>
  /// Defines a named position and where it should appear on the board
  /// </summary>
  internal class PositionDef : DefBase {
    internal PositionValue Position;
    internal Rect Location;
    internal int[] Coords;

    internal bool IsDummy { get { return Location.IsEmpty; } }

    public override string ToString() {
      return String.Format("{0}:{1}", Position, Location);
    }
  }

  ///---------------------------------------------------------------------------
  /// <summary>
  /// Defines a board zone for addition to the board
  /// </summary>
  internal class ZoneDef : DefBase {
    internal ZoneValue Name;
    internal HashSet<PlayerValue> Players;
    internal HashSet<PositionValue> Positions;
  }

  ///---------------------------------------------------------------------------
  /// <summary>
  /// Dummy class just for compiler to use
  /// </summary>
  internal class GridDef : DefBase { }
  internal class DimPosDef : DefBase { }

  //internal class DimensionDef : DefBase {
  //  internal List<Tuple<TextValue, Coord>> DimensionList;
  //}

  ///---------------------------------------------------------------------------
  /// <summary>
  /// Defines pieces that comprise the initial state of the board
  /// </summary>
  internal class SetupDef {
    internal PlayerValue Player;
    internal PieceValue Piece;
    internal IList<PositionValue> Positions;
    internal int OffQuantity;

    public override string ToString() {
      return String.Format("Setup[{0},{1},off={2},pos={3}]", Player, Piece, OffQuantity, Positions.Join());
    }

    internal static SetupDef Create(PlayerValue player, PieceValue piece, int offquantity, IList<PositionValue> positions) {
      return new SetupDef { Player = player, Piece = piece, Positions = positions, OffQuantity = offquantity };
    }
  }

  ///---------------------------------------------------------------------------
  /// <summary>
  /// Compiler only
  /// </summary>
  internal class PlacementDef {
    internal PieceValue Piece;
    internal IList<PositionValue> Positions;
    internal int OffQuantity;
    public override string ToString() {
      return String.Format("{0} off:{1} at:{2}", Piece, OffQuantity, Positions.Join());
    }
  }

  ///---------------------------------------------------------------------------
  /// <summary>
  /// Piece definition
  /// </summary>
  internal class PieceDef : DefBase {
    internal PieceValue Piece;
    internal bool IsDummy = false;
    internal readonly Dictionary<HelpKinds, TextValue> HelpLookup = new Dictionary<HelpKinds, TextValue>();
    internal readonly Dictionary<PlayerValue, IList<TextValue>> ImageLookup = new Dictionary<PlayerValue, IList<TextValue>>();
    internal readonly Dictionary<IdentValue, TypedValue> AttributeLookup = new Dictionary<IdentValue, TypedValue>();
    internal readonly IList<MoveDef> Drops = new List<MoveDef>();
    internal readonly IList<MoveDef> Moves = new List<MoveDef>();
    public long LongHash { get; internal set; }

    internal string Name { get { return Piece.Value; } }

    public override string ToString() {
      return Piece.Value + (IsDummy ? " dummy" : "");
    }

    internal void AddImages(PlayerValue player, IList<TextValue> images) {
      ImageLookup[player] = images;
    }

    internal IList<TextValue> GetImages(PlayerValue player) {
      return ImageLookup.SafeLookup(player) ?? new List<TextValue> { TextValue.Empty };
    }
  }

  ///---------------------------------------------------------------------------
  /// <summary>
  /// Defines a move as a set of steps
  /// </summary>
  internal class MoveDef : DefBase {
  }

}
