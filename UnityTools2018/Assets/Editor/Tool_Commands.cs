using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEngine.Events;

public class Tool_Commands : EditorWindow
{
    #region Variables
    //Commands
    private List<string> _ConsoleCommands = new List<string>();
    private List<string> _ConsoleCommandsResults = new List<string>();
    private Tool_CustomCommands _Commands = new Tool_CustomCommands(); 

    //Search
    private string _Search = "";
    private string _InfoText = "testing";
    private int _ResultID = 0;
    private bool _Loaded = false;

    //Menu
    private string[] _MenuOptions = new string[] {"Commands", "Create Command", "Menu", "Import/Export" };
    private int _MenuSelected = 0;

    private bool _EditMode;
    private int _CommandID;
    #endregion

    //Create Command
    private string[] _NewCommandTypes = new string[]        { "Search","Load","Create","Other" };
    private string[] _NewCommandType_Search = new string[]  { "Scene","Folder","Objects","Tag" };
    private string[] _NewCommandType_Load = new string[]    { "Scene" };
    private string[] _NewCommandType_Create = new string[]  { "Scene","Object" };
    private string[] _NewCommandType_Other = new string[]   { "Info" };
    private int _NewCommandTypesChosen = 0;
    private int _NewCommandTypeOptionChosen = 0;
    private string _NewCommandText = "";
    private int _Type;

    [MenuItem("Tools/Commands %g")]
    static void Init()
    {
        Tool_Commands window = EditorWindow.GetWindow(typeof(Tool_Commands), false, "Commands") as Tool_Commands;
        window.minSize = new Vector2(550, 750);
        window.maxSize = new Vector2(550, 750);
        window.Show();
        window.minSize = new Vector2(50, 50);
        window.maxSize = new Vector2(9999999, 9999999);
    }

    //Window/Menu
    void OnGUI()
    {   
        if (!_Loaded)
        {
            JsonLoad();
            _Loaded = true;
        }
        Menu();
        InfoBox();
    }
    private void Menu()
    {
        HandleInput();
        _MenuSelected = GUILayout.Toolbar(_MenuSelected, _MenuOptions);
        switch (_MenuSelected)
        {
            case 0:
                Search();
                ShowConsoleCommands();
                break;
            case 1:
                CreateCommands();
                break;
            case 2:
                MenuVieuw();
                break;
        }
    }

    //Input/Search
    private void HandleInput()
    {
        Event e = Event.current;
        switch (e.type)
        {
            case EventType.KeyDown:
                {
                    if (Event.current.keyCode == (KeyCode.Return) || Event.current.keyCode == (KeyCode.KeypadEnter))
                    {
                        DoCommand();
                        GUI.FocusControl("SearchField");
                        GUIUtility.ExitGUI();
                    }
                    if (Event.current.keyCode == (KeyCode.DownArrow))
                    {
                        if (_ResultID == _ConsoleCommandsResults.Count - 1)
                            _ResultID = 0;
                        else
                            _ResultID++;
                    }
                    if (Event.current.keyCode == (KeyCode.UpArrow))
                    {
                        if (_ResultID == 0)
                            _ResultID = _ConsoleCommandsResults.Count - 1;
                        else
                            _ResultID--;
                    }
                }
                break;
        }
        if (Event.current.keyCode == (KeyCode.Escape))
        {
            this.Close();
        }
        Repaint();
    }
    private void Search()
    {
        //Input
        GUILayout.Label("Search: ");
        GUI.FocusControl("SearchField");
        GUI.SetNextControlName("SearchField");
        _Search = GUI.TextField(new Rect(5, 40, 500, 20), _Search.ToLower());
        GUILayout.Space(20);
    }

