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
/// Compiler classes
/// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Poly.Common;
using Poly.Engine;
using System.Diagnostics;

/// <summary>
/// Recursive descent parser driven mainly by Builtin type definitions
/// </summary>
namespace Poly.Compiler {
  /// <summary>
  /// Hold top level node structure to facilitate variant processing
  /// </summary>
  internal class GameIndex {
    Func<Node, string> name = (Node a) => a.AsList[0].AsIdent.Name;
    readonly HashSet<string> condlookup = new HashSet<string> {
      "win-condition", "loss-condition", "draw-condition"
    };
    IList<Node> _gnodes;
    SymbolTable _symbols;
    Dictionary<string, Node> _gpiecelookup;

    // Create piece index from array of section nodes
    // Note: Also does sanity check (first peek inside game/variant definition)
    // The passed in parser is used to generate meaningful errors
    Dictionary<string, Node> CreatePieceLookup(NodeListParser nlp) {
      Dictionary<string, Node> lookup = new Dictionary<string, Node>();
      while (!nlp.Done) {
        var pnode = nlp.Current; // could be a piece section
        var nlp2 = nlp.GetParser();
        // note: cannot use anything that relies on CheckedIdent
        nlp.Expect(nlp2.IsIdent, "section identifier");
        if (nlp2.GetNode().AsIdent.Name == "piece") {
          while (true) {
            nlp2.Expect(nlp2.IsList, "piece name");
            var nlp3 = nlp2.GetParser();
            if (nlp3.IsIdent && nlp3.GetNode().AsIdent.Name == "name") {
              if (nlp3.IsIdent) {
                lookup[nlp3.GetNode().AsIdent.Name] = pnode;
                break;
              }
            }
          }
        }
      }
      return lookup;
    }

    // record info about base game
    internal IList<Node> IndexGame(NodeListParser nlp) {
      _symbols = nlp.Symbols;

      // Step 1. Construct piece index and remove pieces
      _gpiecelookup = CreatePieceLookup(nlp);
      _gnodes = nlp.Nodes.Where(n => name(n) != "piece").ToList();

      // step 2. Reconstruct node list while predefining piece names (arg is empty dict)
      return _gnodes.Concat(GetPieceList(new Dictionary<string, Node>())).ToList();
    }

    // merge game and variant to create new game
    internal IList<Node> MergeVariant(NodeListParser nlp) {

      // Rule 1. Remove all piece nodes -- special handling at end
      var plookup = CreatePieceLookup(nlp);
      var vnodes = nlp.Nodes.Where(n => name(n) != "piece").ToList();

      // Rule 2. If variant has any conditions, remove all game conditions
      var hascond = vnodes.Any(v => condlookup.Contains(name(v)));
      var gnodesng = (hascond) ? _gnodes.Where(n => !condlookup.Contains(name(n))) : _gnodes;

      // Rule 3. For each game node, replace with matching variant node if any
      // Note: must use each variant node only once; preserves order of input file
      var newnodes = new List<Node>();
      foreach (var n in gnodesng) {
        var i = vnodes.FindIndex(v => name(n) == name(v));
        if (i != -1) {
          newnodes.Add(vnodes[i]);
          vnodes.RemoveAt(i);
        } else newnodes.Add(n);
      }

      // Rule 3. Append any unmatched variant nodes to the end, followed by pieces
      newnodes.AddRange(vnodes);
      newnodes.AddRange(GetPieceList(plookup));

//      Logger.WriteLine(3, "Merge {0} '{1}' '{2}'", hascond, vnodes.Select(n => name(n)).Join(),
//        newnodes.Select(n => name(n)).Join());
      return newnodes;
    }

    // Combine two piece lookups into a list, adding each to the symbol table
    IList<Node> GetPieceList(Dictionary<string, Node> variantpiecelookup) {
      var piecelist = new List<Node>();
      foreach (var name in _gpiecelookup.Keys) {
        piecelist.Add(variantpiecelookup.ContainsKey(name) ? variantpiecelookup[name] : _gpiecelookup[name]);
        _symbols.DefineValue(name, PieceValue.Create(name));
      }
      foreach (var name in variantpiecelookup.Keys)
        if (!_gpiecelookup.ContainsKey(name)) {
          piecelist.Add(variantpiecelookup[name]);
          _symbols.DefineValue(name, PieceValue.Create(name));
        }
      return piecelist;
    }
  }

