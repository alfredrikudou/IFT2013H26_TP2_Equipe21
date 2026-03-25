using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.Controls;

namespace Controls.InputBinding
{
    public class AxisEmulator : IEmulator
    {
        private readonly float _sensitivity;
        private readonly float _gravity;
        private readonly float _deadZone;
        private readonly IBindableInput[] _plusActions;
        private readonly IBindableInput[] _minusActions;
        static int cpt = 0;
        private int id;
        public int GetId() => id;

        public float Value { get; private set; }

        public AxisEmulator(IBindableInput[] plusActions, IBindableInput[] minusActions, float sensitivity = 1f, float gravity = 1f, float deadZone = 0f)
        {
            id = cpt++;
            _plusActions = plusActions;
            _minusActions = minusActions;
            _sensitivity = sensitivity;
            _gravity = gravity;
            _deadZone = deadZone;
            Value = 0;
        }

        public void Tick(float deltatime)
        {
            var plusHeld = _plusActions.Any(x => x.GetState() == InputState.Pressed || x.GetState() == InputState.Held);
            var minusHeld = _minusActions.Any(x => x.GetState() == InputState.Pressed || x.GetState() == InputState.Held);

            if (plusHeld && !minusHeld)
            {
                Value = Mathf.Min(Value + _sensitivity * deltatime, 1f);
            }
            else if (minusHeld && !plusHeld)
            {
                Value = Mathf.Max(Value - _sensitivity * deltatime, -1f);
            }
            else if (Value > _deadZone)
            {
                Value = Mathf.Max(Value - _gravity * deltatime, 0f);
            }
            else if (Value < -_deadZone)
            {
                Value = Mathf.Min(Value + _gravity * deltatime, 0f);
            }
            else
            {
                Value = 0;
            }
        }
    }
    
    public class StickEmulator : IStick, IEmulator
    {
        static int cpt = 0;
        private int id;
        public int GetId() => id;
        
        private readonly AxisEmulator _xAxis;
        private readonly  AxisEmulator _yAxis;
        private readonly bool _isInverted;
        public Vector2 Value { get; private set; }
        public Vector2 ReadValue() => _isInverted ? Value * -1f : Value;

        public StickEmulator(AxisEmulator xAxis, AxisEmulator yaxis, bool isInverted = false)
        {
            id =  cpt++;
            _xAxis = xAxis;
            _yAxis = yaxis;
            _isInverted = isInverted;
            Value = new Vector2(0f, 0f);
        }


        public void Tick(float deltatime)
        {
            _xAxis.Tick(deltatime);
            _yAxis.Tick(deltatime);
            Value = Vector2.ClampMagnitude(new Vector2(_xAxis.Value,_yAxis.Value), 1f);
        }
    }

    public interface IEmulator
    {
        public void Tick(float deltatime);
        public int GetId();
    }

    public class UnityStick : IStick
    {
        private readonly StickControl _stick;
        private readonly bool _isInverted;
        public UnityStick(StickControl stick, bool isInverted = false) {
            _stick = stick;
            _isInverted = isInverted;
        }

        public Vector2 ReadValue()
        {
            Vector2 value = _stick?.ReadValue() ?? Vector2.zero;
            return _isInverted ? -value : value;
        }
    }

    public interface IStick
    {
        public Vector2 ReadValue();
    } 
};