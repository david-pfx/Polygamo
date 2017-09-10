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
/// Lexer
/// 
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Poly.Common;

namespace Poly.Compiler {
  internal enum TokenTypes {
    // first group all count as white space (White must be last)
    Nul, LINE, Directive, Bad, White, 
    // these are ungrouped
    Number, HexNumber, Identifier, Operator, Punctuation, Binary, Time,
    // this group is in order for aggregation of tokens -- nothing after here
    IdLit, CharDouble, CharSingle, CharHex, CharDec, CharNoQuote,
  }

  /// <summary>
  /// Implement a single token, including how to extract a value from it
  /// </summary>
  internal struct Token {
    internal const string EolName = ":eol";
    internal const string EofName = ":eof";

    internal string Value { get; set; }
    internal TokenTypes TokenType { get; set; }
    internal string SourcePath { get; set; }
    internal int LineNumber { get; set; }
    public override string ToString() {
      return String.Format("'{0}':{1}", Value, TokenType);
    }
    internal bool IsDefinable { 
      get { 
        return TokenType == TokenTypes.Identifier || TokenType == TokenTypes.IdLit || TokenType == TokenTypes.Operator || TokenType == TokenTypes.Punctuation; 
      }
    }
    // Real white space is discarded, but some other things left in still count as white
    internal bool IsWhite { get { return TokenType <= TokenTypes.White; } }

    internal static Token Create(string name, TokenTypes type, string sourcepath, int lineno) {
      var ret = new Token { 
        Value = name, 
        TokenType = type, 
        SourcePath = sourcepath,
        LineNumber = lineno,
      };
      if (!ret.IsValid) ret.TokenType = TokenTypes.Bad;
      return ret;
    }

    internal Decimal? GetNumber() {
      decimal dret;
      if (TokenType == TokenTypes.Number && Decimal.TryParse(Value, out dret))
        return dret;
      Int64 iret;
      if (TokenType == TokenTypes.HexNumber && Int64.TryParse(Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out iret))
        return Convert.ToDecimal(iret);
      return null;
    }
    internal DateTime? GetTime() {
      DateTime tret;
      if (TokenType == TokenTypes.Time && DateTime.TryParse(Value, out tret))
        return tret;
      return null;
    }
    internal byte[] GetBinary() {
      var b = new byte[Value.Length / 2];
      for (var i = 0; i < b.Length; ++i) {
        int n;
        if (!Int32.TryParse(Value.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out n))
          return null;
        b[i] = (byte)n;
      }
      return b;
    }

    internal bool IsValid {
      get {
        if (TokenType == TokenTypes.Bad) return false;
        if (TokenType == TokenTypes.Number) return GetNumber() != null;
        if (TokenType == TokenTypes.Time) return GetTime() != null;
        if (TokenType == TokenTypes.Binary) return GetBinary() != null;
        return true;
      }
    }
  }

  /// <summary>
  /// Implement a lexer, that can be called to deliver a stream of tokens
  /// </summary>
  internal class Lexer {
    SymbolTable _symbols;
    //Catalog _catalog;
    //bool _stop = false;
    List<Token> _tokenlist = new List<Token>();
    int _tokenindex = -1;  // must call GetNext() first
    Token _lasttoken = new Token();
    Symbol _currentsymbol = Symbol.None;
    Stack<string> _inputpaths = new Stack<string>();

    internal Token CurrentToken {  get { return _tokenlist[_tokenindex];  } }
    internal Symbol CurrentSymbol { get { return _currentsymbol; } }

    // Create new lexer on given reader
    internal static Lexer Create(SymbolTable symbols) {
      var lexer = new Lexer() {
        _symbols = symbols,
      };
      lexer.InitRegexTable();
      return lexer;
    }

    // Load up the first source file
    internal void Start(TextReader input, string filename) {
      _symbols.Find("$filename$").Value = TextValue.Create(filename);
      _inputpaths.Push(filename);
      PrepareTokens(input);
      Next();
    }

    // Insert a file into this one
    internal bool Include(string input) {
      if (!File.Exists(input)) return false;
      using (StreamReader sr = File.OpenText(input)) {
        _symbols.Find("$filename$").Value = TextValue.Create(input);
        _inputpaths.Push(input);
        Include(sr);
        _inputpaths.Pop();
        _symbols.Find("$filename$").Value = TextValue.Create(_inputpaths.Peek());
      }
      return true;
    }

    internal void Include(TextReader reader) {
      var svtl = _tokenlist;
      _tokenlist = new List<Token>();
      PrepareTokens(reader, true);
      _tokenindex--;
      svtl.InsertRange(_tokenindex + 1, _tokenlist);
      _tokenlist = svtl;
      MoveNext();
    }

