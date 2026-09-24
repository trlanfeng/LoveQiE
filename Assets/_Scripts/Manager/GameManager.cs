using UnityEngine;

public enum GameState { Init, Ready, Play, Win, Faild, Complete, Home, LevelSelect, Paused }
public class GameManager : MonoBehaviour
{
    public static TileManager TM;
    public static NativeLevel CurrentLevel { get; private set; }
    public GameObject charactorLeft;
    public GameObject charactorRight;
    public GameState gameState;
    public int CurrentScene { get; private set; }
    public const int MaxScene = 99;
    private GameObject sceneInstance;
    private CharactorManager left;
    private CharactorManager right;
    public float ElapsedSeconds { get; private set; }
    public int MoveCount { get; private set; }
    public int EarnedStars { get; private set; }
    public event System.Action StateChanged;
    private double startedAt;
    private float elapsedBeforePause;
    private GameUI ui;
    private void Start()
    {
        TM = GetComponent<TileManager>();
        if (charactorLeft == null || charactorRight == null)
        {
            Debug.LogError("Player references are missing from the main scene.");
            gameState = GameState.Faild; return;
        }
        left = charactorLeft.GetComponent<CharactorManager>();
        right = charactorRight.GetComponent<CharactorManager>();
        left.GM = right.GM = this;
        left.TM = right.TM = TM;
        ui = gameObject.AddComponent<GameUI>();
        ui.Initialize(this);
        ShowHome();
    }
    public bool LoadLevel(int number)
    {
        if (number < 1 || number > MaxScene || left == null || right == null) return false;
        GameObject prefab = Resources.Load<GameObject>("Maps/Scenes/Scene " + number);
        if (prefab == null || prefab.GetComponent<NativeLevel>() == null)
        {
            Debug.LogError("Native Tilemap level could not be loaded: " + number);
            gameState = GameState.Faild; return false;
        }
        if (sceneInstance != null) { sceneInstance.SetActive(false); Destroy(sceneInstance); }
        sceneInstance = Instantiate(prefab);
        // Normalize editor-only offsets so every level aligns with the persistent floor.
        sceneInstance.transform.position = new Vector3(0, 0, 1);
        CurrentLevel = sceneInstance.GetComponent<NativeLevel>();
        CityLevelVisuals.Apply(CurrentLevel);
        CurrentScene = number;
        charactorLeft.SetActive(true);
        charactorRight.SetActive(true);
        left.ResetAt(CurrentLevel.CellCenter(7, 10));
        right.ResetAt(CurrentLevel.CellCenter(9, 10));
        ElapsedSeconds = elapsedBeforePause = 0;
        MoveCount = EarnedStars = 0;
        startedAt = Time.realtimeSinceStartupAsDouble;
        SetState(GameState.Play);
        return true;
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (gameState == GameState.Play) Pause();
            else if (gameState == GameState.Paused) Resume();
            else if (gameState == GameState.LevelSelect) ShowHome();
            return;
        }
        if (Input.GetKeyDown(KeyCode.R) && (gameState == GameState.Play || gameState == GameState.Paused))
        { LoadLevel(CurrentScene); return; }
        if (gameState != GameState.Play || CurrentLevel == null) return;
        ElapsedSeconds = elapsedBeforePause + (float)(Time.realtimeSinceStartupAsDouble - startedAt);
        if (left.IsMoving || right.IsMoving) return;
        if (ArePlayersAtGoal()) { CompleteLevel(); return; }
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) TryMove(Vector2Int.up);
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) TryMove(Vector2Int.down);
        else if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) TryMove(Vector2Int.left);
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) TryMove(Vector2Int.right);
    }
    public bool TryMove(Vector2Int direction)
    {
        if (gameState != GameState.Play || left == null || right == null || left.IsMoving || right.IsMoving) return false;
        if (Mathf.Abs(direction.x) + Mathf.Abs(direction.y) != 1) return false;
        bool movedLeft = left.TryMove(direction);
        bool movedRight = right.TryMove(direction);
        if (movedLeft || movedRight) MoveCount++;
        return movedLeft || movedRight;
    }
    public bool ArePlayersAtGoal()
    {
        if (CurrentLevel == null || left == null || right == null) return false;
        Vector2Int a = CurrentLevel.WorldToIndex(left.transform.position);
        Vector2Int b = CurrentLevel.WorldToIndex(right.transform.position);
        return a.y == 1 && b.y == 1 && ((a.x == 7 && b.x == 9) || (a.x == 9 && b.x == 7));
    }
    public static int StarsForTime(float seconds) => seconds <= 30f ? 3 : seconds <= 60f ? 2 : 1;

    public int BestStars(int level) => PlayerPrefs.GetInt("LoveQiE.stars." + level, 0);

    private void CompleteLevel()
    {
        EarnedStars = StarsForTime(ElapsedSeconds);
        // Automated regression must not replace a player's real records.
        if (!System.Array.Exists(System.Environment.GetCommandLineArgs(), a => a == "-nativeSmoke"))
            SaveBestStars(CurrentScene, EarnedStars);
        SetState(CurrentScene == MaxScene ? GameState.Complete : GameState.Win);
    }
    public static void SaveBestStars(int level, int stars)
    {
        string key = "LoveQiE.stars." + level;
        PlayerPrefs.SetInt(key, Mathf.Max(PlayerPrefs.GetInt(key, 0), Mathf.Clamp(stars, 1, 3)));
        PlayerPrefs.Save();
    }
    public bool NextLevel()
    {
        return gameState == GameState.Win && CurrentScene < MaxScene && LoadLevel(CurrentScene + 1);
    }
    public void ShowHome() { LeaveLevel(); SetState(GameState.Home); }
    public void ShowLevelSelect() { LeaveLevel(); SetState(GameState.LevelSelect); }
    private void LeaveLevel()
    {
        Time.timeScale = 1;
        if (sceneInstance != null) { sceneInstance.SetActive(false); Destroy(sceneInstance); }
        sceneInstance = null;
        CurrentLevel = null;
        charactorLeft.SetActive(false);
        charactorRight.SetActive(false);
    }
    public void Pause()
    {
        if (gameState != GameState.Play) return;
        ElapsedSeconds = elapsedBeforePause + (float)(Time.realtimeSinceStartupAsDouble - startedAt);
        elapsedBeforePause = ElapsedSeconds;
        Time.timeScale = 0;
        SetState(GameState.Paused);
    }
    public void Resume()
    {
        if (gameState != GameState.Paused) return;
        startedAt = Time.realtimeSinceStartupAsDouble;
        Time.timeScale = 1;
        SetState(GameState.Play);
    }
    private void OnApplicationFocus(bool focused)
    {
        if (!focused && gameState == GameState.Play && !Application.isBatchMode
            && !System.Array.Exists(System.Environment.GetCommandLineArgs(), a => a == "-nativeSmoke")) Pause();
    }
    private void SetState(GameState state)
    {
        if (state == GameState.Play && Time.timeScale == 0) Time.timeScale = 1;
        gameState = state;
        StateChanged?.Invoke();
    }
    private void OnDestroy()
    {
        if (sceneInstance != null) Destroy(sceneInstance);
        CurrentLevel = null; TM = null;
        Time.timeScale = 1;
    }
}
