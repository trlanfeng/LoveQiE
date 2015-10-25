using UnityEngine;
using System.Collections;

public class test : MonoBehaviour {

    public Vector2 position;

	// Use this for initialization
	void Update () {
        RaycastHit2D hit = Physics2D.Raycast(position, position);
        Debug.DrawLine(position, position+Vector2.up, Color.red, 1);
        if (hit.collider != null)
        {
            print(hit.collider.gameObject.name);
        }
	}

}
