using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SecurityCam : PowerConsumer {
    [SerializeField] private VisionCone visionCone;
    [SerializeField] private float rotationDegrees = 90;
    [SerializeField] private float interval = 5;
    [SerializeField] private float visionAngle = 40;
    [SerializeField] private float visionLength = 7;

    private float startRot;
    private float timer;

    private bool powerOn = true;
    
    // Start is called before the first frame update
    void Start() {
        startRot = Mathf.Atan2(transform.right.y, transform.right.x);
    }

    // Update is called once per frame
    void FixedUpdate() {
        if (!powerOn) {
            return;
        }
        timer += Time.fixedDeltaTime;
        float t = (timer % interval) / interval;

        float rot = startRot + Mathf.Sin(t * Mathf.PI * 2) * (rotationDegrees*0.5f * Mathf.Deg2Rad);
        transform.right = new Vector3(Mathf.Cos(rot), Mathf.Sin(rot));
        
        UpdateVision();
    }
    
    void UpdateVision() {
        var pos = transform.position;
        float dt = Time.fixedDeltaTime;
        
        var dir = transform.right;

        const int num = 15;
        float angleStepRad = visionAngle / num * Mathf.Deg2Rad;
        float dirRad = Mathf.Atan2(dir.y, dir.x);
        Vector3[] endPoints = new Vector3[num];
        for (int i = 0; i < num; ++i) {
            float rad = dirRad - ((num/2) * angleStepRad) + i * angleStepRad;
            var lineDir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad));
            var hitInfo = Physics2D.Linecast(pos, pos + lineDir * visionLength);
            if (hitInfo.collider) {
                Debug.DrawLine(pos, pos + lineDir * hitInfo.distance, Color.red);
                endPoints[i] = pos + lineDir * hitInfo.distance;
            }
            else {
                Debug.DrawLine(pos, pos + lineDir * visionLength, Color.red);
                endPoints[i] = pos + lineDir * visionLength;
            }
        }
        visionCone.SetEndPoints(endPoints);

    }

    public override void OnPowerToggle(bool on) {
        base.OnPowerToggle(on);
        powerOn = on;
        visionCone.gameObject.SetActive(on);
    }
}