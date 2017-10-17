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
using B83.Image.BMP;
using PolygamoUnity;

internal class ScriptInfo {
  internal string Folder { get; private set; }   // folder for finding other things
  internal string Filename { get; private set; }     // name of script (including extension)
  internal bool IsAsset { get; private set; }    // true if Unity asset (no extension)

  public override string ToString() {
    return String.Format("{0}:{1}:{2}", Folder, Filename, IsAsset);
  }

  internal static ScriptInfo Create(string folder, string script, bool isasset) {
    return new ScriptInfo { Folder = folder, Filename = script, IsAsset = isasset };
  }
}

public class ItemManager : MonoBehaviour {
  const string GamesFolder = "Games";
  const string CommonFolder = "Common";

  //-- public settables
  public string UserGamesFolder = "User Games";
  public Sprite EmptySprite;
  public Color EmptyColor;

  internal List<ScriptInfo> ScriptList = new List<ScriptInfo>();
  internal string RootFolderReadme;

  readonly string[] GameNamePatterns = new string[] { "*.poly", "*.zrf" };
  string UserGamesPath;

  internal string LoadReadme(string name) {
    var game = ScriptList.Find(g => g.Filename == name);
    if (game == null) return "game not found!";
    if (game.IsAsset) {
      var resource = Resources.Load<TextAsset>(Path.Combine(game.Folder, "readme"));
      return resource == null ? "no readme" : resource.text;
    }
    return Util.LoadText(game.Folder, "readme.txt");
  }

  // Find all games and add to script list
  internal void FindAllGames() {
    UserGamesPath = Util.Combine(Path.GetDirectoryName(Application.dataPath), UserGamesFolder);
    // find all games included as text resources
    var texts = Resources.LoadAll<TextAsset>(GamesFolder);
    for (int i = 0; i < texts.Length; i++) {
      AddGame(texts[i].name);
      Resources.UnloadAsset(texts[i]);
    }
    RootFolderReadme = Util.LoadText(UserGamesPath, "readme.txt");
    AddGamesRaw(UserGamesPath);
    Util.Trace(2, "[Find games {0}]", ScriptList.Select(s => s.Filename).Join());
  }

  // Load by name provided text asset in folder of same name (need that to find images etc)
  internal void AddGame(string name) {
    var folder = Path.Combine(GamesFolder, name);
    var script = Resources.Load<TextAsset>(Path.Combine(folder, name));
    if (script != null) ScriptList.Add(ScriptInfo.Create(folder, name, true));
    Resources.UnloadAsset(script);
  }

  // Load games found in a folder: folder with script of same name
  private void AddGamesRaw(string folder) {
    try {
      foreach (var gamefolder in Directory.GetDirectories(folder)) {
        foreach (var pattern in GameNamePatterns) {
          var scripts = Directory.GetFiles(gamefolder, pattern);
          foreach (var script in scripts)
            ScriptList.Add(ScriptInfo.Create(gamefolder, Path.GetFileName(script), false));
        }
      }
    } catch (Exception) { }
  }

  // load a script by name
  internal string LoadScript(ScriptInfo script) {
    Util.Trace(2, "Load script {0}", script.Filename);
    if (script.IsAsset) {
      var rc = Resources.Load<TextAsset>(Util.Combine(script.Folder, script.Filename));
      return (rc == null) ? null : rc.text;
    } else
      return Util.LoadText(script.Folder, script.Filename);
  }

  // load image, colour and rotation into object by decoding sprite name
  internal bool LoadImage(ScriptInfo script, string loadinfo, GameObject obj) {
    Util.Trace(3, "Load image script={0} {1}", script, loadinfo);
    var image = obj.GetComponent<Image>();
    if (script == null) {
      image.sprite = EmptySprite;
      image.color = EmptyColor;
    } else {
      var split = (loadinfo + "::").Split(':');
      var sprite = LoadSprite(script, split[0]) ?? LoadSprite(CommonFolder, split[0]);
      if (sprite == null) return false;
      image.preserveAspect = true;
      image.sprite = sprite;
      image.color = DecodeColour(split[1]);
      obj.transform.localRotation = DecodeRotation(split[2]);
    }
    return true;
  }

  // load sprite from assets or from game folder or from user games
  Sprite LoadSprite(ScriptInfo script, string filename) {
    if (script.IsAsset)
      return LoadSprite(script.Folder, Path.ChangeExtension(filename, null));
    return LoadSpriteRaw(script.Folder, filename) ?? LoadSpriteRaw(UserGamesPath, filename);
  }

  // load sprite from resource
  Sprite LoadSprite(string folder, string filename) {
    var path = Path.Combine(folder, filename);
    return Resources.Load<Sprite>(path);
  }

  // load sprite by name from raw file
  Sprite LoadSpriteRaw(string folder, string filename) {
    if (Path.GetExtension(filename).ToLower() == ".bmp") return LoadSpriteBmp(folder, filename);
    var tex = new Texture2D(2, 2); // size not used
    var bits = Util.LoadBinary(folder, filename);
    tex.LoadImage(bits);
    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
  }

  Sprite LoadSpriteBmp(string folder, string filename) {
    var bits = Util.LoadBinary(folder, filename);
    if (bits == null) return null;
    var bmpload = new BMPLoader();
    if (bmpload == null) return null;
    var bmp = bmpload.LoadBMP(bits);
    if (bmp == null) return null;
    var transparency = Color.green;    // pure green is transparent
    for (int i = 0; i < bmp.imageData.Length; ++i)
      if (bmp.imageData[i] == transparency)
        bmp.imageData[i] = Color.clear;
    var tex = bmp.ToTexture2D();
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

