using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hellgate.Editor
{
    // Verifica geometria, collisioni e riferimenti della scena graybox.
    public static class GrayboxValidation
    {
        const string ScenePath = "Assets/Scenes/Graybox.unity";

        public static void RunBatch()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidateCurrentScene();
        }

        [MenuItem("HELLGATE/Validate Graybox")]
        public static void ValidateCurrentScene()
        {
            if (SceneManager.sceneCount != 1 || SceneManager.GetActiveScene().path != ScenePath)
                throw new InvalidOperationException("Open only Assets/Scenes/Graybox.unity before validating.");

            Physics.SyncTransforms();
            // Sample floor continuity and space for a 1.8 m tall, 0.6 m wide character.
            for (float z = 1; z <= 51; z += 0.25f)
            {
                RequireSurface(new Vector3(0, 0.5f, z), 0);
                if (Physics.CheckCapsule(new Vector3(0, 0.35f, z), new Vector3(0, 1.55f, z), 0.3f))
                    throw new InvalidOperationException("Blocked main route at z=" + z);
            }

            foreach (float z in new[] { 24.25f, 26f, 28f, 30f, 31.75f, 32f, 32.25f, 36f, 39.75f })
                RequireSurface(new Vector3(9.5f, 5, z), z < 32 ? (z - 24) / 4 : 2);

            // Every outer wall must stop a horizontal ray from inside its room.
            CheckRoomWalls(6, 6);
            CheckRoomWalls(16, 2);
            CheckRoomWalls(32, 12);
            CheckRoomWalls(48, 4);
            RequireWall(new Vector3(0, 2, 1), Vector3.back, 1);
            RequireWall(new Vector3(0, 2, 51), Vector3.forward, 1);

            var geometry = GameObject.Find("Graybox Geometry");
            if (geometry == null) throw new InvalidOperationException("Missing graybox geometry root.");
            foreach (var renderer in geometry.GetComponentsInChildren<MeshRenderer>(true))
            {
                var collider = renderer.GetComponent<BoxCollider>();
                if (collider == null || !collider.enabled || collider.isTrigger || !renderer.gameObject.activeInHierarchy)
                    throw new InvalidOperationException("Non-solid geometry: " + renderer.name);
            }
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) > 0)
                        throw new InvalidOperationException("Missing script: " + transform.name);
                    var renderer = transform.GetComponent<MeshRenderer>();
                    if (renderer != null && (renderer.sharedMaterial == null || renderer.sharedMaterial.shader == null))
                        throw new InvalidOperationException("Missing material: " + transform.name);
                }

            Debug.Log("HELLGATE Graybox PASS: route clearance, floor continuity, ramp/platform, walls, colliders and asset references.");
        }

        static void RequireSurface(Vector3 origin, float expectedHeight)
        {
            if (!Physics.Raycast(origin, Vector3.down, out var hit, 6, ~0, QueryTriggerInteraction.Ignore)
                || Mathf.Abs(hit.point.y - expectedHeight) > 0.02f)
                throw new InvalidOperationException("Floor height mismatch below " + origin);
        }

        static void CheckRoomWalls(float z, float halfWidth)
        {
            RequireWall(new Vector3(0, 2.5f, z), Vector3.left, halfWidth);
            RequireWall(new Vector3(0, 2.5f, z), Vector3.right, halfWidth);
        }

        static void RequireWall(Vector3 origin, Vector3 direction, float distance)
        {
            if (!Physics.Raycast(origin, direction, out var hit, distance + 0.1f, ~0, QueryTriggerInteraction.Ignore)
                || Mathf.Abs(hit.distance - distance) > 0.02f)
                throw new InvalidOperationException("Wall mismatch from " + origin + " toward " + direction);
        }
    }
}
