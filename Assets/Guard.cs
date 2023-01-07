using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public enum EnemyState {
    Idle,
    StandingAtPoint,
    WalkingToPoint,
    Chasing,
    BeingHarvested,
}

public class Guard : MonoBehaviour {
    [SerializeField] private VisionCone visionCone;
    [SerializeField] private PatrolPoint[] patrolPoints;
    [SerializeField] private float walkSpeed = 2;
    [SerializeField] private float visionAngle = 70;
    [SerializeField] private float visionLength = 8;
    [SerializeField] private float radius = 0.5f;
    private float rotateSpeed = 720;
    private float targetRot;

    public float Radius => radius;

    private EnemyState state = EnemyState.WalkingToPoint;

    private int currentPoint = 0;
    private float pointTimer;

    public void SetState(EnemyState newState) {
        state = newState;
        pointTimer = 0;
    }

    void Awake() {
        if (patrolPoints.Length == 0) {
            patrolPoints = new[] { new PatrolPoint { pos = transform.position, lookDir = transform.right, timeToStay = 10 } };
        }
    }

    int GetPrevPoint() {
        if (currentPoint == 0) {
           return patrolPoints.Length - 1;
        }

        return currentPoint - 1;
    }

    private void FixedUpdate() {
        float dt = Time.fixedDeltaTime;

        switch (state) {
            case EnemyState.StandingAtPoint: {
                pointTimer += dt;
                var lookDir = patrolPoints[currentPoint].lookDir;
                targetRot = Mathf.Atan2(lookDir.y, lookDir.x);
                if (pointTimer >= patrolPoints[currentPoint].timeToStay) {
                    currentPoint = (currentPoint + 1) % patrolPoints.Length;
                    SetState(EnemyState.WalkingToPoint);
                }
            }
                break;
            case EnemyState.WalkingToPoint: {
                pointTimer += dt;
                int prevPoint = GetPrevPoint();
                var targetPos = patrolPoints[currentPoint].pos;
                var pos = transform.position;
                var toTarget = targetPos - pos;
                var dir = toTarget.normalized;
                targetRot = Mathf.Atan2(dir.y, dir.x);
                pos += dir * (walkSpeed * dt);
                transform.position = pos;
                if (toTarget.sqrMagnitude < 0.01f) {
                     SetState(EnemyState.StandingAtPoint);
                }
            }
                break;
        }

        if (state != EnemyState.BeingHarvested) {
            UpdateVision();
        }
    }

    void UpdateVision() {
        var pos = transform.position;
        float dt = Time.fixedDeltaTime;
        
        float rot = Mathf.Atan2(transform.right.y, transform.right.x);
        rot = Mathf.MoveTowardsAngle(rot * Mathf.Rad2Deg, targetRot * Mathf.Rad2Deg, rotateSpeed * dt) * Mathf.Deg2Rad;
        transform.right = new Vector3(Mathf.Cos(rot), Mathf.Sin(rot));
        
        var dir = transform.right;

        const int num = 9;
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

    private void OnDrawGizmos() {
        Handles.Label(transform.position, currentPoint + ", " + state.ToString() + ", " + pointTimer.ToString("F1"));
    }
}