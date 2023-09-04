//#define THROTTLE_DEBUG_TEST
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using FullInspector.Rotorz.ReorderableList;

namespace Peg.Systems.LoD.Editor
{
    /// <summary>
    /// Helper for displaying power modes using Rotorz
    /// </summary>
    class ModeAdaptor : IReorderableListAdaptor
    {
        static readonly float StdVertDist = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        static readonly float Indent = 15;

        LevelOfDetail Throttle;

        void MoveRect(ref Rect r, float x, float y)
        {
            r.position = new Vector2(r.x + x, r.y + y);
        }

        public ModeAdaptor(LevelOfDetail throttle)
        {
            Throttle = throttle;
        }

        public int Count { get { return Throttle.PowerModes.Length; } }

        public void Add()
        {
            ArrayUtility.Add(ref Throttle.PowerModes, new LevelOfDetail.Mode());
        }

        public bool CanDrag(int index)
        {
            return Throttle.PowerModes.Length > 1;
        }

        public bool CanRemove(int index)
        {
            return Throttle.PowerModes.Length > 0;
        }

        public void Clear()
        {
            ArrayUtility.Clear(ref Throttle.PowerModes);
        }

        public float GetItemHeight(int index)
        {
            var mode = Throttle.PowerModes[index];

            //title
            int count = 2;

            //dist thresh
            if (index != 0) count++;

            //scan rate
            count++;

            //tally of all keys
            count += mode.GameObjectKeys.Count;
            count += mode.BehaviourKeys.Count;
            count += mode.Collider3DKeys.Count;

            return StdVertDist * (float)count;
        }

        GUIStyle GOStyle;
        GUIStyle BehStyle;
        GUIStyle ColStyle;
        static void BuildStyle(ref GUIStyle style, GUIStyle baseStyle, Color normal, Color focus)
        {
            if (style == null)
            {
                style = new GUIStyle(baseStyle);
                style.normal.textColor = normal;
                style.onNormal.textColor = normal;
                style.hover.textColor = normal;
                style.onHover.textColor = normal;
                style.focused.textColor = focus;
                style.onFocused.textColor = focus;
                style.active.textColor = focus;
                style.onActive.textColor = focus;
            }
        }

        public void DrawItem(Rect position, int index)
        {
            var mode = Throttle.PowerModes[index];

            string title = string.Empty;
            if (index == 0) title = " (Highest Power)";
            else if (index == Throttle.PowerModes.Length - 1) title = " (Lowest Power)";

            Rect dt = position;
            dt.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.LabelField(dt, index.ToString() + title, EditorStyles.boldLabel);

            if (index != 0)
            {
                MoveRect(ref dt, 0, StdVertDist);
                mode.Distance = EditorGUI.FloatField(dt, "Distance Threshold", mode.Distance);
            }

            MoveRect(ref dt, 0, StdVertDist);
            mode.Frequency = EditorGUI.FloatField(dt, "Scan Rate", mode.Frequency);

            MoveRect(ref dt, Indent, 0);

            BuildStyle(ref GOStyle, EditorStyles.label, Color.black, new Color(.3f, .3f, .3f));
            foreach (var key in mode.GameObjectKeys)
            {
                if (key != null)
                {
                    MoveRect(ref dt, 0, StdVertDist);
                    bool state = EditorGUI.ToggleLeft(dt, key.name, mode.GetGameObjectState(key), GOStyle);
                    mode.SetGameObjectState(key, state);
                }
            }

            BuildStyle(ref BehStyle, EditorStyles.label, new Color(0, 0, .5f), new Color(0, 0, .75f));
            foreach (var key in mode.BehaviourKeys)
            {
                if (key != null)
                {
                    MoveRect(ref dt, 0, StdVertDist);
                    bool state = EditorGUI.ToggleLeft(dt, key.GetType().Name + " (" + key.name + ")", mode.GetBehaviourState(key), BehStyle);
                    mode.SetBehaviourState(key, state);
                }
            }

            BuildStyle(ref ColStyle, EditorStyles.label, new Color(0, .5f, 0), new Color(0, .75f, 0));
            foreach (var key in mode.Collider3DKeys)
            {
                if (key != null)
                {
                    MoveRect(ref dt, 0, StdVertDist);
                    bool state = EditorGUI.ToggleLeft(dt, key.GetType().Name + " (" + key.name + ")", mode.GetColliderState(key), ColStyle);
                    mode.SetColliderState(key, state);
                }
            }

            MoveRect(ref dt, -Indent, 0);
        }

