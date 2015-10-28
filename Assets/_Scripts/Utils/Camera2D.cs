using UnityEngine;
using System.Collections;

public class Camera2D : MonoBehaviour
{
    public bool isUpdate = true;
    public int gameWidth = 1920;
    public int gameHeight = 1080;
    public int PixelPerUnit = 100;

    private Camera camera2d;

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
        camera2d = transform.GetComponent<Camera>();
        camera2d.backgroundColor = backgroundColor;
        screenWidth = Screen.width;
        screenHeight = Screen.height;
        deviceAspectRatio = (float)screenWidth / screenHeight;
        gameAspectRatio = (float)gameWidth / gameHeight;
        switch (ScaleMode)
        {
            //按宽度适配
            case scaleMode.FitWidth:
                camera2d.orthographicSize = (float)gameWidth / screenWidth * screenHeight / 2f / PixelPerUnit;
                break;
            case scaleMode.FitHeight:
                camera2d.orthographicSize = gameHeight / 2f / PixelPerUnit;
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
        camera2d.backgroundColor = backgroundColor;
        switch (ScaleMode)
        {
            //按宽度适配
            case scaleMode.FitWidth:
                camera2d.orthographicSize = (float)gameWidth / screenWidth * screenHeight / 2f / PixelPerUnit;
                break;
            case scaleMode.FitHeight:
                camera2d.orthographicSize = gameHeight / 2f / PixelPerUnit;
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
