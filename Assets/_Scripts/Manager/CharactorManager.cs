using UnityEngine;
using System.Collections;
using Rotorz.Tile;
using DG.Tweening;

public class CharactorManager : MonoBehaviour
{
    public GameManager GM;
    public TileManager TM;
    public int moveDirection;
    int moveStep = 0;

    void Update()
    {
        if (GM.gameState != GameState.Play)
        {
            return;
        }
        if (Input.GetKey(KeyCode.W) && moveStep == 0)
        {
            checkByDirection(TileDirection.Up);
        }
        if (Input.GetKey(KeyCode.S) && moveStep == 0)
        {
            checkByDirection(TileDirection.Down);
        }
        if (Input.GetKey(KeyCode.A) && moveStep == 0)
        {
            if (moveDirection == 1)
            {
                checkByDirection(TileDirection.Left);
            }
            else
            {
                checkByDirection(TileDirection.Right);
            }
        }
        if (Input.GetKey(KeyCode.D) && moveStep == 0)
        {
            if (moveDirection == 1)
            {
                checkByDirection(TileDirection.Right);
            }
            else
            {
                checkByDirection(TileDirection.Left);
            }
        }
    }
    void checkByDirection(TileDirection TD)
    {
        //传入position和位置，得到需要走的那一格的类型
        TileType tileType = TM.getTargetTileData(transform.position, TD, moveDirection);
        //根据tile类型进行相应操作
        switch (tileType)
        {
            case TileType.Solid:
                return;
            case TileType.Item:
                break;
            case TileType.Blank:
                moveStep = 1;
                moveByDirection(TD);
                break;
            case TileType.OutSide:
                break;
        }
    }

    void moveByDirection(TileDirection TD)
    {
        switch (TD)
        {
            case TileDirection.Up:
                moveUp();
                break;
            case TileDirection.Down:
                moveDown();
                break;
            case TileDirection.Left:
                moveLeft();
                break;
            case TileDirection.Right:
                moveRight();
                break;
        }
    }

    void moveUp()
    {
        transform.DOMoveY(transform.position.y + moveStep, 0.2f).OnComplete(clearMoveStep);
    }
    void moveDown()
    {
        transform.DOMoveY(transform.position.y - moveStep, 0.2f).OnComplete(clearMoveStep);
    }
    void moveLeft()
    {
        transform.DOMoveX(transform.position.x - moveStep, 0.2f).OnComplete(clearMoveStep);
    }
    void moveRight()
    {
        transform.DOMoveX(transform.position.x + moveStep, 0.2f).OnComplete(clearMoveStep);
    }
    void clearMoveStep()
    {
        moveStep = 0;
    }
}
