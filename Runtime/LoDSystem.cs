using System.Collections.Generic;
using Peg.AutoCreate;
using Peg.UpdateSystem;
using UnityEngine;

namespace Peg.Systems.LoD
{
    /// <summary>
    /// Central system for checking LoDs. Each LoD behaviour can register with this central object and then be ticked on a regular interval
    /// to check for distance thresholds.
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
        double LastTime;
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
        }

        public void AutoDestroy()
        {
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
            if (Time.timeAsDouble - LastTime < Frequency) return;
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


        public void RegisterLod(GameObjectLod target)
        {
            GoLods.Add(target);
        }

        public void UnregisterLod(GameObjectLod target)
        {
            GoLods.Remove(target);
        }

        public void RegisterLod(BehaviourLod target)
        {
            BLods.Add(target);
        }

        public void UnregisterLod(BehaviourLod target)
        {
            BLods.Remove(target);
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
