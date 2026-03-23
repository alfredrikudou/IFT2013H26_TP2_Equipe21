using Controls;
using Controls.InputBinding;
using UnityEngine;

namespace Player
{
    public class Player : MonoBehaviour
    {
        private static int playerCount = 0;
        private string playerName = "";
        PlayerControlManager _pcm;

        void Start()
        {
            _pcm = new PlayerControlManager();
            playerName = $"Player{playerCount++}";
        }

        void Update()
        {
            if (_pcm.GetActionState(MappableAction.Shoot) == InputState.Pressed)
            {
                Debug.Log($"{playerName} has shot");
            }
            Debug.Log($"{playerName} move: {_pcm.GetActionValue(MappableAction.Move).ToString()}");
        }
    }
}