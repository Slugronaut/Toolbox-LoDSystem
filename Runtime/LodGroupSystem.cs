using System;
using System.Collections;
using System.Collections.Generic;
using Peg.AutoCreate;
using Peg.MessageDispatcher;
using Peg.UpdateSystem;
using UnityEngine;

namespace Peg.Systems.LoD
{
    /// <summary>
    /// Processes object LoDs based on a series of bands where each band should be ticked with a different frequency.
    /// </summary>
    [AutoCreate(CreationActions.DeserializeSingletonData)]
    public class LodGroupSystem : Updatable
    {
        [Serializable]
        public struct LodBand
        {
            public float Distance;
            public float Freq;
        }

        [Tooltip("The id for the Center-of-Universe that is tracked for distance bands.")]
        public string CouId;

        [Tooltip("The distance bands that represent each stage of the LoD group.")]
        public LodBand[] LodBands;

        [Tooltip("If an object is not within range of any of the previous distance bands, this one will be used.")]
        public LodBand DefaultBand;

        [AutoResolve]
        IMessageDispatcher<GameObject> Dispatcher;
        HashedString HashedCoUGroupId;
        List<LodGroupMember> Members;


        public void AutoStart()
        {
            AutoCreator.Resolve(this);
            Members = new(8);
            HashedCoUGroupId = new HashedString(CouId);//because serialization doesn't update the hash value itself
            Dispatcher.AddListener<LodGroupMember.SpawnedMsg>(HandleSpawnedLod);
            Dispatcher.AddListener<LodGroupMember.DespawnedMsg>(HandleDespawnedLod);
        }

        public void AutoDestroy()
        {
            Dispatcher.RemoveListener<LodGroupMember.SpawnedMsg>(HandleSpawnedLod);
            Dispatcher.RemoveListener<LodGroupMember.DespawnedMsg>(HandleDespawnedLod);
        }

        public override void Update()
        {
            for(int memberIndex = 0; memberIndex < Members.Count; memberIndex++)
            {
                var member = Members[memberIndex];
                var memberPos = member.transform.position;
                var CoU = CenterOfUniverse.GetClosest(HashedCoUGroupId.Hash, memberPos);
                if (CoU == null) continue;
                Vector3 couPos = CoU.transform.position;
                var distSqr = (couPos - memberPos).sqrMagnitude;

                DetermineDistanceBand(distSqr, member);
            }
        }

        void DetermineDistanceBand(float distSqr, LodGroupMember member)
        {
            int len = LodBands.Length;
            for (int bandIndex = 0; bandIndex < LodBands.Length; bandIndex++)
            {
                var lodBand = LodBands[bandIndex];
                if (distSqr < lodBand.Distance * lodBand.Distance)
                {
                    DetermineBandCooldown(member, lodBand, bandIndex);
                    return;
                }
            }

            //none were appropriate, use the default band
            DetermineBandCooldown(member, DefaultBand, len);
        }

        bool DetermineBandCooldown(LodGroupMember member, LodBand band, int bandIndex)
        {
            if (member.CurrentBand != bandIndex)
            {
                if (Time.time - member.LastCheckedTime > band.Freq)
                {
                    member.LastCheckedTime = Time.time;
                    member.CurrentBand = bandIndex;
                    member.OnLodBandChanged.Invoke(bandIndex);
                    return true;
                }
            }
            return false;
        }

        void HandleSpawnedLod(LodGroupMember.SpawnedMsg msg)
        {
            if (msg.Target.GroupId.Hash != HashedCoUGroupId.Hash)
                return;

            Debug.Log($"Matched Spawned: {msg.Target.GroupId.Hash} / {HashedCoUGroupId.Hash} ");
            Members.Add(msg.Target);
            GlobalCoroutine.Start(WaitForCoU(msg.Target));
        }

        void HandleDespawnedLod(LodGroupMember.DespawnedMsg msg)
        {
            if (msg.Target.GroupId.Hash != HashedCoUGroupId.Hash)
                return;

            Members.Remove(msg.Target);
        }

        IEnumerator WaitForCoU(LodGroupMember member)
        {
            while (true)
            {
                var cou = CenterOfUniverse.GetClosest(HashedCoUGroupId.Hash, member.transform.position);
                if (cou != null)
                {
                    AssignCoU(member, cou);
                    yield break;
                }
                yield return null;
            }

        }

        void AssignCoU(LodGroupMember member, CenterOfUniverse cou)
        {
            Vector3 couPos = cou.transform.position;
            var distSqr = (couPos - member.transform.position).sqrMagnitude;
            DetermineDistanceBand(distSqr, member);
        }
    }
}
