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

using System;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;
using System.IO;

namespace PolygamoUnity {
  public static class Util {
    public static int MaxLevel = 2;
    public static void Trace(int level, string format, params object[] args) {
      if (level <= MaxLevel) Debug.Log(string.Format(format, args));
    }

    public static void IsTrue(bool test, object msg = null) {
      if (test) return;
      Trace(0, "Assertion IsTrue failed: {0}", msg);
    }

    public static void NotNull(object obj, object msg = null) {
      if (obj != null) return;
      Trace(0, "Assertion NotNull failed: {0}", msg);
    }

    public static string Join<T>(this IEnumerable<T> args, string delim = ",") {
      return string.Join(delim, args.Select(i => (i == null) ? "null" : i.ToString()).ToArray());
    }

    // truncate a string if too long
    public static string Shorten(this string argtext, int len) {
      var text = argtext.Replace('\n', '.');
      if (text.Length <= len) return text;
      return text.Substring(0, len - 3) + "...";
    }

    public static string ShortenLeft(this string argtext, int len) {
      var text = argtext.Replace('\n', '.');
      if (text.Length <= len) return text;
      return "..." + text.Substring(text.Length - len + 3);
    }

    public static string Repeat(this string arg, int count) {
      var tmp = new StringBuilder();
      while (count-- > 0)
        tmp.Append(arg);
      return tmp.ToString();
    }


    // Pick at random from sequence using RNG
    public static T Pick<T>(this IEnumerable<T> input, Random rng) {
      var count = input.Count();
      if (count <= 1) return input.FirstOrDefault();
      return input.Skip(rng.Next(count)).First();
    }

    // Shuffle a sequence using a common RNG
    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> input, Random rng) {
      var list = new List<T>(input);
      while (list.Count > 0) {
        var x = rng.Next(list.Count);
        yield return list[x];
        list.RemoveAt(x);
      }
    }

    // positive modulus
    public static int PosMod(this int n, int d) {
      var mod = n % d;
      return (mod >= 0) ? mod : mod + d;
    }

    public static float? SafeFloatParse(this string s) {
      float value;
      return float.TryParse(s, out value) ? value as float? : null;
    }

    public static int? SafeIntParse(this string s) {
      int value;
      return int.TryParse(s, out value) ? value as int? : null;
    }

    public static U SafeLookup<T, U>(this Dictionary<T, U> dict, T key) {
      U ret;
      if (dict.TryGetValue(key, out ret)) return ret;
      return default(U);
    }

    //===> IO functions

    // augmented Path.Combine, ignore null components, convert '\' to '/'
    public static string Combine(string arg, params string[] args) {
      var ret = arg;
      for (int i = 0; i < args.Length; i++)
        if (args[i] != null) ret = Path.Combine(ret, args[i]);
      return ret.Replace('\\', '/');
    }

    public static byte[] LoadBinary(string folder, string name) {
      var path = Path.Combine(Application.dataPath, Path.Combine(folder, name));
      using (var fs = new FileStream(path, FileMode.Open)) {
        var buf = new byte[fs.Length];
        fs.Read(buf, 0, (int)fs.Length);
        return buf;
      }
    }

    public static string LoadText(string folder, string name) {
      var path = Path.Combine(Application.dataPath, Path.Combine(folder, name));
      using (var sr = new StreamReader(path)) {
        return sr.ReadToEnd();
      }
    }

  }
}