    //Search/Show Commands
    private void ShowConsoleCommands()
    {
        _ConsoleCommandsResults.Clear();
        GUILayout.BeginVertical("Box");
        int resultamount = 0;
        string resultcommand = "";
        for (int i = 0; i < _ConsoleCommands.Count; i++)
        {
            if (_ConsoleCommands[i].Contains(_Search))
            {
                _ConsoleCommandsResults.Add(_ConsoleCommands[i]);
                resultamount++;
                resultcommand = _ConsoleCommands[i];
            }
        }

        if (resultamount > 1)
        {
            for (int i = 0; i < _ConsoleCommandsResults.Count; i++)
            {
                GUILayout.BeginHorizontal();
                if (_ConsoleCommandsResults[i] == _Search)
                {
                    _ResultID = 0;
                    GUILayout.Label(">" + _ConsoleCommandsResults[i]);
                }
                else
                {
                    if (i == _ResultID)
                    {
                        GUILayout.Label(">" + _ConsoleCommandsResults[i]);
                    }
                    else
                    {
                        GUILayout.Label(_ConsoleCommandsResults[i]);
                    }
                }
                //GUILayout.Label(_ConsoleCommandsInfoResults[i]);
                GUILayout.EndHorizontal();
            }
        }
        else
        {
            GUILayout.Label(">" + resultcommand);
        }
        if (_ResultID < 0)
            _ResultID = 0;
        if (_ResultID >= _ConsoleCommandsResults.Count - 1)
            _ResultID = _ConsoleCommandsResults.Count - 1;
        GUILayout.EndVertical();
    }
    private void DoCommand()
    {
        //GetCommand
        if (_ResultID < _ConsoleCommands.Count)
        {
            if (_NewCommandTypes[_Commands._ToolCommands[_ResultID]._Type] == "Search")
                DoCommand_Search();
            if (_NewCommandTypes[_Commands._ToolCommands[_ResultID]._Type] == "Load")
                DoCommand_Load();
            if (_NewCommandTypes[_Commands._ToolCommands[_ResultID]._Type] == "Create")
                DoCommand_Create();
            if (_NewCommandTypes[_Commands._ToolCommands[_ResultID]._Type] == "Other")
                DoCommand_Create();
        }
    }
    private void DoCommand_Search()
    {
        if(_Commands._ToolCommands[_ResultID]._CommandData[0] == "Objects")
        {
            FindObjects(_Commands._ToolCommands[_ResultID]._CommandData[1], false);
        }
        if (_Commands._ToolCommands[_ResultID]._CommandData[0] == "Folder")
        {
            string relativePath = _Commands._ToolCommands[_ResultID]._CommandData[1].Substring(_Commands._ToolCommands[_ResultID]._CommandData[1].IndexOf("Assets/"));
            Selection.activeObject = AssetDatabase.LoadAssetAtPath(relativePath, typeof(Object));
        }
        if (_Commands._ToolCommands[_ResultID]._CommandData[0] == "Tag")
        {
            FindObjects(_Commands._ToolCommands[_ResultID]._CommandData[1], true);
        }
    }
    private void DoCommand_Load()
    {
        if (_Commands._ToolCommands[_ResultID]._CommandData[0] == "Scene")
        {
           EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
           EditorSceneManager.OpenScene(_Commands._ToolCommands[_ResultID]._CommandData[1]);
        }
    }
    private void DoCommand_Create()
    {

    }
    private void DoCommand_Other()
    {
        if (_Commands._ToolCommands[_ResultID]._CommandData[0] == "Info")
        {
            _InfoText = _Commands._ToolCommands[_ResultID]._CommandData[1];
        }
    }

    //Info Box
    private void InfoBox()
    {
        if (_InfoText != "")
        {
            GUILayout.BeginVertical("Box");
            GUILayout.Label(_InfoText);
            GUILayout.EndVertical();
        }
    }

