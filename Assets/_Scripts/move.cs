using UnityEngine;
using System.Collections;
using Holoville.HOTween;
//using DG.Tweening;

public class move : MonoBehaviour {
    //角色移动相关
    public Vector3 leftDistance = Vector3.zero;
    public bool isMoving = false;
    public bool canMove = true;
    public float moveSpeed = 0.3f;

    public void movePlayer()
    {
        HOTween.To(this.gameObject.transform, moveSpeed, new TweenParms().Prop("position", leftDistance, true).Ease(EaseType.Linear).OnComplete(changeMovingState));
        //transform.DOMove(transform.position + leftDistance, moveSpeed).SetEase(Ease.Linear).OnComplete(changeMovingState);
    }
    void changeMovingState()
    {
        isMoving = false;
    }
    

}
