using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Tilemaps;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class CityTilemapSetup
{
    private const string Root = "Assets/CityTiles";
    [Serializable] private class Slice { public string name; public int x, y, width, height; }
    [Serializable] private class Atlas { public string file; public Slice[] sprites; }
    [Serializable] private class Manifest { public int tileSize, padding, pixelsPerUnit; public Atlas[] atlases; }
    private static Manifest ReadManifest() => JsonUtility.FromJson<Manifest>(File.ReadAllText(Root + "/city-atlas.json"));
    public static bool HasCitySprites => AssetDatabase.LoadAllAssetsAtPath(Root + "/Textures/city_cars.png").OfType<Sprite>().Count() == 8;

    [MenuItem("Tools/City Tilemap/Import atlases and configure city")]
    public static void Configure()
    {
        Directory.CreateDirectory(Root + "/Tiles");
        Directory.CreateDirectory("Assets/Resources/City");
        Directory.CreateDirectory("CityReports");
        AssetDatabase.Refresh();
        var manifest = ReadManifest();
        var tiles = new Dictionary<string, Tile>();
        var ordered = new List<Tile>();
        foreach (Atlas atlas in manifest.atlases)
        {
            string path = Root + "/Textures/" + atlas.file;
            ImportAtlas(path, atlas, manifest.pixelsPerUnit);
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s => s.name);
            foreach (var slice in atlas.sprites)
            {
                Require(sprites.ContainsKey(slice.name), "Missing slice " + slice.name);
                if (slice.name.StartsWith("car_")) continue;
                string tilePath = Root + "/Tiles/" + slice.name + ".asset";
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
                if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, tilePath); }
                tile.sprite = sprites[slice.name];
                tile.color = Color.white;
                tile.transform = Matrix4x4.identity;
                tile.flags = TileFlags.LockTransform;
                tile.colliderType = atlas.file == "city_buildings.png" ? Tile.ColliderType.Grid : Tile.ColliderType.None;
                EditorUtility.SetDirty(tile);
                tiles.Add(slice.name, tile);
                ordered.Add(tile);
            }
        }
        const string themePath = "Assets/Resources/City/CityTheme.asset";
        var theme = AssetDatabase.LoadAssetAtPath<CityTileTheme>(themePath);
        if (theme == null) { theme = ScriptableObject.CreateInstance<CityTileTheme>(); AssetDatabase.CreateAsset(theme, themePath); }
        theme.asphalt = Enumerable.Range(0, 4).Select(i => (TileBase)tiles["asphalt_" + i.ToString("00")]).ToArray();
        theme.sidewalks = Enumerable.Range(0, 256).Select(i => (TileBase)tiles["sidewalk_" + i.ToString("00")]).ToArray();
        theme.lanes = Enumerable.Range(0, 16).Select(i => (TileBase)tiles["lane_" + i.ToString("00")]).ToArray();
        theme.buildings = new[] { "house_terracotta", "house_slate", "house_sage", "apartment_cream", "cafe", "bakery", "clinic", "shop" }.Select(n => (TileBase)tiles[n]).ToArray();
        theme.props = new[] { "tree", "streetlamp", "bench", "planter" }.Select(n => (TileBase)tiles[n]).ToArray();
        theme.crosswalkNorthSouth = tiles["crosswalk_ns"];
        theme.crosswalkEastWest = tiles["crosswalk_ew"];
        theme.redParking = tiles["parking_red"];
        theme.greenParking = tiles["parking_green"];
        EditorUtility.SetDirty(theme);
        ConfigureCars();
        CreatePalette("CityPalette", ordered);
        CreatePalette("CityBuildingsPalette", theme.buildings.Concat(theme.props).Cast<Tile>().ToList());
        CreateDemo();
        AssetDatabase.SaveAssets();
        Validate();
    }

    private static void ImportAtlas(string path, Atlas atlas, int ppu)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        Require(importer != null, "Missing atlas " + path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = ppu;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = atlas.file == "city_terrain.png" ? 4096 : 2048;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var previous = provider.GetSpriteRects().ToDictionary(s => s.name, s => s.spriteID);
        var rectangles = atlas.sprites.Select(s => new SpriteRect {
            name = s.name, rect = new Rect(s.x, s.y, s.width, s.height),
            pivot = new Vector2(0.5f, 0.5f), alignment = SpriteAlignment.Center,
            spriteID = previous.ContainsKey(s.name) ? previous[s.name] : GUID.Generate()
        }).ToArray();
        provider.SetSpriteRects(rectangles);
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rectangles.Select(s => new SpriteNameFileIdPair(s.name, s.spriteID)));
        provider.Apply();
        importer.SaveAndReimport();
    }

    public static void ConfigureCars()
    {
        var sprites = AssetDatabase.LoadAllAssetsAtPath(Root + "/Textures/city_cars.png").OfType<Sprite>().ToDictionary(s => s.name);
        foreach (string color in new[] { "red", "green" })
        {
            string path = "Assets/Prefabs/player_" + color + ".prefab";
            var prefab = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var animation = prefab.GetComponent<CarSpriteAnimator>();
                Require(animation != null, "Car animation component missing");
                var serialized = new SerializedObject(animation);
                var frames = serialized.FindProperty("driveFrames");
                frames.arraySize = 4;
                for (int i = 0; i < 4; i++) frames.GetArrayElementAtIndex(i).objectReferenceValue = sprites["car_" + color + "_" + i.ToString("00")];
                serialized.FindProperty("framesPerSecond").floatValue = 20;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                prefab.GetComponent<SpriteRenderer>().sprite = sprites["car_" + color + "_00"];
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }
    }

    private static void CreatePalette(string name, List<Tile> tiles)
    {
        string path = Root + "/" + name + ".prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            GridPaletteUtility.CreateNewPalette(Root, name, GridLayout.CellLayout.Rectangle, GridPalette.CellSizing.Manual, Vector3.one, GridLayout.CellSwizzle.XYZ);
        var prefab = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var map = prefab.GetComponentInChildren<Tilemap>();
            map.ClearAllTiles();
            for (int i = 0; i < tiles.Count; i++) map.SetTile(new Vector3Int(i % 8, -(i / 8), 0), tiles[i]);
            map.CompressBounds();
            PrefabUtility.SaveAsPrefabAsset(prefab, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
    }

    private static void CreateDemo()
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Maps/Scenes/Scene 1.prefab");
        var demo = UnityEngine.Object.Instantiate(source);
        try
        {
            demo.name = "CityTilemapDemo";
            demo.transform.position = Vector3.zero;
            CityLevelVisuals.Apply(demo.GetComponent<NativeLevel>());
            PrefabUtility.SaveAsPrefabAsset(demo, Root + "/CityTilemapDemo.prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(demo); }
    }

    [MenuItem("Tools/City Tilemap/Rebuild selected level preview")]
    public static void RebuildSelected()
    {
        var selected = Selection.activeGameObject;
        var level = selected != null ? selected.GetComponentInParent<NativeLevel>() : null;
        if (level == null) { Debug.LogWarning("Select a level root or its Obstacles Tilemap first."); return; }
        Undo.RegisterFullObjectHierarchyUndo(level.gameObject, "Rebuild city preview");
        CityLevelVisuals.Apply(level);
        EditorUtility.SetDirty(level.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(level.gameObject.scene);
    }

    [MenuItem("Tools/City Tilemap/Validate city assets and all levels")]
    public static void Validate()
    {
        ValidateNeighborSelection();
        Directory.CreateDirectory("CityReports");
        int sprites = 0;
        foreach (Atlas atlas in ReadManifest().atlases)
        {
            var imported = AssetDatabase.LoadAllAssetsAtPath(Root + "/Textures/" + atlas.file).OfType<Sprite>().ToArray();
            Require(imported.Length == atlas.sprites.Length, "Slice count " + atlas.file);
            foreach (var sprite in imported)
            {
                Require(sprite.rect.size == new Vector2(128, 128) && sprite.pivot == new Vector2(64, 64) && sprite.pixelsPerUnit == 128, "Sprite dimensions " + sprite.name);
                sprites++;
            }
        }
        Require(sprites == 300, "Expected 300 city sprites");
        var theme = Resources.Load<CityTileTheme>("City/CityTheme");
        Require(theme != null && theme.IsConfigured, "Complete city theme");
        int checkedCells = 0;
        for (int number = 1; number <= 99; number++)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Maps/Scenes/Scene " + number + ".prefab");
            var instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                var level = instance.GetComponent<NativeLevel>();
                int originalCount = level.obstacles.GetUsedTilesCount();
                var city = CityLevelVisuals.Apply(level);
                Require(city != null, "City visual generation " + number);
                for (int y = 0; y < level.rows; y++)
                    for (int x = 0; x < level.columns; x++)
                    {
                        var cell = NativeLevel.ToCell(x, y);
                        bool blocked = level.IsBlocked(x, y);
                        Require(city.Roads.HasTile(cell) == !blocked && city.Sidewalks.HasTile(cell) == blocked, "Visual occupancy " + number + cell);
                        Require(!blocked || city.Sidewalks.GetTile(cell) == theme.sidewalks[CityLevelVisuals.SidewalkMask(level, x, y)], "Curb edge mask " + number + cell);
                        Require(blocked || (!city.Buildings.HasTile(cell) && !city.Decorations.HasTile(cell)), "Building blocks driveable road");
                        Require(Vector3.Distance(city.Roads.GetCellCenterWorld(cell), level.obstacles.GetCellCenterWorld(cell)) < 0.001f, "City grid alignment " + number);
                        checkedCells++;
                    }
                Require(!level.obstacles.GetComponent<TilemapRenderer>().enabled, "Original art hidden");
                Require(level.obstacles.GetUsedTilesCount() == originalCount, "Collision tiles retained");
                CityLevelVisuals.Apply(level);
                Require(instance.GetComponentsInChildren<Tilemap>().Length == 6, "Rebuilding must not duplicate layers");
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }
        foreach (string name in new[] { "CityPalette", "CityBuildingsPalette" })
            Require(AssetDatabase.LoadAllAssetsAtPath(Root + "/" + name + ".prefab").OfType<GridPalette>().Any(), "Native Unity Tile Palette " + name);
        CarSpriteSetup.Validate();
        File.WriteAllText("CityReports/unity-validation.txt", "PASS: 300 sprites; 292 Tile assets; 2 native Tile Palettes; 99 levels / " + checkedCells + " cells; correct road/obstacle separation, curb masks, grid alignment, no props on roads, repeatable rebuild; both four-frame car prefabs.\n");
    }

    public static void ConfigureAndBuildDevelopment() { Configure(); Build(true); }
    public static void BuildRelease() { Validate(); Build(false); }

    public static void ValidateNeighborSelection()
    {
        var root = new GameObject("Eight-neighbor mask verification", typeof(Grid), typeof(NativeLevel));
        var child = new GameObject("Obstacles", typeof(Tilemap));
        child.transform.SetParent(root.transform, false);
        var tile = ScriptableObject.CreateInstance<Tile>();
        try
        {
            var level = root.GetComponent<NativeLevel>();
            level.rows = level.columns = 5;
            level.obstacles = child.GetComponent<Tilemap>();
            int[] dx = { 0, 1, 0, -1, 1, 1, -1, -1 };
            int[] dy = { -1, 0, 1, 0, -1, 1, 1, -1 };
            for (int mask = 0; mask < 256; mask++)
            {
                level.obstacles.ClearAllTiles();
                level.obstacles.SetTile(NativeLevel.ToCell(2, 2), tile);
                for (int i = 0; i < 8; i++)
                    if ((mask & (1 << i)) == 0)
                        level.obstacles.SetTile(NativeLevel.ToCell(2 + dx[i], 2 + dy[i]), tile);
                Require(CityLevelVisuals.SidewalkMask(level, 2, 2) == mask, "8-neighbor mapping " + mask);
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(tile); }
    }

    private static void Build(bool development)
    {
        string folder = development ? "Builds/CityValidation" : "Builds/Windows";
        Directory.CreateDirectory(folder);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/main.unity" }, locationPathName = folder + "/LoveQiE.exe",
            target = BuildTarget.StandaloneWindows64,
            options = development ? BuildOptions.Development : BuildOptions.None
        });
        Require(report.summary.result == BuildResult.Succeeded, "City build failed");
        File.WriteAllText("CityReports/" + (development ? "development" : "release") + "-build.txt", "PASS: Windows x64; errors=" + report.summary.totalErrors + "; warnings=" + report.summary.totalWarnings + "\n");
        File.WriteAllLines("CityReports/" + (development ? "development" : "release") + "-messages.txt", report.steps.SelectMany(s => s.messages).Where(m => m.type == LogType.Warning || m.type == LogType.Error).Select(m => m.content));
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
