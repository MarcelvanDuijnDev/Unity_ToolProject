using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;

public class MapEditor : EditorWindow 
{
    #region Variables
    private Color _DefaultColor;

    //Layers
    private List<MapEditorLayers> _MapLayers = new List<MapEditorLayers>();
    private GameObject[] _SelectedObjects;
    private bool _ShowObjects;
    private int _SelectedLayer;

    //Prefab Array
    private GameObject[] _Prefabs = new GameObject[0];
    private Texture2D[] _PrefabImg = new Texture2D[0];
    private string[] _Search_results = new string[0];

    //Array Options
    private string _SearchPrefab = "";
    private bool _HideNames = true;
    private float _ImageSize = 1;

    //Array Selection
    private float _CollomLength = 4;
    private int _SelectedID, _CheckSelectedID, _ShowOption;

    private float _SnapToSnapPosDistance = 2;
    private float _SnapDistanceCheck, _MouseDistanceCheck;
    private bool _Selected_SnapRight, _Selected_SnapLeft, _FoundSnapPoint;
    private bool _DoneActive, _ClearActive;
    private Vector3 _MousePos = new Vector3(0,0,0);
    private Vector3 _SnapPosition = new Vector3(0,0,0);
    private Vector3 _SnapPositionCheck = new Vector3(0,0,0);

    //Placement
    private GameObject _ParentObject, _ExampleObj, _LeftSnap, _RightSnap;
    private bool _MouseDown;
    private float _Timer;

    //Other
    private Vector2 _ScrollPos1, _ScrollPos2;
    #endregion

    [MenuItem("Tools/Map Editor")]
    static void Init() {
        MapEditor window = EditorWindow.GetWindow(typeof(MapEditor), false, "Map Editor") as MapEditor;
        window.minSize = new Vector2(550, 750);
        window.maxSize = new Vector2(550, 750);
        window.Show();
        window.minSize = new Vector2(50, 50);
        window.maxSize = new Vector2(9999999, 9999999);
    }

    //Open/Close Window
    private void Awake() {
        LoadPrefabs();
        LoadPrefabs();
    }
    void OnEnable() {
        SceneView.onSceneGUIDelegate += this.OnSceneGUI;
        _MapLayers.Add(new MapEditorLayers());
        _DefaultColor = GUI.backgroundColor;

        try {
            _ParentObject = GameObject.Find("MapObjects");
        }
        catch { Debug.Log("Can't find gameobject named: 'Parent'"); }
    }
    void OnDisable() {
        if (_ExampleObj != null)
            DestroyImmediate(_ExampleObj);
        SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
    }

