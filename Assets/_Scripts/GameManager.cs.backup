using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameManager : MonoBehaviour {

    public tk2dTileMap tm;
    public GameObject player1;
    public GameObject player2;
    private move player1move;
    private move2 player2move;

    private Vector3 winPosition;
    private bool isWin = false;

    public Text scoreText;
    public int score = 0;

	// Use this for initialization
	void Start () {
        player1move = player1.GetComponent<move>();
        player2move = player2.GetComponent<move2>();
        winPosition = new Vector3(8, 10, -5);
	}
	
	// Update is called once per frame
	void Update () {
        scoreText.text = score.ToString();
        if ((player1.transform.localPosition == player2.transform.localPosition) && (player1.transform.localPosition == winPosition))
        {
            if (isWin == false)
            winGame();
        }
        else
        {
            if (player1move.canMove == true && player1move.isMoving == false)
            {
                if (Input.GetKey(KeyCode.LeftArrow))
                {
                    if (checkTile1("left"))
                    {
                        player1move.isMoving = true;
                        player1move.leftDistance = Vector3.left;
                        player1move.movePlayer();
                    }
                }
                else if (Input.GetKey(KeyCode.RightArrow))
                {
                    if (checkTile1("right"))
                    {
                        player1move.isMoving = true;
                        player1move.leftDistance = Vector3.right;
                        player1move.movePlayer();
                    }
                }
                else if (Input.GetKey(KeyCode.UpArrow))
                {
                    if (checkTile1("up"))
                    {
                        player1move.isMoving = true;
                        player1move.leftDistance = Vector3.up;
                        player1move.movePlayer();
                    }
                }
                else if (Input.GetKey(KeyCode.DownArrow))
                {
                    if (checkTile1("down"))
                    {
                        player1move.isMoving = true;
                        player1move.leftDistance = Vector3.down;
                        player1move.movePlayer();
                    }
                }
            }
            if (player2move.canMove == true && player2move.isMoving == false)
            {
                if (Input.GetKey(KeyCode.LeftArrow))
                {
                    if (checkTile2("left"))
                    {
                        player2move.isMoving = true;
                        player2move.leftDistance = Vector3.right;
                        player2move.movePlayer();
                    }
                }
                else if (Input.GetKey(KeyCode.RightArrow))
                {
                    if (checkTile2("right"))
                    {
                        player2move.isMoving = true;
                        player2move.leftDistance = Vector3.left;
                        player2move.movePlayer();
                    }
                }
                else if (Input.GetKey(KeyCode.UpArrow))
                {
                    if (checkTile2("up"))
                    {
                        player2move.isMoving = true;
                        player2move.leftDistance = Vector3.up;
                        player2move.movePlayer();
                    }
                }
                else if (Input.GetKey(KeyCode.DownArrow))
                {
                    if (checkTile2("down"))
                    {
                        player2move.isMoving = true;
                        player2move.leftDistance = Vector3.down;
                        player2move.movePlayer();
                    }
                }
            }
        }
	}

    bool checkTile1(string arrow)
    {
        int x;
        int y;
        tm.GetTileAtPosition(player1.transform.position, out x, out y);
        switch (arrow)
        {
            case "up":
                y += 1;
                break;
            case "down":
                y -= 1;
                break;
            case "left":
                x -= 1;
                break;
            case "right":
                x += 1;
                break;
            default:
                break;
        }
        int tileid = tm.GetTile(x, y, 1);
        tk2dRuntime.TileMap.TileInfo tileinfo = tm.GetTileInfoForTileId(tileid);
        if (tileinfo != null && tileinfo.stringVal == "wall")
        {
            return false;
        }
        else
        {
            return true;
        }
    }
    bool checkTile2(string arrow)
    {
        int x;
        int y;
        tm.GetTileAtPosition(player2.transform.position, out x, out y);
        switch (arrow)
        {
            case "up":
                y += 1;
                break;
            case "down":
                y -= 1;
                break;
            case "left":
                x += 1;
                break;
            case "right":
                x -= 1;
                break;
            default:
                break;
        }
        int tileid = tm.GetTile(x, y, 1);
        tk2dRuntime.TileMap.TileInfo tileinfo = tm.GetTileInfoForTileId(tileid);
        if (tileinfo != null)
        {
            if (tileinfo.stringVal == "wall")
            {
                return false;
            }
            if (tileinfo.stringVal == "daoju")
            {

                getDaoju(tileinfo.intVal, tileinfo.floatVal);
                print(x + "," + y);
                tm.ClearTile(x, y, 1);
                tm.SetTile(x, y, 1, 2);
                tm.Build();
                print("456");
            }
            return true;
        }
        else
        {
            return true;
        }
    }
    void winGame()
    {
        isWin = true;
        score += 100;
        Debug.Log("win");
    }
    void getDaoju(int typeid,float metadata)
    {
        print("123");
        score += 5800;
    }
}
