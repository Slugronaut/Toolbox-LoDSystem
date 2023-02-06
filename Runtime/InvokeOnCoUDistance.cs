using UnityEngine;
using UnityEngine.Events;

namespace Toolbox.Behaviours
{
    /// <summary>
    /// Triggers a UnityEvent when a certain distance from any CoU is detected.
    /// </summary>
    public class InvokeOnCoUDistance : MonoBehaviour
    {
        public enum DistanceModes
        {
            GreaterThan,
            LessThan,
        }

        public HashedString CoUId;
        public float Interval = 5;
        public float Distance;
        public DistanceModes Mode;
        public UnityEvent OnTrigger;


        float LastTime;
        Transform Trans;

        private void Awake()
        {
            Trans = GetComponent<Transform>();
        }

        private void OnEnable()
        {
            LastTime = Time.time;
        }

        public void Update()
        {
            if (Time.time - LastTime < Interval)
                return;
            LastTime = Time.time;

            Vector3 pos = Trans.position;
            var cou = CoUId.NoValue ? CenterOfUniverse.GetClosest(pos) : CenterOfUniverse.GetClosest(CoUId.Hash, pos);

            if(cou != null)
            {
                var dist = Vector3.Distance(pos, cou.transform.position);
                if(Mode == DistanceModes.GreaterThan)
                {
                    if (dist > Distance) OnTrigger.Invoke();
                }
                else
                {
                    if (dist < Distance) OnTrigger.Invoke();
                }
            }
        }


    }
}
