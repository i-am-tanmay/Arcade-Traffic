using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BushSlowTrigger : MonoBehaviour
{
    private void OnTriggerStay(Collider other)
    {
        Player_Movement playerhit = other.transform.root.GetComponent<Player_Movement>();
        if (playerhit != null && playerhit.playerNum == GameManager.Instance.currPlayerNumber)
            playerhit.isBushSlowed = true;
    }
}
