using UnityEngine;
using UnityEngine.InputSystem.Controls;

namespace Controls
{
    public class AxisEmulator : ICustomSerializable
    {
        private readonly float _sensitivity;
        private readonly float _gravity;
        private readonly float _deadZone;
        private readonly MappableAction _plusAction;
        private readonly MappableAction _minusAction;
        private readonly InputManager _im;
        public float Value { get; private set; }

        public string Serialize() => $"AxisEmulator:sensitivity={_sensitivity},gravity={_gravity}," +
                                     $"deadzone={_deadZone},plusAction={_plusAction.ToString()}," +
                                     $"minusAction={_minusAction.ToString()}";

        public AxisEmulator(MappableAction plusAction, MappableAction minusAction, float sensitivity, float gravity, float deadZone)
        {
            _plusAction = plusAction;
            _minusAction = minusAction;
            _sensitivity = sensitivity;
            _gravity = gravity;
            _deadZone = deadZone;
            Value = 0;
            _im = InputManager.Instance;
        }

        public void Tick()
        {
            if (_im.GetActionDown(_plusAction))
            {
                Value = Mathf.Min(Value + _sensitivity, 1f);
            }
            else if (_im.GetActionDown(_minusAction))
            {
                Value = Mathf.Max(Value - _sensitivity, -1f);
            }
            else if (Value >= _deadZone)
            {
                Value -= _gravity;
            }
            else if (Value <= -_deadZone)
            {
                Value += _gravity;
            }
            else
            {
                Value = 0;
            }
        }
    }
    
    public class StickEmulator : IStick
    {
        private readonly AxisEmulator _xAxis;
        private readonly  AxisEmulator _yAxis;
        public Vector2 Value { get; private set; }
        public Vector2 ReadValue() => Value;
        public string Serialize() => $"StickEmulator:xAxis={_xAxis.Serialize()},yAxis={_yAxis.Serialize()}";

        public StickEmulator(AxisEmulator xAxis, AxisEmulator yaxis)
        {
            _xAxis = xAxis;
            _yAxis = yaxis;
            Value = new Vector2(0f, 0f);
        }


        public void Tick()
        {
            _xAxis.Tick();
            _yAxis.Tick();
            Value = Vector2.ClampMagnitude(new Vector2(_xAxis.Value,_yAxis.Value), 1f);
        }
    }

    public class UnityStick : IStick
    {
        private readonly StickControl _stick;
        public UnityStick(StickControl stick) => _stick = stick;
        public Vector2 ReadValue() => _stick?.ReadValue() ?? Vector2.zero;
        public string Serialize() => $"UnityStick:path={_stick?.path ?? ""}";
    }

    public interface IStick : ICustomSerializable
    {
        public Vector2 ReadValue();
    } 
};