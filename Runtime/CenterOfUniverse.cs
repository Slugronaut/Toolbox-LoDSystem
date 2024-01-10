using UnityEngine;
using System.Collections.Generic;
using Peg.Collections;
using UnityEngine.Assertions;
using Peg.MessageDispatcher;
using System;

namespace Peg.Systems.LoD
{
    /// <summary>
    /// Attach this component to an object to mark it as a Center-of-Universe.
    /// Distances to all CoUs can be obtained through static methods.
    /// 
    /// TODO: Implement a KD-tree for broad-phase filtering. Only needed for large numbers of objects?
    /// </summary>
    [AddComponentMenu("Peg/System/LoD/Center-of-Universe")]
    public class CenterOfUniverse : MonoBehaviour
    {
        /// <summary>
        /// Helper class to avoid the nonesense of boxing on primitives due to the dogshit lack of IEquatable<> on their types.
        /// </summary>
        struct BoxedIntHash : IEquatable<BoxedIntHash>
        {
            public int HashId;


            public override int GetHashCode()
            {
                return HashId.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return Equals((BoxedIntHash)obj);// as BoxedIntHash);
            }

            public bool Equals(BoxedIntHash other)
            {
                //if (other == null)
                //    return false;

                return HashId == other.HashId;
            }

            public BoxedIntHash(int intValue)
            {
                HashId = intValue;
            }
        }

        public class HashComparerer : IEqualityComparer<BoxedIntHash>
        {
            bool IEqualityComparer<BoxedIntHash>.Equals(BoxedIntHash x, BoxedIntHash y)
            {
                return x.Equals(y);
            }

            int IEqualityComparer<BoxedIntHash>.GetHashCode(BoxedIntHash obj)
            {
                return obj.GetHashCode();
            }
        }


        #region Instance Members
        public HashedString GroupId
        {
            get { return _GroupId; }
            set
            {
                if (value.Hash == _GroupId.Hash) return;
                
                #if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    _GroupId = value;
                    return;
                }
                #endif

                RemoveFromMultiverse(_GroupId.Hash);
                AddToMultiverse(value.Hash);
                _GroupId = value;
            }
        }

        [Tooltip("Used to classify groupings of CentersOfUniverses. Useful if you need to judge distances to one group without including another. Once this value is set it cannot be changed at runtime.")]
        [SerializeField]
        private HashedString _GroupId;
        CoUSpawnedEvent Buffered;

        public Transform TransReadOnly;

        private void Awake()
        {
            TransReadOnly = transform;
        }

        void Start()
        {
            Buffered = new CoUSpawnedEvent(this);
            GlobalMessagePump.Instance.PostMessage(Buffered);
        }

        void OnDestroy()
        {
            if(Buffered != null) GlobalMessagePump.Instance.RemoveBufferedMessage(Buffered);
            GlobalMessagePump.Instance.PostMessage(new CoURemovedEvent(this));
        }

        void OnEnable()
        {
            AddToMultiverse(GroupId.Hash);
        }

        void OnDisable()
        {
            RemoveFromMultiverse(GroupId.Hash);
        }
        
        void AddToMultiverse(int groupId)
        {
            var boxedGroupId = new BoxedIntHash(groupId);
            if (Multiverse.TryGetValue(boxedGroupId, out var tempList))
                tempList.Add(this);
            else
            {
                tempList = new(1) { this };
                Multiverse[boxedGroupId] = tempList;
            }

        }

        void RemoveFromMultiverse(int groupId)
        {
            var boxedGroupId = new BoxedIntHash(groupId);
            if (Multiverse.TryGetValue(boxedGroupId, out var tempList))
            {
                tempList.Remove(this);
                if (tempList.Count < 1) Multiverse.Remove(boxedGroupId);
            }
        }
        #endregion


        #region Static Members
        static readonly HashMap<BoxedIntHash, List<CenterOfUniverse>> Multiverse = new(new HashComparerer());
        //static List<CenterOfUniverse> TempList = new List<CenterOfUniverse>();
        static float xMin, yMin, zMin, xMax, yMax, zMax;


        /// <summary>
        /// Returns true if the closest CoU is within the given distance threshold.
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static bool IsClosestWithinDistance(Vector3 point, float distance)
        {
            if (Multiverse.Count < 1) return false;
            var cou = GetClosest(point);

            if (distance < 1 && distance > 0)
                distance /= distance;
            else distance *= distance;

            var diff = cou.transform.position - point;
            if (diff.sqrMagnitude < distance)
                return true;
            else return false;

        }

