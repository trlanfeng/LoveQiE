using UnityEngine;

public enum GameState { Init, Ready, Play, Win, Faild, Complete }
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
    private float winTimer;
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
        LoadLevel(1);
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
        left.ResetAt(CurrentLevel.CellCenter(7, 10));
        right.ResetAt(CurrentLevel.CellCenter(9, 10));
        winTimer = 0; gameState = GameState.Play; return true;
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) { LoadLevel(CurrentScene > 0 ? CurrentScene : 1); return; }
        if (gameState == GameState.Win)
        {
            winTimer += Time.deltaTime;
            if (winTimer >= 1f)
            {
                if (CurrentScene < MaxScene) LoadLevel(CurrentScene + 1);
                else gameState = GameState.Complete;
            }
            return;
        }
        if (gameState != GameState.Play || CurrentLevel == null || left.IsMoving || right.IsMoving) return;
        if (ArePlayersAtGoal()) { gameState = GameState.Win; winTimer = 0; return; }
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
        return movedLeft || movedRight;
    }
    public bool ArePlayersAtGoal()
    {
        if (CurrentLevel == null || left == null || right == null) return false;
        Vector2Int a = CurrentLevel.WorldToIndex(left.transform.position);
        Vector2Int b = CurrentLevel.WorldToIndex(right.transform.position);
        return a.y == 1 && b.y == 1 && ((a.x == 7 && b.x == 9) || (a.x == 9 && b.x == 7));
    }
    private void OnGUI()
    {
        GUI.Label(new Rect(12, 8, 500, 24), "LEVEL " + CurrentScene + " / 99    WASD / Arrows: Move    R: Restart");
        if (gameState == GameState.Complete)
        {
            GUI.Box(new Rect(Screen.width / 2 - 140, Screen.height / 2 - 45, 280, 90), "All 99 levels completed!");
            if (GUI.Button(new Rect(Screen.width / 2 - 60, Screen.height / 2, 120, 30), "Play again")) LoadLevel(1);
        }
    }
    private void OnDestroy()
    {
        if (sceneInstance != null) Destroy(sceneInstance);
        CurrentLevel = null; TM = null;
    }
}
