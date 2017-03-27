using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Valve.VR;

public class SaveLoadBindingsUIController : MonoBehaviour
{
    public InputField inputFilePath;
    public Text textConsole;
    public string filePathConfig = "./vive_role_bindings_cfg_path.cfg";
    public bool prettyPrint = true;
    public bool autoLoadBindings = true;
    public UnityEvent OnBindAllComplete = new UnityEvent();
    public UnityEvent OnUnbindAllComplete = new UnityEvent();
    public UnityEvent OnSaveComplete = new UnityEvent();
    public UnityEvent OnLoadComplete = new UnityEvent();

    public void Awake()
    {
        LoadConfig();

        if (autoLoadBindings)
        {
            LoadBindings();
        }
    }

    public void SaveConfig()
    {
        if (!Directory.Exists(Path.GetDirectoryName(filePathConfig)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePathConfig));
        }

        using (var outputFile = new StreamWriter(filePathConfig))
        {
            outputFile.Write(inputFilePath.text);
        }
    }

    public void LoadConfig()
    {
        if (File.Exists(filePathConfig))
        {
            using (var inputFile = new StreamReader(filePathConfig))
            {
                inputFilePath.text = inputFile.ReadLine();
            }
        }
    }

    public void BindAll()
    {
        ViveRoleBindingsHelper.BindAllCurrentDeviceClassMappings(ETrackedDeviceClass.Controller);
        ViveRoleBindingsHelper.BindAllCurrentDeviceClassMappings(ETrackedDeviceClass.GenericTracker);

        if (OnBindAllComplete != null)
        {
            OnBindAllComplete.Invoke();
        }
    }

    public void UnbindAll()
    {
        ViveRoleBindingsHelper.UnbindAllCurrentBindings();

        if (OnUnbindAllComplete != null)
        {
            OnUnbindAllComplete.Invoke();
        }
    }

    public void SaveBindings()
    {
        if (!Directory.Exists(Path.GetDirectoryName(inputFilePath.text)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(inputFilePath.text));
        }

        ViveRoleBindingsHelper.SaveRoleBindings(inputFilePath.text, prettyPrint);

        SaveConfig();

        textConsole.text = "Bindings Save Complete...";

        if (OnSaveComplete != null)
        {
            OnSaveComplete.Invoke();
        }
    }

    public void LoadBindings()
    {
        if (!File.Exists(inputFilePath.text))
        {
            Debug.LogWarning("LoadBindings config " + inputFilePath.text + " not found");
            textConsole.text = "Bindings Load Fail! config file not found...";
            return;
        }

        ViveRoleBindingsHelper.LoadRoleBindings(inputFilePath.text);

        SaveConfig();

        textConsole.text = "Bindings Load Complete...";

        if (OnLoadComplete != null)
        {
            OnLoadComplete.Invoke();
        }
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(20f, 20f, 400, 150));
        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Bindings Config Path:", GUILayout.ExpandWidth(false));
        var input = GUILayout.TextField(inputFilePath.text, GUILayout.ExpandWidth(true));
        if (GUI.changed)
        {
            inputFilePath.text = input;
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Bind All")) { BindAll(); }
        if (GUILayout.Button("Unbind All")) { UnbindAll(); }
        if (GUILayout.Button("Save")) { SaveBindings(); }
        if (GUILayout.Button("Load")) { LoadBindings(); }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
