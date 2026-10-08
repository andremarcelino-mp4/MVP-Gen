using GenJutsu.AR;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Meta;

namespace GenJutsu.EditorTools
{
    public static class ARSetupBuilder
    {
        const string k_MaterialFolder = "Assets/GenJutsu/AR/Materials";
        const string k_GhostMatPath = k_MaterialFolder + "/GhostStructure.mat";
        const string k_RoomMatPath = k_MaterialFolder + "/ARRoom.mat";

        [MenuItem("GenJutsu/Setup AR Experience")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[GenJutsu] Exit Play Mode before setting up the AR experience.");
                return;
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            EnablePassthroughFeature();
            EnsureCameraClear();
            EnsureFolders();
            Material ghostMat = EnsureGhostMaterial();
            Material roomMat = EnsureRoomMaterial();
            GameObject session = EnsureARSession();
            ARCameraManager cameraManager = EnsureCameraManager();
            ARRoomController room = EnsureRoom(roomMat);
            GhostSystem ghostSystem;
            ARModeController modeController = EnsureSystems(ghostMat, room, cameraManager, out ghostSystem);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            Debug.Log("[GenJutsu] AR experience ready: passthrough feature=" + IsPassthroughEnabled()
                + ", session=" + (session != null)
                + ", cameraManager=" + (cameraManager != null)
                + ", room=" + (room != null)
                + ", ghostSystem=" + (ghostSystem != null)
                + ", modeController=" + (modeController != null));
        }

        static void EnablePassthroughFeature()
        {
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (settings == null)
            {
                Debug.LogError("[GenJutsu] OpenXR settings for Android not found.");
                return;
            }
            var sessionFeature = settings.GetFeature<ARSessionFeature>();
            if (sessionFeature != null && !sessionFeature.enabled)
            {
                sessionFeature.enabled = true;
                EditorUtility.SetDirty(sessionFeature);
                Debug.Log("[GenJutsu] Meta Quest: Session feature enabled for Android.");
            }
            var feature = settings.GetFeature<ARCameraFeature>();
            if (feature == null)
            {
                Debug.LogError("[GenJutsu] ARCameraFeature (Meta Quest: Camera Passthrough) not found.");
                return;
            }
            if (!feature.enabled)
            {
                feature.enabled = true;
                EditorUtility.SetDirty(feature);
                Debug.Log("[GenJutsu] Passthrough feature enabled for Android.");
            }
            if (sessionFeature != null || (feature != null && feature.enabled))
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }
        }

        static bool IsPassthroughEnabled()
        {
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (settings == null)
                return false;
            var feature = settings.GetFeature<ARCameraFeature>();
            return feature != null && feature.enabled;
        }

