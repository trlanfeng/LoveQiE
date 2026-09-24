using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.UIElements;

public static class GameUISetup
{
    [MenuItem("Tools/Game UI/Configure UI assets")]
    public static void Configure()
    {
        foreach (string file in Directory.GetFiles("Assets/UI", "*.png", SearchOption.AllDirectories))
        {
            string path = file.Replace('\\', '/');
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }
        const string target = "Assets/Resources/GameUI/Skin.asset";
        var skin = AssetDatabase.LoadAssetAtPath<GameUISkin>(target);
        if (skin == null) { skin = ScriptableObject.CreateInstance<GameUISkin>(); AssetDatabase.CreateAsset(skin, target); }
        skin.play = Image("play"); skin.home = Image("home"); skin.pause = Image("pause");
        skin.time = Image("count_time"); skin.steps = Image("count_steps");
        skin.selection = Image("level_select"); skin.success = Image("level_success");
        skin.star = Image("Prepared/star_clean");
        skin.levels = Enumerable.Range(1, 5).Select(i => Image("level_" + i)).ToArray();
        skin.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Prepared/JourneyUI.otf");
        if (skin.font == null) throw new Exception("Run python Tools/prepare_game_ui.py first.");
        EditorUtility.SetDirty(skin);
        const string panelPath = "Assets/Resources/GameUI/Panel.asset";
        var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelPath);
        if (panel == null) { panel = ScriptableObject.CreateInstance<PanelSettings>(); AssetDatabase.CreateAsset(panel, panelPath); }
        panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/Resources/GameUI/GameTheme.tss");
        if (panel.themeStyleSheet == null) throw new Exception("Missing UI theme stylesheet.");
        EditorUtility.SetDirty(panel);
        AssetDatabase.SaveAssets();
        Debug.Log("GAME_UI_ASSETS_READY");
    }
    private static Texture2D Image(string name)
    {
        var image = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/UI/" + name + ".png");
        if (image == null) throw new Exception("Missing UI artwork: " + name);
        return image;
    }
    public static void BuildDevelopment() { Configure(); Build(true); }
    public static void BuildRelease() { Configure(); Build(false); }
    private static void Build(bool development)
    {
        string folder = development ? "Builds/UIValidation" : "Builds/Windows";
        Directory.CreateDirectory(folder);
        Directory.CreateDirectory("UIReports");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/main.unity" }, locationPathName = folder + "/LoveQiE.exe",
            target = BuildTarget.StandaloneWindows64, options = development ? BuildOptions.Development : BuildOptions.None
        });
        File.WriteAllText("UIReports/" + (development ? "development" : "release") + "-build.txt",
            report.summary.result + "; errors=" + report.summary.totalErrors + "; warnings=" + report.summary.totalWarnings);
        File.WriteAllLines("UIReports/build-messages.txt", report.steps.SelectMany(s => s.messages)
            .Where(m => m.type == LogType.Warning || m.type == LogType.Error).Select(m => m.content));
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("UI build failed");
    }
}