  /// <summary>
  /// Base class provides lookup tables and utility methods
  /// </summary>
  internal class CompilerBase {

    internal static string BasePath;
    internal static SymbolTable Symbols;
    protected Generator _gen;
    // context for low level error messages that may not have their own parser
    protected NodeListParser _currentparser = null;

    // these functions serve to ensure that symbols in nodes are defined as the right type
    protected TypedValue DefSymbolValue(IdentNode node, TypedValue value) {
      if (node.Sym.IsValue && node.Sym.DataType == value.DataType)
        return node.Sym.Value;
      Logger.WriteLine(3, "Getsym adding {0} value:{1} type:{2}", node.Sym.Name, value, value.DataType);
      // will create new symbol and store in symbol table, so other nodes see the same thing
      if (node.Sym.IsUndef)
        Symbols.DefineValue(node.Name, value);
      else if (value.GetType() == typeof(IdentValue) && node.Sym.Value is IdentValue)
        ; // any subclass will do for flag. TODO: reverse operation if flag defined first
      else _currentparser.Syntax("already defined {0} as type {1}", node.Name, node.Sym.DataType);
      return value;
    }

    protected AttributeValue DefAttribute(IdentNode node) {
      return DefSymbolValue(node, AttributeValue.Create(node.Name)) as AttributeValue;
    }

    protected DirectionValue DefDirection(IdentNode node) {
      return DefSymbolValue(node, new DirectionValue { Value = node.Name }) as DirectionValue;
    }

    protected MoveTypeValue DefMoveType(IdentNode node) {
      return DefSymbolValue(node, new MoveTypeValue { Value = node.Name }) as MoveTypeValue;
    }

    protected PieceValue DefPiece(IdentNode node) {
      return DefSymbolValue(node, PieceValue.Create(node.Name)) as PieceValue;
    }

    protected PlayerValue DefPlayer(IdentNode node) {
      return DefSymbolValue(node, PlayerValue.Create(node.Name)) as PlayerValue;
    }

    protected PositionValue DefPosition(IdentNode node) {
      return DefSymbolValue(node, PositionValue.Create(node.Name)) as PositionValue;
    }

    protected IdentValue DefIdent(IdentNode node) {
      return DefSymbolValue(node, new IdentValue { Value = node.Name }) as IdentValue;
    }

    protected ZoneValue DefZone(IdentNode node) {
      return DefSymbolValue(node, new ZoneValue { Value = node.Name }) as ZoneValue;
    }

    static readonly Dictionary<string, OptionValue> _optionlookup = new Dictionary<string, OptionValue> {
      { "true", OptionValue.True },
      { "false", OptionValue.False },
      { "default", OptionValue.Default },
      { "forced", OptionValue.Forced },
    };

    protected OptionValue GetOption(NodeListParser nlp) {
      if (nlp.IsNumber) return OptionValue.Create(nlp.GetNumber().Value);
      if (nlp.IsBool) return OptionValue.Create(nlp.GetBool().Value ? 1 : 0);
      // note that options are case insensitive
      if (nlp.IsIdent && _optionlookup.ContainsKey(nlp.CurrentIdent.Name.ToLower()))
        return _optionlookup[nlp.GetIdent().Name.ToLower()];
      nlp.Expected("number or option keyword");
      return OptionValue.Default;
    }
  }