        /// <summary>
        /// Returns true if the closest CoU of the specified group is within the given distance threshold.
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static bool IsClosestWithinDistance(int group, Vector3 point, float distance)
        {
            if (Multiverse.Count < 1) return false;
            var cou = GetClosest(group, point);
            if (cou == null) return false;

            if (distance < 1 && distance > 0)
                distance /= distance;
            else distance *= distance;
            if ((cou.transform.position - point).sqrMagnitude < distance)
                return true;
            else return false;

        }

        /// <summary>
        /// Returns the closest CenterOfUniverse to the given point.
        /// Null is returned if there are no CoUs.
        /// </summary>
        /// <param name="point"></param>
        public static CenterOfUniverse GetClosest(Vector3 point)
        {
            if (Multiverse.Count < 1) return null;

            CenterOfUniverse bestMatch = null;
            float bestMatchMag = float.MaxValue;
            var lists = Multiverse.ValuesNonAlloc;
            List<CenterOfUniverse> list;
            for (int i = 0; i < Multiverse.Count; i++)
            {
                list = lists[i];
                if (list == null) continue;
                for (int j = 0; j < list.Count; j++)
                {
                    float mag = (list[j].transform.position - point).sqrMagnitude;
                    if (mag <= bestMatchMag)
                    {
                        bestMatchMag = mag;
                        bestMatch = list[j];
                    }
                }
            }

            return bestMatch;
        }

        /// <summary>
        /// Returns the closest of a group of CenterOfUniverses to the given point.
        /// Null is returned if there are no CoUs.
        /// </summary>
        /// <param name="point"></param>
        public static CenterOfUniverse GetClosest(int groupId, Vector3 point)
        {
            if (Multiverse.Count < 1) return null;
            if (!Multiverse.TryGetValue(new BoxedIntHash(groupId), out var tempList)) return null;

            int bestMatchIndex = 0;
            float bestMatchMag = float.MaxValue;

            for (int i = 0; i < tempList.Count; i++)
            {
                float mag = (tempList[i].transform.position - point).sqrMagnitude;
                if (mag <= bestMatchMag)
                {
                    bestMatchMag = mag;
                    bestMatchIndex = i;
                }
            }

            return tempList[bestMatchIndex];
        }

        /// <summary>
        /// Returns the furthest CenterOfUniverse from the given point.
        /// Null is returned if there are no CoUs.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static CenterOfUniverse GetFurthest(Vector3 point)
        {
            if (Multiverse.Count < 1) return null;

            CenterOfUniverse bestMatch = null;
            float bestMatchMag = float.MaxValue;
            var lists = Multiverse.ValuesNonAlloc;
            List<CenterOfUniverse> list;
            for (int i = 0; i < Multiverse.Count; i++)
            {
                list = lists[i];
                if (list == null) continue;
                for (int j = 0; j < list.Count; j++)
                {
                    float mag = (list[j].transform.position - point).sqrMagnitude;
                    if (mag >= bestMatchMag)
                    {
                        bestMatchMag = mag;
                        bestMatch = list[j];
                    }
                }
            }

            return bestMatch;
        }

        /// <summary>
        /// Returns the furthest of a group of CenterOfUniverses from the given point.
        /// Null is returned if there are no CoUs. Only active and enabled CoUs will
        /// be considered.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static CenterOfUniverse GetFurthest(int groupId, Vector3 point)
        {
            if (Multiverse.Count < 1) return null;
            if (!Multiverse.TryGetValue(new BoxedIntHash(groupId), out var tempList)) return null;

            int bestMatchIndex = 0;
            float bestMatchMag = 0;

            for (int i = 0; i < tempList.Count; i++)
            {
                float mag = (tempList[i].transform.position - point).sqrMagnitude;
                if (mag >= bestMatchMag)
                {
                    bestMatchMag = mag;
                    bestMatchIndex = i;
                }
            }

            return tempList[bestMatchIndex];
        }

        /// <summary>
        /// Returns a copy of the list of all center-of-universes for the group id.
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public static void GetAll(int groupId, ref List<CenterOfUniverse> output)
        {
            Assert.IsNotNull(output);
            output.Clear();
            if (Multiverse.Count < 1) return;
            if (!Multiverse.TryGetValue(new BoxedIntHash(groupId), out var tempList)) return;
            for (int i = 0; i < tempList.Count; i++)
                output.Add(tempList[i]);
        }

