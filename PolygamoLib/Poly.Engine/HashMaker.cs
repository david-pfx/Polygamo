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
/// Hashmaker
/// 
using System;
using System.Collections.Generic;
using Poly.Common;

/// <summary>
/// Hashmaker is a static class accessible from anywhere to calculate and remember
/// a random 64-bit hash on a name. The approach is as per Zobrist.
/// </summary>
namespace Poly.Engine {
  internal static class HashMaker {
    static Dictionary<string, long> _hashlookup = new Dictionary<string, long>();
    static Random _rng = new Random();

    static internal long GetHash(IdentValue name) {
      long hash;
      if (_hashlookup.TryGetValue(name.Value, out hash)) return hash;
      hash = _rng.Next();
      hash = (hash << 32) | (long)_rng.Next();
      _hashlookup[name.Value] = hash;
      return hash;
    }
  }
}
