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
/// Global builtins
/// 
using System;
using Poly.Common;

/// <summary>
/// These are the built in functions not part of any game.
/// </summary>
namespace Poly.Engine {

  ///---------------------------------------------------------------------------
  /// <summary>
  /// </summary>
  internal class Builtin {

    protected BoolValue s_Not(BoolValue arg) {
      return BoolValue.Create(!arg.Value);
    }

    protected PositionOrDirection s__PosOrDir(TypedValue value) {
      if (!(value.DataType == DataTypes.Position || value.DataType == DataTypes.Direction))
        Error.Assert("not position or direction");
      return new PositionOrDirection { Value = value };
    }
  }
}