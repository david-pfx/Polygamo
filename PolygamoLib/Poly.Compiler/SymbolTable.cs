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
/// Compiler symbol table
/// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Poly.Common;
using Poly.Engine;

/// <summary>
/// The symbol table
/// </summary>
namespace Poly.Compiler {
  /// <summary>
  /// Implements a lexical atom ie a token in the input stream
  /// </summary>
  internal enum Atoms {
    NUL,
    EOF,      // eod of file
    EOL,      // end of line
    LP,
    RP,
    LITERAL,
    IDENT,
    ALIAS,
    DEFINE,
    INCLUDE,
    VERSION,
    MACRO,    // macro called with arguments
    REPLACE,  // simple replacement
    NOISY,
  };

  /// <summary>
  /// Kind of symbol represented by atom
  /// </summary>
  internal enum SymKinds {
    NUL,
    UNDEF,      // not defined
    LITERAL,    // value known at compile time
    PREDEF,     // predefines function with arguments
    VALUE,      // value at compile time
    VARIABLE,   // variable set at runtime
    KEYWORD,    // just a word with meaning
  }

  /// <summary>
  /// Scope of predefined symbol. 
  /// All keywords are loaded into static scope BASE. Other scopes are dynamic.
  /// </summary>
  internal enum PredefScopes {
    NONE,
    BASE,
    BOARD,
    GAME,
    GEN,
    GOAL,
    GRID,
    MENU,
    MOVE,
    OCCUPIER,
    PIECE,
    ZONE,
  }

  /// <summary>
  /// Kind of predef symbol within scope
  /// </summary>
  internal enum PredefKinds {
    NUL,
    PROG,     // void functions
    PROGM,    // void functions with merge
    EXPR,     // value returning functions (???)
    CONTROL,  // if/else/while/and/or
    GO,       // arguments for (go)
  }

  /// <summary>
  /// Various keywords used as required
  /// </summary>
  internal enum Keywords {
    IF, ELSE, WHILE, AND, OR, NOT,
  }

  /// <summary>
  /// Implements a symbol table entry.
  /// </summary>
  internal class Symbol {
    internal Atoms Atom { get; set; }
    internal SymKinds Kind { get; set; }
    internal DataTypes DataType { get; set; }
    internal string Name { get; set; }
    internal int NumArgs { get; set; }
    internal TypedValue Value { get; set; }
    internal Symbol Link { get; set; }
    internal int Level { get; set; }
    internal List<Token> Body { get; set; }
    internal PredefScopes PredefScope { get; set; }
    internal PredefKinds PredefKind { get; set; }
    internal Keywords Keyword { get; set; }
    internal GoKinds GoKind { get; set; }

    internal bool IsControl { get { return PredefKind == PredefKinds.CONTROL; } }

    internal static Symbol None = new Symbol() { Atom = Atoms.NUL, Name = "" };

    public override string ToString() {
      return String.Format("{0}:{1}:{2}:{3}:{4}", Name, Atom, Kind, DataType, Level);
    }

    // series of tests used by parser
    internal bool IsLiteral { get { return Atom == Atoms.LITERAL; } }
    internal bool IsIdent { get { return Atom == Atoms.IDENT; } }
    internal bool IsFunc { get { return Atom == Atoms.IDENT && Kind == SymKinds.PREDEF; } }
    internal bool IsValueIdent { get { return Atom == Atoms.IDENT && Kind == SymKinds.VALUE; } }
    internal bool IsVoidFunc { get { return this is BuiltinSymbol
                                     && (this as BuiltinSymbol).CallInfo.ReturnType == typeof(void); } }
    internal bool IsValueFunc { get { return this is BuiltinSymbol
                                     && (this as BuiltinSymbol).CallInfo.ReturnType != typeof(void); } }
    //internal bool IsVoidFunc { get { return Atom == Atoms.IDENT && Kind == SymKinds.PREDEF 
    //                                 && (this as BuiltinSymbol).CallInfo.ReturnType == typeof(void); } }
    //internal bool IsValueFunc { get { return Atom == Atoms.IDENT && Kind == SymKinds.PREDEF 
    //                                 && (this as BuiltinSymbol).CallInfo.ReturnType != typeof(void); } }
    internal bool IsUndef { get { return Atom == Atoms.IDENT && Kind == SymKinds.UNDEF; } }
    internal bool IsValue { get { return IsLiteral || IsValueIdent; } }
    //internal bool IsValue { get { return IsLiteral || IsValueIdent || IsValueFunc; } }
    internal bool IsVariable { get { return IsIdent && Kind == SymKinds.VARIABLE; } }
    internal bool IsBase { get { return PredefScope == PredefScopes.BASE; } }
  }

