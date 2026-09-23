using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Opt-in regression harness; inactive during normal play and omitted from release builds.
public sealed class NativeGameplaySmoke : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [Serializable] private class Solution { public int number; public bool solvable; public string steps; }
    [Serializable] private class Solutions { public Solution[] levels; }
    private static readonly List<string> errors = new List<string>();
    private string reportPath;
    private int passed;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (!Environment.GetCommandLineArgs().Contains("-nativeSmoke")) return;
        new GameObject("Native gameplay regression").AddComponent<NativeGameplaySmoke>();
    }
    private void OnEnable() { Application.logMessageReceived += CaptureError; }
    private void OnDisable() { Application.logMessageReceived -= CaptureError; }
    private static void CaptureError(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message + "\n" + stack);
    }
    private void Check(bool condition, string description)
    {
        if (condition) { passed++; return; }
        errors.Add(description);
        Finish(false);
        throw new Exception(description);
    }
    private IEnumerator Start()
    {
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-smokeReport");
        reportPath = index >= 0 && index + 1 < args.Length ? args[index + 1] : Path.Combine(Application.persistentDataPath, "native-smoke.txt");
        yield return null;
        var manager = FindObjectOfType<GameManager>();
        Check(manager != null && GameManager.CurrentLevel != null && manager.CurrentScene == 1, "Main scene boots level 1");
        var left = manager.charactorLeft.GetComponent<CharactorManager>();
        var right = manager.charactorRight.GetComponent<CharactorManager>();
        Check(left.moveDirection == -1 && right.moveDirection == 1, "Original mirrored controls retained");
        Vector3 leftStart = left.transform.position, rightStart = right.transform.position;
        Check(!manager.TryMove(Vector2Int.down), "Bottom wall must block both players");
        Check(left.transform.position == leftStart && right.transform.position == rightStart, "Blocked movement stays still");
        Check(!manager.TryMove(new Vector2Int(1, 1)), "Diagonal command rejected");
        Check(manager.TryMove(Vector2Int.right), "Horizontal movement accepted");
        Check(!manager.TryMove(Vector2Int.right), "Movement cannot overlap itself");
        yield return new WaitForSeconds(0.3f);
        Check(GameManager.CurrentLevel.WorldToIndex(left.transform.position) == new Vector2Int(6, 10), "Left player mirrors right input");
        Check(GameManager.CurrentLevel.WorldToIndex(right.transform.position) == new Vector2Int(10, 10), "Right player follows right input");
        Check(manager.LoadLevel(1), "Restart level");
        Check(left.transform.position == leftStart && right.transform.position == rightStart, "Restart restores spawn cells");
        Check(!manager.LoadLevel(0) && !manager.LoadLevel(100), "Invalid level number rejected");
        yield return new WaitForEndOfFrame();
        string screenshot = Path.Combine(Path.GetDirectoryName(reportPath), "runtime-scene-1.png");
        // Explicit camera render also works in the batch player, where the backbuffer can be black.
        Camera camera = Camera.main;
        var target = new RenderTexture(1020, 768, 24);
        camera.targetTexture = target;
        camera.Render();
        RenderTexture.active = target;
        var pixels = new Texture2D(1020, 768, TextureFormat.RGB24, false);
        pixels.ReadPixels(new Rect(0, 0, 1020, 768), 0, 0);
        pixels.Apply();
        File.WriteAllBytes(screenshot, pixels.EncodeToPNG());
        camera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(pixels); target.Release(); Destroy(target);
        var solutions = JsonUtility.FromJson<Solutions>(Resources.Load<TextAsset>("Maps/solutions").text);
        // Exercise real coroutines, collision lookup, goal detection and automatic level switching.
        Time.timeScale = 20;
        foreach (Solution solution in solutions.levels)
        {
            Check(manager.LoadLevel(solution.number), "Load level " + solution.number);
            yield return null;
            Check(!GameManager.CurrentLevel.IsBlocked(7, 10) && !GameManager.CurrentLevel.IsBlocked(9, 10), "Spawn cells clear " + solution.number);
            foreach (char key in solution.steps)
            {
                Vector2Int input = key == 'W' ? Vector2Int.up : key == 'S' ? Vector2Int.down : key == 'A' ? Vector2Int.left : Vector2Int.right;
                Check(manager.TryMove(input), "Solution move accepted level " + solution.number + " key " + key);
                while (left.IsMoving || right.IsMoving) yield return null;
                Check(!GameManager.CurrentLevel.IsBlocked(GameManager.CurrentLevel.WorldToIndex(left.transform.position).x, GameManager.CurrentLevel.WorldToIndex(left.transform.position).y), "Left player never enters obstacle");
                Check(!GameManager.CurrentLevel.IsBlocked(GameManager.CurrentLevel.WorldToIndex(right.transform.position).x, GameManager.CurrentLevel.WorldToIndex(right.transform.position).y), "Right player never enters obstacle");
            }
            Check(manager.ArePlayersAtGoal(), "Both players reach goal " + solution.number);
            float deadline = Time.realtimeSinceStartup + 5;
            while (manager.CurrentScene == solution.number && manager.gameState != GameState.Complete && Time.realtimeSinceStartup < deadline) yield return null;
            if (solution.number < 99) Check(manager.CurrentScene == solution.number + 1, "Automatic next level " + solution.number);
            else Check(manager.gameState == GameState.Complete, "Final level completes without loading missing Scene 100");
            Debug.Log("NATIVE_SMOKE_LEVEL_PASS " + solution.number);
        }
        Check(manager.LoadLevel(1), "Can replay after completing all levels");
        Time.timeScale = 1;
        yield return null;
        Check(FindObjectsOfType<NativeLevel>().Length == 1, "Old level instances released");
        Check(errors.Count == 0, "No runtime exceptions/errors");
        Finish(true);
    }
    private void Finish(bool success)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
        File.WriteAllText(reportPath, (success ? "PASS" : "FAIL") + ": " + passed + " checks; all 99 solutions executed through gameplay; errors=" + errors.Count + "\n" + string.Join("\n", errors));
        Debug.Log(success ? "NATIVE_GAMEPLAY_SMOKE_PASS" : "NATIVE_GAMEPLAY_SMOKE_FAIL");
        Application.Quit(success ? 0 : 1);
    }
#endif
}
