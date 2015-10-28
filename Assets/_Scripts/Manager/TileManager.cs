using UnityEngine;
using Rotorz.Tile;

public enum TileType
{
    Solid,
    Item,
    Blank,
    OutSide
}

public enum TileDirection
{
    Up,
    Down,
    Left,
    Right
}

public class TileManager
{

    private TileSystem tileSystem;
    private TileIndex baseTileIndex;
    private int x;
    private int y;

    public TileManager(TileSystem tileSystem)
    {
        this.tileSystem = tileSystem;

    }

    public TileData getTileData(int x, int y)
    {
        if (tileSystem == null)
        {
            return null;
        }
        return tileSystem.GetTile(y, x);
    }

    public TileType getTargetTileData(Vector3 position,TileDirection tileDirection,int dir)
    {
        TileIndex ti = tileSystem.ClosestTileIndexFromWorld(position);
        int x = ti.column;
        int y = ti.row;
        switch (tileDirection)
        {
            case TileDirection.Up:
                y -= 1;
                break;
            case TileDirection.Down:
                y += 1;
                break;
            case TileDirection.Left:
                x -= 1;
                break;
            case TileDirection.Right:
                x += 1;
                break;
        }
        if (x < 0 || y < 0 || x > tileSystem.ColumnCount || y > tileSystem.RowCount)
        {
            return TileType.OutSide;
        }
        else
        {
            TileData td = getTileData(x, y);
            if (td != null)
            {
                if (td.Empty)
                {
                    return TileType.Blank;
                }
                if (td.SolidFlag)
                {
                    return TileType.Solid;
                }
                return TileType.Solid;
            }
            else
            {
                return TileType.Blank;
            }
        }
    }
}