  /// <summary>
  /// 
  /// </summary>
  internal class Compiler : CompilerBase {
    // dictionary for types that can be compiled into a value
    static Dictionary<Type, Func<Type, NodeListParser, Compiler, object>> _typeactiondict = new Dictionary<Type, Func<Type, NodeListParser, Compiler, object>> {
      // general typed values suitable for EmitLoadValue
      { typeof(TypedValue), (t,nlp,cb) => nlp.GetValue() },
      { typeof(BoolValue), (t,nlp,cb) => nlp.GetBool() },
      { typeof(NumberValue), (t,nlp,cb) => nlp.GetNumber() },
      { typeof(TextValue), (t,nlp,cb) => nlp.GetText() },
      { typeof(OptionValue), (t,nlp,cb) => cb.GetOption(nlp) },

      { typeof(AttributeValue), (t,nlp,cb) => cb.DefAttribute(nlp.GetIdent()) },
      { typeof(DirectionValue), (t,nlp,cb) => cb.DefDirection(nlp.GetIdent()) },
      { typeof(IdentValue), (t,nlp,cb) => cb.DefIdent(nlp.GetIdent()) },
      { typeof(MoveTypeValue), (t,nlp,cb) => cb.DefMoveType(nlp.GetIdent()) },
      { typeof(PieceValue), (t,nlp,cb) => cb.DefPiece(nlp.GetIdent()) },
      { typeof(PlayerValue), (t,nlp,cb) => cb.DefPlayer(nlp.GetIdent()) },
      { typeof(PositionValue), (t,nlp,cb) => cb.DefPosition(nlp.GetIdent()) },
      { typeof(ZoneValue), (t,nlp,cb) => cb.DefZone(nlp.GetIdent()) },

      { typeof(PositionOrZone), (t,nlp,cb) => cb.ParsePositionOrZone(nlp) },
      { typeof(Maybe<PositionOrZone>), (t,nlp,cb) => cb.ParseMaybePositionOrZone(nlp) },
      //{ typeof(PositionOrDirection), (t,nlp,cb) => cb.ParsePositionOrDirection(nlp) },
      { typeof(GoKinds), (t,nlp,cb) => cb.ParseGoKind(nlp) },
      { typeof(Maybe<PlayerValue[]>), (t,nlp,cb) => cb.ParseMaybePlayers(nlp) },
      { typeof(Maybe<PlayerValue>), (t,nlp,cb) => cb.ParseMaybePlayer(nlp) },
      { typeof(Maybe<PieceValue>), (t,nlp,cb) => cb.ParseMaybePiece(nlp) },
      // specials for complex structures
      { typeof(TurnDef), (t,nlp,cb) => cb.ParseTurnDef(nlp) },
      { typeof(List<PlacementDef>), (t,nlp,cb) => cb.ParsePlacements(nlp) },
      { typeof(List<OccupierDef>), (t,nlp,cb) => cb.ParseOccupiers(nlp) },
      { typeof(DimPosDef), (t,nlp,cb) => cb.ParseDimensionPositions(nlp) },
      { typeof(PieceImages), (t,nlp,cb) => cb.ParsePieceImages(nlp) },

      { typeof(Coord), (t,nlp,cb) => {
        var n = nlp.GetIntList(2);
        return Coord.Create(n[0], n[1]); } },
      { typeof(Rect), (t,nlp,cb) => {
        var n = nlp.GetIntList(4);
        return Rect.Create(n[0], n[1], n[2], n[3]); } },
      { typeof(List<NumberValue>), (t,nlp,cb) => {
        var n = nlp.GetNumberTail();
        return n.ToList(); } },
      { typeof(Pair<PositionValue,Rect>), (t,nlp0,cb) => {
        var nlp = nlp0.GetParser();
        var pv = cb.DefPosition(nlp.GetIdent());
        var nn = nlp.GetIntTail(4);
        return Pair.Create(pv, Rect.Create(nn[0], nn[1], nn[2], nn[3]));
      } },
    };

    // lookup keyword type (from symbol) and dispatch to compile function
    static Dictionary<Keywords, Action<Symbol, NodeListParser, Compiler>> _controllookup = new Dictionary<Keywords, Action<Symbol, NodeListParser, Compiler>> {
      { Keywords.IF,   (s,nlp,cb)=>cb.CompileIf(s,nlp) },
      { Keywords.WHILE,(s,nlp,cb)=>cb.CompileWhile(s,nlp) },
      { Keywords.AND,  (s,nlp,cb)=>cb.CompileAnd(s,nlp) },
      { Keywords.OR,   (s,nlp,cb)=>cb.CompileOr(s,nlp) },
    };

    // list of nodes for base game, used to compile variants
    GameIndex _game_index = null;
    
    //--------------------------------------------------------------------------
    //-- factory
    internal static Compiler Create(string path, Generator generator, SymbolTable symbols) {
      BasePath = Path.GetFullPath(path);
      Symbols = symbols;
      return new Compiler { _gen = generator, };
    }

    // main entry point, parses all the variants and returns a list
    internal void CompileMenu(IList<Node> nodes) {
      Logger.WriteLine(1, "CompileMenu {0} nodes", nodes.Count);
      CompileProg(PredefScopes.MENU, NodeListParser.Create(nodes, Symbols));
    }