    //Window/Scene
    void OnGUI() {
        PrefabBox();
        LayerBox();
    }
    void OnSceneGUI(SceneView sceneView) {
        Event e = Event.current;
        Ray worldRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        RaycastHit hitInfo;

        if (Physics.Raycast(worldRay, out hitInfo)) {
            _MousePos = new Vector3(hitInfo.point.x, hitInfo.point.y, hitInfo.point.z - 0.1f);
            if (!_FoundSnapPoint) {
                _SnapPosition = new Vector3(hitInfo.point.x, hitInfo.point.y, hitInfo.point.z - 0.1f);
            }
            else {
                _MouseDistanceCheck = Vector3.Distance(hitInfo.point, _SnapPosition);
                if (_MouseDistanceCheck > _SnapToSnapPosDistance) {
                    _FoundSnapPoint = false;
                    _ExampleObj.transform.position = hitInfo.point;
                    _SnapPosition = hitInfo.point;
                }
            }

            //Example Object
            if (_SelectedID <= _Prefabs.Length) {
                if (_CheckSelectedID != _SelectedID) {
                    DestroyImmediate(_ExampleObj);
                    _ExampleObj = Instantiate(_Prefabs[_SelectedID], hitInfo.point, Quaternion.identity);
                    _ExampleObj.layer = LayerMask.NameToLayer("Ignore Raycast");
                    for (int i = 0; i < _ExampleObj.transform.childCount; i++) {
                        _ExampleObj.transform.GetChild(i).gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
                        for (int o = 0; o < _ExampleObj.transform.GetChild(i).childCount; o++) {
                            _ExampleObj.transform.GetChild(i).GetChild(o).gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
                        }
                    }
                    _CheckSelectedID = _SelectedID;
                }
            }
            if (_ExampleObj != null && !Event.current.alt && _SelectedID != 99999999) {
                if (Event.current.type == EventType.Layout) {
                    HandleUtility.AddDefaultControl(0);
                }

                //Place Object
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0) {
                    _MouseDown = true;
                    if (!_ExampleObj.transform.name.Contains("Pannel")) {
                        CheckExampleObjectSnapPoints(false);
                        CreatePrefab(hitInfo.point);
                    }
                }
                if (Event.current.type == EventType.MouseUp && Event.current.button == 0) { 
                    _MouseDown = false;
                }

                if (_MouseDown && _ExampleObj.transform.name.Contains("Pannel") && _FoundSnapPoint) {
                    _Timer += 1 * Time.deltaTime;

                    CheckExampleObjectSnapPoints(false);
                    if (_SnapPositionCheck != _SnapPosition) {
                        CheckExampleObjectSnapPoints(false);
                        CreatePrefab(hitInfo.point);
                        _SnapPositionCheck = _SnapPosition;
                        Debug.Log("Creating");
                    }
                }

                // Draw obj location
                Handles.color = new Color(1, 0, 0);
                Handles.DrawLine(new Vector3(hitInfo.point.x - 0.3f, hitInfo.point.y, hitInfo.point.z), new Vector3(hitInfo.point.x + 0.3f, hitInfo.point.y, hitInfo.point.z));
                Handles.DrawLine(new Vector3(hitInfo.point.x, hitInfo.point.y - 0.3f, hitInfo.point.z), new Vector3(hitInfo.point.x, hitInfo.point.y + 0.3f, hitInfo.point.z));
                Handles.SphereHandleCap(0, _SnapPosition, Quaternion.identity, 0.1f, EventType.Repaint);

                //Draw snap points
                CheckExampleObjectSnapPoints(false);

                if (_SelectedLayer != 99999999 && _MapLayers[_SelectedLayer]._P_ObjectSnapPoints != null) {
                    for (int i = 0; i < _MapLayers[_SelectedLayer]._P_ObjectSnapPoints.Count; i++) {
                        if (!_ExampleObj.transform.name.Contains("Pannel") && !_ExampleObj.transform.name.Contains("Platform")) {
                            if (_MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_Left != null) {
                                if (_Selected_SnapLeft) Handles.color = new Color(0, 1, 0); else Handles.color = new Color(1, 0, 0);
                                Vector3 spherePoint = _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_Left.transform.position;
                                Handles.SphereHandleCap(0, new Vector3(spherePoint.x, spherePoint.y, spherePoint.z), Quaternion.identity, 0.2f, EventType.Repaint);
                            }
                            if (_MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_Right != null) {
                                if (_Selected_SnapRight) Handles.color = new Color(0, 1, 0); else Handles.color = new Color(1, 0, 0);
                                Vector3 spherePoint = _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_Right.transform.position;
                                Handles.SphereHandleCap(0, new Vector3(spherePoint.x, spherePoint.y, spherePoint.z), Quaternion.identity, 0.2f, EventType.Repaint);
                            }
                        }
                        else {
                            if (_ExampleObj.transform.name.Contains("Pannel")) {
                                if (_MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_Pannel.Count != 0) {
                                    for (int o = 0; o < _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_Pannel.Count; o++) {
                                        Vector3 spherePoint = _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_Pannel[o].transform.position;
                                        Handles.SphereHandleCap(0, new Vector3(spherePoint.x, spherePoint.y, spherePoint.z), Quaternion.identity, 0.2f, EventType.Repaint);
                                    }
                                }
                            }
                            if (_ExampleObj.transform.name.Contains("Platform")) {
                                if (_MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_PlatformLeft != null) {
                                    if (_Selected_SnapLeft) Handles.color = new Color(0, 1, 0); else Handles.color = new Color(1, 0, 0);
                                    Vector3 spherePoint = _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_PlatformLeft.transform.position;
                                    Handles.SphereHandleCap(0, new Vector3(spherePoint.x, spherePoint.y, spherePoint.z), Quaternion.identity, 0.2f, EventType.Repaint);
                                }
                                if (_MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_PlatformRight != null) {
                                    if (_Selected_SnapRight) Handles.color = new Color(0, 1, 0); else Handles.color = new Color(1, 0, 0);
                                    Vector3 spherePoint = _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_PlatformRight.transform.position;
                                    Handles.SphereHandleCap(0, new Vector3(spherePoint.x, spherePoint.y, spherePoint.z), Quaternion.identity, 0.2f, EventType.Repaint);
                                }
                            }
                        }
                    }
                }
            }
        }

