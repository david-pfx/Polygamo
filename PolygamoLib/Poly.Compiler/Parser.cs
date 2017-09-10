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
/// Parser and macro expansion
/// 
using System;
using System.Collections.Generic;
using System.IO;
using Poly.Common;

namespace Poly.Compiler {
  internal class Parser {
    // get whether error happened on this statement
    internal bool Error { get { return ErrorCount > 0; } }
    // get or set debug level
    internal int ErrorCount { get; private set; }
    // get instance of symbol table
    internal SymbolTable Symbols { get; private set; }

    // Lexer instance
    Lexer _lexer;
    // Output destination for syntax errors
    TextWriter _output;

    internal static Parser Create() {
      return new Parser() {
        Symbols = SymbolTable.Create(),
      };

    }

    // Begin parse, return iterator over nodes.
    // Enumerating iterator expands define and include (and triggers include error)
    internal IEnumerable<Node> ParseNodes(TextReader input, TextWriter output, string filename) {
      Logger.WriteLine(3, "Parse program {0}", filename);
      _lexer = Lexer.Create(Symbols);
      _output = output;

      // only when everything is ready -- process initial directives to set flags
      _lexer.Start(input, filename);
      while (!Check(Atoms.EOF)) {
        Node node;
        try {
          node = ParseNode();
          Logger.WriteLine(4, "Node: {0}", node);
        } catch (PolyException ex) {
          _output.WriteLine("*** {0}", ex.Message);
          ErrorCount++;
          break;
        }
        if (!node.IsEmpty) yield return node;
      }
    }

    // Parse entire program, return list of nodes
    internal List<Node> ParseAllNodes(TextReader input, TextWriter output, string filename) {
      Logger.WriteLine(4, "Parse program {0}", filename);
      _lexer = Lexer.Create(Symbols);
      _output = output;

      // only when everything is ready -- process initial directives to set flags
      _lexer.Start(input, filename);
      var program = ParseAll();
      return program;
    }

    // parse an entire program, basically a list of expression nodes
    List<Node> ParseAll() {
      var program = new List<Node>();
      while (!Check(Atoms.EOF)) {
        try {
          var node = ParseNode();
          Logger.WriteLine(3, "Node: {0}", node);
          if (!node.IsEmpty) program.Add(node);  // empty nodes are macros
        } catch (PolyException ex) {
          _output.WriteLine("*** {0}", ex.Message);
        }
      }
      return program;
    }

    // Parse a single node, either an atom or a list in parens
    // Strip out and handle macros too
    Node ParseNode() {
      // atom node
      var sym = Take();
      switch (sym.Atom) {
      case Atoms.IDENT:
        return IdentNode.Create(sym.Name, sym);
      case Atoms.LITERAL:
        return LiteralNode.Create(sym.Value);
      case Atoms.LP:
        break;
      default:
        throw ErrNotExpect(sym.Atom);
      }
      // list node
      sym = Look();
      switch (sym.Atom) {
      case Atoms.DEFINE:
        Take();
        return DefineMacro();
      case Atoms.INCLUDE:
        Take();
        return Include();
      case Atoms.MACRO:
        Take();
        return ExpandMacro(sym);
      case Atoms.NOISY:
        Take();
        return Noisy();
      case Atoms.RP:
        Take();
        return Node.Empty();
      case Atoms.VERSION:
        Take();
        return CheckVersion();
      default:
        // usually an ident, but can be any kind of list, including start with LP
        var nodes = new List<Node>();
        while (!Match(Atoms.RP))
          nodes.Add(ParseNode());
        return ListNode.Create(nodes);
      }
    }

    // Macro definition is just a name for a list of tokens inside balanced LP RP
    // Defer parsing because tokens will include $n, which must be substituted first
    Node DefineMacro() {
      if (!Check(Atoms.IDENT)) throw ErrExpect(Atoms.IDENT);
      var name = TakeToken().Value;
      var body = new List<Token>();
      for (var nesting = 0; ; ) {
        if (Check(Atoms.LP)) ++nesting;
        else if (Check(Atoms.RP) && --nesting < 0) break;
        body.Add(TakeToken());
      }
      if (!Match(Atoms.RP)) throw ErrExpect(Atoms.RP);
      Symbols.AddExpansion(name, Atoms.MACRO, body);
      return Node.Empty();
    }

