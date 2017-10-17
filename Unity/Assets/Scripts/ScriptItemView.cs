using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PolygamoUnity;

public class ScriptItemView : MonoBehaviour {
  public Text NameText;

  static string _clicked;
  ScriptSelectionView _parent;

  void Start() {
    _parent = FindObjectOfType<ScriptSelectionView>();
  }

  private void OnEnable() {
    _clicked = "";
  }

  // Adding event handler kills off the scroll wheel for the list so only click comes here. 
  // So reluctantly we don't do hover, and emulate it instead.
  public void ButtonHandler(string input = "click") {
    Util.Trace(2, "Handled {0} for {1}", input, NameText.text);
    //_parent.ItemButtonHandler(NameText.text, input, NameText.text);
    if (NameText.text != _clicked) {
      _clicked = NameText.text;
      _parent.ItemButtonHandler(NameText.text, "enter", "Click again to play " + NameText.text);
    } else
      _parent.ItemButtonHandler(NameText.text, input, NameText.text);
  }
}
