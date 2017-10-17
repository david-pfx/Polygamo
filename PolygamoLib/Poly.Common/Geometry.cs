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
/// Geometry
/// 
using System;
using System.Collections.Generic;

namespace Poly.Common {

  // Simple rectangle
  // Note that (0,0) is top left
  public struct Rect {
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
    public static Rect Empty = new Rect();

    public bool IsEmpty { get { return this.Equals(Empty); } }
    // Given coords of corners
    public int Width { get { return Right - Left + 1; } }
    public int Height { get { return Bottom - Top + 1; } }
    //public int Width { get { return Right - Left; } }
    //public int Height { get { return Bottom - Top; } }

    public override string ToString() {
      return String.Format("[{0},{1},{2},{3}]", Left, Top, Right, Bottom);
    }

    public static Rect Create(int left, int top, int right, int bottom) {
      return new Rect { Left = left, Top = top, Right = right, Bottom = bottom };
    }
    public static Rect Create(IList<int> coords) {
      return new Rect { Left = coords[0], Top = coords[1], Right = coords[2], Bottom = coords[3] };
    }
    public Rect Offset(Coord coord) {
      return Create(Left + coord.X, Top + coord.Y, Right + coord.X, Bottom + coord.Y);
    }
  }

  public struct Coord {
    public int X;
    public int Y;

    public override string ToString() {
      return String.Format("[{0},{1}]", X, Y);
    }

    public static Coord Create(int x, int y) {
      return new Coord { X = x, Y = y };
    }

    public Coord Times(int n) {
      return Create(X * n, Y * n);
    }
  }
}

