using UnityEngine;
using System.Collections;
using DG.Tweening;

public class BreathIt : MonoBehaviour
{

    // Use this for initialization
    void Start()
    {
        transform.DOScale(1f, 0.5f).SetLoops(100,LoopType.Yoyo).SetEase(Ease.InBack);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