  internal class BuiltinSymbol : Symbol {
    public CallInfo CallInfo { get; internal set; }
    internal string NameAs { get; set; }
  }

  internal class BuiltinScopeInfo {
    internal PredefScopes Predef;
    internal Scope Scope;
    internal Type CodeType;
    internal Type DefType;
    internal PredefKinds Kind;
  }

  ///-------------------------------------------------------------------

  /// <summary>
  /// SymbolTable implements the main compiler symbol table.
  /// </summary>
  internal class SymbolTable {
    // current scope
    internal Scope CurrentScope { get; set; }
    internal Dictionary<PredefScopes, BuiltinScopeInfo> PredefScopeDict { get; set; }

    Scope _predefscope; // level = 0
    Scope _globalscope; // level = 1

    //--- creation

    internal static SymbolTable Create() {
      return new SymbolTable {
        PredefScopeDict = new Dictionary<PredefScopes, BuiltinScopeInfo>(),
      }.Init();
    }


    // Find existing symbol by name
    internal Symbol Find(string name) {
      return CurrentScope.FindAny(name);
    }

    internal Symbol Find(string name, PredefKinds kind) {
      return CurrentScope.FindAny(name, s => s.PredefKind == kind);
    }

    // Get symbol from token, add to symbol table as needed
    // Look for existing symbol in catalog and nested scopes
    // If not found, define according to lexer type
    internal Symbol GetSymbol(Token token) {
      // First look for existing symbol
      Symbol sym;
      if (token.IsDefinable) {
        sym = Find(token.Value);
        if (sym != null) {
          while (sym.Atom == Atoms.ALIAS)
            sym = sym.Link;
          return sym;
        }
      }
      // source code line token masquerades as another eol
      if (token.TokenType == TokenTypes.LINE)
        return Find(Token.EolName);
      // Create new symbol from token
      if (token.TokenType == TokenTypes.Number || token.TokenType == TokenTypes.HexNumber)
        sym = MakeLiteral(NumberValue.Create(token.GetNumber() ?? Decimal.Zero));
      else if (token.TokenType == TokenTypes.Time)
        sym = MakeLiteral(TimeValue.Create(token.GetTime() ?? DateTime.MinValue));
      else if (token.TokenType == TokenTypes.Identifier || token.TokenType == TokenTypes.IdLit)
        sym = MakeIdent();
      else if (token.TokenType == TokenTypes.Binary)
        sym = MakeLiteral(BinaryValue.Create(token.GetBinary()));
      else
        sym = MakeLiteral(TextValue.Create(token.Value));
      // note: only names for those we might define
      sym.Name = token.Value;
      return sym;
    }

    //-- ops

    // Set parent of global scope to required set of predefined symbols
    // built in methods always have predefined as parent
    Stack<Scope> _predefscopestack = new Stack<Scope>();

    internal void PushPredefScope(PredefScopes predef) {
      Logger.WriteLine(4, "Push predef {0} = {1}", _predefscopestack.Count, predef);
      _predefscopestack.Push(CurrentScope.Parent);
      CurrentScope.Parent = PredefScopeDict[predef].Scope;
    }

    internal void PopPredefScope() {
      CurrentScope.Parent = _predefscopestack.Pop();
      Logger.WriteLine(4, "Pop predef {0}", _predefscopestack.Count);
    }

    // add an undefined ident to the symbol table
    internal Symbol GetIdentSym(string name) {
      var sym = Find(name);
      if (sym != null) return sym;
      return AddIdent(name, SymKinds.UNDEF);
    }

    // add a value to the symbol table
    internal void DefineValue(string name, TypedValue value) {
      Add(name, new Symbol {
        Atom = Atoms.IDENT,
        Kind = SymKinds.VALUE,
        Value = value,
        DataType = value.DataType,
      });
    }

    internal Symbol AddExpansion(string name, Atoms atom, List<Token> symbols) {
      return Add(name, new Symbol {
        Atom = atom,
        Body = symbols,
      });
    }

    // add a variable to the symbol table
    internal void DefineVariable(string name, DataTypes datatype, TypedValue value = null) {
      Add(name, new Symbol {
        Atom = Atoms.IDENT,
        Kind = SymKinds.VARIABLE,
        Value = value,
        DataType = datatype,
      });
    }

    //------------------------------------------------------------------
    //--- impl

    static Symbol MakeLiteral(TypedValue value) {
      Symbol sym = new Symbol {
        Atom = Atoms.LITERAL,
        Kind = SymKinds.LITERAL,
        DataType = value.DataType,
        Value = value
      };
      return sym;
    }