        /// <summary>
        /// Returns the center of the bounding volume of all active Center-Of-Universes.
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public static Vector3 GetCentroid()
        {
            if (Multiverse.Count < 1) return Vector3.zero;
            var lists = Multiverse.ValuesNonAlloc;
            List<CenterOfUniverse> list;

            var min = lists[0][0].transform.position;
            var max = min;
            xMin = min.x;
            yMin = min.y;
            zMin = min.z;

            xMax = max.x;
            yMax = max.y;
            zMax = max.z;

            for (int i = 0; i < Multiverse.Count; i++)
            {
                list = lists[i];
                for (int j = 0; j < list.Count; j++)
                {
                    Vector3 pos = list[j].transform.position;
                    if (pos.x < xMin) xMin = pos.x;
                    if (pos.y < yMin) yMin = pos.y;
                    if (pos.z < zMin) zMin = pos.z;

                    if (pos.x > xMax) xMax = pos.x;
                    if (pos.y > yMax) yMax = pos.y;
                    if (pos.z > zMax) zMax = pos.z;
                }
            }

            min = new Vector3(xMin, yMin, zMin);
            max = new Vector3(xMax, yMax, zMax);
            return min + ((max - min) * 0.5f);
        }

        /// <summary>
        /// Returns the center of the bounding volume of all active Center-Of-Universes.
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public static Vector3 GetCentroid(int groupId)
        {
            if (Multiverse.Count < 1) return Vector3.zero;
            if (!Multiverse.TryGetValue(new BoxedIntHash(groupId), out var tempList)) throw new UnityException("No group id '" + groupId + "' available.");

            var min = tempList[0].transform.position;
            var max = min;
            xMin = min.x;
            yMin = min.y;
            zMin = min.z;

            xMax = max.x;
            yMax = max.y;
            zMax = max.z;

            for (int i = 1; i < tempList.Count; i++)
            {
                Vector3 pos = tempList[i].transform.position;
                if (pos.x < xMin) xMin = pos.x;
                if (pos.y < yMin) yMin = pos.y;
                if (pos.z < zMin) zMin = pos.z;

                if (pos.x > xMax) xMax = pos.x;
                if (pos.y > yMax) yMax = pos.y;
                if (pos.z > zMax) zMax = pos.z;
            }

            min = new Vector3(xMin, yMin, zMin);
            max = new Vector3(xMax, yMax, zMax);
            return min + ((max - min) * 0.5f);
        }

        /// <summary>
        /// Returns the average position of all active Centers-Of-Universes.
        /// </summary>
        /// <returns></returns>
        public static Vector3 GetAveragePosition()
        {
            if (Multiverse.Count < 1) return Vector3.zero;
            var lists = Multiverse.ValuesNonAlloc;
            List<CenterOfUniverse> list;

            int tally = 0;
            var avg = Vector3.zero;

            for (int i = 0; i < Multiverse.Count; i++)
            {
                list = lists[i];
                for (int j = 0; j < list.Count; j++)
                {
                    avg += list[j].transform.position;
                    tally++;
                }
            }

            return avg / tally;
        }

        /// <summary>
        /// Returns the average position of all active Centers-Of-Universes.
        /// </summary>
        /// <returns></returns>
        public static Vector3 GetAveragePosition(int groupId)
        {
            if (Multiverse.Count < 1) return Vector3.zero;
            if (!Multiverse.TryGetValue(new BoxedIntHash(groupId), out var tempList)) throw new UnityException("No group id '" + groupId + "' available.");

            var avg = tempList[0].transform.position;
            for (int i = 1; i < tempList.Count; i++)
                avg += tempList[i].transform.position;
            
            return avg / tempList.Count;
        }

        

        #endregion


    }


    public class CoUSpawnedEvent : TargetMessage<CenterOfUniverse, CoURemovedEvent>, IBufferedMessage, IDeferredMessage
    {
        public CoUSpawnedEvent(CenterOfUniverse avatar) : base(avatar) { }
    }


    public class CoURemovedEvent : TargetMessage<CenterOfUniverse, CoURemovedEvent>
    {
        public CoURemovedEvent(CenterOfUniverse avatar) : base(avatar) { }
    }
}
