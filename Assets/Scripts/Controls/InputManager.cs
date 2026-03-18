using UnityEngine;

public class InputManager: MonoBehaviour
{
    public static InputManager Instance { get; private set; }
    
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    public bool GetActionDown(string actionName)
    {
        return true;
    }
}
