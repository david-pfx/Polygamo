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
/// Node management
/// 
using System;
using System.Collections.Generic;
using System.Linq;
using Poly.Common;

namespace Poly.Compiler {
  abstract internal class NodeBase {
    virtual internal DataTypes DataType { get { return DataTypes.Void; } }
    virtual internal bool IsFunc { get { return false; } }
    virtual internal bool IsValue { get { return false; } }
    //virtual internal string AsName { get { return ""; } }
    //internal bool IsValue { get { return IsLiteral || IsIdent && AsIdent.Sym.IsValue; } }

    internal bool IsList { get { return this is ListNode; } }
    internal bool IsIdent { get { return this is IdentNode; } }
    internal bool IsLiteral { get { return this is LiteralNode; } }

    internal bool IsAttribute { get { return DataType == DataTypes.Attribute; } }
    internal bool IsDirection { get { return DataType == DataTypes.Direction; } }
    internal bool IsMoveType { get { return DataType == DataTypes.MoveType; } }
    internal bool IsPiece { get { return DataType == DataTypes.Piece; } }
    internal bool IsPlayer { get { return DataType == DataTypes.Player; } }
    //internal bool IsPosition { get { return DataType == DataTypes.Position; } }
    internal bool IsZone { get { return DataType == DataTypes.Zone; } }

    internal bool IsEmpty { get { return IsList && AsListNode.Nodes.Count == 0; } }
    internal bool IsBool { get { return DataType == DataTypes.Bool; } }
    internal bool IsNumber { get { return DataType == DataTypes.Number; } }
    internal bool IsString { get { return DataType == DataTypes.Text; } }

    internal ListNode AsListNode { get { return this as ListNode; } }
    internal IList<Node> AsList { get { return AsListNode.Nodes; } }

    // BUG: *** use of AsIdent may release ident with out of date symbol ***
    internal IdentNode AsIdent { get { return this as IdentNode; } }
    internal LiteralNode AsLiteral { get { return this as LiteralNode; } }

    internal TypedValue AsValue { get { return IsLiteral ? AsLiteral.Value : AsIdent.Sym.Value; } }
    internal BoolValue AsBool { get { return (AsValue as BoolValue); } }
    //internal BoolValue AsBool { get { return (AsLiteral.Value as BoolValue); } }
    internal NumberValue AsNumber { get { return (AsValue as NumberValue); } }
    //internal NumberValue AsNumber { get { return (AsLiteral.Value as NumberValue); } }
    internal TextValue AsText { get { return (AsValue as TextValue); } }
    internal int AsInt { get { return (int)(AsValue as NumberValue).Value; } }
  }

  internal class Node : NodeBase {

    // default dummy node
    internal static Node Empty() {
      return ListNode.Create(new List<Node>());
    }
  }

  /// <summary>
  /// A node for an ident in the symbol table
  /// </summary>
  internal class IdentNode : Node {
    internal string Name { get; private set; }
    internal Symbol Sym { get; private set; }

    internal override DataTypes DataType { get { return Sym.DataType; } }
    internal override bool IsFunc { get { return Sym.IsFunc; } }
    internal override bool IsValue { get { return Sym.IsValue; } }

    public override string ToString() {
      return Name;
    }
    internal static IdentNode Create(string ident, Symbol sym) {
      return new IdentNode { Name = ident, Sym = sym };
    }
  }

  /// <summary>
  /// 
  /// </summary>
  internal class LiteralNode : Node {
    internal TypedValue Value { get; private set; }

    internal override DataTypes DataType { get { return Value.DataType; } }
    internal override bool IsValue { get { return true; } }
    //internal override string AsName { get { return Value.ToString(); } }

    public override string ToString() {
      return Value.Format();
    }

    internal static LiteralNode Create(TypedValue value) {
      return new LiteralNode { Value = value };
    }
  }

  /// <summary>
  /// 
  /// </summary>
  internal class ListNode : Node {
    internal IList<Node> Nodes { get; private set; }

    internal Node Head { get { return Nodes[0]; } }
    internal IList<Node> Tail { get { return Nodes.Skip(1).ToList(); } }

    public override string ToString() {
      return "(" + Nodes.Join(" ") + ")";
    }

