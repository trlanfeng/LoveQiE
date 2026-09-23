using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class CarSpriteSetup
{
    private const string ImageFolder = "Assets/Images/Cars/";

    [MenuItem("Tools/Player Cars/Configure generated sprites")]
    public static void Configure()
    {
        AssetDatabase.Refresh();
        if (CityTilemapSetup.HasCitySprites)
        {
            CityTilemapSetup.ConfigureCars();
            AssetDatabase.SaveAssets();
            Validate();
            return;
        }
        foreach (string color in new[] { "red", "green" })
        {
            var frames = new Sprite[4];
            for (int i = 0; i < frames.Length; i++)
            {
                string path = ImageFolder + "car_" + color + "_" + i.ToString("00") + ".png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Require(importer != null, "Missing generated image: " + path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 128;
                importer.spritePivot = new Vector2(0.5f, 0.5f);
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 128;
                importer.SaveAndReimport();
                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            string prefabPath = "Assets/Prefabs/player_" + color + ".prefab";
            var prefab = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var renderer = prefab.GetComponent<SpriteRenderer>();
                renderer.sprite = frames[0];
                renderer.color = Color.white;
                var animation = prefab.GetComponent<CarSpriteAnimator>() ?? prefab.AddComponent<CarSpriteAnimator>();
                var serialized = new SerializedObject(animation);
                var sprites = serialized.FindProperty("driveFrames");
                sprites.arraySize = frames.Length;
                for (int i = 0; i < frames.Length; i++) sprites.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
                serialized.FindProperty("framesPerSecond").floatValue = 20;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }
        AssetDatabase.SaveAssets();
        Validate();
    }

    [MenuItem("Tools/Player Cars/Validate car assets")]
    public static void Validate()
    {
        foreach (string color in new[] { "red", "green" })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/player_" + color + ".prefab");
            var animation = prefab.GetComponent<CarSpriteAnimator>();
            Require(animation != null && animation.FrameCount == 4, color + " car needs four animation frames");
            var frames = new SerializedObject(animation).FindProperty("driveFrames");
            for (int i = 0; i < frames.arraySize; i++)
            {
                var sprite = frames.GetArrayElementAtIndex(i).objectReferenceValue as Sprite;
                Require(sprite != null && sprite.rect.width == 128 && sprite.rect.height == 128, color + " frame " + i);
                Require(Mathf.Approximately(sprite.pixelsPerUnit, 128), "Car must fit inside a single map cell");
                Require(sprite.pivot == new Vector2(64, 64), "Consistent centered pivots");
            }
            Require(prefab.GetComponent<SpriteRenderer>().sprite == frames.GetArrayElementAtIndex(0).objectReferenceValue, "Parked preview frame");
            Require(prefab.GetComponent<CharactorManager>().moveDirection == (color == "red" ? -1 : 1), "Mirrored controls retained");
        }
        Directory.CreateDirectory("CarReports");
        File.WriteAllText("CarReports/assets-validation.txt", "PASS: both car prefabs; 8 valid 128x128 sprites; centered pivots; 128 PPU; 4 ordered frames each; parked sprite and mirrored controls.\n");
    }

    public static void ConfigureAndBuildDevelopment()
    {
        Configure();
        Build(true);
    }

    public static void BuildRelease() { Validate(); Build(false); }

    private static void Build(bool development)
    {
        string folder = development ? "Builds/CarValidation" : "Builds/Windows";
        Directory.CreateDirectory(folder);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/main.unity" },
            locationPathName = folder + "/LoveQiE.exe",
            target = BuildTarget.StandaloneWindows64,
            options = development ? BuildOptions.Development : BuildOptions.None
        });
        Require(report.summary.result == BuildResult.Succeeded, "Car build failed");
        File.WriteAllText("CarReports/" + (development ? "development" : "release") + "-build.txt",
            "PASS: Windows x64; errors=" + report.summary.totalErrors + "; warnings=" + report.summary.totalWarnings + "\n");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
