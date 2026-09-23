using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "LoveQiE/City Tile Theme")]
public sealed class CityTileTheme : ScriptableObject
{
    public TileBase[] asphalt;
    [Tooltip("N=1, E=2, S=4, W=8: edges facing a drivable cell.")]
    public TileBase[] sidewalks;
    [Tooltip("N=1, E=2, S=4, W=8: connected road directions.")]
    public TileBase[] lanes;
    public TileBase[] buildings;
    [Tooltip("Tree, streetlamp, bench, planter.")]
    public TileBase[] props;
    public TileBase crosswalkNorthSouth;
    public TileBase crosswalkEastWest;
    public TileBase redParking;
    public TileBase greenParking;

    public bool IsConfigured => HasTiles(asphalt, 4) && HasTiles(sidewalks, 16)
        && HasTiles(lanes, 16) && HasTiles(buildings, 8) && HasTiles(props, 4)
        && crosswalkNorthSouth != null && crosswalkEastWest != null
        && redParking != null && greenParking != null;

    private static bool HasTiles(TileBase[] tiles, int length)
    {
        if (tiles == null || tiles.Length != length) return false;
        foreach (var tile in tiles) if (tile == null) return false;
        return true;
    }
}
