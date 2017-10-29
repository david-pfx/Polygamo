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
/// Common routines
/// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Poly.Common {
  // General extensions
  public static class UtilExtensions {

    public static string Format(this byte[] value) {
      var s = value.Select(b => String.Format("{0:x2}", b)).ToArray();
      return String.Join("", s);
    }

    // string join that works on any enumerable
    public static string Join<T>(this IEnumerable<T> values, string delim = ",") {
      return String.Join(delim, values.Select(v => v == null ? "null" : v.ToString()).ToArray());
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

    // return simple split with trim
    public static string[] Split(this string target, string delim = ",") {
      var parts = target.Split(delim[0]);
      return parts.Select(p => p.Trim()).ToArray();
    }

    // return nth split on ';' as an array of strings split on ','
    public static string[] SplitXY(this string target, int index = 0) {
      var parts = target.Split(';');
      return (index < parts.Length) ? parts[index].Split(',') : null;
    }

    // safe parsing routines, return null on error
    public static bool? SafeBoolParse(this string s) {
      bool value;
      return bool.TryParse(s, out value) ? value as bool? : null;
    }

    public static DateTime? SafeDatetimeParse(this string s) {
      DateTime value;
      return DateTime.TryParse(s, out value) ? value as DateTime? : null;
    }

    public static decimal? SafeDecimalParse(this string s) {
      decimal value;
      return Decimal.TryParse(s, out value) ? value as decimal? : null;
    }

    public static double? SafeDoubleParse(this string s) {
      double value;
      return double.TryParse(s, out value) ? value as double? : null;
    }

    public static float? SafeFloatParse(this string s) {
      float value;
      return float.TryParse(s, out value) ? value as float? : null;
    }

    public static int? SafeIntParse(this string s) {
      int value;
      return int.TryParse(s, out value) ? value as int? : null;
    }

    // Simplistic manipulation of MultiDictionary
    public static void AddMulti<T, U>(this Dictionary<T, List<U>> dict, T key, U item) {
      List<U> value;
      if (dict.TryGetValue(key, out value)) value.Add(item);
      else dict.Add(key, new List<U> { item });
    }

    public static List<U> GetMulti<T, U>(this Dictionary<T, List<U>> dict, T key) {
      List<U> value;
      if (dict.TryGetValue(key, out value)) return value;
      else return null;
    }

    public static U SafeLookup<T, U>(this Dictionary<T, U> dict, T key) {
      U ret;
      if (dict.TryGetValue(key, out ret)) return ret;
      return default(U);
    }

  }
}