    // Compile top level scoped program block, scoped expression or variant merge
    private void CompileProg(BuiltinScopeInfo info, NodeListParser nlp) {
      if (info.Kind == PredefKinds.PROGM)
        CompileGameOrVariant(info.Predef, nlp);
      else if (info.Kind == PredefKinds.PROG)
        CompileProg(info.Predef, nlp);
      else if (info.Kind == PredefKinds.EXPR)
        CompileExpr(info.Predef, nlp);
      else throw Error.Assert("{0}", info.Kind);
    }

    // Compile and merge nodes from base game and variant
    internal void CompileGameOrVariant(PredefScopes predef, NodeListParser nlp) {
      Logger.WriteLine(1, "CompileGameOrVariant {0} <{1}>", predef, nlp);

      Symbols.PushPredefScope(predef);
      Symbols.CurrentScope.Push(); // game index will define pieces
      if (_game_index == null) {
        _game_index = new GameIndex();
        _currentparser = NodeListParser.Create(_game_index.IndexGame(nlp), nlp.Symbols);
      } else {
        _currentparser = NodeListParser.Create(_game_index.MergeVariant(nlp), nlp.Symbols);
      }
      var codetype = Symbols.PredefScopeDict[predef].CodeType;
      _gen.EmitEntry(codetype);
      CompileProg(_currentparser);
      _gen.EmitExit(false);
      Symbols.CurrentScope.Pop();
      Symbols.PopPredefScope();
    }

    internal void CompileProg(PredefScopes predef, NodeListParser nlp) {
      Logger.WriteLine(2, "CompileProg {0} <{1}>", predef, nlp);

      _currentparser = nlp;
      Symbols.PushPredefScope(predef);
      if (predef == PredefScopes.GAME) Symbols.CurrentScope.Push();
      var codetype = Symbols.PredefScopeDict[predef].CodeType;
      _gen.EmitEntry(codetype);
      CompileProg(nlp);
      _gen.EmitExit(false);
      Symbols.PopPredefScope();
      if (predef == PredefScopes.GAME) Symbols.CurrentScope.Pop();
    }

    // compile a prog block inside an existing scope
    internal void CompileProg(NodeListParser nlp) {
      Logger.WriteLine(2, "CompileProg <{0}>", nlp);
      // iterate over the action items in the prog block
      while (!nlp.Done) {

        // value -- special handler
        if (nlp.IsValue || nlp.IsValueCallable) {
          if (!CompileHandler("--value", nlp)) nlp.Syntax("bare value not allowed");

        // callable function, call it
        } else if (nlp.IsCallable) {
          if (nlp.IsFunc && nlp.CurrentIdent.Sym.Keyword == Keywords.ELSE) return; // sneaky!
          var sexpr = nlp.GetSexprNode();
          nlp.Expect(sexpr.DataType == DataTypes.Void, "void function");
          CompileSexpr(sexpr, nlp);

        // list -- special handler
        } else if (nlp.IsList) {
          var handler = Symbols.Find("--list") as BuiltinSymbol;
          if (handler != null)
            CompileBuiltin(handler, nlp.GetParser());
          else nlp.Unexpected("unknown function");
        } else nlp.Expected("function call");
      }
    }

    // Compile a scoped expression that returns a typed value
    DataTypes CompileExpr(PredefScopes predef, NodeListParser nlp) {
      Logger.WriteLine(2, "CompileExpr {0} <{1}>", predef, nlp);

      Symbols.PushPredefScope(predef);
      var codetype = Symbols.PredefScopeDict[predef].CodeType;
      _gen.EmitEntry(codetype);

      nlp.Expect(nlp.IsCallable, "callable function");
      var sexpr = nlp.GetSexprNode();
      nlp.Expect(sexpr.DataType != DataTypes.Void, "typed function");
      CompileSexpr(sexpr, nlp);

      Symbols.PopPredefScope();
      _gen.EmitExit(true);
      return sexpr.DataType;
    }

    void CompileSexpr(SexprNode sexpr, NodeListParser nlp) {
      Logger.WriteLine(4, "CompileSexpr {0} <{1}>", sexpr, nlp);

      var snlp = NodeListParser.Create(sexpr.Args, Symbols);
      _currentparser = snlp; 

      // intrinsics with individual parser and code generator
      if (sexpr.Sym.IsControl) {
        if (_controllookup.ContainsKey(sexpr.Sym.Keyword))
          _controllookup[sexpr.Sym.Keyword](sexpr.Sym, snlp, this);
        else nlp.Unexpected("unknown control function");
      } else CompileBuiltin(sexpr.Sym as BuiltinSymbol, snlp);
    }

