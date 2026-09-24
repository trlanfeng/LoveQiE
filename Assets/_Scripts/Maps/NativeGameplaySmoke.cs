using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

// Opt-in regression harness; inactive during normal play and omitted from release builds.
public sealed class NativeGameplaySmoke : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [Serializable] private class Solution { public int number; public bool solvable; public string steps; }
    [Serializable] private class Solutions { public Solution[] levels; }
    private static readonly List<string> errors = new List<string>();
    private string reportPath;
    private int passed;
    private int completedLevels;
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
        Check(manager != null && manager.gameState == GameState.Home && GameManager.CurrentLevel == null, "Main scene boots home without starting the clock");
        var ui = manager.GetComponent<GameUI>();
        Check(ui != null && ui.Root != null, "UI document is built");
        Check(!manager.TryMove(Vector2Int.up), "Home blocks gameplay input");
        yield return CaptureUI("01-home", manager);
        Click(ui, "start-game");
        Check(manager.gameState == GameState.LevelSelect, "Start button opens level selection");
        yield return CaptureUI("02-level-select", manager);
        for (int p = 0; p < 8; p++) Click(ui, "next-page");
        Check(ui.Root.Q<Button>("level-99") != null && !ui.Root.Q<Button>("next-page").enabledSelf, "Last page includes level 99 and disables next page");
        for (int p = 0; p < 8; p++) Click(ui, "previous-page");
        Click(ui, "level-1");
        Check(GameManager.CurrentLevel != null && manager.CurrentScene == 1 && manager.gameState == GameState.Play, "Level card starts chosen map");
        foreach (float seconds in new[] { 0f, 29.999f, 30f, 30.001f, 59.999f, 60f, 60.001f, 3600f })
            Check(GameManager.StarsForTime(seconds) == (seconds <= 30 ? 3 : seconds <= 60 ? 2 : 1), "Rating boundary " + seconds);
        const string scoreKey = "LoveQiE.stars.1000";
        bool hadScore = PlayerPrefs.HasKey(scoreKey);
        int originalScore = PlayerPrefs.GetInt(scoreKey);
        try
        {
            PlayerPrefs.DeleteKey(scoreKey);
            GameManager.SaveBestStars(1000, 2);
            GameManager.SaveBestStars(1000, 1);
            Check(manager.BestStars(1000) == 2, "Lower replay score preserves best rating");
            GameManager.SaveBestStars(1000, 3);
            Check(manager.BestStars(1000) == 3, "Higher rating is saved");
        }
        finally { if (hadScore) PlayerPrefs.SetInt(scoreKey, originalScore); else PlayerPrefs.DeleteKey(scoreKey); PlayerPrefs.Save(); }
        CheckCityVisuals();
        var left = manager.charactorLeft.GetComponent<CharactorManager>();
        var right = manager.charactorRight.GetComponent<CharactorManager>();
        Check(manager.TryMove(Vector2Int.right), "Start movement before pause");
        Click(ui, "pause-game");
        float pausedAt = manager.ElapsedSeconds;
        Vector3 pausedPosition = left.transform.position;
        yield return new WaitForSecondsRealtime(0.15f);
        Check(manager.gameState == GameState.Paused && manager.ElapsedSeconds == pausedAt && left.transform.position == pausedPosition, "Pause freezes timer and moving cars");
        Check(!manager.TryMove(Vector2Int.up), "Pause blocks movement");
        yield return CaptureUI("04-paused", manager);
        Click(ui, "resume-game");
        yield return new WaitForSeconds(0.3f);
        Check(manager.gameState == GameState.Play && manager.ElapsedSeconds > pausedAt, "Resume restarts clock");
        Click(ui, "restart-level");
        Check(manager.MoveCount == 0 && manager.ElapsedSeconds == 0, "Restart resets steps and timer");
        yield return CaptureUI("03-playing", manager);
        yield return CheckCars(manager, left, right);
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
        // Exercise real coroutines, collision lookup, timed results and explicit next-level buttons.
        Time.timeScale = 20;
        foreach (Solution solution in solutions.levels)
        {
            Check(manager.LoadLevel(solution.number), "Load level " + solution.number);
            // Move the real-time origin instead of waiting a minute for each rating case.
            float ratingElapsed = solution.number == 2 ? 40 : solution.number == 3 ? 70 : 0;
            typeof(GameManager).GetField("startedAt", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(manager, Time.realtimeSinceStartupAsDouble - ratingElapsed);
            CheckCityVisuals();
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
            while (manager.gameState == GameState.Play && Time.realtimeSinceStartup < deadline) yield return null;
            Check(manager.CurrentScene == solution.number && (manager.gameState == GameState.Win || manager.gameState == GameState.Complete), "Results wait for player choice " + solution.number);
            Check(ui.Root.Q<VisualElement>("results-dialog") != null, "Results popup is shown " + solution.number);
            Check(manager.EarnedStars == GameManager.StarsForTime(manager.ElapsedSeconds), "Popup rating agrees with completion time " + solution.number);
            if (solution.number <= 3)
            {
                Check(manager.EarnedStars == 4 - solution.number, "Real completion awards " + (4 - solution.number) + " stars");
                float finishedAt = manager.ElapsedSeconds;
                yield return new WaitForSecondsRealtime(0.1f);
                Check(manager.ElapsedSeconds == finishedAt && !manager.TryMove(Vector2Int.down), "Results freeze clock and block movement");
                yield return CaptureUI("05-result-" + manager.EarnedStars + "-stars", manager);
            }
            if (solution.number < 99)
            {
                Click(ui, "next-level");
                Check(manager.CurrentScene == solution.number + 1 && manager.gameState == GameState.Play && manager.MoveCount == 0 && manager.ElapsedSeconds == 0, "Next level button starts fresh level " + solution.number);
            }
            else
            {
                Check(manager.gameState == GameState.Complete && !manager.NextLevel(), "Final level never loads missing Scene 100");
                yield return CaptureUI("06-all-complete", manager);
                Click(ui, "next-level");
                Check(manager.gameState == GameState.LevelSelect, "Final result button returns to selection");
            }
            Debug.Log("NATIVE_SMOKE_LEVEL_PASS " + solution.number);
            completedLevels++;
        }
        Check(manager.LoadLevel(1), "Can replay after completing all levels");
        Time.timeScale = 1;
        yield return null;
        Check(FindObjectsOfType<NativeLevel>().Length == 1, "Old level instances released");
        Check(errors.Count == 0, "No runtime exceptions/errors");
        Finish(true);
    }
    private void Click(GameUI ui, string name)
    {
        var button = ui.Root.Q<Button>(name);
        Check(button != null && button.enabledInHierarchy, "UI button is available: " + name);
        using (var evt = NavigationSubmitEvent.GetPooled()) { evt.target = button; button.SendEvent(evt); }
    }
    private IEnumerator CaptureUI(string name, GameManager manager)
    {
        // Render the real UI Toolkit panel to an offscreen texture (batch-safe).
        var settings = manager.GetComponent<UIDocument>().panelSettings;
        var target = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        target.Create();
        settings.targetTexture = target;
        yield return null;
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        var previous = RenderTexture.active;
        RenderTexture.active = target;
        var overlay = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
        overlay.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        overlay.Apply();
        settings.targetTexture = null;
        Camera camera = Camera.main;
        var cameraTarget = new RenderTexture(Screen.width, Screen.height, 24);
        camera.targetTexture = cameraTarget;
        camera.Render();
        RenderTexture.active = cameraTarget;
        var background = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        background.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        background.Apply();
        var colors = overlay.GetPixels();
        var behind = background.GetPixels();
        for (int i = 0; i < colors.Length; i++) colors[i] = Color.Lerp(behind[i], colors[i], colors[i].a);
        overlay.SetPixels(colors); overlay.Apply();
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
        File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(reportPath), name + ".png"), overlay.EncodeToPNG());
        camera.targetTexture = null;
        RenderTexture.active = previous;
        Destroy(overlay); Destroy(background);
        target.Release(); cameraTarget.Release(); Destroy(target); Destroy(cameraTarget);
    }
    private void CheckCityVisuals()
    {
        var level = GameManager.CurrentLevel;
        var city = level.GetComponent<CityLevelVisuals>();
        Check(city != null && city.Roads != null && city.Sidewalks != null, "City theme loaded");
        var theme = Resources.Load<CityTileTheme>("City/CityTheme");
        for (int y = 0; y < level.rows; y++)
            for (int x = 0; x < level.columns; x++)
            {
                var cell = NativeLevel.ToCell(x, y);
                bool blocked = level.IsBlocked(x, y);
                if (blocked)
                {
                    int[] dx = { 0, 1, 0, -1, 1, 1, -1, -1 };
                    int[] dy = { -1, 0, 1, 0, -1, 1, 1, -1 };
                    int expected = 0;
                    for (int i = 0; i < 8; i++)
                        if (!level.IsBlocked(x + dx[i], y + dy[i])) expected |= 1 << i;
                    Check(city.Sidewalks.GetTile(cell) == theme.sidewalk
                        && city.Sidewalks.GetSprite(cell).name == "sidewalk_" + CitySidewalkRuleTile.NormalizeMask(expected).ToString("00"),
                        "Blob47 automatic sidewalk selection " + cell);
                }
                Check(city.Roads.HasTile(cell) == !blocked && city.Sidewalks.HasTile(cell) == blocked,
                    "City artwork agrees with collision cell " + cell);
                Check(blocked || (!city.Buildings.HasTile(cell) && !city.Decorations.HasTile(cell)),
                    "Drivable cells have no building or decorative obstruction");
            }
    }

    private IEnumerator CheckCars(GameManager manager, CharactorManager left, CharactorManager right)
    {
        var red = left.GetComponent<CarSpriteAnimator>();
        var green = right.GetComponent<CarSpriteAnimator>();
        Check(red != null && green != null && red.FrameCount == 4 && green.FrameCount == 4, "Both car frame sequences assigned");
        Check(left.GetComponent<SpriteRenderer>().sprite != right.GetComponent<SpriteRenderer>().sprite, "Cars use separate colored sprites");
        Check(red.Facing == Vector2.up && green.Facing == Vector2.up && !red.IsPlaying && !green.IsPlaying, "Cars spawn parked facing up");
        Sprite parked = left.GetComponent<SpriteRenderer>().sprite;
        Check(!manager.TryMove(Vector2Int.down) && !red.IsPlaying && !green.IsPlaying, "Blocked cars do not animate or turn");
        Check(manager.TryMove(Vector2Int.right), "Cars start horizontal drive");
        Check(red.IsPlaying && green.IsPlaying && red.Facing == Vector2.left && green.Facing == Vector2.right, "Both cars face their actual mirrored displacement");
        Check(Vector3.Dot(left.transform.up, Vector3.left) > 0.99f && Vector3.Dot(right.transform.up, Vector3.right) > 0.99f, "Car artwork rotates with facing");
        yield return new WaitForSeconds(0.065f);
        Check(red.FrameIndex > 0 && green.FrameIndex > 0 && left.GetComponent<SpriteRenderer>().sprite != parked, "Driving advances real sprite frames");
        yield return new WaitForSeconds(0.2f);
        Check(!red.IsPlaying && !green.IsPlaying && red.FrameIndex == 0 && green.FrameIndex == 0, "Completed moves return to parked frame");
        Check(red.Facing == Vector2.left && green.Facing == Vector2.right, "Parking preserves heading");
        Check(manager.TryMove(Vector2Int.left), "Cars drive back");
        Check(red.Facing == Vector2.right && green.Facing == Vector2.left, "Reverse horizontal input changes both headings");
        Check(manager.LoadLevel(1), "Restart during animation");
        Check(!left.IsMoving && !right.IsMoving && !red.IsPlaying && !green.IsPlaying && red.FrameIndex == 0, "Restart cancels movement and animation");
        Check(red.Facing == Vector2.up && green.Facing == Vector2.up, "Restart restores up facing");
        // The spawn has a wall above it. Place both cars on a clear vertical pair
        // so this check measures animation rather than the level's wall layout.
        NativeLevel level = GameManager.CurrentLevel;
        bool verticalLaneFound = false;
        for (int y = 2; y < level.rows - 1 && !verticalLaneFound; y++)
            for (int x = 1; x < level.columns - 1 && !verticalLaneFound; x++)
                if (!level.IsBlocked(x, y) && !level.IsBlocked(x, y - 1))
                {
                    left.ResetAt(level.CellCenter(x, y));
                    right.ResetAt(level.CellCenter(x, y));
                    verticalLaneFound = true;
                }
        Check(verticalLaneFound, "Clear vertical animation test lane exists");
        Check(manager.TryMove(Vector2Int.up), "Cars drive upward");
        Check(red.Facing == Vector2.up && green.Facing == Vector2.up, "Vertical direction is shared");
        yield return new WaitForSeconds(0.3f);
        Check(manager.TryMove(Vector2Int.down), "Cars drive downward");
        Check(red.Facing == Vector2.down && green.Facing == Vector2.down, "Both car noses face down");
        left.gameObject.SetActive(false);
        Check(!left.IsMoving && !red.IsPlaying, "Disabling car stops movement and animation");
        left.gameObject.SetActive(true);
        Check(red.FrameIndex == 0 && red.Facing == Vector2.up, "Reenabled car resets its pose");
        Check(manager.LoadLevel(1), "Restore level before full gameplay regression");
        yield return null;
    }
    private void Finish(bool success)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
        File.WriteAllText(reportPath, (success ? "PASS" : "FAIL") + ": " + passed + " checks; " + completedLevels + "/99 solutions executed through gameplay; errors=" + errors.Count + "\n" + string.Join("\n", errors));
        Debug.Log(success ? "NATIVE_GAMEPLAY_SMOKE_PASS" : "NATIVE_GAMEPLAY_SMOKE_FAIL");
        Application.Quit(success ? 0 : 1);
    }
#endif
}
