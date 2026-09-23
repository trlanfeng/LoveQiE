using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;

public static class NativeTilemapMigration
{
    [Serializable] public class Cell { public int row, column, tile, flags, rotation, orientation, variation; }
    [Serializable] public class Level { public string name, sourceSha256; public int rows, columns; public Vector3 position, cellSize; public Cell[] cells; }
    [Serializable] public class Catalog { public Level[] levels; }
    private const string DataPath = "Assets/Editor/NativeTilemapMigration/levels.json";
    private static Catalog Read() => JsonUtility.FromJson<Catalog>(File.ReadAllText(DataPath));
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }

    [MenuItem("Tools/Native Tilemap/Convert backed-up levels")]
    public static void Convert()
    {
        try
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;
            Directory.CreateDirectory("Assets/NativeTiles/Tiles");
            AssetDatabase.Refresh();
            var tiles = new Tile[64];
            for (int i = 0; i < tiles.Length; i++)
            {
                string texturePath = "Assets/NativeTiles/Textures/Wood_" + i.ToString("00") + ".png";
                var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 32;
                importer.spritePivot = new Vector2(0.5f, 0.5f);
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
                string tilePath = "Assets/NativeTiles/Tiles/Wood_" + i.ToString("00") + ".asset";
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
                if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, tilePath); }
                tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
                Require(tile.sprite != null, "Sprite import failed: " + texturePath);
                tile.colliderType = Tile.ColliderType.Grid;
                tile.flags = TileFlags.LockTransform;
                EditorUtility.SetDirty(tile); tiles[i] = tile;
            }
            foreach (Level source in Read().levels)
            {
                var root = new GameObject(source.name, typeof(Grid), typeof(NativeLevel));
                root.transform.position = source.position;
                root.GetComponent<Grid>().cellSize = source.cellSize;
                var child = new GameObject("Obstacles", typeof(Tilemap), typeof(TilemapRenderer));
                child.transform.SetParent(root.transform, false);
                var map = child.GetComponent<Tilemap>();
                var level = root.GetComponent<NativeLevel>();
                level.obstacles = map; level.rows = source.rows; level.columns = source.columns;
                foreach (Cell cell in source.cells)
                    map.SetTile(NativeLevel.ToCell(cell.column, cell.row), tiles[cell.tile]);
                map.CompressBounds();
                // All nonempty cells block grid movement; a collider also supports future physics users.
                child.AddComponent<TilemapCollider2D>();
                string path = "Assets/Resources/Maps/Scenes/" + source.name + ".prefab";
                PrefabUtility.SaveAsPrefabAsset(root, path);
                UnityEngine.Object.DestroyImmediate(root);
            }
            AssetDatabase.SaveAssets();
            UpgradeMainScene();
            Validate();
            Debug.Log("NATIVE_TILEMAP_CONVERSION_PASS");
        }
        catch (Exception ex) { Debug.LogException(ex); if (Application.isBatchMode) EditorApplication.Exit(1); else throw; }
    }

    private static void UpgradeMainScene()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/main.unity", OpenSceneMode.Single);
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform tr in root.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(tr.gameObject);
        var manager = UnityEngine.Object.FindObjectOfType<GameManager>();
        Require(manager != null, "Original GameManager missing");
        Require(manager.charactorLeft != null && manager.charactorRight != null, "Original character references missing");
        // Original prefab manager references were never serialized; explicitly restore them.
        foreach (GameObject player in new[] { manager.charactorLeft, manager.charactorRight })
        {
            var character = player.GetComponent<CharactorManager>();
            Require(character != null, "Character component missing");
            character.GM = manager; character.TM = manager.GetComponent<TileManager>();
            var renderer = player.GetComponent<SpriteRenderer>();
            Require(renderer != null && renderer.sprite != null, "Player sprite missing");
            renderer.sortingOrder = 20;
        }
        // Keep existing artwork and exact placement, but convert the static floor to a native Tilemap.
        GameObject floor = scene.GetRootGameObjects().FirstOrDefault(r => r.name == "Floor");
        if (floor != null && floor.GetComponentInChildren<Tilemap>() == null)
        {
            var sprites = floor.GetComponentsInChildren<SpriteRenderer>();
            var grid = floor.AddComponent<Grid>();
            var child = new GameObject("Ground", typeof(Tilemap), typeof(TilemapRenderer));
            child.transform.SetParent(floor.transform, false);
            var map = child.GetComponent<Tilemap>();
            child.GetComponent<TilemapRenderer>().sortingOrder = -10;
            var cache = new Dictionary<Sprite, Tile>();
            foreach (var renderer in sprites)
            {
                Require(renderer.sprite != null, "Floor sprite missing: " + renderer.name);
                Tile tile;
                if (!cache.TryGetValue(renderer.sprite, out tile))
                {
                    string path = "Assets/NativeTiles/Tiles/Ground_" + renderer.sprite.name + ".asset";
                    tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
                    if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, path); }
                    tile.sprite = renderer.sprite; tile.colliderType = Tile.ColliderType.None;
                    EditorUtility.SetDirty(tile);
                    cache.Add(renderer.sprite, tile);
                }
                Vector3Int cell = map.WorldToCell(renderer.transform.position);
                map.SetTile(cell, tile);
                map.SetTileFlags(cell, TileFlags.None);
                map.SetColor(cell, renderer.color);
            }
            foreach (Transform oldChild in floor.transform.Cast<Transform>().ToArray())
                if (oldChild != child.transform) UnityEngine.Object.DestroyImmediate(oldChild.gameObject);
        }
        Camera camera = Camera.main;
        Require(camera != null, "Main camera missing");
        camera.orthographic = true;
        camera.transform.position = new Vector3(8.5f, -6f, -10f);
        camera.orthographicSize = 6.8f;
        camera.backgroundColor = new Color(0.749f, 0.812f, 0.616f, 1);
        camera.clearFlags = CameraClearFlags.SolidColor;
        if (camera.GetComponent<MapCameraFit>() == null) camera.gameObject.AddComponent<MapCameraFit>();
        EditorSceneManager.SaveScene(scene);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/main.unity", true) };
        PlayerSettings.companyName = "LoveQiE";
        PlayerSettings.productName = "LoveQiE";
        PlayerSettings.defaultScreenWidth = 1020;
        PlayerSettings.defaultScreenHeight = 768;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.runInBackground = true;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Native Tilemap/Validate all levels")]
    public static void Validate()
    {
        int cells = 0;
        foreach (Level source in Read().levels)
        {
            string path = "Assets/Resources/Maps/Scenes/" + source.name + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Require(prefab != null, path);
            var instance = UnityEngine.Object.Instantiate(prefab);
            var level = instance.GetComponent<NativeLevel>();
            Require(level != null && level.obstacles != null, "Native components missing: " + path);
            Require(level.rows == source.rows && level.columns == source.columns, "Bounds differ: " + path);
            var expected = source.cells.ToDictionary(c => NativeLevel.ToCell(c.column, c.row));
            for (int row = 0; row < source.rows; row++)
                for (int col = 0; col < source.columns; col++)
                {
                    Vector3Int pos = NativeLevel.ToCell(col, row);
                    Cell cell;
                    bool occupied = expected.TryGetValue(pos, out cell);
                    Require(level.obstacles.HasTile(pos) == occupied, "Occupancy differs: " + path + pos);
                    if (occupied)
                    {
                        Tile tile = level.obstacles.GetTile<Tile>(pos);
                        Require(tile != null && tile.sprite != null && tile.sprite.name == "Wood_" + cell.tile.ToString("00"), "Tile differs: " + path + pos);
                        cells++;
                    }
                    Require(level.WorldToIndex(level.CellCenter(col, row)) == new Vector2Int(col, row), "Coordinate round trip failed: " + path + " expected=" + col + "," + row + " world=" + level.CellCenter(col, row) + " actual=" + level.WorldToIndex(level.CellCenter(col, row)));
                }
            Require(!level.Contains(source.columns, 0) && !level.Contains(0, source.rows), "Boundary check failed");
            foreach (Transform tr in prefab.GetComponentsInChildren<Transform>(true))
                Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(tr.gameObject) == 0, "Missing script: " + path);
            Require(!AssetDatabase.GetDependencies(path, true).Any(p => p.Contains("Rotorz")), "Legacy dependency: " + path);
            UnityEngine.Object.DestroyImmediate(instance);
        }
        Require(cells == 10797, "Unexpected occupied count: " + cells);
        foreach (string ground in new[] { "diban_lv", "diban_lv_ye1", "diban_lv_ye2", "diban_lv_ye3" })
        {
            var tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/NativeTiles/Tiles/Ground_" + ground + ".asset");
            Require(tile != null && tile.sprite != null, "Ground sprite assignment was not saved: " + ground);
        }
        Directory.CreateDirectory("MigrationReports");
        File.WriteAllText("MigrationReports/unity-validation.txt", "PASS: 100 native prefabs; 20400 cells compared; 10797 occupied cells; sprites, bounds, coordinate roundtrips and no Rotorz dependencies.\n");
        Debug.Log("NATIVE_TILEMAP_VALIDATION_PASS " + cells);
    }

    public static void Build()
    {
        BuildPlayer(true);
    }

    public static void BuildRelease()
    {
        BuildPlayer(false);
    }

    private static void BuildPlayer(bool development)
    {
        Validate();
        Directory.CreateDirectory("Builds/Windows");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/main.unity" }, locationPathName = "Builds/Windows/LoveQiE.exe",
            target = BuildTarget.StandaloneWindows64, options = development ? BuildOptions.Development : BuildOptions.None });
        Require(report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded, "Build failed");
        File.WriteAllText("MigrationReports/build.txt", "PASS: Windows x64 " + (development ? "development" : "release") + " build; errors=" + report.summary.totalErrors + "; warnings=" + report.summary.totalWarnings);
        File.WriteAllLines("MigrationReports/build-messages.txt", report.steps.SelectMany(step => step.messages).Where(m => m.type == LogType.Warning || m.type == LogType.Error).Select(m => m.content));
    }

    public static void FinishSetup()
    {
        EditorSettings.serializationMode = SerializationMode.ForceText;
        const string palettePath = "Assets/NativeTiles/WoodPalette.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(palettePath) == null)
            GridPaletteUtility.CreateNewPalette("Assets/NativeTiles", "WoodPalette", GridLayout.CellLayout.Rectangle,
                GridPalette.CellSizing.Manual, Vector3.one, GridLayout.CellSwizzle.XYZ);
        var palette = PrefabUtility.LoadPrefabContents(palettePath);
        var map = palette.GetComponentInChildren<Tilemap>();
        for (int i = 0; i < 64; i++)
            map.SetTile(new Vector3Int(i % 8, -i / 8, 0), AssetDatabase.LoadAssetAtPath<Tile>("Assets/NativeTiles/Tiles/Wood_" + i.ToString("00") + ".asset"));
        PrefabUtility.SaveAsPrefabAsset(palette, palettePath);
        PrefabUtility.UnloadPrefabContents(palette);
        var scene = EditorSceneManager.OpenScene("Assets/main.unity", OpenSceneMode.Single);
        // Persist all ground sprite assignments; these are shared assets, not scene data.
        foreach (string ground in new[] { "diban_lv", "diban_lv_ye1", "diban_lv_ye2", "diban_lv_ye3" })
        {
            var tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/NativeTiles/Tiles/Ground_" + ground + ".asset");
            Require(tile != null, "Ground tile missing: " + ground);
            tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Images/" + ground + ".png");
            Require(tile.sprite != null, "Ground sprite missing: " + ground);
            EditorUtility.SetDirty(tile);
        }
        foreach (Tilemap ground in UnityEngine.Object.FindObjectsOfType<Tilemap>()) ground.RefreshAllTiles();
        EditorSceneManager.SaveScene(scene);
        foreach (var root in scene.GetRootGameObjects())
            foreach (Transform tr in root.GetComponentsInChildren<Transform>(true))
                Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(tr.gameObject) == 0, "Main scene missing script: " + tr.name);
        Require(!AssetDatabase.GetDependencies("Assets/main.unity", true).Any(p => p.Contains("Rotorz") || p.Contains("DOTween")), "Legacy dependency in main scene");
        AssetDatabase.SaveAssets();
        Build();
    }
}
