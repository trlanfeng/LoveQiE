using System.Collections;
using UnityEngine;

public class CharactorManager : MonoBehaviour
{
    public GameManager GM;
    public TileManager TM;
    public int moveDirection = 1;
    public bool IsMoving { get; private set; }
    public bool TryMove(Vector2Int input)
    {
        NativeLevel level = GameManager.CurrentLevel;
        if (IsMoving || level == null || GM == null || GM.gameState != GameState.Play) return false;
        Vector2Int current = level.WorldToIndex(transform.position);
        Vector2Int next = current + new Vector2Int(input.x * moveDirection, -input.y);
        if (level.IsBlocked(next.x, next.y)) return false;
        StartCoroutine(MoveTo(level.CellCenter(next.x, next.y)));
        return true;
    }
    private IEnumerator MoveTo(Vector3 target)
    {
        IsMoving = true;
        Vector3 from = transform.position;
        target.z = from.z;
        float elapsed = 0;
        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / 0.2f);
            transform.position = Vector3.Lerp(from, target, 1 - (1 - t) * (1 - t));
            yield return null;
        }
        transform.position = target;
        IsMoving = false;
    }
    public void ResetAt(Vector3 position)
    {
        StopAllCoroutines(); IsMoving = false; transform.position = position;
    }
    private void OnDisable() { StopAllCoroutines(); IsMoving = false; }
}
