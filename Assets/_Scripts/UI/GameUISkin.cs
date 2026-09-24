using UnityEngine;

// References the original Assets/UI artwork, so it is also included in player builds.
public sealed class GameUISkin : ScriptableObject
{
    public Texture2D play, home, pause, time, steps, selection, success, star;
    public Texture2D[] levels;
    public Font font;
}
