using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SecurityCam : MonoBehaviour {
    [SerializeField] private float rotationDegrees = 90;
    [SerializeField] private float interval = 5;

    private float startRot;
    
    // Start is called before the first frame update
    void Start() {
        startRot = Mathf.Atan2(transform.right.y, transform.right.x);
    }

    // Update is called once per frame
    void FixedUpdate() {
        float t = (Time.fixedTime % interval) / interval;

        float rot = startRot + Mathf.Sin(t * Mathf.PI * 2) * (rotationDegrees*0.5f * Mathf.Deg2Rad);
        transform.right = new Vector3(Mathf.Cos(rot), Mathf.Sin(rot));

    }
}