        //Camera Vieuw
        var height = 2 * Camera.main.orthographicSize;
        var width = height * Camera.main.aspect;
        Handles.color = new Color(1, 0, 0);
        Handles.DrawLine(new Vector3(width * 0.5f, height * 0.5f, 0), new Vector3(-width * 0.5f, height * 0.5f, 0));
        Handles.DrawLine(new Vector3(width * 0.5f, -height * 0.5f, 0), new Vector3(-width * 0.5f, -height * 0.5f, 0));
        Handles.DrawLine(new Vector3(width * 0.5f, height * 0.5f, 0), new Vector3(width * 0.5f, -height * 0.5f, 0));
        Handles.DrawLine(new Vector3(-width * 0.5f, height * 0.5f, 0), new Vector3(-width * 0.5f, -height * 0.5f, 0));
    }

    //Prefabs/Layer InfoBox
    void PrefabBox()
    {
        GUILayout.BeginVertical("Box");
        GUILayout.Label("Objects");
        if (GUILayout.Button("Refresh", GUILayout.Width(70)))
            FixPreview();

        GUILayout.BeginHorizontal();
        _ShowOption = GUILayout.Toolbar(_ShowOption, new string[] { "Icon", "Text" });
        _ImageSize = EditorGUILayout.Slider(_ImageSize, 0.25f, 2);
        if (!_HideNames && GUILayout.Button("Hide Names", GUILayout.Width(100)))
            _HideNames = true;
        if (_HideNames && GUILayout.Button("Show Names", GUILayout.Width(100)))
            _HideNames = false;
        GUILayout.EndHorizontal();
        _SearchPrefab = EditorGUILayout.TextField("Search: ", _SearchPrefab);
        GUILayout.BeginVertical("Box");
        _CollomLength = position.width / (100 * _ImageSize);
        int x = 0; int y = 0;
        _ScrollPos1 = GUILayout.BeginScrollView(_ScrollPos1, GUILayout.Width(position.width - 20), GUILayout.Height(position.height - 350));
        for (int i = 0; i < _Search_results.Length; i++)
        {
            if (_Prefabs[i] != null && _Prefabs[i].name.ToLower().Contains(_SearchPrefab.ToLower()))
            {
                if (_ShowOption == 0)
                {
                    if (_SelectedID == i) { GUI.backgroundColor = new Color(0, 1, 0); } else { GUI.backgroundColor = new Color(1, 0, 0); }
                    GUIContent content = new GUIContent();
                    content.image = _PrefabImg[i];
                    GUI.skin.button.imagePosition = ImagePosition.ImageAbove;
                    if (!_HideNames)
                        content.text = _Prefabs[i].name;
                    if (GUI.Button(new Rect(x * 100 * _ImageSize, y * 100 * _ImageSize, 100 * _ImageSize, 100 * _ImageSize), content))
                        if (_SelectedID == i) { _SelectedID = 99999999; _CheckSelectedID = 99999999; DestroyImmediate(_ExampleObj); } else { _SelectedID = i; }
                    x++;
                    if (x >= _CollomLength - 1)
                    {
                        y++;
                        x = 0;
                    }
                    GUI.backgroundColor = _DefaultColor;
                }
                else
                {
                    if (_SelectedID == i) { GUI.backgroundColor = new Color(0, 1, 0); } else { GUI.backgroundColor = _DefaultColor; }
                    if (GUILayout.Button(_Prefabs[i].name))
                        if (_SelectedID == i) { _SelectedID = 99999999; _CheckSelectedID = 99999999; DestroyImmediate(_ExampleObj); } else { _SelectedID = i; }
                    GUI.backgroundColor = _DefaultColor;
                }
            }
        }
        if (_ShowOption == 0)
            GUILayout.Space(y * 100 * _ImageSize + 100);
        GUILayout.EndScrollView();
        GUILayout.EndVertical();
        GUILayout.EndVertical();
    }
    void LayerBox()
    {
        GUILayout.BeginVertical("Box");
        GUILayout.Label("Layers");
        GUILayout.BeginVertical("Box");

        if (!_DoneActive)
        {
            if (GUILayout.Button("Done"))
                _DoneActive = true;
        }
        else
        {
            GUILayout.BeginVertical("Box");
            GUILayout.Label("Are u sure?");
            if (GUILayout.Button("Yes"))
            {
                Fix();
                Debug.Log("Level Creation Complete");
            }
            if (GUILayout.Button("No"))
            {
                _DoneActive = false;
            }
            GUILayout.EndVertical();
        }

        if (!_ClearActive)
        {
            if (GUILayout.Button("Clear"))
            {
                _ClearActive = true;
            }
        }
        else
        {
            GUILayout.BeginVertical("Box");
            GUILayout.Label("Are u sure?");
            if (GUILayout.Button("Yes Clear map"))
            {
                Clear();
                _ClearActive = false;
                Debug.Log("Level Cleared");
            }
            if (GUILayout.Button("No Keep map"))
            {
                _ClearActive = false;
            }
            GUILayout.EndVertical();
        }

        if (GUILayout.Button("Add Layer"))
        {
            _MapLayers.Add(new MapEditorLayers());
            _SelectedLayer = _MapLayers.Count - 1;
        }
        if (GUILayout.Button("Remove Layer"))
        {
            for (int i = 0; i < _MapLayers[_SelectedLayer]._P_Objects.Count; i++)
            {
                _MapLayers[_SelectedLayer]._P_Objects[i].SetActive(true);
            }
            _MapLayers.Remove(_MapLayers[_SelectedLayer]);
        }
        GUILayout.EndVertical();
        GUILayout.BeginVertical("Box");
        ShowLayers();
        GUILayout.EndVertical();
        GUILayout.EndVertical();
    }

    //Load/Fix Prefabs
    void LoadPrefabs() {
        _Search_results = System.IO.Directory.GetFiles("Assets/Game/Prefabs/Editorobjects/MapEditor", "*.prefab", System.IO.SearchOption.AllDirectories);
        _Prefabs = new GameObject[_Search_results.Length];
        _PrefabImg = new Texture2D[_Search_results.Length];

        for (int i = 0; i < _Search_results.Length; i++) {
            Object prefab = null;
            prefab = AssetDatabase.LoadAssetAtPath(_Search_results[i], typeof(GameObject));
            _Prefabs[i] = prefab as GameObject;

            _PrefabImg[i] = AssetPreview.GetAssetPreview(_Prefabs[i]);
        }
    }
    void FixPreview() {
        LoadPrefabs();
        _Search_results = System.IO.Directory.GetFiles("Assets/Game/Prefabs/Editorobjects/MapEditor", "*.prefab", System.IO.SearchOption.AllDirectories);

        for (int i = 0; i < _Search_results.Length; i++) {
            if (_PrefabImg[i] == null) {
                AssetDatabase.ImportAsset(_Search_results[i]);
            }
        }
        LoadPrefabs();
    }

    //Create/Check Prefab
    void CreatePrefab(Vector3 createPos) {
        GameObject createdObj = null;
        if (_ExampleObj.transform.name.Contains("Pannel")) {
            if (_FoundSnapPoint) {
                createdObj = PrefabUtility.InstantiatePrefab(_Prefabs[_SelectedID]) as GameObject;
                createdObj.transform.position = new Vector3(createPos.x, createPos.y, createPos.z);
            }
        }
        else {
             createdObj = PrefabUtility.InstantiatePrefab(_Prefabs[_SelectedID]) as GameObject;
             createdObj.transform.position = new Vector3(createPos.x, createPos.y, createPos.z);
        }
        if (_ExampleObj.transform.name.Contains("Pannel")) {
            if (_FoundSnapPoint) {
                createdObj.transform.position = _SnapPosition;
                createdObj.transform.parent = _ParentObject.transform;
            }
        }
        else {
            createdObj.transform.position = _SnapPosition;
            createdObj.transform.parent = _ParentObject.transform;
        }
        if(createdObj != null)
        AddObjectToLayer(createdObj);
    }
    void CheckExampleObjectSnapPoints(bool creating) {
        if (_ExampleObj != null) {
            if (!_ExampleObj.transform.name.Contains("Pannel") && !_ExampleObj.transform.name.Contains("Platform")) {
                Vector2 check = new Vector2(0, 0);
                if (_ExampleObj.transform.childCount == 0) {
                    check = new Vector2(0, 0);
                }
                for (int i = 0; i < _ExampleObj.transform.childCount; i++) {
                    if (_ExampleObj.transform.Find("SnapPoint_Left"))
                        check.x = 1;
                    if (_ExampleObj.transform.Find("SnapPoint_Right"))
                        check.y = 1;
                }
                if (check.x == 1)
                    _Selected_SnapRight = true;
                else
                    _Selected_SnapRight = false;
                if (check.y == 1)
                    _Selected_SnapLeft = true;
                else
                    _Selected_SnapLeft = false;
                for (int i = 0; i < _MapLayers[_SelectedLayer]._P_ObjectSnapPoints.Count; i++) {
                    if (_Selected_SnapLeft && _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_Left != null) {
                        _SnapDistanceCheck = Vector3.Distance(_SnapPosition, _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_Left.transform.position);
                        if (_SnapDistanceCheck < _SnapToSnapPosDistance) {
                            _SnapPosition = new Vector3(_MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_Left.transform.position.x - _ExampleObj.transform.Find("SnapPoint_Right").gameObject.transform.localPosition.x, _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_Left.transform.position.y, _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_Left.transform.position.z);
                            _FoundSnapPoint = true;
                            if (creating) {
                                _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_Left = null;
                                _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[_MapLayers[_SelectedLayer]._P_ObjectSnapPoints.Count]._P_Snap_Right = null;
                            }
                        }
                        else {
                            _FoundSnapPoint = false;
                        }
                    }
                    if (_Selected_SnapRight && _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_Right != null) {
                        _SnapDistanceCheck = Vector3.Distance(_SnapPosition, _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_Right.transform.position);
                        if (_SnapDistanceCheck < _SnapToSnapPosDistance) {
                            _SnapPosition = new Vector3(_MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_Right.transform.position.x - _ExampleObj.transform.Find("SnapPoint_Left").gameObject.transform.localPosition.x, _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_Right.transform.position.y, _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_Right.transform.position.z);
                            _FoundSnapPoint = true;
                            if (creating) {
                                _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_Right = null;
                                _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[_MapLayers[_SelectedLayer]._P_ObjectSnapPoints.Count]._P_Snap_Left = null;
                            }
                        }
                        else {
                            _FoundSnapPoint = false;
                        }
                    }
                }
                _ExampleObj.transform.position = new Vector3(_SnapPosition.x, _SnapPosition.y, _SnapPosition.z - 0.1f);

                Handles.color = new Color(0.25f, 0.25f, 0.35f);
                Handles.SphereHandleCap(0, _SnapPosition, Quaternion.identity, 0.5f, EventType.Repaint);
            }
            else {
                if (_ExampleObj.transform.name.Contains("Pannel")) {
                    int check = 0;
                    for (int i = 0; i < _MapLayers[_SelectedLayer]._P_ObjectSnapPoints.Count; i++) {
                        for (int o = 0; o < _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_Pannel.Count; o++) {
                            _SnapDistanceCheck = Vector3.Distance(_MousePos, _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_Pannel[o].transform.position);
                            if (_SnapDistanceCheck < 1f) {
                                _SnapPosition = new Vector3(_MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_Pannel[o].transform.position.x, _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_Pannel[o].transform.position.y, _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_Pannel[o].transform.position.z);
                                check++;
                                if (creating) {
                                    _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_Pannel[o] = null;
                                    _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[_MapLayers[_SelectedLayer]._P_ObjectSnapPoints.Count]._P_Snap_Pannel[o] = null;
                                }
                            }
                        }
                    }

                    if (check > 0) {
                        _FoundSnapPoint = true;
                    }
                    else {
                        _FoundSnapPoint = false;
                    }
                        _ExampleObj.transform.position = new Vector3(_SnapPosition.x, _SnapPosition.y, _SnapPosition.z - 0.1f);

                    Handles.color = new Color(0.25f, 0.25f, 0.35f);
                    Handles.SphereHandleCap(0, _SnapPosition, Quaternion.identity, 0.5f, EventType.Repaint);
                }
                if (_ExampleObj.transform.name.Contains("Platform")) {
                    Vector2 check = new Vector2(0, 0);
                    if (_ExampleObj.transform.childCount == 0) {
                        check = new Vector2(0, 0);
                    }
                    for (int i = 0; i < _ExampleObj.transform.childCount; i++) {
                        if (_ExampleObj.transform.Find("SnapPoint_PlatformLeft"))
                            check.x = 1;
                        if (_ExampleObj.transform.Find("SnapPoint_PlatformRight"))
                            check.y = 1;
                    }
                    if (check.x == 1)
                        _Selected_SnapRight = true;
                    else
                        _Selected_SnapRight = false;
                    if (check.y == 1)
                        _Selected_SnapLeft = true;
                    else
                        _Selected_SnapLeft = false;
                    for (int i = 0; i < _MapLayers[_SelectedLayer]._P_ObjectSnapPoints.Count; i++) {
                        if (_Selected_SnapLeft && _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_PlatformLeft != null) {
                            _SnapDistanceCheck = Vector3.Distance(_SnapPosition, _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_PlatformLeft.transform.position);
                            if (_SnapDistanceCheck < _SnapToSnapPosDistance) {
                                _SnapPosition = new Vector3(_MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_PlatformLeft.transform.position.x - _ExampleObj.transform.Find("SnapPoint_PlatformRight").gameObject.transform.localPosition.x, _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_PlatformLeft.transform.position.y, _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_PlatformLeft.transform.position.z);
                                _FoundSnapPoint = true;
                                if (creating) {
                                    _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_PlatformLeft = null;
                                    _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[_MapLayers[_SelectedLayer]._P_ObjectSnapPoints.Count]._P_Snap_PlatformRight = null;
                                }
                            }
                            else {
                                _FoundSnapPoint = false;
                            }
                        }
                        if (_Selected_SnapRight && _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_PlatformRight != null) {
                            _SnapDistanceCheck = Vector3.Distance(_SnapPosition, _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_PlatformRight.transform.position);
                            if (_SnapDistanceCheck < _SnapToSnapPosDistance) {
                                _SnapPosition = new Vector3(_MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_PlatformRight.transform.position.x - _ExampleObj.transform.Find("SnapPoint_PlatformLeft").gameObject.transform.localPosition.x, _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_PlatformRight.transform.position.y, _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_PlatformRight.transform.position.z);
                                _FoundSnapPoint = true;
                                if (creating) {
                                    _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[i]._P_Snap_PlatformRight = null;
                                    _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[_MapLayers[_SelectedLayer]._P_ObjectSnapPoints.Count]._P_Snap_PlatformLeft = null;
                                }
                            }
                            else {
                                _FoundSnapPoint = false;
                            }
                        }
                    }
                    _ExampleObj.transform.position = new Vector3(_SnapPosition.x, _SnapPosition.y, _SnapPosition.z - 0.1f);

                    Handles.color = new Color(0.25f, 0.25f, 0.35f);
                    Handles.SphereHandleCap(0, _SnapPosition, Quaternion.identity, 0.5f, EventType.Repaint);
                }
            }
        }
    }

    //Check Object Existing/Snap points
    bool CheckObjectExist(GameObject createdObject)
    {
        for (int i = 0; i < _MapLayers[_SelectedLayer]._P_Objects.Count; i++) {
            if (_MapLayers[_SelectedLayer]._P_Objects[i] != null) {
                if (_MapLayers[_SelectedLayer]._P_Objects[i].transform.position == _SnapPosition) {
                    return true;
                }
            }
        }
        return false;
    }
    void CheckSnapPoints(GameObject createdObject)
    {
        _MapLayers[_SelectedLayer]._P_ObjectSnapPoints.Add(new MapEditorObjectSnapPoints());

        if (createdObject.transform.childCount > 0)
        {
            for (int i = 0; i < createdObject.transform.childCount; i++)
            {
                if (createdObject.transform.GetChild(i).name == "SnapPoint_Left")
                    _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[_MapLayers[_SelectedLayer]._P_ObjectSnapPoints.Count - 1]._P_Snap_Left = createdObject.transform.Find("SnapPoint_Left").gameObject;
                if (createdObject.transform.GetChild(i).name == "SnapPoint_PlatformLeft")
                    _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[_MapLayers[_SelectedLayer]._P_ObjectSnapPoints.Count - 1]._P_Snap_PlatformLeft = createdObject.transform.Find("SnapPoint_PlatformLeft").gameObject;
                if (createdObject.transform.GetChild(i).name == "SnapPoint_Right")
                    _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[_MapLayers[_SelectedLayer]._P_ObjectSnapPoints.Count - 1]._P_Snap_Right = createdObject.transform.Find("SnapPoint_Right").gameObject;
                if (createdObject.transform.GetChild(i).name == "SnapPoint_PlatformRight")
                    _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[_MapLayers[_SelectedLayer]._P_ObjectSnapPoints.Count - 1]._P_Snap_PlatformRight = createdObject.transform.Find("SnapPoint_PlatformRight").gameObject;
                if (createdObject.transform.GetChild(i).name == "SnapPoint_Pannel")
                    _MapLayers[_SelectedLayer]._P_ObjectSnapPoints[_MapLayers[_SelectedLayer]._P_ObjectSnapPoints.Count - 1]._P_Snap_Pannel.Add(createdObject.transform.GetChild(i).gameObject);
            }
        }
    }

    //Layers
    void ShowLayers() {
        _ScrollPos2 = GUILayout.BeginScrollView(_ScrollPos2, GUILayout.Width(position.width - 20), GUILayout.Height(position.height - position.height + 185));
        for (int i = 0; i < _MapLayers.Count; i++) {
            GUILayout.BeginHorizontal("Box");
            GUILayout.Label("Layer " + i.ToString());

            if (_SelectedLayer == i) { GUI.backgroundColor = new Color(0, 1, 0); } else { GUI.backgroundColor = _DefaultColor; }
            if (GUILayout.Button("Selected")) _SelectedLayer = i;GUI.backgroundColor = _DefaultColor;

            GUILayout.Label("Obj: (" +  _MapLayers[i]._P_Objects.Count.ToString() + ")");
        
            if (GUILayout.Button("Add")) AddObjectsToLayer(i);
            if (GUILayout.Button("Clear")) _MapLayers[i]._P_Objects.Clear();
            if (GUILayout.Button("Select")) Selection.objects = _MapLayers[i]._P_Objects.ToArray();

            if (_MapLayers[i]._P_ShowObjects) { GUI.backgroundColor = new Color(0, 1, 0); } else { GUI.backgroundColor = _DefaultColor; }
            if (GUILayout.Button("Show")) _MapLayers[i]._P_ShowObjects = !_MapLayers[i]._P_ShowObjects;
            if (_MapLayers[i]._P_Visable) { GUI.backgroundColor = new Color(0, 1, 0); } else { GUI.backgroundColor = _DefaultColor; }
            if (GUILayout.Button("O")) {
                _MapLayers[i]._P_Visable = !_MapLayers[i]._P_Visable;
                Visable(i, _MapLayers[i]._P_Visable);
            }
            GUI.backgroundColor = _DefaultColor;

            //Show objects in layer
            GUI.backgroundColor = _DefaultColor;
            if (_MapLayers[i]._P_ShowObjects) {
                GUILayout.EndHorizontal();
                GUILayout.BeginVertical("Box");
                GUILayout.BeginHorizontal();
                    if (GUILayout.Button("(High/Low)")) {
                        ReordelList(i, false);          
                    }
                    if (GUILayout.Button("(Low/High)")) {
                        ReordelList(i, true);
                    }
                GUILayout.EndHorizontal();

                for (int o = 0; o < _MapLayers[i]._P_Objects.Count; o++) {
                    GUILayout.BeginHorizontal();
                    _MapLayers[i]._P_Objects[o] = (GameObject)EditorGUILayout.ObjectField(_MapLayers[i]._P_Objects[o], typeof(GameObject), true);
                    _MapLayers[i]._P_OrderInLayer[o] = EditorGUILayout.IntField("Order in Layer", _MapLayers[i]._P_OrderInLayer[o]);
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
                GUILayout.BeginHorizontal();
            }
            GUILayout.EndHorizontal();
        }
        for (int i = 0; i < _MapLayers.Count; i++) {
            if(_MapLayers[i]._P_ShowObjects)
            GUILayout.Space(_MapLayers[i]._P_Objects.Count);
        }
        GUILayout.EndScrollView();
    }

    //Add to layer
    void AddObjectsToLayer(int layerID) {
        for (int i = 0; i < _SelectedObjects.Length; i++) {
            if (!_MapLayers[layerID]._P_Objects.Contains(_SelectedObjects[i])) {
                _MapLayers[layerID]._P_OrderInLayer.Add(0);
                _MapLayers[layerID]._P_Objects.Add(_SelectedObjects[i]);
                CheckSnapPoints(_SelectedObjects[i]);
            }
        }
        CheckLayerOrderInLayer(layerID);
    }
    void AddObjectToLayer(GameObject createdObject) {
        if (CheckObjectExist(createdObject) && createdObject.name.Contains("Pannel")) {
            for (int i = 0; i < _MapLayers[_SelectedLayer]._P_Objects.Count; i++) {
                if (_MapLayers[_SelectedLayer]._P_Objects[i].transform.position == _SnapPosition) {
                    DestroyImmediate(_MapLayers[_SelectedLayer]._P_Objects[i].transform.gameObject);
                    _MapLayers[_SelectedLayer]._P_Objects[i] = createdObject;
                }
            }
        }
        else {
            _MapLayers[_SelectedLayer]._P_OrderInLayer.Add(0);
            _MapLayers[_SelectedLayer]._P_Objects.Add(createdObject);
            CheckSnapPoints(createdObject);
        }
    }

    //ListOrder Options
    void Visable(int layerID, bool state) 
    {
        for (int i = 0; i < _MapLayers[layerID]._P_Objects.Count; i++) {
            _MapLayers[layerID]._P_Objects[i].SetActive(state);
        }
    }
    void CheckLayerOrderInLayer(int layerID) {
        for (int i = 0; i < _MapLayers[layerID]._P_Objects.Count; i++) {
            if(_MapLayers[layerID]._P_Objects[i].GetComponent<SpriteRenderer>() != null) {
                _MapLayers[layerID]._P_OrderInLayer[i] = _MapLayers[layerID]._P_Objects[i].GetComponent<SpriteRenderer>().sortingOrder;
            }
        }
    }
    void ReordelList(int layerID, bool sortingID) {
        List<int> newListOrderInLayer = new List<int>(0);
        List<GameObject> newListObject = new List<GameObject>(0);
        if (sortingID) {
            for (int i = -200; i < 200; i++) {
                for (int o = 0; o < _MapLayers[layerID]._P_Objects.Count; o++) {
                    if (_MapLayers[layerID]._P_OrderInLayer[o] == i) {
                        newListObject.Add(_MapLayers[layerID]._P_Objects[o]);
                        newListOrderInLayer.Add(_MapLayers[layerID]._P_OrderInLayer[o]);
                    }
                }
            }
        }
        else 
        {
            for (int i = 200; i > -200; i--) {
                for (int o = 0; o < _MapLayers[layerID]._P_Objects.Count; o++) {
                    if (_MapLayers[layerID]._P_OrderInLayer[o] == i) {
                        newListObject.Add(_MapLayers[layerID]._P_Objects[o]);
                        newListOrderInLayer.Add(_MapLayers[layerID]._P_OrderInLayer[o]);
                    }
                }
            }
        }
        _MapLayers[layerID]._P_Objects = newListObject;
        _MapLayers[layerID]._P_OrderInLayer = newListOrderInLayer;
    }


    void Update() {
        _SelectedObjects = Selection.gameObjects;
    }

    void Fix() {
        CleanUpSnapPoints();
        this.Close();
    }

    void CleanUpSnapPoints() {
        List<GameObject> snapObjects = new List<GameObject>();

        object[] obj = GameObject.FindObjectsOfType(typeof(GameObject));
        foreach (object o in obj) {
            GameObject g = (GameObject)o;
            if (g.name.Contains("SnapPoint")) {
                snapObjects.Add(g);
            }
        }

        for (int i = snapObjects.Count - 1; i >= 0; i--) {
            DestroyImmediate(snapObjects[i]);
        }
    }

    void Clear() {
        _MapLayers = new List<MapEditorLayers>();
        _MapLayers.Add(new MapEditorLayers());

        for (int i = _ParentObject.transform.childCount -1; i >= 0; i--) {
            DestroyImmediate(_ParentObject.transform.GetChild(i).gameObject);
        }
    }
}

public class MapEditorLayers
{
    public bool _P_Visable = true;
    public bool _P_ShowObjects;
    public LayerMask _P_SortingLayer;
    public List<int> _P_OrderInLayer = new List<int>();
    public List<GameObject> _P_Objects = new List<GameObject>();
    public List<MapEditorObjectSnapPoints> _P_ObjectSnapPoints = new List<MapEditorObjectSnapPoints>();
}

public class MapEditorObjectSnapPoints
{
    public GameObject _P_Snap_Left;
    public GameObject _P_Snap_PlatformLeft;
    public GameObject _P_Snap_Right;
    public GameObject _P_Snap_PlatformRight;
    public List<GameObject> _P_Snap_Pannel = new List<GameObject>();
}