    // Insert new tokens at the current position
    // Back up and step forward so everything gets processed right
    internal void Insert(List<Token> body) {
      _tokenindex--;
      _tokenlist.InsertRange(_tokenindex + 1, body);
      Next();
      //Logger.WriteLine(5, "[Insert index:{0} tokens:{1}]", _tokenindex, _tokenlist.Skip(_tokenindex).Take(body.Count + 1).Join(" "));
    }

    // Look N tokens ahead, using smarts in lexer
    internal Symbol LookAhead(int n) {
      var pos = LookNext(n);
      //return (pos == _tokenindex) ? _currentsymbol 
      return (pos == 0) ? _currentsymbol // BUG: too many lookups!!!
        : _symbols.GetSymbol(_tokenlist[pos]);
    }

    internal void Next() {
      while(true) {
        MoveNext();
        _currentsymbol = _symbols.GetSymbol(_tokenlist[_tokenindex]);
        //Logger.WriteLine(5, "{0}: Token=<{1}> Sym=<{2}>", _tokenindex, _tokenlist[_tokenindex], _currentsymbol);
        if (_currentsymbol.Atom == Atoms.REPLACE)
          _tokenlist.InsertRange(_tokenindex + 1, _currentsymbol.Body);
        else break;
      }
    }

    //internal void Back() {
    //  Logger.Assert(_tokenindex > 0);
    //  Logger.WriteLine(4, "Token -- back");
    //  _tokenindex--;
    //  _currentsymbol = _symbols.GetSymbol(_tokenlist[_tokenindex]);
    //}

    ///=================================================================
    /// Implementation
    /// 

    internal class RegexRow {
      internal TokenTypes TokenType;
      internal Regex Re;
    }
    List<RegexRow> _regextable = new List<RegexRow>();
    Regex _stringcont = new Regex(@"\G [\x00-\x20]* ( [^\x22\x00-\x1f]* \x22? )", RegexOptions.IgnorePatternWhitespace);
    bool _strcont = false;
    string _strpart;

    void AddRegex(TokenTypes tokentype, string regex, RegexOptions options = RegexOptions.None) {
      _regextable.Add(new RegexRow { 
        TokenType = tokentype, 
        Re = new Regex(regex, options | RegexOptions.IgnorePatternWhitespace) 
      });
    }

    void InitRegexTable() {
      AddRegex(TokenTypes.White, @"\G( [\x00-\x20]+ | ;.* )");             // whitespace and control chars and comments
      AddRegex(TokenTypes.Directive, @"\G\#.*");                            // directive
      AddRegex(TokenTypes.CharDouble, @"\G\x22([^\x22\x00-\x1f]* \x22?)");  // printable chars in double quotes (0x22), CRLF allowed
      AddRegex(TokenTypes.Binary, @"\Gb'( [0-9a-f]* )'", RegexOptions.IgnoreCase);        // binary literal
      AddRegex(TokenTypes.Time, @"\Gt'( [a-z0-9/:. -]+ )'", RegexOptions.IgnoreCase);      // time literal
      AddRegex(TokenTypes.Number, @"\G(-?[.]?[0-9]+[0-9.]*)");                // various kinds of number
      AddRegex(TokenTypes.HexNumber, @"\G 0x ([0-9]+[0-9a-f]*)", RegexOptions.IgnoreCase);    // hex number
      AddRegex(TokenTypes.Punctuation, @"\G[()]");                       // one single char from known set
      AddRegex(TokenTypes.Identifier, @"\G[^\x00-\x20\x22()']+", RegexOptions.IgnoreCase); // identifiers, any printable
    }

    // Tokenise the input and keep until asked
    void PrepareTokens(TextReader reader, bool include = false) {
      var lineno = 0;
      for (var line = reader.ReadLine(); line != null; line = reader.ReadLine()) {
        lineno++;
        AddToken(line, TokenTypes.LINE, lineno);
        //AddToken(line, TokenTypes.LINE, lineno);
        Match m = Match.Empty;
        for (var col = 0; col < line.Length; col += m.Length) {
          var tt = TokenTypes.Nul;
          if (_strcont) {
            m = _stringcont.Match(line, col);
            Logger.Assert(m.Success, m);
            AddToken(m.Groups[m.Groups.Count - 1].Value, TokenTypes.CharDouble, lineno);
          } else {
            for (int i = 0; i < _regextable.Count && tt == TokenTypes.Nul; ++i) {
              m = _regextable[i].Re.Match(line, col);
              if (m.Success) {
                tt = _regextable[i].TokenType;
                if (tt != TokenTypes.White) {
                  AddToken(m.Groups[m.Groups.Count - 1].Value, tt, lineno); // Take the innermost/last group
                }
              }
            }
          }
          Logger.Assert(m.Length != 0, m);
        }
      }
      if (!include) AddToken(Token.EofName, TokenTypes.Punctuation, lineno);
      Logger.WriteLine(4, "[Tokens {0}]", _tokenlist.Count);
    }