    // Compile a function with given arguments
    void CompileBuiltin(BuiltinSymbol funcsym, NodeListParser nlp) {
      Logger.WriteLine(3, "CompileBuiltin {0} <{1}>", funcsym.Name, nlp);
      var ci = funcsym.CallInfo;
      for (int i = 0; i < ci.Arguments.Count; i++) {
        if (ci.ArgOptional[i] && nlp.Done)
          _gen.EmitLoadValue(null);
        else if (ci.Arguments[i] == typeof(AttributeValue))
          _gen.EmitLoadValue(DefAttribute(nlp.GetIdent())); // special for attribute
        // TODO: could possibly check for combo of boolean expected and undefined ident, and emit false
        else CompileArg(ci.Arguments[i], nlp);
      }
      nlp.Expect(nlp.Done, "no more arguments for {0}", funcsym.Name);
      _gen.EmitCall(funcsym);
    }

    // compile a handler for a single value, caller handles errors
    bool CompileHandler(string name, NodeListParser nlp) {
      var funcsym = Symbols.Find(name) as BuiltinSymbol;
      if (funcsym == null) return false;
      var ci = funcsym.CallInfo;
      if (ci.NumArgs != 1) throw Error.Assert("arg count");
      CompileArg(ci.Arguments[0], nlp);
      _gen.EmitCall(funcsym);
      return true;
    }

    // Compile an argument of given type
    void CompileArg(Type type, NodeListParser nlp) {
      Logger.WriteLine(4, "CompileArg {0} <{1}>", type, nlp);

      var func = _typeactiondict.SafeLookup(type);

      // special for variable assignment and implicit definition
      if (type == typeof(Variable)) {
        var ident = nlp.GetIdent();
        var datatype = DataTypes.Bool; // TODO: handle other types
        if (ident.Sym.IsVariable)
          nlp.Expect(ident.Sym.DataType == datatype, "variable of type {0}", datatype);
        else if (ident.Sym.IsUndef)
          Symbols.DefineVariable(ident.Name, datatype, BoolValue.False);
        else nlp.Expected("defined variable");
        _gen.EmitRefVar(ident.Sym);

      // handle these separately, could be variable or function
      } else if (type.IsSubclassOf(typeof(TypedValue))) {
        // TODO: type checking
        CompileValue(type, nlp);
        //var exptype = TypedValue.DataTypeDict.SafeLookup(type);
        //var rettype = CompileValue(type, nlp);
        //nlp.Expect(rettype == exptype, "value of type {0}", exptype);

        // direct lookup gets special cases; else continue
      } else if (func != null) {
        var value = func(type, nlp, this);
        _gen.EmitLoadValue(value);

      // Array means parse a list of this type
      } else if (type.IsArray) {
        var nargs = 0;
        for (var nlp2 = nlp.GetParser(); !nlp2.Done; nargs++)
          CompileArg(type.GetElementType(), nlp2);
        _gen.EmitToArray(type, nargs);

      // List<> means parse a tail of this type
      } else if (type.IsGenericType && type.Name.StartsWith("List")) {
        var nargs = 0;
        for (; !nlp.Done; ++nargs)
          CompileArg(type.GetGenericArguments()[0], nlp);
        _gen.EmitToList(type, nargs);

      // Pair<> means parse a pair of arbitrary types
      } else if (type.IsGenericType && type.Name.StartsWith("Pair")) {
        var nlp2 = nlp.GetParser();
        foreach (var subtype in type.GetGenericArguments())
          CompileArg(subtype, nlp2);
        _gen.EmitToPair(type);

      // nested prog block
      } else if (type.IsSubclassOf(typeof(CodeBase))) {
        var info = Symbols.PredefScopeDict.First(kv => kv.Value.CodeType == type).Value;
        CompileProg(info, nlp);

      } else if (type == typeof(PositionOrDirection)) {
        CompilePositionOrDirection(nlp);

        // function call on built in
      } else if (nlp.IsSexpr) {
        var sexpr = nlp.GetSexprNode();
        var datatype = TypedValue.DataTypeDict.SafeLookup(type);
        nlp.Expect(sexpr.DataType == datatype, "value of type {0}", datatype);
        CompileBuiltin(sexpr.Sym as BuiltinSymbol, NodeListParser.Create(sexpr.Args, Symbols));

      } else nlp.Syntax("unknown type {0}", type);
    }

