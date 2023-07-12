using Toolbox.Collections;
using Toolbox;
using UnityEngine;


namespace PegGames.Lod
{
    /// <summary>
    /// Represents a LoD list for Behaviours whose active states are
    /// managed by a central LoDSystem.
    /// </summary>
    public class BehaviourLod : MonoBehaviour
    {

        public class SpawnedMsg : TargetMessage<BehaviourLod, SpawnedMsg>
        {
            public SpawnedMsg(BehaviourLod trackable) : base(trackable) { }
            public static SpawnedMsg Shared = new(null);
        }

        public class DespawnedMsg : TargetMessage<BehaviourLod, SpawnedMsg>
        {
            public DespawnedMsg(BehaviourLod trackable) : base(trackable) { }
            public static DespawnedMsg Shared = new(null);
        }

        [Tooltip("The distance threshold at which the GameObjects list is switched on.")]
        public float DistanceThresholdMin;

        [Tooltip("The distance threshold at which the GameObjects list is switched off.")]
        public float DistanceThresholdMax;

        [Tooltip("If true, the GameObjects in the list will have their state enabled when beyond the LoD distance and disabled otherwise.")]
        public bool InvertState = false;

        [Tooltip("The Behaviours to manage with this LoD.")]
        [SerializeField]
        Behaviour[] Behaviours;

        [Tooltip("The initial state of this LoD group. It should match the active state of the gameobjects in the list.")]
        public bool Enabled = true;

        /// <summary>
        /// 
        /// </summary>
        public void OnEnable()
        {
            GlobalMessagePump.Instance.PostMessage(new SpawnedMsg(this));// SpawnedMsg.Shared.Change(this));
        }

        /// <summary>
        /// 
        /// </summary>
        public void OnDisable()
        {
            GlobalMessagePump.Instance.PostMessage(new DespawnedMsg(this));// DespawnedMsg.Shared.Change(this));
        }

        /// <summary>
        /// 
        /// </summary>
        public void LodEnable()
        {
            if (Enabled) return;
            Enabled = true;

            var gos = Behaviours;
            for (int i = 0; i < gos.Length; i++)
                gos[i].enabled = !InvertState;
        }

        /// <summary>
        /// 
        /// </summary>
        public void LodDisable()
        {
            if (!Enabled) return;
            Enabled = false;

            var gos = Behaviours;
            for (int i = 0; i < gos.Length; i++)
                gos[i].enabled = InvertState;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="behaviour"></param>
        public void Add(Behaviour behaviour)
        {
            Behaviours = Behaviours.AddElement(behaviour);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="behaviour"></param>
        public void Remove(Behaviour behaviour)
        {
            Behaviours = Behaviours.RemoveElement(behaviour);
        }
    }
}