    static Symbol MakeIdent(string name = null) {
      Symbol sym = new Symbol {
        Name = name,
        Atom = Atoms.IDENT,
        Kind = SymKinds.UNDEF,
        DataType = DataTypes.Unknown,
      };
      return sym;
    }

    //--- setup

    SymbolTable Init() {
      _predefscope = Scope.Create(this);    // level for predefined symbols
      AddPredefinedSymbols();
      AddBuiltinMethods();
      _globalscope = _predefscope.Push();  // level for globally defined symbols
      _globalscope.IsGlobal = true;
      CurrentScope = _globalscope;
      PredefScopeDict[PredefScopes.BASE] = new BuiltinScopeInfo {
        Scope = _predefscope,
      };
      return this;
    }

    private void AddBuiltinMethods() {
      // predefined functions in outer scope
      foreach (var binfo in CallInfo.GetBuiltinInfo(typeof(Builtin)))
        AddMethod(binfo.Name, binfo, PredefScopes.BASE);

      //                                code                instance
      AddBuiltinMethods(PredefScopes.GAME , typeof(GameCode),  typeof(GameDef), PredefKinds.PROGM);
      AddBuiltinMethods(PredefScopes.BOARD, typeof(BoardCode), typeof(BoardDef), PredefKinds.PROG);
      AddBuiltinMethods(PredefScopes.GOAL , typeof(GoalCode),  typeof(GoalDef), PredefKinds.EXPR);
      AddBuiltinMethods(PredefScopes.GRID,  typeof(GridCode),  typeof(GridDef), PredefKinds.PROG);
      AddBuiltinMethods(PredefScopes.MENU , typeof(MenuCode),  typeof(MenuDef), PredefKinds.PROG);
      AddBuiltinMethods(PredefScopes.MOVE , typeof(MoveCode),  typeof(MoveDef), PredefKinds.PROG);
      AddBuiltinMethods(PredefScopes.OCCUPIER, typeof(OccupierCode),  typeof(OccupierDef), PredefKinds.PROG);
      AddBuiltinMethods(PredefScopes.PIECE, typeof(PieceCode), typeof(PieceDef), PredefKinds.PROG);
      AddBuiltinMethods(PredefScopes.GEN, typeof(MoveGenCode), typeof(MoveGenState), PredefKinds.PROG);
      AddBuiltinMethods(PredefScopes.ZONE, typeof(ZoneCode), typeof(ZoneDef), PredefKinds.PROG);

    }

    // Add a specific set of methods into a new scope
    // Pop the scope but keep a pointer to it in a look up table
    private void AddBuiltinMethods(PredefScopes predefscope, Type codetype, Type deftype, PredefKinds kind) {
      var scope = CurrentScope.Push();
      PredefScopeDict[predefscope] = new BuiltinScopeInfo {
        Predef = predefscope,
        CodeType = codetype,
        DefType = deftype,
        Scope = scope,
        Kind = kind,
      };
      foreach (var binfo in CallInfo.GetBuiltinInfo(codetype))
        AddMethod(binfo.Name, binfo, predefscope);
      scope.Pop();
    }

    private void AddMethod(string name, CallInfo binfo, PredefScopes scope) {
      var sb = new StringBuilder();
      foreach (var ch in name)
        if (Char.IsUpper(ch)) {
          if (sb.Length > 0) sb.Append("-");
          sb.Append(Char.ToLower(ch));
        }  else if (ch == '_') sb.Append('-');
        else sb.Append(ch);
      if (sb[sb.Length - 1] == '-') sb[sb.Length - 1] = '?';
      AddBuiltin(sb.ToString(), binfo, scope);
    }

    // Load and initialise the symbol table
    void AddPredefinedSymbols() {
      AddSym(Token.EolName, Atoms.EOL);
      AddSym(Token.EofName, Atoms.EOF);
      AddSym("(", Atoms.LP);
      AddSym(")", Atoms.RP);
      AddSym("define", Atoms.DEFINE);
      AddSym("include", Atoms.INCLUDE);
      AddSym("version", Atoms.VERSION);
      AddSym("noisy", Atoms.NOISY);

      AddLiteral("true", BoolValue.True);
      AddLiteral("false", BoolValue.False);
      AddLiteral("$lineno$", NumberValue.Zero);
      AddLiteral("$filename$", TextValue.Empty);

      AddPredef("-menu", PredefScopes.MENU, PredefKinds.PROG);
      // control functions generate custom code (incl shortcircuit eval)
      AddControl("if", Keywords.IF, DataTypes.Void);
      AddControl("else", Keywords.ELSE, DataTypes.Void);
      AddControl("while", Keywords.WHILE, DataTypes.Void);
      AddControl("and", Keywords.AND, DataTypes.Bool);
      AddControl("or", Keywords.OR, DataTypes.Bool);
      // go kinds
      AddGoKind("from", GoKinds.From);
      AddGoKind("to", GoKinds.To);
      AddGoKind("mark", GoKinds.Mark);
      AddGoKind("last-from", GoKinds.LastFrom);
      AddGoKind("last-to", GoKinds.LastTo);

    }

