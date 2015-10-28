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
        return tileSystem.GetTile(x, y);
    }

    private TileData getTileDataByDirection(TileDirection tDir)
    {
        switch (tDir)
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
            return null;
        }
        else
        {
            return getTileData(x, y);
        }
    }

    public TileType getTileType(Vector3 position, TileDirection tileDirection)
    {
        TileIndex ti = getTileIndexByPosition(position);
        x = ti.column;
        y = ti.row;
        TileData td = getTileDataByDirection(tileDirection);
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

    public TileIndex getTileIndexByPosition(Vector3 position)
    {
        return tileSystem.ClosestTileIndexFromWorld(position);
    }
}