        public void Duplicate(int index)
        {
            var src = Throttle.PowerModes[index];
            ArrayUtility.Insert(ref Throttle.PowerModes, index, new LevelOfDetail.Mode(src));
        }

        public void Insert(int index)
        {
            ArrayUtility.Insert(ref Throttle.PowerModes, index, new LevelOfDetail.Mode());
        }

        public void Move(int sourceIndex, int destIndex)
        {
            //we have to do this because Rotorz has a stupid way of deciding indexes
            if (destIndex > sourceIndex) destIndex--;

            var src = Throttle.PowerModes[sourceIndex];
            ArrayUtility.RemoveAt(ref Throttle.PowerModes, sourceIndex);
            ArrayUtility.Insert(ref Throttle.PowerModes, destIndex, src);
        }

        public void Remove(int index)
        {
            ArrayUtility.RemoveAt(ref Throttle.PowerModes, index);
        }
    }

    /// <summary>
    /// Editor for LevelOfDetail.
    /// </summary>
    [CustomEditor(typeof(LevelOfDetail))]
    public class LevelOfDetailEditor : UnityEditor.Editor
    {
        LevelOfDetail Throttle;
        SerializedProperty goProp;
        SerializedProperty behProp;
        SerializedProperty colProp;

        IReorderableListAdaptor ListAdaptor;


        void OnEnable()
        {
            if (target == null) return;
            Throttle = target as LevelOfDetail;
            goProp = this.serializedObject.FindProperty("GameObjects");
            behProp = this.serializedObject.FindProperty("Behaviours");
            colProp = this.serializedObject.FindProperty("Colliders3d");
            ListAdaptor = new ModeAdaptor(Throttle);
        }



        /// <summary>
        /// Dispplays a control for editing the HashedString object in an inspector.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="input"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static string HashedStringField(GUIContent content, HashedString input, params GUILayoutOption[] options)
        {
            string output = EditorGUILayout.TextField(content, input.Value);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("Hashed Value", input.Hash.ToString());
            EditorGUI.EndDisabledGroup();
            return output;
        }

