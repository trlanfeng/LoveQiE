using UnityEngine;
using System.Collections;

public class Camera2D : MonoBehaviour
{
    public bool isUpdate = true;
    public int gameWidth = 1920;
    public int gameHeight = 1080;
    public int PixelPerUnit = 100;

    private Camera camera;

    public enum scaleMode
    {
        FitWidth,
        FitHeight,
        CropEdge,
        FillEdge,
        Stretch
    }

    public scaleMode ScaleMode;

    public Color backgroundColor;

    private int screenWidth;
    private int screenHeight;
    private float deviceAspectRatio;
    private float gameAspectRatio;

    // Use this for initialization
    void Start()
    {
        camera = transform.GetComponent<Camera>();
        camera.backgroundColor = backgroundColor;
        screenWidth = Screen.width;
        screenHeight = Screen.height;
        deviceAspectRatio = (float)screenWidth / screenHeight;
        gameAspectRatio = (float)gameWidth / gameHeight;
        switch (ScaleMode)
        {
            //按宽度适配
            case scaleMode.FitWidth:
                camera.orthographicSize = (float)gameWidth / screenWidth * screenHeight / 2f / PixelPerUnit;
                break;
            case scaleMode.FitHeight:
                camera.orthographicSize = gameHeight / 2f / PixelPerUnit;
                break;
            case scaleMode.CropEdge:
                break;
            case scaleMode.FillEdge:
                break;
            case scaleMode.Stretch:
                break;
            default:
                break;
        }
    }
    void Update()
    {
        if (!isUpdate)
        {
            return;
        }
        camera.backgroundColor = backgroundColor;
        switch (ScaleMode)
        {
            //按宽度适配
            case scaleMode.FitWidth:
                camera.orthographicSize = (float)gameWidth / screenWidth * screenHeight / 2f / PixelPerUnit;
                break;
            case scaleMode.FitHeight:
                camera.orthographicSize = gameHeight / 2f / PixelPerUnit;
                break;
            case scaleMode.CropEdge:
                break;
            case scaleMode.FillEdge:
                break;
            case scaleMode.Stretch:
                break;
            default:
                break;
        }
    }
}
