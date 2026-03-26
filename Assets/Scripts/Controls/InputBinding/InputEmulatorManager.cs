using System;
using System.Linq;
using UnityEngine;

namespace Controls.InputBinding
{
    public class InputEmulatorManager : MonoBehaviour
    {
        private static InputEmulatorManager _instance;

        public static InputEmulatorManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("InputEmulatorManager");
                    _instance = go.AddComponent<InputEmulatorManager>();
                    DontDestroyOnLoad(go);
                }

                return _instance;
            }
            private set => _instance = value;
        }

        private IEmulator[] _emulators = Array.Empty<IEmulator>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
            _emulators = Array.Empty<IEmulator>();
        }

        public T Register<T>(T emulator) where T : IEmulator
        {
            Array.Resize(ref _emulators, _emulators.Length + 1);
            _emulators[^1] = emulator;
            return emulator;
        }

        public void Unregister(IEmulator emulator)
        {
            var index = Array.IndexOf(_emulators, emulator);
            if (index < 0) return;

            _emulators[index] = _emulators[^1];
            Array.Resize(ref _emulators, _emulators.Length - 1);
        }

        private void Update()
        {
            foreach (var emulator in _emulators)
                emulator.Tick(Time.deltaTime);
        }
    }
}