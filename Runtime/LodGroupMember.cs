using Peg.MessageDispatcher;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace Peg.Systems.LoD
{
    /// <summary>
    /// Attach to any GameObject that wants to track it's whereabouts in relation to all center-of-universes
    /// in the scene to determine what LoD to use.
    /// </summary>
    public class LodGroupMember : MonoBehaviour
    {
        public class SpawnedMsg : TargetMessage<LodGroupMember, SpawnedMsg>
        {
            public SpawnedMsg(LodGroupMember trackable) : base(trackable) { }
            public static SpawnedMsg Shared = new(null);
        }

        public class DespawnedMsg : TargetMessage<LodGroupMember, SpawnedMsg>
        {
            public DespawnedMsg(LodGroupMember trackable) : base(trackable) { }
            public static DespawnedMsg Shared = new(null);
        }

        [NonSerialized]
        public int CurrentBand = -1;
        [NonSerialized]
        public float LastCheckedTime = -10000000;

        public HashedString GroupId;
        [Tooltip("Invoked whenever this GameObject is found to be in a new distance band of the")]
        public UnityEvent<int> OnLodBandChanged;


        /// <summary>
        /// 
        /// </summary>
        public void OnEnable()
        {
            GlobalMessagePump.Instance.PostMessage(SpawnedMsg.Shared.Change(this));
        }

        /// <summary>
        /// 
        /// </summary>
        public void OnDisable()
        {
            GlobalMessagePump.Instance.PostMessage(DespawnedMsg.Shared.Change(this));
        }
    }
}
