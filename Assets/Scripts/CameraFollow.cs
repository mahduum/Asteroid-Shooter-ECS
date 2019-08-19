using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public GameObject target;
    float spray = 0.15f;

    Vector3 offset;
    // Start is called before the first frame update
    void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player");
   
        transform.position = target.transform.position + new Vector3(0, 0, -10f);
        offset = transform.position - target.transform.position;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (target != null)
        {
            Vector3 nextPosition = target.transform.position + offset;
            Vector3 sprayedPosition = Vector3.Lerp(transform.position, nextPosition, spray);
            transform.position = sprayedPosition;
        }
    }
}
