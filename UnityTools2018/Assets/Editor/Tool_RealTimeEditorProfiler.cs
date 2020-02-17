using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class Tool_RealTimeEditorProfiler : EditorWindow
{
    private bool _ShowInScene = true;
    private bool _Pauze;
    private float _FPS;
    private int _TotalObjects;

    [MenuItem("Tools/RealTime Profiler")]
    static void Init()
    {
        Tool_RealTimeEditorProfiler window = (Tool_RealTimeEditorProfiler)EditorWindow.GetWindow(typeof(Tool_RealTimeEditorProfiler));
        window.Show();
    }

    //Enable/Disable
    void OnEnable()
    {
        SceneView.onSceneGUIDelegate += this.OnScene;
    }
    void OnDisable()
    {
        SceneView.onSceneGUIDelegate -= this.OnScene;
    }

    private void Update()
    {
        if (!_Pauze)
        {
            Object[] Objects = FindObjectsOfType(typeof(GameObject));

            _FPS = (int)(1.0f / Time.smoothDeltaTime);
            _TotalObjects = Objects.Length;
        }
    }

    void OnScene(SceneView sceneView)
    {
        if (_ShowInScene)
        {
            Handles.BeginGUI();
            GUI.Label(new Rect(5, 5, 500, 20), "FPS: " + _FPS.ToString());
            GUI.Label(new Rect(5, 20, 500, 20), "Objects in scene: " + _TotalObjects.ToString());
            Handles.EndGUI();
        }
    }

    void OnGUI()
    {
        GUILayout.BeginVertical("Box");
        _ShowInScene = EditorGUILayout.Toggle("SceneView: ",_ShowInScene);
        _Pauze = EditorGUILayout.Toggle("Pauze: ", _Pauze);

        GUILayout.BeginVertical("Box");
        GUILayout.Label("FPS: " + _FPS.ToString());
        GUILayout.Label("Total Objects: " + _TotalObjects.ToString());
        GUILayout.EndVertical();

        GUILayout.EndVertical();
    }
}


