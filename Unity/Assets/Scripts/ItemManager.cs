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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using PolygamoUnity;

public class ItemManager : MonoBehaviour {

  //-- public settables
  public Sprite EmptySprite;
  public Color EmptyColor;

  internal List<string> ScriptList = new List<string>();
  internal int CurrentGame { get; private set; }

  // Make a list of every text asset under Games that is in a folder of the same name
  internal void FindAllGames() {
    var scripts = Resources.LoadAll<TextAsset>("Games");
    Util.Trace(2, "Load games {0}", scripts.Select(s=>s.name).Join());
    for (int i = 0; i < scripts.Length; i++) {
      var name = scripts[i].name;
      var path = Util.Combine("Games", name, name);
      var script = Resources.Load<TextAsset>(path);
      if (script != null) ScriptList.Add(name);
      Util.Trace(3, "Game {0} {1} {2}", name, path, script);
      Resources.UnloadAsset(scripts[i]);
      Resources.UnloadAsset(script);
    }
  }

  // load a script by name from folder (usually same name)
  internal string LoadScript(string folder, string script) {
    Util.Trace(2, "Load script folder={0} script={1}", folder, script);
    if (!ScriptList.Contains(folder)) return null;
    var path = Path.ChangeExtension(Util.Combine("Games", folder, script), null);
    var rc = Resources.Load<TextAsset>(path);
    return rc == null ? null : rc.text;
  }

  // load image, colour and rotation into object by decoding sprite name
  internal bool LoadImage(GameObject obj, string game, string loadinfo) {
    //Util.Trace(2, "Load image {0} {1}", game, loadinfo);
    var image = obj.GetComponent<Image>();
    if (loadinfo == "") {
      image.sprite = EmptySprite;
      image.color = EmptyColor;
    } else {
      var split = (loadinfo + "::").Split(':');
      var sprite = LoadSprite(game, split[0]);
      if (sprite == null) sprite = LoadSprite(null, split[0]);
      if (sprite == null) return false;
      image.preserveAspect = true;
      image.sprite = sprite;
      image.color = DecodeColour(split[1]);
      obj.transform.localRotation = DecodeRotation(split[2]);
    }
    return true;
  }

  // load sprite from assets by game and image name
  private Sprite LoadSprite(string game, string name) {
    var path = (game == null) ? Path.ChangeExtension(Util.Combine("Common", name), null)
      : Path.ChangeExtension(Util.Combine("Games", game, name), null);
    return Resources.Load<Sprite>(path);
  }

  // load sprite by name from raw file
  private Sprite LoadSpriteRaw(string name) {
    var tex = new Texture2D(2, 2); // size not used
    var bits = Util.LoadBinary("Games", name);
    tex.LoadImage(bits);
    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
  }

  static Dictionary<string, Color> _colorlookup = new Dictionary<string, Color> {
      { "white", Color.white },
      { "grey", Color.grey },
      { "black", Color.black },
      { "red", Color.red },
      { "blue", Color.blue },
      { "green", Color.green },
      { "yellow", Color.yellow},
      { "cyan", Color.cyan },
      { "magenta", Color.magenta },
    };

  private Color DecodeColour(string colour) {
    if (_colorlookup.ContainsKey(colour)) return _colorlookup[colour];
    int hexvalue = 0;
    if (int.TryParse(colour, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out hexvalue))
      return new Color((hexvalue & 0xff0000) >> 16, (hexvalue & 0xff00) >> 8, (hexvalue & 0xff), (hexvalue & 0xff00) >> 24);
    return Color.white;
  }

  private Quaternion DecodeRotation(string angle) {
    return angle == "" ? Quaternion.identity 
      : Quaternion.AngleAxis(angle.SafeFloatParse() ?? 0f, Vector3.forward);
  }
}