    internal static ListNode Create(IList<Node> nodes) {
      return new ListNode { Nodes = nodes };
    }
  }

  /// <summary>
  /// A node representing an expression that can be evaluated
  /// </summary>
  internal class SexprNode : Node {
    internal Symbol Sym { get; private set; }
    internal IList<Node> Args { get; private set; }

    internal override DataTypes DataType { get { return Sym.DataType; } }

    public override string ToString() {
      return Sym.Name + "(" + Args.Join(",") + ")";
    }

    internal static SexprNode Create(Symbol symbol, IList<Node> args) {
      return new SexprNode {
        Sym = symbol,
        Args = args,
      };
    }
  }

  /// <summary>
  /// Assist with parsing nodes by returning a match
  /// Throw exception on mismatch
  /// </summary>
  internal class NodeListParser {
    int _position = 0;
    IList<Node> _nodes;
    SymbolTable _symbols;

    // raw current node, may not match symbol table
    Node _current { get { return _nodes[_position]; } }
    // static null parser
    static internal NodeListParser Null = new NodeListParser { _nodes = new List<Node>() };

    internal IList<Node> Nodes { get { return _nodes; } }
    internal SymbolTable Symbols { get { return _symbols; } }
    internal int Count { get { return _nodes.Count; } }
    internal bool Done { get { return _position >= _nodes.Count; } }
    internal bool IsList { get { return !Done && _current.IsList; } }
    internal bool IsIdent { get { return !Done && _current.IsIdent; } }

    // updated current node, will match symbol table, must be guarded
    internal Node Current { get { return CheckedIdent(_current); } }
    internal Node PeekList { get { return CheckedIdent(_current.AsList.First()); } }
    internal IdentNode CurrentIdent { get { return Current as IdentNode; } }

    internal bool IsValue { get { return !Done && (_current.IsLiteral || Current.IsValue); } }
    internal bool IsBool { get { return IsValue && Current.IsBool; } }
    internal bool IsString { get { return IsValue && Current.IsString; } }
    internal bool IsNumber { get { return IsValue && Current.IsNumber; } }
    internal bool IsAttribute { get { return IsIdent && Current.IsAttribute; } }

    internal bool IsVariable { get { return IsIdent && CurrentIdent.Sym.IsVariable; } }
    internal bool IsFunc { get { return IsIdent && Current.IsFunc; } }
    internal bool IsSexpr { get { return IsList && PeekList.IsFunc; } }
    internal bool IsCallable { get { return IsFunc || IsSexpr; } }

    // check ident, return new node if undef or base scope
    internal Node CheckedIdent(Node node) {
      if (node.IsIdent) {
        var ident = node as IdentNode;
        if (!(ident.Sym.IsUndef || ident.Sym.IsBase)) throw Error.Assert("checked ident {0}", ident);
        var newsym = _symbols.Find(ident.Sym.Name);
        if (newsym != null && !newsym.IsUndef)
          return IdentNode.Create(ident.Name, newsym);
      }
      return node;
    }

    public override string ToString() {
      return String.Format("{0}^{1}", 
        _nodes.Take(_position).Join(" ").ShortenLeft(40), _nodes.Skip(_position).Join(" ").Shorten(40)); //TODO: shorten
    }

    // create from node that is either list or not
    internal static NodeListParser Create(Node node, SymbolTable symbols = null) {
      return Create(node.IsList ? node.AsListNode.Nodes : new List<Node>() { node }, symbols);
    }

    // create from list
    internal static NodeListParser Create(IList<Node> nodes, SymbolTable symbols = null) {
      return new NodeListParser {
        _nodes = nodes,
        _symbols = symbols,
      };
    }

    // create a clone for lookahead.
    internal NodeListParser Clone() {
      return new NodeListParser {
        _nodes = _nodes,
        _symbols = _symbols,
      };
    }

    internal void Syntax(string exp, params object[] args) {
      throw Error.Syntax("{0}, near <{1}>", String.Format(exp, args), this);
    }

    internal void Unexpected(string exp, params object[] args) {
      throw Error.Syntax("found {0}, near <{1}>", String.Format(exp, args), this);
    }

    internal void Expected(string exp, params object[] args) {
      throw Error.Syntax("expected {0}, near <{1}>", String.Format(exp, args), this);
    }

