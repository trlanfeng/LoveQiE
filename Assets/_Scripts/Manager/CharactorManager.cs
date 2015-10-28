using UnityEngine;
using System.Collections;
using Rotorz.Tile;
using DG.Tweening;

public class CharactorManager : MonoBehaviour
{
    GameManager GM;
    public int moveDirection;
    void Start()
    {
        GM = GameObject.Find("Main Camera").GetComponent<GameManager>();
    }
    void Update()
    {
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
            checkByDirection(TileDirection.Left);
        }
        if (Input.GetKey(KeyCode.D) && moveStep == 0)
        {
            checkByDirection(TileDirection.Right);
        }
    }
    void checkByDirection(TileDirection TD)
    {
        moveStep = 1;
        TileManager TM = new TileManager(GM.tileSystem);
        TileIndex ti = TM.getTileIndexByPosition(transform.position);
        //获得tile类型
        TileType tileType = TM.getTileType(transform.position, TD);
        //根据tile类型进行相应操作
        switch (tileType)
        {
            case TileType.Solid:
                break;
            case TileType.Item:
                break;
            case TileType.Blank:
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
                if (moveDirection == 1)
                {
                    moveLeft();
                }
                else
                {
                    moveRight();
                }
                break;
            case TileDirection.Right:
                if (moveDirection == 1)
                {
                    moveRight();
                }
                else
                {
                    moveLeft();
                }
                break;
        }
    }
    int moveStep = 0;
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
