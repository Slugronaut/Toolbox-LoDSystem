using UnityEngine;
using System;
using System.Collections.Generic;
using Toolbox.Collections;
using Sirenix.Utilities;
using System.Linq;

namespace Toolbox.Behaviours
{
    
    /// <summary>
    /// Handles how much processing an entity should use by switching
    /// certain behaviours on or off based on closest object with
    /// the 'Center-of-Universe' component attached.
    /// 
    /// Once this object has been enabled during runtime you should never change the list of
    /// GameObjects or Behaviours supplied to it. Its state may become corrupted if you do.
    /// 
    /// TODO: dispatch local message with power mode change
    /// 
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LevelOfDetail : MonoBehaviour
    {
        public bool UseGroupId;
        public HashedString GroupId;
        [Tooltip("The powermode index that should be default when OnEnable is called.")]
        public int DefaultModeIndex = -1;
        public GameObject[] GameObjects;
        public Behaviour[] Behaviours;
        public Collider[] Colliders3d;
        public Mode[] PowerModes = new Mode[0]; //need to due this due to Rotorz being stupid

        int Power = 0;
        float LastTime;
        

        /// <summary>
        /// Stores the state of a single mode of operation
        /// at a know 'processing power level'. Basically
        /// this is just a hard-coded dictionary.
        /// </summary>
        [Serializable]
        public class Mode
        {
            public float Distance;
            public float Frequency;
            
            public List<GameObject> GameObjectKeys = new List<GameObject>();
            public List<bool> GameObjectValues = new List<bool>();
            public List<Behaviour> BehaviourKeys = new List<Behaviour>();
            public List<bool> BehaviourValues = new List<bool>();
            public List<Collider> Collider3DKeys = new List<Collider>();
            public List<bool> Collider3DValues = new List<bool>();

            public Mode()
            {

            }

            public Mode(Mode src)
            {
                Distance = src.Distance;
                Frequency = src.Frequency;
                foreach (var go in src.GameObjectKeys)
                    GameObjectKeys.Add(go);
                foreach (var gov in src.GameObjectValues)
                    GameObjectValues.Add(gov);

                foreach (var bek in src.BehaviourKeys)
                    BehaviourKeys.Add(bek);
                foreach (var bev in src.BehaviourValues)
                    BehaviourValues.Add(bev);

                foreach (var colk in src.Collider3DKeys)
                    Collider3DValues.Add(colk);
                foreach (var colv in src.Collider3DValues)
                    Collider3DValues.Add(colv);
            }
            

            public bool GetGameObjectState(GameObject key)
            {
                return GameObjectValues[GameObjectKeys.IndexOf(key)];
            }

            public bool GetBehaviourState(Behaviour key)
            {
                return BehaviourValues[BehaviourKeys.IndexOf(key)];
            }

            public bool GetColliderState(Collider key)
            {
                return Collider3DValues[Collider3DKeys.IndexOf(key)];
            }

            public void SetGameObjectState(GameObject key, bool state)
            {
                int index = GameObjectKeys.IndexOf(key);
                if (index == -1)
                {
                    GameObjectKeys.Add(key);
                    GameObjectValues.Add(state);
                }
                else GameObjectValues[index] = state;
            }

            public void SetBehaviourState(Behaviour key, bool state)
            {
                int index = BehaviourKeys.IndexOf(key);
                if (index == -1)
                {
                    BehaviourKeys.Add(key);
                    BehaviourValues.Add(state);
                }
                else BehaviourValues[index] = state;
            }

            public void SetColliderState(Collider key, bool state)
            {
                int index = Collider3DKeys.IndexOf(key);
                if (index == -1)
                {
                    Collider3DKeys.Add(key);
                    Collider3DValues.Add(state);
                }
                else Collider3DValues[index] = state;
            }

            public void Remove(GameObject key)
            {
                int index = GameObjectKeys.IndexOf(key);
                if (index >= 0)
                {
                    GameObjectKeys.RemoveAt(index);
                    GameObjectValues.RemoveAt(index);
                }
            }

            public void Remove(Behaviour key)
            {
                int index = BehaviourKeys.IndexOf(key);
                if (index >= 0)
                {
                    BehaviourKeys.RemoveAt(index);
                    BehaviourValues.RemoveAt(index);
                }
            }

            public void Remove(Collider key)
            {
                int index = Collider3DKeys.IndexOf(key);
                if(index >= 0)
                {
                    Collider3DKeys.RemoveAt(index);
                    Collider3DValues.RemoveAt(index);
                }
            }
            
        }

        #if UNITY_EDITOR
        public void OnDrawGizmos()
        {
            if (PowerModes.Length < 2 || Power == 0) return;

            float alpha = (float)(Power+1.0f) / (float)PowerModes.Length;
            Gizmos.color = Color.Lerp(Color.yellow, Color.grey, alpha);
            Gizmos.DrawWireCube(transform.position, Vector3.one);
        }
        #endif
        

        void OnEnable()
        {
            Power = DefaultModeIndex;
            Activate(PowerModes[Power]);
        }

        void Update()
        {
            float freq = PowerModes[Power].Frequency;
            float time = Time.unscaledTime;
            if (time - LastTime < freq) return;

            Vector3 myPos = transform.position;
            CenterOfUniverse cou = UseGroupId ? CenterOfUniverse.GetClosest(GroupId.Hash, myPos) : CenterOfUniverse.GetClosest(myPos);

            if (cou != null)
            {
                LastTime = time;
                //float dist = Vector3.Distance(myPos, cou.transform.position);
                float dist = (myPos - cou.transform.position).sqrMagnitude;

                //zero is always the 'highest' power level
                int pow = 0;
                var modes = PowerModes;
                for (int i = 1; i < modes.Length; i++)
                {
                    float pmDist = modes[i].Distance;
                    if (dist > pmDist * pmDist) pow = i;
                }

                if (pow != Power)
                {
                    Power = pow;
                    Activate(modes[Power]);
                }
            }
        }

        public void ForceLowestPower()
        {
            Activate(PowerModes[PowerModes.Length - 1]);
        }

        public void ForceHighestPower()
        {
            Activate(PowerModes[0]);
        }

        public void Activate(Mode mode)
        {
            var gok = mode.GameObjectKeys;
            for (int i = 0; i < gok.Count; i++)
                mode.GameObjectKeys[i].SetActive(mode.GameObjectValues[i]);

            var bhk = mode.BehaviourKeys;
            for (int i = 0; i < bhk.Count; i++)
                mode.BehaviourKeys[i].enabled = mode.BehaviourValues[i];

            var colk = mode.Collider3DKeys;
            for (int i = 0; i < colk.Count; i++)
                mode.Collider3DKeys[i].enabled = mode.Collider3DValues[i];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        public void DelayTimer(float time)
        {
            LastTime = Time.time + time;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameObject"></param>
        public void AddGameObject(GameObject gameObject, params bool[] flags)
        {
            GameObjects = GameObjects.AddElement(gameObject);

            for(int i = 0; i < flags.Length; i++)
            {
                var mode = PowerModes[i];
                mode.GameObjectKeys.Add(gameObject);
                mode.GameObjectValues.Add(flags[i]);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameObject"></param>
        public void RemoveGameObject(GameObject gameObject)
        {

            GameObjects = GameObjects.RemoveElement(gameObject);

            foreach(var mode in PowerModes)
            {
                var i = mode.GameObjectKeys.IndexOf(gameObject);
                mode.GameObjectKeys.RemoveAt(i);
                mode.GameObjectValues.RemoveAt(i);
            }
        }


    }


}