    // Compile a literal, symbol or expression that returns a typed value
    // Return datatype to caller for type checking
    DataTypes CompileValue(Type type, NodeListParser nlp) {
      Logger.WriteLine(4, "CompileValue {0} <{1}>", type, nlp);

      // bracketed expression with arguments
      if (nlp.IsSexpr) {
        var sexpr = nlp.GetSexprNode();
        CompileSexpr(sexpr, nlp);
        return sexpr.DataType;
      }
      if (nlp.IsFunc) {
        // bare function no arguments
        var sym = nlp.GetIdent().Sym as BuiltinSymbol;
        CompileBuiltin(sym, NodeListParser.Null);
        return sym.DataType;
      }
      if (nlp.IsVariable) {
        var sym = nlp.GetIdent().Sym;
        _gen.EmitLoadVar(sym);
        return sym.DataType;
      }
      if (nlp.IsAttribute || (nlp.IsList && nlp.CheckedHead.IsAttribute)) {
        var handler = Symbols.Find("--attribute") as BuiltinSymbol;
        if (handler != null)
          CompileBuiltin(handler, nlp.GetParser());
        else nlp.Syntax("attribute not allowed here");
        return handler.DataType;
      }
      var func = _typeactiondict.SafeLookup(type);
      if (func != null) {
        // direct lookup to get constant value
        var value = func(type, nlp, this) as TypedValue;
        _gen.EmitLoadValue(value);
        return value.DataType;
      }
      nlp.Unexpected("unknown type {0}", type);
      return DataTypes.Unknown;
    }

    //void CompileValue(Type type, NodeListParser nlp) {
    //  Logger.WriteLine(4, "CompileValue {0} <{1}>", type, nlp);

    //  var func = _typeactiondict.SafeLookup(type);
    //  // bracketed expression with arguments
    //  if (nlp.IsSexpr) {
    //    var sexpr = nlp.GetSexprNode();
    //    var datatype = TypedValue.DataTypeDict.SafeLookup(type);
    //    nlp.Expect(sexpr.DataType == datatype, "value of type {0}", datatype);
    //    CompileSexpr(sexpr, nlp);
    //  } else if (nlp.IsFunc) {
    //    // bare function no arguments
    //    var sym = nlp.GetIdent().Sym as BuiltinSymbol;
    //    CompileBuiltin(sym, NodeListParser.Null);
    //  } else if (nlp.IsVariable) {
    //    var sym = nlp.GetIdent().Sym;
    //    _gen.EmitLoadVar(sym);
    //  } else if (nlp.IsAttribute || (nlp.IsList && nlp.PeekList.IsAttribute)) {
    //    var handler = Symbols.Find("--attribute") as BuiltinSymbol;
    //    if (handler != null)
    //      CompileBuiltin(handler, nlp.GetParser());
    //    else nlp.Syntax("attribute not allowed here");
    //  } else if (func != null) {
    //    // direct lookup to get constant value
    //    var value = func(type, nlp, this);
    //    _gen.EmitLoadValue(value);
    //  } else nlp.Unexpected("unknown type {0}", type);
    //}

    //--------------------------------------------------------------------------
    // Compile control constructs that generate GOTOs and emit custom code

    void CompileIf(Symbol sym, NodeListParser nlp) {
      CompileArg(typeof(BoolValue), nlp);
      var pos1 = _gen.Counter;
      _gen.EmitGoFalse(0);
      CompileProg(nlp); // will return without taking else
      if (nlp.IsCallable && nlp.CurrentIdent.Sym.Keyword == Keywords.ELSE) {
        nlp.GetSexprNode();
        var pos2 = _gen.Counter;
        _gen.EmitGoTo(0);
        _gen.Fixup(pos1);
        CompileProg(nlp);
        _gen.Fixup(pos2);
      } else _gen.Fixup(pos1);
    }

    void CompileWhile(Symbol sym, NodeListParser nlp) {
      var pos1 = _gen.Counter;
      CompileArg(typeof(BoolValue), nlp);
      var pos2 = _gen.Counter;
      _gen.EmitGoFalse(0);
      CompileProg(nlp);
      _gen.EmitGoTo(pos1);
      _gen.Fixup(pos2);
    }

