using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Derived city layers follow edits to the authoritative obstacle grid.</summary>
[InitializeOnLoad]
public static class CityLivePreview
{
    private static readonly HashSet<NativeLevel> Pending = new HashSet<NativeLevel>();
    private static bool updating;

    static CityLivePreview()
    {
        Tilemap.tilemapTileChanged += OnTilesChanged;
        Undo.undoRedoPerformed += OnUndoRedo;
    }

    private static void OnTilesChanged(Tilemap map, Tilemap.SyncTile[] changes)
    {
        if (updating || EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer || map == null) return;
        var level = map.GetComponentInParent<NativeLevel>();
        if (level != null && level.obstacles == map && !EditorUtility.IsPersistent(level)) Queue(level);
    }

    private static void OnUndoRedo()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        // Includes inactive roots and the currently open Prefab Stage.
        foreach (var level in Resources.FindObjectsOfTypeAll<NativeLevel>())
            if (!EditorUtility.IsPersistent(level) && level.gameObject.scene.IsValid()) Queue(level);
    }

    private static void Queue(NativeLevel level)
    {
        Pending.Add(level);
        EditorApplication.delayCall -= FlushPending;
        EditorApplication.delayCall += FlushPending;
    }

    public static void FlushPending()
    {
        EditorApplication.delayCall -= FlushPending;
        if (updating) return;
        updating = true;
        try
        {
            foreach (var level in Pending)
            {
                if (level == null || level.obstacles == null || !level.gameObject.scene.IsValid()) continue;
                if (CityLevelVisuals.Apply(level) != null)
                    EditorSceneManager.MarkSceneDirty(level.gameObject.scene);
            }
            Pending.Clear();
            SceneView.RepaintAll();
        }
        finally { updating = false; }
    }
}
