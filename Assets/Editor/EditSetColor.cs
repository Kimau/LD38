using UnityEditor;
using UnityEngine;
using System.Text.RegularExpressions;

[CustomEditor(typeof(setMatColour))]
public class EditSetColor : Editor
{
  public override void OnInspectorGUI()
  {

    if (Event.current.type != EventType.Layout)
      GUI.changed = false;

    DrawDefaultInspector();

    if (Event.current.type == EventType.Layout)
      return;

    if (GUI.changed)
    {
      setMatColour smc = target as setMatColour;
      smc.UpdateValues();
    }
  }
}
