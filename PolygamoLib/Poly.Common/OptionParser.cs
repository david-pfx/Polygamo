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
/// Parse command line options
/// 
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Poly.Common {
  /// <summary>
  /// Parse some options and filenames
  /// </summary>
  public class OptionParser {
    public int Noisy { get { return Logger.Level; } }
    public int PathsCount { get { return _paths.Count; } }
    public string GetPath(int n) {
      return (n < _paths.Count) ? _paths[n] : null;
    }

    List<string> _paths = new List<string>();
    Dictionary<string, Action<string>> _options;
    string _help;

    public static OptionParser Create(Dictionary<string, Action<string>> options, string help) {
      return new OptionParser { _options = options, _help = help };
    }

    public bool Parse(string[] args) {
      for (var i = 0; i < args.Length; ++i) {
        if (args[i].StartsWith("/") || args[i].StartsWith("-")) {
          if (!Option(args[i].Substring(1), args[i].Substring(2, args[i].Length - 2)))
            return false;
        } else _paths.Add(args[i]);
      }
      return true;
    }

    // Capture the options
    bool Option(string arg, string rest) {
      if (arg == "?") {
        Logger.WriteLine(_help);
        return false;
      } else if (Regex.IsMatch(arg, "[0-9]+")) {
        Logger.Level = int.Parse(arg);
      } else if (_options.ContainsKey(arg)) {
        _options[arg](rest);
      } else {
        Logger.WriteLine("*** Bad option: {0}", arg);
        return false;
      }
      return true;
    }
  }

}
