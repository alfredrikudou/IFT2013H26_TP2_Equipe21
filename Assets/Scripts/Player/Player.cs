using Controls;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class Player : MonoBehaviour, IDeviceSelector
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        public void BindDevice(InputDevice device)
        {
        }

        public void UnBindDevice()
        {
        }

        public string GetSelectorName()
        {
            return "Player";
        }
    }
}
