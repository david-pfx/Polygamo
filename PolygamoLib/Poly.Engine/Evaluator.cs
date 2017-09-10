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
/// Virtual machine
/// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using Poly.Common;
using Poly.Compiler;

namespace Poly.Engine {
  internal enum Opcodes {
    NOP,
    CALL,       // (methodinfo) call with fixed args
    LDVAR,      // (name) load variable value by name
    LDVARREF,   // (name) load reference to variable
    LDVALUE,    // (value) load actual value
    LDNULL,     // () null value, convenience
    TOLIST,     // (type,nargs) convert to list
    TOARRAY,    // (type,nargs) convert to array
    TOPAIR,    // (type) convert to tuple
    NEW,        // () create instance scope with code
    EOS,        // end of statement, pop value as return
    GOTO,       // goto pc offset
    GOFALSE,    // goto pc offset if tos false
    GOTRUE,     // goto pc offset if tos true
  };

  internal class Variable {
    internal TypedValue Value = VoidValue.Void;
  }

  /// <summary>
  /// Implements the runtime for expression evaluation.
  /// Stack based vm code.
  /// </summary>
  internal class Evaluator {
    internal TextWriter Output { get; private set; }
    internal TextReader Input { get; private set; }

    //-- creation
    internal static Evaluator Create(TextWriter output, TextReader input) {
      return new Evaluator() {
        Output = output,
        Input = input,
      };
    }

    // Initial entry point from compiler -- no instance available
    internal object Exec(IList<object> code) {
      return Exec(StartCode.Create(this, code));
    }

    // Entry point from with instance
    internal object Exec(CodeBase instance) {
      Logger.WriteLine(5, "Exec {0}", instance);

      var scope = new EvaluationScope { Parent = this };
      try {
        scope.Run(instance);
      } catch (TargetInvocationException ex) {
        Logger.WriteLine(3, "Exception {0}", ex.ToString());
        throw ex.InnerException;
      }
      return scope.ReturnValue;
    }
  }

  /// <summary>
  /// An evaluation scope, including execution stack and local variable storage
  /// </summary>
  internal class EvaluationScope {

    // parent evaluator
    internal Evaluator Parent;
    // return value
    internal object ReturnValue { get { return _stack.Count > 0 ? _stack.Last(): null; } }

    // logging flag
    bool _logging = false;
    // runtime evaluation stack
    List<object> _stack = new List<object>();
    // variable storage
    Dictionary<string, Variable> _variables = new Dictionary<string, Variable>();

    void PushStack(object value) {
      _stack.Add(value);
    }

    object PopStack() {
      var top = _stack[_stack.Count - 1];
      _stack.RemoveAt(_stack.Count - 1);
      return top;
    }

    object PeekStack() {
      return _stack[_stack.Count - 1];
    }

    void PopStack(int n) {
      _stack.RemoveRange(_stack.Count - n, n);
    }

    List<object> PopStackList(int n) {
      var ret = _stack.GetRange(_stack.Count - n, n);
      _stack.RemoveRange(_stack.Count - n, n);
      return ret;
    }

    static readonly MethodInfo CastMethod = typeof(Enumerable).GetMethod("Cast");
    static readonly MethodInfo ToListMethod = typeof(Enumerable).GetMethod("ToList");
    static readonly MethodInfo ToArrayMethod = typeof(Enumerable).GetMethod("ToArray");

    object PopStackList(Type type, int num) {
      var values = _stack.Skip(_stack.Count - num);
      var cast = CastMethod.MakeGenericMethod(type)
                          .Invoke(null, new object[] { values });
      var list = ToListMethod.MakeGenericMethod(type)
                                .Invoke(null, new object[] { cast });
      _stack.RemoveRange(_stack.Count - num, num);
      return list;
    }

    private object PopStackArray(Type type, int num) {
      var values = _stack.Skip(_stack.Count - num);
      var cast = CastMethod.MakeGenericMethod(type)
                          .Invoke(null, new object[] { values });
      var array = ToArrayMethod.MakeGenericMethod(type)
                                .Invoke(null, new object[] { cast });
      _stack.RemoveRange(_stack.Count - num, num);
      return array;
    }

