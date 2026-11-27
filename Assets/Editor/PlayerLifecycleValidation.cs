using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Hellgate.Editor
{
    // Batch lifecycle/input check. Physical input and the OS cursor need manual testing.
    [InitializeOnLoad]
    public static class PlayerLifecycleValidation
    {
        const string Active = "Hellgate.PlayerValidation.Active";
        const string Cycles = "Hellgate.PlayerValidation.Cycles";
        static double entered;
        static bool checkedCycle;

        static PlayerLifecycleValidation()
        {
            if (!SessionState.GetBool(Active, false)) return;
            EditorApplication.playModeStateChanged += Changed;
            EditorApplication.update += Tick;
        }

        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Lifecycle validation is for batch mode only.");
            SessionState.SetBool(Active, true);
            SessionState.SetInt(Cycles, 0);
            EditorSceneManager.OpenScene("Assets/Scenes/Graybox.unity", OpenSceneMode.Single);
            EditorApplication.playModeStateChanged -= Changed;
            EditorApplication.playModeStateChanged += Changed;
            EditorApplication.EnterPlaymode();
        }

        static void Changed(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                Application.runInBackground = true;
                entered = EditorApplication.timeSinceStartup;
                checkedCycle = false;
            }
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                int cycles = SessionState.GetInt(Cycles, 0) + 1;
                SessionState.SetInt(Cycles, cycles);
                if (cycles < 2) EditorApplication.EnterPlaymode();
                else
                {
                    SessionState.SetBool(Active, false);
                    Debug.Log("HELLGATE Player lifecycle PASS: two Play/Stop cycles, fresh spawn, one player/listener, cloned actions and keyboard Move/Jump bindings.");
                    EditorApplication.Exit(0);
                }
            }
        }

        static void Tick()
        {
            if (!EditorApplication.isPlaying || checkedCycle || EditorApplication.timeSinceStartup - entered < 0.5) return;
            checkedCycle = true;
            try
            {
                var players = UnityEngine.Object.FindObjectsByType<Hellgate.FirstPersonPlayer>();
                if (players.Length != 1 || !players[0].enabled) throw new Exception("Expected one enabled player");
                if (UnityEngine.Object.FindObjectsByType<AudioListener>().Length != 1) throw new Exception("Duplicate listener");
                if (Vector3.Distance(players[0].transform.position, new Vector3(0, 0, 4)) > 0.1f) throw new Exception("Spawn did not reset");
                var owned = (InputActionAsset)typeof(Hellgate.FirstPersonPlayer)
                    .GetField("ownedActions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(players[0]);
                if (owned == null || EditorUtility.IsPersistent(owned)) throw new Exception("Input asset must be a runtime copy");
                var move = owned.FindAction("Player/Move", true);
                var jump = owned.FindAction("Player/Jump", true);
                InputSystem.settings = UnityEngine.Object.Instantiate(InputSystem.settings);
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
                var keyboard = InputSystem.AddDevice<Keyboard>();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.Space));
                InputSystem.Update();
                if (move.ReadValue<Vector2>().y < 0.9f || !jump.IsPressed()) throw new Exception("Keyboard bindings did not resolve; enabled=" + move.enabled + ", controls=" + move.controls.Count + ", W=" + keyboard.wKey.isPressed + ", Move=" + move.ReadValue<Vector2>() + ", Jump=" + jump.IsPressed());
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                if (move.ReadValue<Vector2>() != Vector2.zero || jump.IsPressed()) throw new Exception("Input release did not reset actions");
                InputSystem.RemoveDevice(keyboard);
                Debug.Log("HELLGATE Play cycle verified: " + (SessionState.GetInt(Cycles, 0) + 1));
                EditorApplication.ExitPlaymode();
            }
            catch (Exception exception)
            {
                SessionState.SetBool(Active, false);
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }
    }
}
