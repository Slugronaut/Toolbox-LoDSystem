using System.Collections.Generic;
using Peg.AutoCreate;
using Peg.MessageDispatcher;
using Peg.UpdateSystem;
using UnityEngine;

namespace Peg.Systems.LoD
{
    /// <summary>
    /// 
    /// </summary>
    [AutoCreate(CreationActions.DeserializeSingletonData)]
    public class LoDSystem : Updatable
    {
        //public
        public string CoUGroupId;
        public float Frequency = -1000;

        static LoDSystem _Instance;
        public static LoDSystem Instance => _Instance;

        List<GameObjectLod> GoLods;
        List<BehaviourLod> BLods;
        float LastTime;
        HashedString HashedCoUGroupId;


        #region AutoCreate & Loop Events
        public void AutoAwake()
        {
            _Instance = this;
        }

        public void AutoStart()
        {
            GoLods = new(32);
            BLods = new(32);
            HashedCoUGroupId = new HashedString(CoUGroupId);//because serialization doesn't update the hash value itself
            GlobalMessagePump.Instance.AddListener<GameObjectLod.SpawnedMsg>(HandleSpawnedLod);
            GlobalMessagePump.Instance.AddListener<GameObjectLod.DespawnedMsg>(HandleDespawnedLod);

            GlobalMessagePump.Instance.AddListener<BehaviourLod.SpawnedMsg>(HandleSpawnedLod);
            GlobalMessagePump.Instance.AddListener<BehaviourLod.DespawnedMsg>(HandleDespawnedLod);
        }

        public void AutoDestroy()
        {
            GlobalMessagePump.Instance.RemoveListener<GameObjectLod.SpawnedMsg>(HandleSpawnedLod);
            GlobalMessagePump.Instance.RemoveListener<GameObjectLod.DespawnedMsg>(HandleDespawnedLod);

            GlobalMessagePump.Instance.RemoveListener<BehaviourLod.SpawnedMsg>(HandleSpawnedLod);
            GlobalMessagePump.Instance.RemoveListener<BehaviourLod.DespawnedMsg>(HandleDespawnedLod);
        }

        public override void Awake()
        {
            
        }

        public override void OnEnable()
        {

        }

        public override void OnDisable()
        {

        }

        public override void Update()
        {
            if (Time.time - LastTime < Frequency) return;
            LastTime = Time.time;

            var CoU = CenterOfUniverse.GetClosest(HashedCoUGroupId.Hash, Vector3.zero);
            if (CoU == null) return;

            Vector3 couPos = CoU.transform.position;

            //iterate through all trackables and see which ones
            //are close enough to update their LoD level
            var glods = GoLods;
            for(int i = 0; i < glods.Count; i++)
            {
                var lod = glods[i];
                var pos = lod.transform.position;
                var distSqr = (couPos - pos).sqrMagnitude;

                if (distSqr < lod.DistanceThresholdMin * lod.DistanceThresholdMin)
                    lod.LodEnable();
                else if (distSqr > lod.DistanceThresholdMax * lod.DistanceThresholdMax)
                    lod.LodDisable();
            }

            var blods = BLods;
            for (int i = 0; i < blods.Count; i++)
            {
                var lod = blods[i];
                var pos = lod.transform.position;
                var distSqr = (couPos - pos).sqrMagnitude;

                if (distSqr < lod.DistanceThresholdMin * lod.DistanceThresholdMin)
                    lod.LodEnable();
                else if (distSqr > lod.DistanceThresholdMax * lod.DistanceThresholdMax)
                    lod.LodDisable();
            }
        }
        #endregion


        void HandleSpawnedLod(GameObjectLod.SpawnedMsg msg)
        {
            GoLods.Add(msg.Target);
        }

        void HandleDespawnedLod(GameObjectLod.DespawnedMsg msg)
        {
            GoLods.Remove(msg.Target);
        }

        void HandleSpawnedLod(BehaviourLod.SpawnedMsg msg)
        {
            BLods.Add(msg.Target);
        }

        void HandleDespawnedLod(BehaviourLod.DespawnedMsg msg)
        {
            BLods.Remove(msg.Target);
        }

        /// <summary>
        /// Adds a delay to the frequnecy timer so that it will not tick
        /// for at least this amount of time into the future.
        /// </summary>
        /// <param name="time"></param>
        public void DelayTimer(float time)
        {
            LastTime = Time.time + time;
        }
    }
    

    /// <summary>
    /// 
    /// </summary>
    public interface ILod
    {
        float DistanceThresholdMin { get; }
        float DistanceThresholdMax { get; }
        void LodEnable();
        void LodDisable();
    }
}