        static void EnsureCameraClear()
        {
            var cam = Camera.main;
            if (cam == null)
                return;
            if (cam.clearFlags == CameraClearFlags.SolidColor && cam.backgroundColor.a == 0f)
                return;
            Undo.RecordObject(cam, "AR Camera Clear");
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            EditorUtility.SetDirty(cam);
            Debug.Log("[GenJutsu] Main Camera clear set to SolidColor alpha 0 for passthrough validation.");
        }

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/GenJutsu/AR"))
                AssetDatabase.CreateFolder("Assets/GenJutsu", "AR");
            if (!AssetDatabase.IsValidFolder(k_MaterialFolder))
                AssetDatabase.CreateFolder("Assets/GenJutsu/AR", "Materials");
        }

        static Material EnsureGhostMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(k_GhostMatPath);
            var shader = Shader.Find("GenJutsu/GhostStructure");
            if (shader == null)
            {
                Debug.LogError("[GenJutsu] GhostStructure shader not found.");
                return existing;
            }
            Material mat;
            if (existing != null)
            {
                mat = existing;
            }
            else
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, k_GhostMatPath);
            }
            mat.SetColor("_BaseTint", Color.white);
            mat.SetColor("_GhostTint", new Color(0.3f, 0.8f, 1f, 1f));
            mat.SetFloat("_SolidMix", 0f);
            mat.SetColor("_RimColor", new Color(0.55f, 0.95f, 1f, 1f));
            mat.SetFloat("_RimPower", 3f);
            mat.SetFloat("_FillAlpha", 0.22f);
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        static Material EnsureRoomMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(k_RoomMatPath);
            if (existing != null)
                return existing;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                Debug.LogError("[GenJutsu] URP Unlit shader not found.");
                return null;
            }
            var mat = new Material(shader);
            mat.SetColor("_BaseColor", new Color(0.75f, 0.88f, 1f, 0.85f));
            AssetDatabase.CreateAsset(mat, k_RoomMatPath);
            AssetDatabase.SaveAssets();
            return mat;
        }

        static GameObject EnsureARSession()
        {
            var go = GameObject.Find("AR Session");
            if (go == null)
            {
                go = new GameObject("AR Session");
                Undo.RegisterCreatedObjectUndo(go, "Create AR Session");
            }
            if (go.GetComponent<ARSession>() == null)
                Undo.AddComponent<ARSession>(go);
            if (go.GetComponent<ARInputManager>() == null)
                Undo.AddComponent<ARInputManager>(go);
            return go;
        }

        static ARCameraManager EnsureCameraManager()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("[GenJutsu] Main Camera not found.");
                return null;
            }
            var manager = cam.GetComponent<ARCameraManager>();
            if (manager == null)
                manager = Undo.AddComponent<ARCameraManager>(cam.gameObject);
            manager.enabled = false;
            return manager;
        }

        static ARRoomController EnsureRoom(Material roomMat)
        {
            var root = GameObject.Find("ARRoom");
            if (root == null)
            {
                root = new GameObject("ARRoom");
                Undo.RegisterCreatedObjectUndo(root, "Create ARRoom");
            }
            root.transform.position = new Vector3(0f, 0f, -4f);

            var floor = EnsureQuad(root.transform, "Floor", new Vector3(0f, 0.01f, 0f), new Vector3(90f, 0f, 0f));
            var ceiling = EnsureQuad(root.transform, "Ceiling", new Vector3(0f, 2.5f, 0f), new Vector3(-90f, 0f, 0f));
            floor.GetComponent<MeshRenderer>().sharedMaterial = roomMat;
            ceiling.GetComponent<MeshRenderer>().sharedMaterial = roomMat;

            var room = root.GetComponent<ARRoomController>();
            if (room == null)
                room = Undo.AddComponent<ARRoomController>(root);

            var so = new SerializedObject(room);
            so.FindProperty("floorRenderer").objectReferenceValue = floor.GetComponent<MeshRenderer>();
            so.FindProperty("ceilingRenderer").objectReferenceValue = ceiling.GetComponent<MeshRenderer>();
            so.FindProperty("roomMaterialTemplate").objectReferenceValue = roomMat;
            so.ApplyModifiedPropertiesWithoutUndo();
            return room;
        }

        static GameObject EnsureQuad(Transform parent, string name, Vector3 localPos, Vector3 localEuler)
        {
            var child = parent.Find(name);
            GameObject quad;
            if (child == null)
            {
                quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Undo.RegisterCreatedObjectUndo(quad, "Create " + name);
                quad.name = name;
                quad.transform.SetParent(parent, false);
            }
            else
            {
                quad = child.gameObject;
            }
            quad.transform.localPosition = localPos;
            quad.transform.localRotation = Quaternion.Euler(localEuler);
            quad.transform.localScale = new Vector3(8f, 8f, 1f);
            var col = quad.GetComponent<Collider>();
            if (col != null)
                Object.DestroyImmediate(col);
            return quad;
        }

        static ARModeController EnsureSystems(Material ghostMat, ARRoomController room, ARCameraManager cameraManager, out GhostSystem ghostSystem)
        {
            ghostSystem = null;
            var systems = GameObject.Find("AR Systems");
            if (systems == null)
            {
                systems = new GameObject("AR Systems");
                Undo.RegisterCreatedObjectUndo(systems, "Create AR Systems");
            }

            ghostSystem = systems.GetComponent<GhostSystem>();
            if (ghostSystem == null)
                ghostSystem = Undo.AddComponent<GhostSystem>(systems);
            var ghostSo = new SerializedObject(ghostSystem);
            ghostSo.FindProperty("ghostMaterial").objectReferenceValue = ghostMat;
            ghostSo.FindProperty("tableSetRoot").objectReferenceValue = GameObject.Find("JapaneseTableSet")?.transform;
            ghostSo.ApplyModifiedPropertiesWithoutUndo();

            var modeController = systems.GetComponent<ARModeController>();
            if (modeController == null)
                modeController = Undo.AddComponent<ARModeController>(systems);

            var vrOnly = new GameObject[]
            {
                GameObject.Find("BackdropGround"),
                GameObject.Find("Environment"),
                GameObject.Find("Teleport Area Setup")
            };

            var rig = GameObject.Find("XR Origin Hands (XR Rig)");
            var gravity = rig != null ? rig.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity.GravityProvider>(true) : null;

            var modeSo = new SerializedObject(modeController);
            modeSo.FindProperty("targetCamera").objectReferenceValue = Camera.main;
            modeSo.FindProperty("cameraManager").objectReferenceValue = cameraManager;
            modeSo.FindProperty("ghostSystem").objectReferenceValue = ghostSystem;
            modeSo.FindProperty("roomController").objectReferenceValue = room;
            modeSo.FindProperty("gravityProvider").objectReferenceValue = gravity;
            modeSo.FindProperty("startInAR").boolValue = false;
            var vrArray = modeSo.FindProperty("vrOnlyObjects");
            vrArray.arraySize = vrOnly.Length;
            for (int i = 0; i < vrOnly.Length; i++)
                vrArray.GetArrayElementAtIndex(i).objectReferenceValue = vrOnly[i];
            modeSo.ApplyModifiedPropertiesWithoutUndo();

            return modeController;
        }
    }
}
