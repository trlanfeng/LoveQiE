using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Rotorz.Tile;
using DG.Tweening;

public enum GameState
{
    Init,
    Ready,
    Play,
    Win,
    Faild
}

public class GameManager : MonoBehaviour
{
    public TileSystem tileSystem;
    public GameObject charactorLeft;
    public GameObject charactorRight;
    private int curScene;
    private int maxScene;

    public GameState gameState;
    void Start()
    {
        maxScene = 5;
        curScene = 1;
        gameState = GameState.Init;
    }
    void Update()
    {
        switch (gameState)
        {
            case GameState.Init:
                onInit();
                break;
            case GameState.Ready:
                onReady();
                break;
            case GameState.Play:
                onPlay();
                break;
            case GameState.Win:
                onWin();
                break;
            case GameState.Faild:
                onFaild();
                break;
        }
    }
    GameObject curSceneGameObject;
    void onInit()
    {
        initCharactorPosition();
        loadScene();
        tileSystem = curSceneGameObject.GetComponent<TileSystem>();
        if (tileSystem != null)
        {
            winTimer = 0;
            gameState = GameState.Ready;
        }
        else
        {
            Debug.LogError("关卡加载失败！");
        }
    }
    void initCharactorPosition()
    {
        charactorRight.transform.position = new Vector3(9.5f, -10.5f, 0);
        charactorLeft.transform.position = new Vector3(7.5f, -10.5f, 0);
    }
    void loadScene()
    {
        tileSystem = null;
        GameObject temp = curSceneGameObject;
        curSceneGameObject = Instantiate(Resources.Load<GameObject>("Maps/Scenes/Scene" + curScene.ToString()));
        if (temp != null)
        {
            Destroy(temp);
        }
    }
    void onReady()
    {
        if (tileSystem == null)
        {
            gameState = GameState.Init;
        }
        gameState = GameState.Play;
    }
    void onPlay()
    {
        if (tileSystem == null)
        {
            gameState = GameState.Init;
        }
        TileIndex tiRight = tileSystem.ClosestTileIndexFromWorld(charactorRight.transform.position);
        if (tiRight.row == 1 && tiRight.column == 7)
        {
            TileIndex tiLeft = tileSystem.ClosestTileIndexFromWorld(charactorLeft.transform.position);
            if (tiLeft.row == 1 && tiLeft.column == 9)
            {
                gameState = GameState.Win;
            }
        }
        else if (tiRight.row == 1 && tiRight.column == 9)
        {
            TileIndex tiLeft = tileSystem.ClosestTileIndexFromWorld(charactorLeft.transform.position);
            if (tiLeft.row == 1 && tiLeft.column == 7)
            {
                gameState = GameState.Win;
            }
        }
    }
    void checkResult()
    {

    }
    float winTimer = 0;
    void onWin()
    {
        if (winTimer == 0)
        {
            charactorRight.transform.DOMoveX(7.5f, 0.3f);
            charactorLeft.transform.DOMoveX(9.5f, 0.3f);
        }
        winTimer += Time.deltaTime;
        Debug.Log("赢了");
        if (winTimer > 1)
        {
            if (curScene < maxScene)
            {
                curScene += 1;
                gameState = GameState.Init;
                return;
            }
        }
    }
    void onFaild()
    {

    }
}
