using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class NativeLevel : MonoBehaviour
{
    public int rows = 12;
    public int columns = 17;
    public Tilemap obstacles;
    // Rotorz rows go down. Row zero spans Unity y=-1..0.
    public static Vector3Int ToCell(int column, int row) => new Vector3Int(column, -row - 1, 0);
    public Vector2Int WorldToIndex(Vector3 world)
    {
        Vector3Int cell = obstacles.WorldToCell(world);
        return new Vector2Int(cell.x, -cell.y - 1);
    }
    public Vector3 CellCenter(int column, int row)
    {
        Vector3 center = obstacles.GetCellCenterWorld(ToCell(column, row));
        center.z = 0;
        return center;
    }
    public bool Contains(int column, int row) => column >= 0 && row >= 0 && column < columns && row < rows;
    public bool IsBlocked(int column, int row) => !Contains(column, row) || obstacles.HasTile(ToCell(column, row));
}
