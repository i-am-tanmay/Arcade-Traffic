using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionCalc : MonoBehaviour
{

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player_Collidable") || collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            if (GameManager.Instance.isRoundStarted && transform.parent.GetComponent<Player_Movement>())
                transform.parent.GetComponent<Player_Movement>().CollideCalc(Vector3.ProjectOnPlane(collision.GetContact(0).normal, Vector3.up));
        }
    }
}