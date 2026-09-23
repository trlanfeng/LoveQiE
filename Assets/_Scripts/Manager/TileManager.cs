using UnityEngine;
using UnityEngine.Tilemaps;

public enum TileType { Solid, Item, Blank, OutSide }
public enum TileDirection { Up, Down, Left, Right }
public class TileManager : MonoBehaviour
{
    public TileBase getTileData(int x, int y)
    {
        NativeLevel level = GameManager.CurrentLevel;
        return level != null && level.Contains(x, y) ? level.obstacles.GetTile(NativeLevel.ToCell(x, y)) : null;
    }
    public TileType getTargetTileData(Vector3 position, TileDirection direction, int dir)
    {
        NativeLevel level = GameManager.CurrentLevel;
        if (level == null) return TileType.OutSide;
        Vector2Int index = level.WorldToIndex(position);
        switch (direction)
        {
            case TileDirection.Up: index.y--; break;
            case TileDirection.Down: index.y++; break;
            case TileDirection.Left: index.x--; break;
            case TileDirection.Right: index.x++; break;
        }
        if (!level.Contains(index.x, index.y)) return TileType.OutSide;
        // Original gameplay blocked every non-empty tile, regardless of SolidFlag.
        return level.IsBlocked(index.x, index.y) ? TileType.Solid : TileType.Blank;
    }
}