    object PopStackPair(Type type) {
      var num = type.GetGenericArguments().Length;
      var values = PopStackList(num);
      var creator = type.GetConstructor(type.GetGenericArguments());
      return creator.Invoke(values.ToArray());
    }

    // Perform a value lookup for project
    internal Variable Lookup(string name) {
      if (!_variables.ContainsKey(name))
        _variables.Add(name, new Variable { Value = BoolValue.False });
      return _variables[name];
    }

    // Evaluation engine for gencode
    internal void Run(CodeBase instance) {
      _logging = Logger.Level >= 5;
      if (_logging) Logger.WriteLine(4, "Run {0}", instance);

      var gencode = instance.Code;
      StringWriter sw = new StringWriter();

      for (var pc = 0; pc < gencode.Count;) {
        var opcode = (Opcodes)gencode[pc++];
        if (_logging) sw.Write(">>> {0,3}: {1} {2} ", pc, _stack.Count, opcode);
        switch (opcode) {
        case Opcodes.NOP:
          break;
        case Opcodes.NEW:
          var newtype = gencode[pc++] as Type;
          var newcode = gencode[pc++] as List<object>;
          if (_logging) sw.Write("{0} code={1}", newtype, newcode.Count);
          var newinst = Activator.CreateInstance(newtype, true) as CodeBase;
          newinst.Code = newcode;
          newinst.Evaluator = Parent;
          PushStack(newinst);
          break;
        case Opcodes.CALL:
          var calli = gencode[pc++] as CallInfo;
          var calla = PopStackList(calli.NumArgs).ToArray();
          if (_logging) sw.Write("{0} ({1})", calli, calla.Join());
          instance.Counter = pc;
          var ret = calli.MethodInfo.Invoke(instance, calla);
          pc = instance.Counter;
          if (ret != null) PushStack(ret);
          break;
        case Opcodes.GOTO:
          if (_logging) sw.Write("{0}", gencode[pc]);
          var gotopc = (int)gencode[pc++];
          pc = gotopc;
          break;
        case Opcodes.GOFALSE:
          if (_logging) sw.Write("{0} ({1})", gencode[pc], PeekStack());
          var gofpc = (int)gencode[pc++];
          var goftest = PopStack() as BoolValue;
          if (!goftest.Value) pc = gofpc;
          break;
        case Opcodes.GOTRUE:
          if (_logging) sw.Write("{0} ({1})", gencode[pc], PeekStack());
          var gotpc = (int)gencode[pc++];
          var gottest = PopStack() as BoolValue;
          if (gottest.Value) pc = gotpc;
          break;
        case Opcodes.LDVAR:
          if (_logging) sw.Write("{0}", gencode[pc]);
          var varval = Lookup(gencode[pc++] as string);
          PushStack(varval.Value);
          break;
        case Opcodes.LDVARREF:
          if (_logging) sw.Write("{0}", gencode[pc]);
          var varref = Lookup(gencode[pc++] as string);
          PushStack(varref);
          break;
        case Opcodes.LDVALUE:
          if (_logging) sw.Write("{0}", gencode[pc]);
          PushStack(gencode[pc++]);
          break;
        case Opcodes.LDNULL:
          PushStack(null);
          break;
        case Opcodes.TOLIST:
          if (_logging) sw.Write("{0} {1}", gencode[pc], gencode[pc + 1]);
          var toltype = gencode[pc++] as Type;
          var toln = (int)gencode[pc++];
          var tolarg = PopStackList(toltype.GetGenericArguments()[0], toln);
          PushStack(tolarg);
          break;
        case Opcodes.TOARRAY:
          if (_logging) sw.Write("{0} {1}", gencode[pc], gencode[pc + 1]);
          var toatype = gencode[pc++] as Type;
          var toan = (int)gencode[pc++];
          var toaarg = PopStackArray(toatype.GetElementType(), toan);
          PushStack(toaarg);
          break;
        case Opcodes.TOPAIR:
          if (_logging) sw.Write("{0}", gencode[pc]);
          var tottype = gencode[pc++] as Type;
          PushStack(PopStackPair(tottype));
          break;
        case Opcodes.EOS:
          break;
        default:
          throw Error.Assert("bad opcode: {0}", opcode);
        }
        if (_logging) Logger.WriteLine(4, sw.ToString());
        if (_logging) sw.GetStringBuilder().Length = 0;
      }
    }
  }
}
