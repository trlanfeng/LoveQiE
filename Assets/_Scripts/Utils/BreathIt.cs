using UnityEngine;
public class BreathIt : MonoBehaviour
{
    private Vector3 baseScale;
    private void Awake() { baseScale = transform.localScale; }
    private void Update() { transform.localScale = baseScale * (1f + 0.06f * Mathf.Sin(Time.time * Mathf.PI * 2)); }
}
