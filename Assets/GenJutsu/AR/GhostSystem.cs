using System.Collections.Generic;
using UnityEngine;

namespace GenJutsu.AR
{
    public class GhostSystem : MonoBehaviour
    {
        [SerializeField] Material ghostMaterial;
        [SerializeField] Transform tableSetRoot;

        readonly List<GhostObject> objects = new List<GhostObject>();
        readonly List<GhostObject> props = new List<GhostObject>();
        GhostObject table;
        Transform[] sources;
        public bool ArMode { get; private set; }

        void Awake()
        {
            if (tableSetRoot == null)
                tableSetRoot = GameObject.Find("JapaneseTableSet")?.transform;

            objects.Clear();
            props.Clear();
            table = null;

            if (tableSetRoot != null)
            {
                for (int i = 0; i < tableSetRoot.childCount; i++)
                {
                    var child = tableSetRoot.GetChild(i);
                    var ghost = child.GetComponent<GhostObject>();
                    if (ghost == null)
                        ghost = child.gameObject.AddComponent<GhostObject>();
                    ghost.IsTable = child.name == "Table";
                    ghost.SystemRef = this;
                    ghost.Initialize(ghostMaterial);
                    objects.Add(ghost);
                    if (ghost.IsTable)
                        table = ghost;
                    else
                        props.Add(ghost);
                }
            }

            for (int i = 0; i < props.Count; i++)
                props[i].TableRef = table;

            var sourceList = new List<Transform>();
            var cam = Camera.main;
            if (cam != null)
                sourceList.Add(cam.transform);
            var left = GameObject.Find("Left Controller");
            var right = GameObject.Find("Right Controller");
            if (left != null)
                sourceList.Add(left.transform);
            if (right != null)
                sourceList.Add(right.transform);
            sources = sourceList.ToArray();
        }

        void Update()
        {
            if (!ArMode)
                return;
            float dt = Time.deltaTime;
            for (int i = 0; i < objects.Count; i++)
                objects[i].Tick(sources, dt);
        }

        public bool AllPropsGhosted()
        {
            for (int i = 0; i < props.Count; i++)
            {
                if (props[i].State != GhostObject.GhostState.Ghost)
                    return false;
            }
            return true;
        }

        public void NotifyPropGhosted()
        {
        }

        public void SetMode(bool ar)
        {
            ArMode = ar;
            for (int i = 0; i < objects.Count; i++)
            {
                if (ar)
                    objects[i].ForceGhost();
                else
                    objects[i].ForceSolid();
            }
        }
    }
}
