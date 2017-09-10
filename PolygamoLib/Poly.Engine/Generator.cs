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
/// Code generator
/// 
using System;
using System.Collections.Generic;
using System.IO;
using Poly.Common;
using Poly.Engine;

namespace Poly.Compiler {
  internal class CodeScope {
    internal Type Type;
    internal List<object> Code;
  }
  /// <summary>
  /// Code generator
  /// </summary>
  internal class Generator {
    internal List<object> _gcode = new List<object>();
    internal IList<object> Code { get { return _gcode; } }
    internal int Counter { get{ return _gcode.Count; } }

    Stack<CodeScope> _codestack = new Stack<CodeScope>();

    internal static Generator Create() {
      Logger.WriteLine(3, "Generator create");
      return new Generator { };
    }

    internal void EmitLoadVar(Symbol sym) {
      Logger.WriteLine(4, "Emit load var {0}", sym);
      Emit(Opcodes.LDVAR, sym.Name);
    }

    internal void EmitLoadValue(object value) {
      Logger.WriteLine(4, "Emit load value {0}", value);
      if (value == null)
        Emit(Opcodes.LDNULL);
      else Emit(Opcodes.LDVALUE, value);
    }

    internal void EmitRefVar(Symbol sym) {
      Logger.WriteLine(4, "Emit ref var {0}", sym);
      Emit(Opcodes.LDVARREF, sym.Name);
    }

    internal void EmitCall(BuiltinSymbol funcsym) {
      Logger.WriteLine(4, "Emit call {0}", funcsym);
      Emit(Opcodes.CALL, funcsym.CallInfo);
    }

    internal void EmitToList(Type type, int nargs) {
      Logger.WriteLine(4, "Emit to list {0} {1}", type, nargs);
      Emit(Opcodes.TOLIST, type, nargs);
    }

    internal void EmitToArray(Type type, int nargs) {
      Logger.WriteLine(4, "Emit to array {0} {1}", type, nargs);
      Emit(Opcodes.TOARRAY, type, nargs);
    }

    internal void EmitToPair(Type type) {
      Logger.WriteLine(4, "Emit to Pair {0}", type);
      Emit(Opcodes.TOPAIR, type);
    }

    // enter new instance scope
    internal void EmitEntry(Type type) {
      Logger.WriteLine(4, "Emit entry {0}", type);
      //Emit(Opcodes.ENTRY, type);
      _codestack.Push(new CodeScope { Type = type, Code = _gcode });
      _gcode = new List<object>();
    }

    // exit instance scope, capture generated code
    internal void EmitExit(bool keep) {
      Logger.WriteLine(4, "Emit exit");
      var gcode = _gcode;
      var codest = _codestack.Pop();
      _gcode = codest.Code;
      Emit(Opcodes.NEW, codest.Type, gcode);
    }

    internal void EmitGoTo(int counter) {
      Logger.WriteLine(4, "Emit goto {0}", counter);
      Emit(Opcodes.GOTO, counter);
    }

    internal void EmitGoTrue(int counter) {
      Logger.WriteLine(4, "Emit gotrue {0}", counter);
      Emit(Opcodes.GOTRUE, counter);
    }

    internal void EmitGoFalse(int counter) {
      Logger.WriteLine(4, "Emit gofalse {0}", counter);
      Emit(Opcodes.GOFALSE, counter);
    }

    void Emit(Opcodes opcode, params object[] args) {
      _gcode.Add(opcode);
      _gcode.AddRange(args);
    }

    // decode current compiled code
    internal void Decode(TextWriter tw) {
      tw.WriteLine("Decode: {0}", _gcode.Count);
      Decode(tw, _gcode, 0);
    }
    void Decode(TextWriter tw, List<object> gcode, int depth) {
      tw.WriteLine("\n--> depth={0}", depth);
      StringWriter sw = new StringWriter();
      for (int i = 0; i < gcode.Count; ) {
        var opcode = (Opcodes)gcode[i++];
        sw.Write("| {0,4} {1,-10}: ", i - 1, opcode);
        switch (opcode) {
        case Opcodes.NOP:
        case Opcodes.EOS:
        case Opcodes.LDNULL:
          break;
        case Opcodes.CALL:
        case Opcodes.GOTO:
        case Opcodes.GOFALSE:
        case Opcodes.GOTRUE:
        case Opcodes.LDVAR:
        case Opcodes.LDVARREF:
        case Opcodes.LDVALUE:
        case Opcodes.TOPAIR:
          sw.Write("{0}", gcode[i++]);
          break;
        case Opcodes.TOLIST:
        case Opcodes.TOARRAY:
          sw.Write("{0} n={1}", gcode[i++], gcode[i++]);
          break;
        case Opcodes.NEW:
          sw.Write("{0} t={1}", gcode[i++], gcode[i++]);
          Decode(sw, gcode[i - 1] as List<object>, depth + 1);
          break;
        default:
          throw Error.Evaluation("bad opcode: {0}", opcode);
        }
        tw.WriteLine(sw.ToString());
        sw.GetStringBuilder().Length = 0;
      }
      tw.WriteLine("<-- depth={0}", depth);
    }

    internal void Fixup(int fixat) {
      _gcode[fixat + 1] = Counter;
    }

  }
}
