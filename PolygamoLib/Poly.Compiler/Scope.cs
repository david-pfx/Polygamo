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
/// Symbol scope
/// 
using System;
using System.Collections.Generic;
using System.Linq;
using Poly.Common;

/// <summary>
/// Scope levels (env in Lisp speak)
/// </summary>
namespace Poly.Compiler {
  ///-------------------------------------------------------------------
  /// <summary>
  /// Scope implements one level of nested symbols
  /// </summary>
  internal class Scope {
    internal Dictionary<string, Symbol> Dict { get; private set; }

    // Owner table -- do we need this?
    SymbolTable _owner;
    // Link to parent
    internal Scope Parent {
      get { return _parent; }
      set {
        _parent = value;
        Logger.WriteLine(5, "Parent {0}", value);
      }
    }
    Scope _parent = null;

    internal int Level { get { return _parent == null ? 0 : 1 + _parent.Level; } }

    // set this flag for symbols that should be pushed out to catalog
    internal bool IsGlobal { get; set; }

    public override string ToString() {
      return String.Format("Scope {0} glob:{1} syms:{2}", Level, IsGlobal, Dict.Count);
    }
    internal string AllToString() {
      return String.Format("Scope {0} glob:{1} syms:{2} par:{3}", Level, IsGlobal, Dict.Count, _parent);
    }

    // Create a new scope level
    internal static Scope Create(SymbolTable owner) {
      var news = new Scope() {
        Dict = new Dictionary<string, Symbol>(),
        _parent = null,
        _owner = owner,
      };
      owner.CurrentScope = news;
      Logger.WriteLine(5, "Create {0}", news);
      return news;
    }

    internal Scope Push() {
      var news = new Scope() {
        Dict = new Dictionary<string, Symbol>(),
        _parent = this,
        _owner = this._owner,
      };
      _owner.CurrentScope = news;
      Logger.WriteLine(5, "Push {0}", news);
      return news;
    }

    // Create a new function scope level
    // Note that the function name itself lives outside this scope
    internal Scope Push(Symbol[] argsyms) {
      var scope = Push();
      Logger.WriteLine(5, "Add func args {0}", String.Join(",", argsyms.Select(s => s.ToString()).ToArray()));
      foreach (var sym in argsyms)
        scope.Add(sym);
      return scope;
    }

    // Return to previous scope level
    // Propagate any uncleared lookup items back to parent
    internal void Pop() {
      Logger.Assert(!IsGlobal, "pop");
      Logger.WriteLine(5, "Pop {0} => {1}", _owner.CurrentScope, _parent);
      _owner.CurrentScope = _parent;
      Logger.Assert(_owner.CurrentScope != null, "pop");
    }

    // Add a symbol -- all go through here
    internal void Add(Symbol sym, string name = null) {
      if (name != null) sym.Name = name;
      sym.Level = this.Level;
      Logger.Assert(!Dict.ContainsKey(sym.Name), sym.Name);
      Dict.Add(sym.Name, sym);
    }

    // Find a symbol in this scope, or delegate to parent, or return null
    internal Symbol FindAny(string name) {
      Symbol sym = Find(name);
      if (sym == null && _parent != null)
        return _parent.FindAny(name);
      else return sym;
    }

    // Find a filtered symbol in this scope, or delegate to parent, or return null
    internal Symbol FindAny(string name, Func<Symbol,bool> predicate) {
      Symbol sym = Find(name);
      if (sym != null && predicate(sym))
        return sym;
      else if (_parent != null)
        return _parent.FindAny(name, predicate);
      else return null;
    }

    // Find a symbol in this scope
    internal Symbol Find(string name) {
      Symbol sym = null;
      if (Dict.TryGetValue(name, out sym))
        return sym;
      else return null;
    }

  }
}
