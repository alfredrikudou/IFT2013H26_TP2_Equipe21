using System;
using System.Collections.Generic;
using System.Linq;
using Controls;
using Controls.InputBinding;
using Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UIElements;

public class KeybindMenuEvents : MonoBehaviour
{
    [SerializeField] private GameController gameController;
    private Dictionary<string, Dictionary<string, List<string>>> _binds = new Dictionary<string, Dictionary<string, List<string>>>();
    private Dictionary<string, List<string>> _devices = new Dictionary<string, List<string>>();
    private UIDocument _document;
    private VisualElement _container;
    private TabView _playerTabView;

    private void Awake()
    {
        _document = GetComponent<UIDocument>();
        _container = _document.rootVisualElement.Query("Container");
        _playerTabView = _document.rootVisualElement.Q<TabView>("PlayerTabView");
        SetMenuVisible(false);
    }

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            bool isVisible = _document.rootVisualElement.style.display == DisplayStyle.Flex;
            if (!isVisible) BuildUI();
            SetMenuVisible(!isVisible);
        }
    }

    private void BuildUI()
    {
        var players = gameController.GetPlayersProfiles();
        _binds.Clear();
        foreach (var player in players)
        {
            _binds[player.Name] = new Dictionary<string, List<string>>();
            _devices[player.Name] = player.Devices.ToList();
            foreach (string entry in player.BindMap.Split(';'))
            {
                string[] parts = entry.Split(':');
                _binds[player.Name][parts[0]] = parts[1].Split(',').Where(b => !string.IsNullOrWhiteSpace(b)).ToList();
            }
        }

        _playerTabView.Clear();
        foreach (var bind in _binds)
            _playerTabView.Add(CreateTab(bind.Key));
    }

    private void RefreshUI()
    {
        string activeTab = _playerTabView.activeTab?.label;

        _playerTabView.Clear();
        foreach (var bind in _binds)
            _playerTabView.Add(CreateTab(bind.Key));

        if (activeTab != null)
            foreach (Tab tab in _playerTabView.Children().OfType<Tab>())
                if (tab.label == activeTab)
                {
                    _playerTabView.activeTab = tab;
                    break;
                }
    }

    private Tab CreateTab(string playerName)
    {
        var tab = new Tab(playerName);

        tab.Add(CreateDevice(playerName, _devices[playerName]));

        var list = new ScrollView();
        list.name = $"keybind-list-{playerName}";
        foreach (var entry in _binds[playerName])
            list.Add(CreateRow(playerName, entry.Key, entry.Value));

        var saveButton = new Button(() => OnSaveClicked(playerName));
        saveButton.text = "Save";
        saveButton.AddToClassList("save-button");
        
        var exportButton = new Button(() => OnExportClicked(playerName));
        exportButton.text = "Export";
        exportButton.AddToClassList("export-button");

        var importButton = new Button(() => OnImportClicked(playerName));
        importButton.text = "Import";
        importButton.AddToClassList("import-button");

        tab.Add(list);
        tab.Add(saveButton);
        tab.Add(exportButton);
        tab.Add(importButton);

        return tab;
    }
    private void OnExportClicked(string playerName)
    {
        OnSaveClicked(playerName);

        string serialized = string.Join(";", _binds[playerName].Select(kvp =>
            $"{kvp.Key}:{string.Join(",", kvp.Value)}"
        ));
        string path = System.IO.Path.Combine(Application.persistentDataPath, "settings.json");
        System.IO.File.WriteAllText(path, serialized);
        Debug.Log($"Settings exported to {path}");
    }

    private void OnImportClicked(string playerName)
    {
        string path = System.IO.Path.Combine(Application.persistentDataPath, "settings.json");
        if (!System.IO.File.Exists(path))
        {
            Debug.LogWarning($"No settings file found at {path}");
            return;
        }

        string serialized = System.IO.File.ReadAllText(path);
        var newBinds = new Dictionary<string, List<string>>();
        foreach (string entry in serialized.Split(';'))
        {
            string[] parts = entry.Split(':');
            newBinds[parts[0]] = parts[1].Split(',').Where(b => !string.IsNullOrWhiteSpace(b)).ToList();
        }

        _binds[playerName] = newBinds;
        OnSaveClicked(playerName);
        RefreshUI();
    }

    private VisualElement CreateDevice(string playerName, List<string> devices)
    {
        var row = new VisualElement();
        row.AddToClassList("device-container");

        foreach (var device in devices.ToList())
        {
            var button = new Button();
            button.text = device;
            button.AddToClassList("device-button");
            button.clicked += () => OnDeviceClicked(playerName, device);
            row.Add(button);
        }

        var addButton = new Button(() => OnAddDeviceClicked(playerName));
        addButton.text = "Add Device";
        row.Add(addButton);

        return row;
    }

    public void OnDeviceClicked(string playerName, string device)
    {
        _devices[playerName].Remove(device);
        RefreshUI();
    }

    private void OnAddDeviceClicked(string playerName)
    {
        ListenForControl((controlName, device) =>
        {
            if (!DeviceManager.Instance.IsDeviceSupported(device))
            {
                Debug.Log("Device is not supported");
                return;
            }
            if (IsDeviceBound(playerName, device))
            {
                Debug.Log("Device is already bound to this user");
                return;
            }
            _devices[playerName].Add(device.displayName + ":" + device.deviceId);
            RefreshUI();
        });
    }

    private VisualElement CreateRow(string playerName, string action, List<string> binds)
    {
        if (PlayerSettings.IsFloatSetting(Enum.Parse<PlayerSettings.PlayerSetting>(action)))
            return CreateNumericalRow(playerName, action, binds);

        var row = new VisualElement();
        row.AddToClassList("keybind-row");

        var actionLabel = new Label(action);
        actionLabel.AddToClassList("action-label");
        row.Add(actionLabel);

        foreach (var bind in binds.ToList())
        {
            if (string.IsNullOrWhiteSpace(bind)) continue;
            var button = new Button();
            button.text = bind;
            button.AddToClassList("key-button");
            button.clicked += () => OnBindClicked(playerName, action, bind);
            row.Add(button);
        }

        var addButton = new Button(() => OnAddBindClicked(playerName, action));
        addButton.text = "Add";
        row.Add(addButton);

        return row;
    }

    private VisualElement CreateNumericalRow(string playerName, string action, List<string> binds)
    {
        var row = new VisualElement();
        row.AddToClassList("keybind-row");

        var actionLabel = new Label(action);
        actionLabel.AddToClassList("action-label");
        row.Add(actionLabel);

        bool isInverted = action.Contains("Inverted");
        string currentValue = binds.FirstOrDefault() ?? (isInverted ? "0" : "1");

        if (isInverted)
        {
            bool current = currentValue != "0";
            var toggle = new Toggle();
            toggle.value = current;
            toggle.RegisterValueChangedCallback(evt =>
            {
                _binds[playerName][action] = new List<string> { evt.newValue ? "1" : "0" };
            });
            row.Add(toggle);
        }
        else
        {
            float current = float.TryParse(currentValue, out float result) ? result : 1f;
            var slider = new Slider(0f, 10f);
            slider.value = current;
            slider.style.flexGrow = 1;

            var valueLabel = new Label(current.ToString("F2"));
            valueLabel.AddToClassList("slider-value-label");

            slider.RegisterValueChangedCallback(evt =>
            {
                _binds[playerName][action] = new List<string> { evt.newValue.ToString("F2") };
                valueLabel.text = evt.newValue.ToString("F2");
            });

            row.Add(slider);
            row.Add(valueLabel);
        }

        return row;
    }

    private void OnBindClicked(string playerName, string action, string bind)
    {
        _binds[playerName][action].Remove(bind);
        RefreshUI();
    }

    private void OnAddBindClicked(string playerName, string action)
    {
        if (PlayerSettings.IsFloatSetting(Enum.Parse<PlayerSettings.PlayerSetting>(action))) return;

        ListenForControl((controlName, device) =>
        {
            if (!PlayerSettings.IsValidBindForAction(action, controlName))
            {
                Debug.Log("Control not valid for action");
                return;
            }
            if (!IsDeviceBound(playerName, device))
            {
                Debug.Log("Device is not bound to this player");
                return;
            }
            _binds[playerName][action].RemoveAll(string.IsNullOrWhiteSpace);
            if(!_binds[playerName][action].Contains(controlName))
                _binds[playerName][action].Add(controlName);
            RefreshUI();
        });
    }

    private void SetMenuVisible(bool visible)
    {
        _document.rootVisualElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void OnSaveClicked(string playerName)
    {
        var dto = new PlayerControlDTO(playerName, _binds[playerName], _devices[playerName]);
        gameController.UpdatePlayerControl(dto);
    }

    private bool IsDeviceBound(string playerName, InputDevice device)
    {
        foreach (var d in _devices[playerName])
        {
            var parts = d.Split(':');
            if (parts[0] == device.displayName && parts[1] == device.deviceId.ToString())
                return true;
        }
        return false;
    }

    private Action<string, InputDevice> _pendingControlFound;
    private IDisposable _pendingListen;

    private void ListenForControl(Action<string, InputDevice> onControlFound)
    {
        _pendingListen?.Dispose();
        _pendingControlFound = onControlFound;
        _pendingListen = InputSystem.onEvent.Call(OnInputEvent);
    }

    private void OnInputEvent(InputEventPtr eventPtr)
    {
        InputDevice device = InputSystem.GetDeviceById(eventPtr.deviceId);
        if (device == null) return;

        foreach (InputControl control in device.allControls)
        {
            if (control.name == "anyKey") continue;
            if (control.synthetic) continue;
            if (control.noisy) continue;
            
            if (control is ButtonControl button && button.IsPressed())
            {
                _pendingListen?.Dispose();
                _pendingListen = null;
                _pendingControlFound?.Invoke(control.name, device);
                return;
            }

            if (control is StickControl stick && stick.ReadValue().magnitude > 0.5f)
            {
                _pendingListen?.Dispose();
                _pendingListen = null;
                _pendingControlFound?.Invoke(control.name, device);
                return;
            }
        }
    }
}