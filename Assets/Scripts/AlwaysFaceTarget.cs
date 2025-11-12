using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class AlwaysFaceTarget : MonoBehaviour
{
    [SerializeField]
    private GameObject target;

    void FixedUpdate()
    {
        var targetDirection = target.transform.position - transform.position;
        var targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime);
    }
}
