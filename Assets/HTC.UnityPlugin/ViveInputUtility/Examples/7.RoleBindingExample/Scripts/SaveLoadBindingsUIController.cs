using HTC.UnityPlugin.Vive;
using HTC.UnityPlugin.VRModuleManagement;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SaveLoadBindingsUIController : MonoBehaviour
{
    public InputField inputFilePath;
    public Text textConsole;
    public string filePathConfig = "vive_role_bindings_cfg_path.cfg";
    public bool prettyPrint = true;
    public bool autoLoadBindings = true;
    public UnityEvent OnBindAllComplete = new UnityEvent();
    public UnityEvent OnUnbindAllComplete = new UnityEvent();
    public UnityEvent OnSaveComplete = new UnityEvent();
    public UnityEvent OnLoadComplete = new UnityEvent();

    public void Awake()
    {
        if (!LoadConfigPath() || string.IsNullOrEmpty(inputFilePath.text))
        {
            inputFilePath.text = ViveRoleBindingsHelper.AUTO_LOAD_CONFIG_PATH;
        }

        if (!string.IsNullOrEmpty(inputFilePath.text) && File.Exists(inputFilePath.text))
        {
            ViveRoleBindingsHelper.LoadBindingConfigFromFile(inputFilePath.text);

            autoLoadBindings = ViveRoleBindingsHelper.bindingConfig.apply_bindings_on_load;

            if (autoLoadBindings)
            {
                ViveRoleBindingsHelper.ApplyBindingConfigToRoleMap();
            }
        }
    }

    private void SaveConfigPath()
    {
        if (string.IsNullOrEmpty(filePathConfig)) { return; }

        var pathConfigDir = Path.GetDirectoryName(filePathConfig);
        if (!string.IsNullOrEmpty(pathConfigDir) && !Directory.Exists(pathConfigDir))
        {
            Directory.CreateDirectory(pathConfigDir);
        }

        using (var outputFile = new StreamWriter(filePathConfig))
        {
            outputFile.Write(inputFilePath.text);
        }
    }

    private bool LoadConfigPath()
    {
        if (!string.IsNullOrEmpty(filePathConfig) && File.Exists(filePathConfig))
        {
            using (var inputFile = new StreamReader(filePathConfig))
            {
                inputFilePath.text = inputFile.ReadLine();
            }

            return true;
        }

        return false;
    }

    public void BindAll()
    {
        ViveRoleBindingsHelper.BindAllCurrentDeviceClassMappings(VRModuleDeviceClass.Controller);
        ViveRoleBindingsHelper.BindAllCurrentDeviceClassMappings(VRModuleDeviceClass.GenericTracker);

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
        ViveRoleBindingsHelper.LoadBindingConfigFromRoleMap();

        ViveRoleBindingsHelper.bindingConfig.apply_bindings_on_load = autoLoadBindings;

        var configDir = Path.GetDirectoryName(inputFilePath.text);
        if (!string.IsNullOrEmpty(configDir) && !Directory.Exists(configDir))
        {
            Directory.CreateDirectory(configDir);
        }

        ViveRoleBindingsHelper.SaveBindingConfigToFile(inputFilePath.text, prettyPrint);

        SaveConfigPath();

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

        ViveRoleBindingsHelper.LoadBindingConfigFromFile(inputFilePath.text);
        ViveRoleBindingsHelper.ApplyBindingConfigToRoleMap();

        autoLoadBindings = ViveRoleBindingsHelper.bindingConfig.apply_bindings_on_load;

        SaveConfigPath();

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