    //Create Commands
    private void CreateCommands()
    {
        GUILayout.BeginVertical("box");
        _NewCommandText = EditorGUILayout.TextField("New Command: ",_NewCommandText);
        //GetType
        _NewCommandTypesChosen = GUILayout.Toolbar(_NewCommandTypesChosen, _NewCommandTypes);
        switch(_NewCommandTypesChosen)
        {
            case 0: //Seach
                _NewCommandTypeOptionChosen = GUILayout.Toolbar(_NewCommandTypeOptionChosen, _NewCommandType_Search);
                if(_NewCommandTypeOptionChosen >= _NewCommandType_Search.Length)
                    _NewCommandTypeOptionChosen--;
                break;
            case 1: //Load
                _NewCommandTypeOptionChosen = GUILayout.Toolbar(_NewCommandTypeOptionChosen, _NewCommandType_Load);
                if (_NewCommandTypeOptionChosen >= _NewCommandType_Load.Length)
                    _NewCommandTypeOptionChosen--;
                break;
            case 2: //Create
                _NewCommandTypeOptionChosen = GUILayout.Toolbar(_NewCommandTypeOptionChosen, _NewCommandType_Create);
                if (_NewCommandTypeOptionChosen >= _NewCommandType_Create.Length)
                    _NewCommandTypeOptionChosen--;
                break;
            case 3: //Other
                _NewCommandTypeOptionChosen = GUILayout.Toolbar(_NewCommandTypeOptionChosen, _NewCommandType_Other);
                if (_NewCommandTypeOptionChosen >= _NewCommandType_Other.Length)
                    _NewCommandTypeOptionChosen--;
                break;
        }

        if (GUILayout.Button("AddCommand"))
        {
            //Create New Command
            Tool_Command newcommand = new Tool_Command();
            newcommand._TypeOption = _NewCommandTypeOptionChosen;

            switch (_NewCommandTypesChosen)
            {
                case 0: //Seach
                    newcommand._CommandData.Add(_NewCommandType_Search[_NewCommandTypeOptionChosen]);
                    newcommand._CommandData.Add("Object Name");
                    break;
                case 1: //Load
                    newcommand._CommandData.Add(_NewCommandType_Load[_NewCommandTypeOptionChosen]);
                    newcommand._CommandData.Add("");
                    break;
                case 2: //Create
                    newcommand._CommandData.Add(_NewCommandType_Create[_NewCommandTypeOptionChosen]);
                    break;
                case 3: //Info
                    newcommand._CommandData.Add(_NewCommandType_Other[_NewCommandTypeOptionChosen]);
                    newcommand._CommandData.Add("");
                    break;
            }

            newcommand._CommandName = _NewCommandText;
            newcommand._Type = _NewCommandTypesChosen;
            _Commands._ToolCommands.Add(newcommand);

            _NewCommandText = "";
            JsonUpdate();

            //Edit Command
            _EditMode = true;
            _CommandID = _Commands._ToolCommands.Count -1;
            EditCommand();
        }
        GUILayout.EndVertical();

        if (_EditMode)
            EditCommand();

        CommandEditList();
    }
    private void EditCommand()
    {
        GUILayout.BeginVertical("box");
        GUILayout.Label("Editing Command: " + _Commands._ToolCommands[_CommandID]._CommandName, EditorStyles.boldLabel);
        GUILayout.BeginVertical();
        if(_NewCommandTypes[_Commands._ToolCommands[_CommandID]._Type] == "Search")
            Edit_Search();
        if (_NewCommandTypes[_Commands._ToolCommands[_CommandID]._Type] == "Load")
            Edit_Load();
        if (_NewCommandTypes[_Commands._ToolCommands[_CommandID]._Type] == "Create")
            Edit_Create();
        if (_NewCommandTypes[_Commands._ToolCommands[_CommandID]._Type] == "Other")
            Edit_Other();
        if (GUILayout.Button("Save"))
        {
            JsonUpdate();
            _EditMode = false;
        }
        GUILayout.EndVertical();
        GUILayout.EndVertical();
    }
    private void CommandEditList()
    {
        GUILayout.BeginVertical("box");
        GUILayout.BeginHorizontal("box");

        GUILayout.BeginHorizontal("box");
        GUILayout.Label("Command Name");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal("box");
        GUILayout.Label("Edit");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal("box");
        GUILayout.Label("Delete");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal("box");
        GUILayout.Label("Add to menu");
        GUILayout.EndHorizontal();

        GUILayout.EndHorizontal();

        for (int i = 0; i < _Commands._ToolCommands.Count; i++)
        {
            GUILayout.BeginHorizontal("box");
            GUILayout.Label(_Commands._ToolCommands[i]._CommandName);
            GUILayout.Label("Type: " + _NewCommandTypes[_Commands._ToolCommands[i]._Type] + " " + _Commands._ToolCommands[i]._CommandData[0]);
            if (GUILayout.Button("Edit"))
            {
                _CommandID = i;
                _EditMode = true;
            }
            if (GUILayout.Button("Delete"))
            {
                _Commands._ToolCommands.Remove(_Commands._ToolCommands[i]);
                JsonUpdate();
                break;
            }
            if (_Commands._ToolCommands[i]._ToMenu)
            {
                if (GUILayout.Button("+", GUILayout.Width(20)))
                {
                    _Commands._ToolCommands[i]._ToMenu = !_Commands._ToolCommands[i]._ToMenu;
                    JsonUpdate();
                }
            }
            else
            {
                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    _Commands._ToolCommands[i]._ToMenu = !_Commands._ToolCommands[i]._ToMenu;
                    JsonUpdate();
                }
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();
    }

    private void Edit_Search()
    {
        if (_NewCommandType_Search[_Commands._ToolCommands[_CommandID]._TypeOption] == "Objects")
        {
            _Commands._ToolCommands[_CommandID]._CommandData[1] = EditorGUILayout.TextField("ObjectName: ", _Commands._ToolCommands[_CommandID]._CommandData[1]);
        }
        if (_NewCommandType_Search[_Commands._ToolCommands[_CommandID]._TypeOption] == "Folder")
        {
            if (GUILayout.Button("Get Path"))
                _Commands._ToolCommands[_CommandID]._CommandData[1] = EditorUtility.OpenFilePanel("Folder Path: ", _Commands._ToolCommands[_CommandID]._CommandData[1], "");
        }
        if (_NewCommandType_Search[_Commands._ToolCommands[_CommandID]._TypeOption] == "Tag")
        {
            _Commands._ToolCommands[_CommandID]._CommandData[1] = EditorGUILayout.TextField("Tag: ", _Commands._ToolCommands[_CommandID]._CommandData[1]);
        }
        if (_NewCommandType_Search[_Commands._ToolCommands[_CommandID]._TypeOption] == "Scene")
        {
            if (GUILayout.Button("Get Path"))
                _Commands._ToolCommands[_CommandID]._CommandData[1] = EditorUtility.OpenFilePanel("Scene Path: ", _Commands._ToolCommands[_CommandID]._CommandData[1], "Unity");
        }
    }
    private void Edit_Load()
    {
        if (_NewCommandType_Search[_Commands._ToolCommands[_CommandID]._TypeOption] == "Scene")
        {
            if (GUILayout.Button("Get Path"))
                _Commands._ToolCommands[_CommandID]._CommandData[1] = EditorUtility.OpenFilePanel("Scene Path: ", _Commands._ToolCommands[_CommandID]._CommandData[1], "");
        }
    }
    private void Edit_Create()
    {

    }
    private void Edit_Other()
    {
        if (_NewCommandType_Other[_Commands._ToolCommands[_CommandID]._TypeOption] == "Info")
        {
            _Commands._ToolCommands[_CommandID]._CommandData[1] = EditorGUILayout.TextField("Info: ", _Commands._ToolCommands[_CommandID]._CommandData[1]);
        }
    }

    //Menu
    private void MenuVieuw()
    {
        GUILayout.BeginVertical("box");
        if (_Commands._ToolCommands.Count > 0)
        {
            for (int i = 0; i < _Commands._ToolCommands.Count; i++)
            {
                if (_Commands._ToolCommands[i]._ToMenu)
                {
                    if (GUILayout.Button(_Commands._ToolCommands[i]._CommandName))
                    {
                        _ResultID = i;
                        DoCommand();
                    }
                }
            }
        }
        else
            GUILayout.Label("Add commands to menu");
        GUILayout.EndVertical();
    }

    //Save/Load
    private void JsonLoad()
    {
        try
        {
            _Commands._ToolCommands.Clear();
            _ConsoleCommands.Clear();
            string dataPath = "Assets/Commands.CommandData";
            string dataAsJson = File.ReadAllText(dataPath);
            _Commands = JsonUtility.FromJson<Tool_CustomCommands>(dataAsJson);

            for (int i = 0; i < _Commands._ToolCommands.Count; i++)
            {
                _ConsoleCommands.Add(_Commands._ToolCommands[i]._CommandName);
            }
        }
        catch
        {
            JsonSave();
            JsonLoad();
        }
    }
    private void JsonSave()
    {
        string json = JsonUtility.ToJson(_Commands);
        File.WriteAllText("Assets/Commands.CommandData", json.ToString());
    }
    private void JsonUpdate()
    {
        JsonSave();
        JsonLoad();
    }

    //Find Objects
    private void FindObjects(string searchtext, bool usetag)
    {
        if (!usetag)
        {
            Selection.activeGameObject = GameObject.Find(searchtext);
        }
        else
        {
            GameObject[] obj = GameObject.FindGameObjectsWithTag(searchtext);
            Selection.objects = obj;
        }

    }
}

[System.Serializable]
public class Tool_CustomCommands
{
    public List<Tool_Command> _ToolCommands = new List<Tool_Command>();
}

[System.Serializable]
public class Tool_Command
{
    public string _CommandName = "";
    public int _Type = 0;
    public int _TypeOption = 0;
    public string _CommandInfo = "";
    public List<string> _CommandData = new List<string>();
    public bool _ToMenu = false;
}