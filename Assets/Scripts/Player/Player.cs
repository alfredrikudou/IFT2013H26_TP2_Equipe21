using System;
using System.Linq;
using System.Net;
using Controls;
using Controls.Device;
using Controls.InputBinding;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.tvOS;

namespace Player
{
    public class Player : MonoBehaviour
    {
        PlayerControlManager _pcm;

        void Start()
        {
            _pcm = new PlayerControlManager("PlayerName");
        }

        void Update()
        {
            if (_pcm.GetActionState(MappableAction.Shoot) == InputState.Pressed)
            {
            }
            //Debug.Log(_pcm.GetActionValue(MappableAction.Move));

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                _pcm.ListenForDeviceChange();
            }

            if (Keyboard.current.tKey.wasPressedThisFrame)
            {
                DeviceManager.Instance.test();
            }
        }
    }
}