    internal void Expect(bool test, string exp, params object[] args) {
      if (!test) Expected(exp, args);
    }

    internal void CheckDone() {
      if (!Done) throw Error.Syntax("expected end, near <{0}>", this);
    }

    internal NodeListParser GetParser() {
      return Create(GetNode(), _symbols);
    }

    // wrapper to call parser until done
    internal IEnumerable<T> UntilDone<T>(Func<NodeListParser, T> func) {
      while (!Done)
        yield return func(this);
    }

    // wrapper to call parser until done
    internal IEnumerable<T> While<T>(Func<NodeListParser, bool> pred, Func<NodeListParser, T> func) {
      while (!Done && pred(this))
        yield return func(this);
    }

    internal IList<T> CheckLength<T>(IList<T> list, int length) {
      Expect(length == 0 || list.Count == length, "list of length {0}", length);
      return list;
    }

    // take a list of headed nodes and return as a dictionary
    // note: key is name of head ident
    internal static Dictionary<string,List<List<Node>>> GetDict(IList<Node> nn) {
      var dict = new Dictionary<string, List<List<Node>>>();
      foreach (var n in nn) {
        NodeListParser nlp = NodeListParser.Create(n);
        //dict.AddMulti(nlp.Current.AsIdent.Ident, nlp.Nodes.ToList()); // breaking!!!
        dict.AddMulti(nlp.GetIdent().Name, nlp.GetTail().ToList());
      }
      return dict;
    }

    internal Node GetNode() {
      Expect(!Done, "Node");
      return _nodes[_position++];
    }

    internal ListNode GetListNode() {
      Expect(IsList, "List");
      return GetNode().AsListNode;
    }

    internal IdentNode GetIdent() {
      Expect(IsIdent, "Ident");
      return CheckedIdent(GetNode()) as IdentNode;
    }

    // get value which may be literal or ident with value
    internal TypedValue GetValue() {
      Expect(IsValue, "Value");
      return (IsIdent) ? GetIdent().AsValue : GetNode().AsValue;
    }

    // Parse list or single atom as Sexpr
    internal SexprNode GetSexprNode() {
      Expect(IsCallable, "Callable");
      if (IsList) {
        var nlp = GetParser();
        return SexprNode.Create(nlp.GetIdent().Sym, nlp.GetTail());
      } else return SexprNode.Create(GetIdent().Sym, new List<Node>());
    }

    internal IList<Node> GetTail() {
      return UntilDone(nlp => nlp.GetNode()).ToList();
    }

    internal IList<Node> GetList() {
      return (IsList) ? GetParser().UntilDone(nlp => nlp.GetNode()).ToList() 
        : new List<Node> { GetNode() };
    }

    internal BoolValue GetBool() {
      Expect(IsBool, "bool");
      return GetNode().AsBool;
    }

    internal TextValue GetText() {
      Expect(IsString, "string");
      return GetNode().AsText;
    }
    internal NumberValue GetNumber() {
      Expect(IsNumber, "number");
      return GetNode().AsNumber;
    }
    internal int GetInt() {
      Expect(IsNumber, "integer");
      return GetNode().AsInt;
    }

    internal IList<IdentNode> GetIdentList(int count = 0) {
      return CheckLength(UntilDone(nlp => nlp.GetIdent()).ToList(), count);
    }

    internal IList<IdentNode> GetIdentTail() {
      return UntilDone(nlp => nlp.GetIdent()).ToList();
    }

    internal IList<string> GetIdentNameTail() {
      return UntilDone(nlp => nlp.GetIdent().Name).ToList();
    }

    internal IList<NumberValue> GetNumberList(int count = 0) {
      return CheckLength(GetParser().UntilDone(n=>n.GetNumber()).ToList(), count);
    }

    internal IList<NumberValue> GetNumberTail(int count = 0) {
      return CheckLength(UntilDone(nlp => nlp.GetNumber()).ToList(), count);
    }

    internal IList<int> GetIntList(int count = 0) {
      return CheckLength(GetParser().UntilDone(nlp => (int)nlp.GetNumber().Value).ToList(), count);
    }

    internal IList<int> GetIntTail(int count = 0) {
      return CheckLength(UntilDone(nlp => (int)nlp.GetNumber().Value).ToList(), count);
    }
  }
}