        public override void OnInspectorGUI()
        {
            if (Throttle == null) return;

            serializedObject.Update();
            GUILayout.Space(10);
            Throttle.UseGroupId = EditorGUILayout.Toggle(new GUIContent("Use Group Id", "If set, this throttle will use a specific GroupId when checking distances to Center-Of-Universes."), Throttle.UseGroupId);
            if (Throttle.UseGroupId)
            {
                EditorGUI.indentLevel++;
                Throttle.GroupId.Value = HashedStringField(new GUIContent("Group Id", "The group id of the Center-Of-Univeres to be checked against."), Throttle.GroupId);
                EditorGUI.indentLevel--;
            }
            GUILayout.Space(15);
            EditorGUILayout.PropertyField(goProp, true);
            GUILayout.Space(15);
            EditorGUILayout.PropertyField(behProp, true);
            GUILayout.Space(15);
            EditorGUILayout.PropertyField(colProp, true);

            //ensure that we have at least one powermode to work with here.
            if (Throttle.PowerModes == null || Throttle.PowerModes.Length < 0)
                Throttle.PowerModes = new LevelOfDetail.Mode[1];

            for (int i = 0; i < Throttle.PowerModes.Length; i++)
            {
                if (Throttle.PowerModes[i] == null) Throttle.PowerModes[i] = new LevelOfDetail.Mode();
            }

            //First, we need to ensure that any GameObjects or Behaviours
            //not currently in our modes lists are appended so that they
            //can have their state displayed.
            if (Throttle.GameObjects != null)
            {
                foreach (var go in Throttle.GameObjects)
                {
                    if (go == null) continue;
                    foreach (var mode in Throttle.PowerModes)
                    {
                        if (!mode.GameObjectKeys.Contains(go)) mode.SetGameObjectState(go, false);
                    }

                }
            }

            if (Throttle.Behaviours != null)
            {
                foreach (var beh in Throttle.Behaviours)
                {
                    if (beh == null) continue;
                    foreach (var mode in Throttle.PowerModes)
                    {
                        if (!mode.BehaviourKeys.Contains(beh)) mode.SetBehaviourState(beh, false);
                    }
                }

                foreach (var col in Throttle.Colliders3d)
                {
                    if (goProp == null) continue;
                    foreach (var mode in Throttle.PowerModes)
                    {
                        if (!mode.Collider3DKeys.Contains(col)) mode.SetColliderState(col, false);
                    }
                }
            }


            //Next, we need to ensure we remove any old GameObjects
            //or Behaviours from our modes lists so that we have
            //properly synced information.
            foreach (var mode in Throttle.PowerModes)
            {
                //tally all gos to remove
                List<GameObject> gosToRemove = new List<GameObject>(1);
                if (Throttle.GameObjects != null)
                {
                    foreach (var go in mode.GameObjectKeys)
                    {
                        if (!Throttle.GameObjects.Contains(go)) gosToRemove.Add(go);
                    }
                }
                else gosToRemove = new List<GameObject>(mode.GameObjectKeys);

                //tally all behaviours to remove
                List<Behaviour> behToRemove = new List<Behaviour>(1);
                if (Throttle.Behaviours != null)
                {
                    foreach (var beh in mode.BehaviourKeys)
                    {
                        if (!Throttle.Behaviours.Contains(beh)) behToRemove.Add(beh);
                    }
                }
                else behToRemove = new List<Behaviour>(mode.BehaviourKeys);

                //tally all colliders to remove
                List<Collider> colToRemove = new List<Collider>(1);
                if (Throttle.Colliders3d != null)
                {
                    foreach (var col in mode.Collider3DKeys)
                    {
                        if (!Throttle.Colliders3d.Contains(col)) colToRemove.Add(col);
                    }
                }
                else colToRemove = new List<Collider>(mode.Collider3DKeys);

                foreach (var rem in gosToRemove)
                    mode.Remove(rem);

                foreach (var rem in behToRemove)
                    mode.Remove(rem);

                foreach (var col in colToRemove)
                    mode.Remove(col);
            }


            EditorGUI.BeginChangeCheck();

            GUILayout.Space(15);
            Throttle.DefaultModeIndex = EditorGUILayout.IntField("Default Power", Throttle.DefaultModeIndex);
            if (Throttle.DefaultModeIndex < 0) Throttle.DefaultModeIndex = 0;
            if (Throttle.DefaultModeIndex >= Throttle.PowerModes.Length) Throttle.DefaultModeIndex = Throttle.PowerModes.Length - 1;
#if THROTTLE_DEBUG_TEST && UNITY_EDITOR
            if(Application.isPlaying)
                LevelOfDetail.DebugDistance = EditorGUILayout.IntSlider(LevelOfDetail.DebugDistance, 0, 100);
#endif

            GUILayout.Space(15);
            ReorderableListGUI.ListField(ListAdaptor);
            GUILayout.Space(15);

            if (EditorGUI.EndChangeCheck() || GUI.changed) EditorUtility.SetDirty(target);

            serializedObject.ApplyModifiedProperties();
        }
    }
}