using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : PowerConsumer {
    [SerializeField] private Vector3 offPos;
    [SerializeField] private Vector3 onPos;

    
    void Start() {
    }
    
    public override void OnPowerToggle(bool on) {
        base.OnPowerToggle(on);
    }

    private void FixedUpdate() {
        var pos = transform.position;
        var targetPos = powerOn ? onPos : offPos;

        pos += (targetPos - pos) * 0.05f;
        transform.position = pos;
    }
}