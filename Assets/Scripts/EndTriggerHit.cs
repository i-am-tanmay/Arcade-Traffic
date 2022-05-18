using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndTriggerHit : MonoBehaviour
{
    public int colliderNumber;

    private void OnTriggerEnter(Collider other)
    {
        Player_Movement playerhit = other.transform.root.GetComponent<Player_Movement>();
        if (playerhit != null && playerhit.playerNum == colliderNumber)
            GameManager.Instance.OnTrigger_End(colliderNumber);
    }
}