    // Add a symbol to the current scope
    Symbol Add(string name, Symbol sym) {
      CurrentScope.Add(sym, name);
      return sym;
    }

    Symbol AddSym(string name, Atoms atom) {
      return Add(name, new Symbol { 
        Atom = atom,
      });
    }

    Symbol AddLiteral(string name, TypedValue value) {
      return Add(name, new Symbol {
        Atom = Atoms.LITERAL,
        Kind = SymKinds.LITERAL,
        Value = value,
        DataType = value.DataType,
      });
    }

    Symbol AddIdent(string name, SymKinds kind, TypedValue value = null) {
      return Add(name, new Symbol {
        Atom = Atoms.IDENT,
        Kind = kind,
        Value = value,
        DataType = (value == null) ? DataTypes.Unknown : value.DataType,
      });
    }

    Symbol AddAlias(string name, string other) {
      return Add(name, new Symbol {
        Atom = Atoms.ALIAS,
        Link = Find(other),
      });
    }

    Symbol AddPredef(string name, PredefScopes pscope, PredefKinds pkind, DataTypes datatype = DataTypes.Unknown) {
      return Add(name, new Symbol {
        Atom = Atoms.IDENT,
        Kind = SymKinds.PREDEF,
        PredefScope = pscope,
        PredefKind = pkind,
        DataType = datatype,
      });
    }

    Symbol AddBuiltin(string name, CallInfo binfo, PredefScopes scope) {
      return Add(name, new BuiltinSymbol {
        Atom = Atoms.IDENT,
        Kind = SymKinds.PREDEF,
        PredefScope = scope,
        CallInfo = binfo,
        DataType = TypedValue.DataTypeDict.SafeLookup(binfo.ReturnType), // UNKNOWN if not found
      });
    }

    Symbol AddControl(string name, Keywords keyword, DataTypes datatype) {
      return Add(name, new Symbol {
        Atom = Atoms.IDENT,
        Kind = SymKinds.PREDEF,
        PredefScope = PredefScopes.BASE,
        PredefKind = PredefKinds.CONTROL,
        Keyword = keyword,
        DataType = datatype,
      });
    }

    Symbol AddGoKind(string name, GoKinds gokind) {
      return Add(name, new Symbol {
        Atom = Atoms.IDENT,
        Kind = SymKinds.KEYWORD,
        PredefScope = PredefScopes.BASE,
        PredefKind = PredefKinds.GO,
        GoKind = gokind,
      });
    }


  }

  /// <summary>
  /// Information about each a function in a Type
  /// </summary>
  internal class CallInfo {
    // function name
    internal string Name { get; set; }
    // instance type
    internal Type InstanceType { get; set; }
    // return type
    internal Type ReturnType { get; set; }
    // array of arguments 
    internal IList<Type> Arguments { get; set; }
    // array of argument optional
    internal IList<bool> ArgOptional { get; set; }
    // method info to allow call
    internal MethodInfo MethodInfo { get; set; }

    public int NumArgs { get { return Arguments.Count; } }

    public override string ToString() {
      return String.Format("{0} {1}({2})", ReturnType.Name, Name, Arguments.Join(","));
    }

    // List of definitions for entry points in various classes
    internal static CallInfo[] GetBuiltinInfo(Type target) {
      var builtins = new List<CallInfo>();
      // extract methods like "s_*" from a class 
      var methods = target.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
      foreach (var method in methods) {
        if (method.Name.StartsWith("s_")) {
          var parms = method.GetParameters();
          builtins.Add(new CallInfo {
            Name = method.Name.Substring(2),
            InstanceType = target,
            ReturnType = method.ReturnType,
            Arguments = parms.Select(p => p.ParameterType).ToArray(),
            ArgOptional = parms.Select(p => p.RawDefaultValue != DBNull.Value).ToArray(),
            MethodInfo = method,
          });
        }
      }
      return builtins.ToArray();
    }
  }
}