    Regex rehex = new Regex("[\x20]*([0-9a-f]+)", RegexOptions.IgnoreCase);

    // Add token to list. Merge tokens here as needed.
    void AddToken(string name, TokenTypes type, int lineno) {
      if (type == TokenTypes.CharHex || type == TokenTypes.CharDec) {
        var m = rehex.Matches(name);
        var s = new StringBuilder();
        for (var i = 0; i < m.Count; ++i) {
          var g = m[i].Groups;
          var n = Int32.Parse(g[0].Value, type == TokenTypes.CharHex ? NumberStyles.HexNumber : NumberStyles.Integer);
          s.Append(Char.ConvertFromUtf32(n));
        }
        name = s.ToString();
        type = TokenTypes.CharDouble;
      } else if (type == TokenTypes.CharDouble) {
        if (name.Length > 0 && name.Last() == '\x22')
          name = name.Substring(0, name.Length - 1);
        else type = TokenTypes.CharNoQuote;
        if (_strcont) {
          var n = name.Trim();
          name = _strpart + (n.Length == 0 ? "\\" : " " + n);
        }
        _strcont = (type == TokenTypes.CharNoQuote);
      }

      if (type == TokenTypes.CharNoQuote) {
        _strpart = name;
      } else {
        // translate string backslash to CRLF and grave to double-quote
        // defer -- does not apply to include
        //if (type == TokenTypes.CharDouble) {
        //  name = name.Replace('\\', '\n').Replace('`', '"');
        //}
        var token = Token.Create(name, type, _inputpaths.Peek(), lineno);
        _tokenlist.Add(token);
        _lasttoken = token;
      }

    }

    // Step to next token, taking action as we go
    void MoveNext() {
      //if (_stop) _tokenindex = _tokenlist.Count - 1;
      while (_tokenindex < _tokenlist.Count - 1) {
        ++_tokenindex;
        var token = _tokenlist[_tokenindex];
        if (token.TokenType == TokenTypes.LINE) {
          _symbols.Find("$lineno$").Value = NumberValue.Create(token.LineNumber);
          if (Logger.Level > 0)
            Logger.WriteLine("{0,3}: {1}", token.LineNumber, token.Value);
        } else if (token.TokenType == TokenTypes.Directive) {
          Directive(token);
        } else if (token.TokenType == TokenTypes.Bad) {
          ErrLexer(token.LineNumber, "bad token '{0}'", token.Value);
        } else break;
      }
      Logger.Assert(!_tokenlist[_tokenindex].IsWhite);
      //Logger.WriteLine(6, "Token=<{0}> <{1}>", _tokenlist[_tokenindex], CurrentSymbol);
    }

    // Lookahead N tokens with no action, return pos
    internal int LookNext(int n) {
      var pos = _tokenindex;
      while (n-- > 0) {
        pos++;
        while (pos < _tokenlist.Count && _tokenlist[pos].IsWhite)
          pos++;
      }
      return pos < _tokenlist.Count ? pos : _tokenlist.Count - 1;
    }


    // Process line as directive, return true if so
    private bool Directive(Token token) {
      var line = token.Value;
      if (line.StartsWith("#")) {
        var cmd = line.Split(null, 2);
        switch (cmd[0]) {
        case "#noisy": 
          Logger.Level = (cmd.Length >= 2) ? int.Parse(cmd[1]) : 1;
          return true;
        case "#stop":
          if (cmd.Length >= 2) Logger.Level = int.Parse(cmd[1]);
          _tokenindex = _tokenlist.Count - 1;
          return true;
        case "#include":
          if (cmd.Length >= 2) {
            if (!Include(cmd[1]))
              ErrLexer(token.LineNumber, "cannot include '{0}'", cmd[1]);
          }
          return true;
        default:
          ErrLexer(token.LineNumber, "bad directive: {0}", cmd[0]);
          return true;
        }
      }
      return false;
    }

    // Lexer error -- just discards token
    bool ErrLexer(int lineno, string message, params object[] args) {
      Logger.WriteLine("Error line {0}: {1}", lineno, String.Format(message, args));
      return true;
    }

    string Unquote(string s) {
      if (Regex.IsMatch(s, "^'.*'$") || Regex.IsMatch(s, "^\".*\"$"))
        return s.Substring(1, s.Length - 2);
      else return s;
    }

  }

}
