using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Builds a city skin from the existing collision map; never changes blocked cells.</summary>
[DisallowMultipleComponent]
public sealed class CityLevelVisuals : MonoBehaviour
{
    [SerializeField] private Tilemap roads;
    [SerializeField] private Tilemap sidewalks;
    [SerializeField] private Tilemap markings;
    [SerializeField] private Tilemap buildings;
    [SerializeField] private Tilemap decorations;
    public Tilemap Roads => roads;
    public Tilemap Sidewalks => sidewalks;
    public Tilemap Buildings => buildings;
    public Tilemap Decorations => decorations;
    public Tilemap Markings => markings;

    public static CityLevelVisuals Apply(NativeLevel level)
    {
        var theme = Resources.Load<CityTileTheme>("City/CityTheme");
        if (theme == null || !theme.IsConfigured) return null;
        var visuals = level.GetComponent<CityLevelVisuals>() ?? level.gameObject.AddComponent<CityLevelVisuals>();
        visuals.Rebuild(level, theme);
        return visuals;
    }

    public void Rebuild(NativeLevel level, CityTileTheme theme)
    {
        roads = Layer(level, "City Roads", -8);
        sidewalks = Layer(level, "City Sidewalks", -6);
        markings = Layer(level, "City Markings", -5);
        buildings = Layer(level, "City Buildings", 2);
        decorations = Layer(level, "City Decorations", 3);
        for (int y = 0; y < level.rows; y++)
            for (int x = 0; x < level.columns; x++)
            {
                var cell = NativeLevel.ToCell(x, y);
                int mask = RoadMask(level, x, y);
                int variant = (x * 7 + y * 11) & 3;
                if (level.IsBlocked(x, y))
                {
                    sidewalks.SetTile(cell, theme.sidewalks[SidewalkMask(level, x, y)]);
                    bool median = x == level.columns / 2 && y > 1 && y < level.rows - 1;
                    bool plantedCorner = (x == 0 || x == level.columns - 1) && y % 3 == 0;
                    if (median || plantedCorner)
                        decorations.SetTile(cell, theme.props[median ? y % 4 : 0]);
                    else
                        buildings.SetTile(cell, theme.buildings[(x * 3 + y * 5) % theme.buildings.Length]);
                }
                else
                {
                    roads.SetTile(cell, theme.asphalt[variant]);
                    // Center lines are useful on narrow streets; leave open plazas uncluttered.
                    if (mask != 15) markings.SetTile(cell, theme.lanes[mask]);
                    if (y == 1 && (x == 7 || x == 9))
                        markings.SetTile(cell, theme.crosswalkEastWest);
                    if (y == 10 && x == 7) markings.SetTile(cell, theme.redParking);
                    if (y == 10 && x == 9) markings.SetTile(cell, theme.greenParking);
                }
            }
        // The original Tilemap and collider remain the authoritative movement map.
        var renderer = level.obstacles.GetComponent<TilemapRenderer>();
        if (renderer != null) renderer.enabled = false;
        foreach (var map in new[] { roads, sidewalks, markings, buildings, decorations }) map.CompressBounds();
    }

    public static int SidewalkMask(NativeLevel level, int x, int y)
    {
        return RoadMask(level, x, y)
            | (!level.IsBlocked(x + 1, y - 1) ? 16 : 0)
            | (!level.IsBlocked(x + 1, y + 1) ? 32 : 0)
            | (!level.IsBlocked(x - 1, y + 1) ? 64 : 0)
            | (!level.IsBlocked(x - 1, y - 1) ? 128 : 0);
    }

    public static int RoadMask(NativeLevel level, int x, int y)
    {
        return (!level.IsBlocked(x, y - 1) ? 1 : 0)
            | (!level.IsBlocked(x + 1, y) ? 2 : 0)
            | (!level.IsBlocked(x, y + 1) ? 4 : 0)
            | (!level.IsBlocked(x - 1, y) ? 8 : 0);
    }

    private Tilemap Layer(NativeLevel level, string layerName, int order)
    {
        Transform existing = level.transform.Find(layerName);
        GameObject child = existing != null ? existing.gameObject : new GameObject(layerName, typeof(Tilemap), typeof(TilemapRenderer));
        child.transform.SetParent(level.transform, false);
        child.transform.localPosition = level.obstacles.transform.localPosition;
        child.transform.localRotation = level.obstacles.transform.localRotation;
        child.transform.localScale = level.obstacles.transform.localScale;
        var map = child.GetComponent<Tilemap>();
        map.ClearAllTiles();
        child.GetComponent<TilemapRenderer>().sortingOrder = order;
        return map;
    }
}
