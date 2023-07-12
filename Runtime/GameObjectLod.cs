using Toolbox;
using Toolbox.Collections;
using UnityEngine;

namespace PegGames.LoD
{
    /// <summary>
    /// Represents a LoD list for GameObjects whose active states are
    /// managed by a central LoDSystem.
    /// </summary>
    public class GameObjectLod : MonoBehaviour
    {
        public class SpawnedMsg : TargetMessage<GameObjectLod, SpawnedMsg>
        {
            public SpawnedMsg(GameObjectLod trackable) : base(trackable) { }
            public static SpawnedMsg Shared = new(null);
        }

        public class DespawnedMsg : TargetMessage<GameObjectLod, SpawnedMsg>
        {
            public DespawnedMsg(GameObjectLod trackable) : base(trackable) { }
            public static DespawnedMsg Shared = new(null);
        }

        [Tooltip("The distance threshold at which the GameObjects list is switched on.")]
        public float DistanceThresholdMin;

        [Tooltip("The distance threshold at which the GameObjects list is switched off.")]
        public float DistanceThresholdMax;

        [Tooltip("If true, the GameObjects in the list will have their state enabled when beyond the LoD distance and disabled otherwise.")]
        public bool InvertState = false;

        [Tooltip("The GameObjects to manage with this LoD.")]
        [SerializeField]
        GameObject[] GameObjects;

        //[Tooltip("The initial state of this LoD group. It should match the active state of the gameobjects in the list.")]
        //public bool Enabled = true;

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
            //if (Enabled) return;
            //Enabled = true;

            var gos = GameObjects;
            for (int i = 0; i < gos.Length; i++)
                gos[i].SetActive(!InvertState);
        }

        /// <summary>
        /// 
        /// </summary>
        public void LodDisable()
        {
            //if (!Enabled) return;
            //Enabled = false;

            var gos = GameObjects;
            for (int i = 0; i < gos.Length; i++)
                gos[i].SetActive(InvertState);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="go"></param>
        public void AddGameObject(GameObject go)
        {
            GameObjects = GameObjects.AddElement(go);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="go"></param>
        public void RemoveGameObject(GameObject go)
        {
            GameObjects = GameObjects.RemoveElement(go);
        }
    }
}
