using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Official RuleTile with the game's finite-map boundary convention.</summary>
[CreateAssetMenu(menuName = "LoveQiE/City Sidewalk Rule Tile")]
public sealed class CitySidewalkRuleTile : RuleTile
{
    // Bits denote road (missing sidewalk), y is north/up in Unity.
    public static int NormalizeMask(int mask)
    {
        if ((mask & 3) != 0) mask |= 16;
        if ((mask & 6) != 0) mask |= 32;
        if ((mask & 12) != 0) mask |= 64;
        if ((mask & 9) != 0) mask |= 128;
        return mask;
    }

    public override bool RuleMatch(int neighbor, TileBase other)
    {
        // Legacy obstacle tiles and this brush belong to the same terrain.
        if (neighbor == TilingRuleOutput.Neighbor.This) return other != null;
        if (neighbor == TilingRuleOutput.Neighbor.NotThis) return other == null;
        return true;
    }

    public override bool RuleMatches(TilingRule rule, Vector3Int position, ITilemap tilemap, ref Matrix4x4 transform)
    {
        var map = tilemap.GetComponent<Tilemap>();
        var level = map != null ? map.GetComponentInParent<NativeLevel>() : null;
        if (level == null) return base.RuleMatches(rule, position, tilemap, ref transform);
        // NativeLevel treats space beyond its bounds as blocked. Apply the same
        // convention without adding invisible padding tiles to the map.
        for (int i = 0; i < rule.m_Neighbors.Count; i++)
        {
            var cell = position + rule.m_NeighborPositions[i];
            TileBase other = level.Contains(cell.x, -cell.y - 1) ? tilemap.GetTile(cell) : this;
            if (!RuleMatch(rule.m_Neighbors[i], other)) return false;
        }
        transform = Matrix4x4.identity;
        return true;
    }
}
