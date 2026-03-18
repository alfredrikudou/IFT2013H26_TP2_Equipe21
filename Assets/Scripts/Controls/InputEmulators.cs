using UnityEngine;

namespace Controls
{
    public class AxisEmulator : MonoBehaviour
    {
        public float Sensitivity;
        public float Gravity;
        public float DeadZone;
        public string PlusAction;
        public string MinusAction;
        private InputManager im;
        public float V { get; private set; }

        public void Start()
        {
            V = 0;
            im = InputManager.Instance;
        }

        public void Update()
        {
            if (im.GetActionDown(PlusAction))
            {
                V = Mathf.Min(V + Sensitivity, 1f);
            }
            else if (im.GetActionDown(MinusAction))
            {
                V = Mathf.Max(V - Sensitivity, -1f);
            }
            else if (V >= DeadZone)
            {
                V -= Gravity;
            }
            else if (V <= -DeadZone)
            {
                V += Gravity;
            }
            else
            {
                V = 0;
            }
        }
    }
    
    public class StickEmulator : MonoBehaviour
    {
        public AxisEmulator XAxis;
        public AxisEmulator Yaxis;
        public Vector2 V { get; private set; }

        public void Start()
        {
            V = new Vector2(0f, 0f);
        }

        public void Update()
        {
            V = Vector2.ClampMagnitude(new Vector2(XAxis.V,Yaxis.V), 1f);
        }
    }
};