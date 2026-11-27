using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hellgate.Editor
{
    public static class PlayerValidation
    {
        const string ScenePath = "Assets/Scenes/Graybox.unity";

        // Runs on a freshly opened scene and restores it afterwards. Never saves test movement.
        public static void RunBatch()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("RunBatch is for batch mode only.");
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            try { RunChecks(); }
            finally { EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single); }
        }

        static void RunChecks()
        {
            GrayboxValidation.ValidateCurrentScene();
            var player = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayer>();
            Require(player != null, "Player component missing");
            var controller = player.GetComponent<CharacterController>();
            var motor = player.GetComponent<PlayerMotor>();
            Require(controller != null && motor != null, "Movement components missing");
            Require(player.gameObject.layer == LayerMask.NameToLayer("Player"), "Incorrect player layer");
            Require(Camera.main != null && Camera.main.transform.parent == player.transform, "Camera must belong to player");
            Require(UnityEngine.Object.FindObjectsByType<AudioListener>().Length == 1, "Expected one AudioListener");
            var data = new SerializedObject(player);
            Require(data.FindProperty("inputActions").objectReferenceValue != null, "Missing input asset");
            Require(data.FindProperty("view").objectReferenceValue == Camera.main.transform, "Missing view reference");

            float Travel(Vector2 input, bool sprint, int fps)
            {
                Reset(new Vector3(0, 0.05f, 25));
                Vector3 start = player.transform.position;
                for (int i = 0; i < fps; i++) motor.Move(input, sprint, false, 1f / fps);
                return Vector3.Distance(start, player.transform.position);
            }

            void Reset(Vector3 position)
            {
                controller.enabled = false;
                player.transform.SetPositionAndRotation(position, Quaternion.identity);
                controller.enabled = true;
                motor.ResetMotion();
                Physics.SyncTransforms();
                for (int i = 0; i < 30; i++) motor.Move(Vector2.zero, false, false, 1f / 60);
                Require(controller.isGrounded, "Player did not settle on ground at " + position);
            }

            float straight = Travel(Vector2.up, false, 60);
            float diagonal = Travel(Vector2.one, false, 60);
            Require(straight > 6 && straight < 7.1f, "Walking speed unexpected: " + straight);
            Require(Mathf.Abs(straight - diagonal) < 0.1f, "Diagonal movement is faster");
            Require(Travel(Vector2.up, true, 60) > straight + 2, "Sprint is not faster");
            Require(Mathf.Abs(Travel(Vector2.up, false, 30) - Travel(Vector2.up, false, 120)) < 0.15f,
                "Movement varies too much with frame rate");

            Reset(new Vector3(0, 0.05f, 4));
            for (int i = 0; i < 420; i++) motor.Move(Vector2.up, false, false, 1f / 60);
            Require(player.transform.position.z > 48 && player.transform.position.y > -0.1f, "Cannot traverse the route");
            Reset(new Vector3(0, 0.05f, 4));
            for (int i = 0; i < 120; i++) motor.Move(Vector2.right, true, false, 1f / 60);
            Require(player.transform.position.x > 5 && player.transform.position.x < 5.8f, "Player passed through the entry wall");

            Reset(new Vector3(9.5f, 0.05f, 23));
            for (int i = 0; i < 110; i++) motor.Move(Vector2.up, false, false, 1f / 60);
            Require(player.transform.position.z > 33 && Mathf.Abs(player.transform.position.y - 2) < 0.1f, "Cannot climb ramp");

            Reset(new Vector3(0, 0.05f, 4));
            float apex = 0;
            for (int i = 0; i < 90; i++)
            {
                // Repeated requests while airborne must not add further upward impulses.
                motor.Move(Vector2.zero, false, i < 20, 1f / 60);
                apex = Mathf.Max(apex, player.transform.position.y);
            }
            Require(apex > 1 && apex < 1.3f, "Jump height or airborne jump gate incorrect: " + apex);
            Require(controller.isGrounded && Mathf.Abs(player.transform.position.y) < 0.1f, "Jump did not land");
            Debug.Log("HELLGATE Player PASS: prefab references, movement at 30/60/120 fps, diagonal speed, sprint, route, walls, ramp, jump and landing.");
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