    void CompileOr(Symbol sym, NodeListParser nlp) {
      var fixups = new Stack<int>();
      _gen.EmitLoadValue(BoolValue.True);
      while (!nlp.Done) {
        CompileArg(typeof(BoolValue), nlp);
        fixups.Push(_gen.Counter);
        _gen.EmitGoTrue(0);
      }
      _gen.EmitCall(Symbols.Find("not") as BuiltinSymbol);
      while (fixups.Count > 0)
        _gen.Fixup(fixups.Pop());
    }

    void CompileAnd(Symbol sym, NodeListParser nlp) {
      var fixups = new Stack<int>();
      _gen.EmitLoadValue(BoolValue.False);
      while (!nlp.Done) {
        CompileArg(typeof(BoolValue), nlp);
        fixups.Push(_gen.Counter);
        _gen.EmitGoFalse(0);
      }
      _gen.EmitCall(Symbols.Find("not") as BuiltinSymbol);
      while (fixups.Count > 0)
        _gen.Fixup(fixups.Pop());
    }

    //--------------------------------------------------------------------------
    // Parsers for individual productions that return a data structure

    //: (TURN-ORDER { player | ( player move-type ) | ( player player ) | ( player player move-type ) | REPEAT }+ )?
    TurnDef ParseTurnDef(NodeListParser nlparg) {
      Logger.WriteLine(4, "ParseTurnDef <{0}>", nlparg);
      nlparg.Expect(nlparg.IsIdent || nlparg.IsList, "player or list");

      var nlp = nlparg.GetParser();
      var ident = nlp.GetIdent();
      if (ident.Name == "repeat") {
        nlp.CheckDone();
        return TurnDef.Repeat;
      } 
      var player = DefPlayer(ident);
      // default is to play as self
      var playeras = nlp.IsIdent && nlp.Current.IsPlayer ? DefPlayer(nlp.GetIdent()) : player;
      var movetype = nlp.IsIdent ? DefMoveType(nlp.GetIdent()) : MoveTypeValue.Any;
      nlp.CheckDone();
      return new TurnDef { TurnPlayer = player, MovePlayer = playeras, MoveType = movetype };
    }

    //: ( BOARD-SETUP ( player placement+ )+ )?
    //: placement := (piece-type { off <int> | position }+ )
    IList<PlacementDef> ParsePlacements(NodeListParser nlp) {
      Logger.WriteLine(4, "ParsePlacements <{0}>", nlp);
      var list = new List<PlacementDef>();
      while (!nlp.Done)
        list.Add(ParsePlacement(nlp.GetParser()));
      return list;
    }

    PlacementDef ParsePlacement(NodeListParser nlp) {
      var off = 0;
      var poslist = new List<PositionValue>();
      var piece = DefPiece(nlp.GetIdent());
      while (!nlp.Done) {
        var ident = nlp.GetIdent();
        if (ident.Name == "off")
          off = nlp.GetInt();
        else poslist.Add(DefPosition(ident));
      }
      return new PlacementDef { Piece = piece, OffQuantity = off, Positions = poslist };
    }

    //: ( ABSOLUTE-CONFIG occupier+ ( { position | zone }+ )
    //: occupier ::= piece | ( OPPONENT piece ) | ( ANY-OWNER piece )
    List<OccupierDef> ParseOccupiers(NodeListParser nlp) {
      var acc = new List<OccupierDef>();
      Symbols.PushPredefScope(PredefScopes.OCCUPIER);
      while (!nlp.Done) {
        var occ = ParseOccupier(nlp);
        if (occ == null) break;
        acc.Add(occ);
      }
      Symbols.PopPredefScope();
      return acc;
    }

    OccupierDef ParseOccupier(NodeListParser nlp) {
      //NOTE: this change allows dropping parens, but breaks order dependence
      //if (nlp.IsIdent && (nlp.CurrentIdent.IsPiece || nlp.CurrentIdent.IsDirection)) {

      // take ident; define as piece if not direction
      if (nlp.IsIdent) {
        var ident = nlp.GetIdent();
        if (ident.IsDirection)      // only used by relative-config
          return OccupierDef.Create(ident.AsValue as DirectionValue);
        else return OccupierDef.Create(PlayerKinds.Friend, DefPiece(ident));
      }
      // note: relies on dummy functions defined in scope
      if (nlp.IsList && nlp.IsCallable) {
        var nlp2 = nlp.GetParser();
        var ident = nlp2.GetIdent();
        if (ident.Sym.Name == "not") {
          return ParseOccupier(nlp2).SetNot();
        } else {
          var piece = DefPiece(nlp2.GetIdent());
          nlp2.CheckDone();
          if (ident.Sym.Name == "opponent")
            return OccupierDef.Create(PlayerKinds.Enemy, piece);
          if (ident.Sym.Name == "any-owner")
            return OccupierDef.Create(PlayerKinds.Any, piece);
        }
      }
      return null;
    }

