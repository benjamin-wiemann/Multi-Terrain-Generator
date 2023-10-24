using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

[CustomEditor(typeof(Test))]
public class TestListGUI : Editor
{
    //public override void OnInspectorGUI()
    //{
    //    serializedObject.Update();

    //    // Draw the default properties of ColorListManager (if any).
    //    DrawDefaultInspector();

    //    // Get the reference to the nameColorPairs property.
    //    SerializedProperty nameColorPairs = serializedObject.FindProperty("nameColorPairs");

    //    // Display a box around the list.
    //    EditorGUILayout.BeginVertical("box");

    //    // Iterate through the list and display elements with customized labels.
    //    for (int i = 0; i < nameColorPairs.arraySize; i++)
    //    {
    //        SerializedProperty element = nameColorPairs.GetArrayElementAtIndex(i);
    //        SerializedProperty name = element.FindPropertyRelative("name");
    //        SerializedProperty color = element.FindPropertyRelative("color");

    //        EditorGUILayout.BeginHorizontal();
    //        EditorGUILayout.PropertyField(name, GUIContent.none);
    //        EditorGUILayout.PropertyField(color, GUIContent.none);
    //        EditorGUILayout.EndHorizontal();
    //    }

    //    EditorGUILayout.EndVertical();

    //    serializedObject.ApplyModifiedProperties();
    //}
}
