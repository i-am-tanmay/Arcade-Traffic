using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectibleTriggerHit : MonoBehaviour
{
    public int extraTime;

    private void OnTriggerEnter(Collider other)
    {
        Player_Movement playerhit = other.transform.root.GetComponent<Player_Movement>();
        if (playerhit != null && playerhit.playerNum == GameManager.Instance.currPlayerNumber)
            GameManager.Instance.OnTrigger_Collectible(extraTime, gameObject);
    }
}