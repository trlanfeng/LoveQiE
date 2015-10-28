using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Rotorz.Tile;

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

    public Vector3 charactorLeft;
    public Vector3 charactorRight;

    public GameState gameState;
    void Start()
    {

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
    void onInit()
    {
        gameState = GameState.Ready;
    }
    void onReady()
    {
        gameState = GameState.Play;
    }
    void onPlay()
    {
        Debug.Log(charactorRight);
        TileIndex tiRight = tileSystem.ClosestTileIndexFromWorld(charactorRight);
        Debug.Log("tile::row:" + tiRight.row + ",column:" + tiRight.column);
        if (tiRight.row == 1 && tiRight.column == 7)
        {
            TileIndex tiLeft = tileSystem.ClosestTileIndexFromWorld(charactorLeft);
            if (tiLeft.row == 1 && tiLeft.column == 9)
            {
                gameState = GameState.Win;
            }
        }
        else if (tiRight.row == 1 && tiRight.column == 9)
        {
            Debug.Log("进来了");
            TileIndex tiLeft = tileSystem.ClosestTileIndexFromWorld(charactorLeft);
            if (tiLeft.row == 1 && tiLeft.column == 7)
            {
                gameState = GameState.Win;
            }
        }
    }
    void checkResult()
    {

    }
    void onWin()
    {
        Debug.Log("赢了");
    }
    void onFaild()
    {

    }
}
