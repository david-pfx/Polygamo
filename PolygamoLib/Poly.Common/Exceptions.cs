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
/// Error handling and exceptions
/// 
using System;

namespace Poly.Common {
  public enum ErrorKind {
    Warn,     // never fatal
    Error,    // fatal if not handled
    Fatal,    // call handler but always fatal
    Assert,    // assertion error
    Panic     // immediately fatal
  };

  [Serializable]
  public class PolyException : Exception {
    public ErrorKind Kind = ErrorKind.Error;
    public PolyException(string msg) : base(msg) { }
    public PolyException(ErrorKind kind, string msg, params object[] args) : base(String.Format(msg, args)) {
      Kind = kind;
    }
  }

  /// <summary>
  /// Shortcut class to throw commonly used exceptions
  /// </summary>
  public class Error {
    public static Exception NullArg(string arg) {
      return new ArgumentNullException(arg);
    }
    public static Exception OutOfRange(string arg) {
      return new ArgumentOutOfRangeException(arg);
    }
    public static Exception Invalid(string msg) {
      return new InvalidOperationException(msg);
    }
    public static Exception MustOverride(string arg) {
      return new NotImplementedException("must override " + arg);
    }
    public static Exception Argument(string msg) {
      return new ArgumentException(msg);
    }
    public static Exception Fatal(string msg) {
      return new PolyException(ErrorKind.Fatal, "Fatal error: " + msg);
    }
    public static Exception Fatal(string origin, string msg, params object[] args) {
      var fmsg = string.Format(msg, args);
      return new PolyException(ErrorKind.Fatal, "Fatal error ({0}): {1}", origin, msg);
    }
    public static Exception NotImpl(string msg) {
      return new PolyException(ErrorKind.Fatal, "Not implemented: " + msg);
    }
    public static Exception Assert(string msg, params object[] args) {
      return new PolyException(ErrorKind.Assert, "Assertion failure: " + msg, args);
    }
    public static Exception Evaluation(object msg, params object[] args) {
      return new PolyException(ErrorKind.Assert, "Evaluation error: " + msg, args);
    }
    public static Exception Syntax(object msg, params object[] args) {
      return new PolyException(ErrorKind.Assert, "Syntax error: " + msg, args);
    }
  }
}
