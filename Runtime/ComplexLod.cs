using Peg.Collections;
using System;
using UnityEngine;

namespace Peg.Systems.LoD
{
    /// <summary>
    /// Represents a LoD list for GameObjects and behaviours whose active states are
    /// managed by a central LoDSystem.
    /// </summary>
    [AddComponentMenu("Peg/System/LoD/Complex LoD")]
    public class ComplexLod : MonoBehaviour
    {
        [Serializable]
        public class LodLevel
        {
            [Tooltip("Should these components be active when within the range of this lod level?")]
            public bool ActiveInRange = true;

            [Tooltip("The distance at which the active state will toggle for all behaviours and gameobjects in this lod level.")]
            public float DistanceThreshold;

            public GameObject[] GameObjects;

            public Behaviour[] Behaviours;
        }

        [Tooltip("The GameObjects and behaviours to manage with this LoD.")]
        public LodLevel[] LodLevels;

        /*
        /// <summary>
        /// 
        /// </summary>
        public void OnEnable()
        {
            LoDSystem.Instance.RegisterLod(this);
        }

        /// <summary>
        /// 
        /// </summary>
        public void OnDisable()
        {
            LoDSystem.Instance.UnregisterLod(this);
        }

        /// <summary>
        /// 
        /// </summary>
        public void LodEnable(int level)
        {
            var gos = GameObjects;
            for (int i = 0; i < gos.Length; i++)
                gos[i].SetActive(!InvertState);
        }

        /// <summary>
        /// 
        /// </summary>
        public void LodDisable(int level)
        {
            var gos = GameObjects;
            for (int i = 0; i < gos.Length; i++)
                gos[i].SetActive(InvertState);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="go"></param>
        public void AddGameObject(GameObject go, int level)
        {
            LodLevels[level].GameObjects = LodLevels[level].GameObjects.AddElement(go);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="go"></param>
        public void RemoveGameObject(GameObject go, int level)
        {
            LodLevels[level].GameObjects = LodLevels[level].GameObjects.RemoveElement(go);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="behaviour"></param>
        public void AddBehaviour(Behaviour behaviour, int level)
        {
            LodLevels[level].Behaviours = LodLevels[level].Behaviours.AddElement(behaviour);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="behaviour"></param>
        public void RemoveBehaviour(Behaviour behaviour, int level)
        {
            LodLevels[level].Behaviours = LodLevels[level].Behaviours.RemoveElement(behaviour);
        }
        */
    }
}