    // Precompile dimensions to pick out position identifiers
    DimPosDef ParseDimensionPositions(NodeListParser nlpx) {
      List<string> _dims = new List<string>();
      for (var nlp = nlpx.Clone(); !nlp.Done; ) {
        var nlp2 = nlp.GetParser();
        _dims.Add(nlp2.GetText().AsString);
      }
      AddPositions(0, "", _dims);
      return null;
    }

    // recursively add symbols
    void AddPositions(int index, string prefix, IList<string> dims) {
      foreach (var frag in dims[index].Split('/')) {
        var name = prefix + frag;
        if (index + 1 < dims.Count) AddPositions(index + 1, name, dims);
        else {
          var sym = Symbols.Find(name);
          if (sym == null)
            Symbols.DefineValue(name, PositionValue.Create(name));
          else if (!(sym.Kind == SymKinds.VALUE && sym.DataType == DataTypes.Position))
            _currentparser.Syntax("cannot define {0} as position", name);
        }
      }
    }

    PieceImages ParsePieceImages(NodeListParser nlp) {
      return new PieceImages {
        Player = DefPlayer(nlp.GetIdent()),
        Images=nlp.While(n=>n.IsString, n=>n.GetText()).ToList(),
      };
    }

    PositionOrZone ParsePositionOrZone(NodeListParser nlp) {
      if (nlp.IsConstant) {
        var value = nlp.GetValue();
        if (value is PositionValue || value is ZoneValue)
          return new PositionOrZone { Value = value };
        else { }
      }
      nlp.Expected("position or zone");
      return null;
    }

    Maybe<PositionOrZone> ParseMaybePositionOrZone(NodeListParser nlp) {
      if (nlp.IsConstant) {
        var value = nlp.Current.AsValue;
        if (value is PositionValue || value is ZoneValue)
          return new Maybe<PositionOrZone> { Value = ParsePositionOrZone(nlp) };
      }
      return new Maybe<PositionOrZone> { };
    }

    // parse a term which may be a position, direction or opposite
    void CompilePositionOrDirection(NodeListParser nlp) {
      var rettype = CompileValue(typeof(TypedValue), nlp);
      nlp.Expect(rettype == DataTypes.Position || rettype == DataTypes.Direction, "position or direction");
      var funcsym = Symbols.Find("--pos-or-dir");
      _gen.EmitCall(funcsym as BuiltinSymbol);
    }

    //protected PositionOrDirection ParsePositionOrDirection(NodeListParser nlp) {
    //  if (nlp.IsValue) {
    //    var value = nlp.GetValue();
    //    if (value is PositionValue || value is DirectionValue)
    //      return new PositionOrDirection { Value = value };
    //  }
    //  nlp.Expected("position or direction");
    //  return null;
    //}

    GoKinds ParseGoKind(NodeListParser nlp) {
      var ident = nlp.GetIdent();
      var gosym = Symbols.Find(ident.Name, PredefKinds.GO);
      nlp.Expect(gosym != null, "valid go target");
      return gosym.GoKind;
    }

    Maybe<PlayerValue[]> ParseMaybePlayers(NodeListParser nlp) {
      if (!(nlp.IsList && nlp.CheckedHead.IsPlayer)) return Maybe<PlayerValue[]>.Null;
      return Maybe<PlayerValue[]>.Create(nlp.GetParser().UntilDone(n => DefPlayer(n.GetIdent())).ToArray());
    }

    Maybe<PieceValue> ParseMaybePiece(NodeListParser nlp) {
      return (nlp.IsIdent && nlp.Current.IsPiece)
        ? Maybe<PieceValue>.Create(DefPiece(nlp.GetIdent()))
        : Maybe<PieceValue>.Null;
    }

    Maybe<PlayerValue> ParseMaybePlayer(NodeListParser nlp) {
      return (nlp.IsIdent && nlp.Current.IsPlayer)
        ? Maybe<PlayerValue>.Create(DefPlayer(nlp.GetIdent()))
        : Maybe<PlayerValue>.Null;
    }
  }
}