    // Macro expansion creates a new scope in which $n refers to MACROARG,
    // which is a token or list of tokens inside balanced LP RP
    Node ExpandMacro(Symbol macro) {
      Symbols.CurrentScope.Push();
      for (var n = 1; !Match(Atoms.RP); ++n) {
        var expansion = new List<Token>();
        if (Match(Atoms.LP)) {
          for (var nesting = 0; ;) {
            if (!Check(Atoms.EOF)) throw ErrNotExpect(Atoms.EOF);
            if (Check(Atoms.LP)) ++nesting;
            else if (Check(Atoms.RP) && --nesting < 0) break;
            expansion.Add(TakeToken());
          }
          if (!Match(Atoms.RP)) throw ErrExpect(Atoms.RP);
        } else expansion.Add(TakeToken());
        Symbols.AddExpansion("$" + n.ToString(), Atoms.REPLACE, expansion);
        //Symbols.AddExpansion("{" + n.ToString() + "}", Atoms.REPLACE, expansion);
      }
      _lexer.Insert(macro.Body);
      var ret = ParseNode();
      Symbols.CurrentScope.Pop();
      return ret;
    }

    // check version, either error or return null node
    Node CheckVersion() {
      if (!Check(Atoms.LITERAL)) throw ErrExpect(Atoms.LITERAL);
      var version = Take().Name;
      if (!Match(Atoms.RP)) throw ErrExpect(Atoms.RP);
      return Node.Empty();
    }

    Node Include() {
      if (!Check(Atoms.LITERAL)) throw ErrExpect(Atoms.LITERAL);
      var path = Take().Name;
      if (!Match(Atoms.RP)) throw ErrExpect(Atoms.RP);
      if (!_lexer.Include(path)) throw ErrSyntax("file not found: {0}", path);
      return Node.Empty();
    }

    Node Noisy() {
      if (!Check(Atoms.LITERAL)) throw ErrExpect(Atoms.LITERAL);
      var path = Take().Name;
      if (!Match(Atoms.RP)) throw ErrExpect(Atoms.RP);
      Logger.Level = path.SafeIntParse() ?? Logger.Level;
      return Node.Empty();
    }

    ///=================================================================
    /// Tokens and errors
    /// 

    Symbol Look(int n = 0) {
      return _lexer.LookAhead(n);
    }

    //// Look ahead by N excluding any EOL
    //Symbol LookOverEol(int n = 0) {
    //  for (var i = 0; i <= n; ++i)
    //    if (_lexer.LookAhead(i).Atom == Atoms.EOL)
    //      ++n;
    //  return _lexer.LookAhead(n);
    //}

    Symbol Take() {
      var ret = Look();
      _lexer.Next();
      return ret;
    }

    Token TakeToken() {
      var ret = _lexer.CurrentToken;
      _lexer.Next();
      return ret;
    }
    //void Untake() {
    //  _lexer.Back();
    //}

    bool Check(Atoms atom) {
      return atom == Look().Atom;
    }

    bool Match() {
      _lexer.Next();
      return true;
    }

    bool Match(Atoms atom) {
      if (atom != Look().Atom) return false;
      _lexer.Next();
      return true;
    }

    //--- error functions return true if error

    Exception ErrSyntax(string message, params object[] args) {
      var msg = String.Format("{0} ({1}): error : {2}", _lexer.CurrentToken.SourcePath,
        _lexer.CurrentToken.LineNumber, String.Format(message, args));
      return new PolyException(msg);
    }

    Exception ErrExpect(string name) {
      return ErrSyntax("expected {0}, found {1}", name, Look().Name);
    }

    Exception ErrExpect(Atoms atom, string found) {
      return ErrSyntax("expected {0}, found {1}", atom.ToString(), found);
    }

    Exception ErrExpect(Atoms atom) {
      return ErrExpect(atom, Look().Name);
    }

    Exception ErrNotExpect(Atoms atom) {
      return ErrSyntax("found unexpected {0}", atom.ToString());
    }

  }
}
