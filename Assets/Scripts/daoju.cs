using UnityEngine;
using System.Collections;
using Holoville.HOTween;

public class daoju : MonoBehaviour {

    public string type;
    public int number;

    private GameManager GM;

    void Start()
    {
        GM = GameObject.Find("GameManager").GetComponent<GameManager>();
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Player")
        {
            print("daoju - get");
            GM.score += 2000;
            HOTween.To(this.gameObject.GetComponent<Renderer>().material, 0.5f, new TweenParms().Prop("color", new Color(1, 1, 1, 0), false));
            HOTween.To(this.gameObject.transform, 0.5f, new TweenParms().Prop("position", Vector3.up, true).OnComplete(getDaoju));
        }
    }
    void getDaoju()
    {
        Destroy(this.gameObject);
    }
}
