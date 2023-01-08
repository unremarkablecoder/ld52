using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public enum EnemyState {
    Idle,
    StandingAtPoint,
    WalkingToPoint,
    LookingAround,
    LookingForPlayer,
    Chasing,
    Attacking,
    Investigating,
    BeingHarvested,
}

public class Guard : MonoBehaviour {
    [SerializeField] private GameObject alertIcon;
    [SerializeField] private GameObject susIcon;
    [SerializeField] private VisionCone visionCone;
    [SerializeField] private VisionCone alertVisionCone;
    [SerializeField] private PatrolPoint[] patrolPoints;
    [SerializeField] private float walkSpeed = 2;
    [SerializeField] private float runSpeed = 3;
    [SerializeField] private float visionAngle = 70;
    [SerializeField] private float visionLength = 8;
    [SerializeField] private float radius = 0.5f;
    [SerializeField] private float alertSpeed = 10.0f;
    [SerializeField] private float alertSpeedDecrease = 5.0f;
    [SerializeField] private float lookAroundChangeSpeedMin = 0.4f;
    [SerializeField] private float lookAroundChangeSpeedMax = 0.8f;
    [SerializeField] private float lookAroundInterval = 0.3f;
    private float rotateSpeed = 720;
    private float targetRot;

    public float Radius => radius;

    private EnemyState state = EnemyState.WalkingToPoint;

    private int currentPoint = 0;
    private float pointTimer;
    private float alertVisionLength = 0;
    private Player player;
    private Corpses corpses;
    private Vector3 pointToInvestigate;
    private float lookAroundTimer = 0;
    private float lookAroundVel = 0;
    private float stateTimer = 0;
    private bool playerInVision;
    private bool playerInAlertVision;

    private List<GuardCorpse> seenCorpses = new List<GuardCorpse>();

    public void SetState(EnemyState newState) {
        if (newState == state) {
            return;
        }

        state = newState;
        pointTimer = 0;
        lookAroundTimer = 0;
        stateTimer = 0;
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

        if (!corpses) {
            return;
        }

        susIcon.transform.rotation = Quaternion.identity;
        alertIcon.transform.rotation = Quaternion.identity;
        UpdateVision();

        switch (state) {
            case EnemyState.StandingAtPoint:
                DoStandingAtPoint(dt);
                break;
            case EnemyState.WalkingToPoint:
                DoWalkingToPoint(dt);
                break;
            case EnemyState.Chasing:
                DoChasing(dt);
                break;
            case EnemyState.Investigating:
                DoInvestigating(dt);
                break;
            case EnemyState.LookingAround:
                DoLookingAround(dt);
                break;
            case EnemyState.LookingForPlayer:
                DoLookingForPlayer(dt);
                break;
            case EnemyState.Attacking: 
                DoAttacking(dt);
                break;
        }

        stateTimer += dt;
    }

    void DoStandingAtPoint(float dt) {
        alertIcon.SetActive(false);
        pointTimer += dt;
        var lookDir = patrolPoints[currentPoint].lookDir;
        targetRot = Mathf.Atan2(lookDir.y, lookDir.x);
        if (pointTimer >= patrolPoints[currentPoint].timeToStay) {
            currentPoint = (currentPoint + 1) % patrolPoints.Length;
            SetState(EnemyState.WalkingToPoint);
            return;
        }

        if (ActOnPlayerVision(dt)) {
            SetState(EnemyState.Chasing);
            return;
        }

        if (CheckForCorpses(out var corpse)) {
            pointToInvestigate = corpse.transform.position;
            seenCorpses.Add(corpse);
            SetState(EnemyState.Investigating);
            return;
        }
    }

    void DoWalkingToPoint(float dt) {
        alertIcon.SetActive(false);
        pointTimer += dt;
        var targetPos = patrolPoints[currentPoint].pos;
        var pos = transform.position;
        var toTarget = targetPos - pos;
        var dir = toTarget.normalized;
        targetRot = Mathf.Atan2(dir.y, dir.x);
        pos += dir * (walkSpeed * dt);
        transform.position = pos;

        if (ActOnPlayerVision(dt)) {
            SetState(EnemyState.Chasing);
            return;
        }

        if (toTarget.sqrMagnitude < 0.01f) {
            SetState(EnemyState.StandingAtPoint);
            return;
        }
    }

    void DoChasing(float dt) {
        alertIcon.SetActive(true);
        alertIcon.transform.rotation = Quaternion.identity;
        susIcon.SetActive(false);
        ActOnPlayerVision(dt);
        var targetPos = pointToInvestigate;
        var pos = transform.position;
        var toTarget = targetPos - pos;
        var toTargetDir = toTarget.normalized;

        var hitInfo = Physics2D.CircleCast(pos, radius, toTargetDir, toTarget.magnitude, 1 << LayerMask.NameToLayer("Walls"));
        if (hitInfo.collider) {
            targetPos = hitInfo.centroid + hitInfo.normal * 0.5f;
            toTarget = targetPos - pos;
            toTargetDir = toTarget.normalized;
        }
            
        targetRot = Mathf.Atan2(toTargetDir.y, toTargetDir.x);
        pos += toTargetDir * (runSpeed * dt);
        transform.position = pos;
        var toPlayer = player.transform.position - pos;
        if (toPlayer.magnitude < 1.5f) {
            SetState(EnemyState.Attacking);
            return;
        }

        if (toTarget.sqrMagnitude < 0.01f) {
            SetState(EnemyState.LookingForPlayer);
            return;
        }
    }

    void DoInvestigating(float dt) {
        susIcon.SetActive(true);
        var targetPos = pointToInvestigate;
        var pos = transform.position;
        var toTarget = targetPos - pos;
        var dir = toTarget.normalized;
        targetRot = Mathf.Atan2(dir.y, dir.x);
        //just look at first
        if (stateTimer > 1.0f) {
            pos += dir * (walkSpeed * dt);
            transform.position = pos;
            if (toTarget.sqrMagnitude < 0.01f) {
                SetState(EnemyState.LookingAround);
                return;
            }
        }

        if (ActOnPlayerVision(dt)) {
            SetState(EnemyState.Chasing);
            return;
        }
    }

    void DoLookingAround(float dt) {
        susIcon.SetActive(true);
        lookAroundTimer -= dt;
        if (lookAroundTimer < 0) {
            lookAroundTimer = lookAroundInterval;
            lookAroundVel = Random.Range(lookAroundChangeSpeedMin, lookAroundChangeSpeedMax) * (Random.Range(0, 2) == 0 ? -1 : 1);
        }

        targetRot += lookAroundVel * dt;
        targetRot = Mathf.Repeat(targetRot, Mathf.PI * 2);
        
        if (ActOnPlayerVision(dt)) {
            SetState(EnemyState.Chasing);
            return;
        }
    }

    void DoLookingForPlayer(float dt) {
        alertIcon.SetActive(true);
        alertIcon.transform.rotation = Quaternion.identity;
        susIcon.SetActive(false);
        lookAroundTimer -= dt;
        if (lookAroundTimer < 0) {
            lookAroundTimer = lookAroundInterval;
            lookAroundVel = Random.Range(lookAroundChangeSpeedMin, lookAroundChangeSpeedMax) * (Random.Range(0, 2) == 0 ? -1 : 1);
        }

        targetRot += lookAroundVel * dt;
        targetRot = Mathf.Repeat(targetRot, Mathf.PI * 2);
        
        if (ActOnPlayerVision(dt)) {
            SetState(EnemyState.Chasing);
            return;
        }
    }

    void DoAttacking(float dt) {
        alertIcon.SetActive(true);
        susIcon.SetActive(false);
        if (stateTimer > 0.5f) {
            //game over
            player.Die();
        }
    }

    bool CheckForCorpses(out GuardCorpse corpseFound) {
        var pos = transform.position;
        var dir = transform.right;
        var corpseList = corpses.GetCorpses();

        foreach (var corpse in corpseList) {
            if (seenCorpses.Contains(corpse)) {
                continue;
            }

            var toCorpse = corpse.transform.position - pos;
            if (toCorpse.magnitude > visionLength) {
                continue;
            }

            var toCorpseDir = toCorpse.normalized;
            float angleDiff = Vector3.Angle(toCorpseDir, dir);
            if (angleDiff < visionAngle / 2) {
                var corpseHitInfo = Physics2D.Linecast(pos, corpse.transform.position, ~(1 << LayerMask.NameToLayer("Player")));
                if (!corpseHitInfo.collider) {
                    corpseFound = corpse;
                    //see corpse
                    pointToInvestigate = corpse.transform.position;
                    SetState(EnemyState.Investigating);
                    seenCorpses.Add(corpse);
                    return true;
                }
            }
        }

        corpseFound = null;
        return false;
    }

    void UpdateVision() {
        var pos = transform.position;
        float dt = Time.fixedDeltaTime;

        float rot = Mathf.Atan2(transform.right.y, transform.right.x);
        rot = Mathf.MoveTowardsAngle(rot * Mathf.Rad2Deg, targetRot * Mathf.Rad2Deg, rotateSpeed * dt) * Mathf.Deg2Rad;
        transform.right = new Vector3(Mathf.Cos(rot), Mathf.Sin(rot));

        var dir = transform.right;

        const int num = 15;
        float angleStepRad = visionAngle / num * Mathf.Deg2Rad;
        float dirRad = Mathf.Atan2(dir.y, dir.x);
        Vector3[] endPoints = new Vector3[num];
        Vector3[] alertEndPoints = new Vector3[num];
        playerInVision = false;
        playerInAlertVision = false;
        for (int i = 0; i < num; ++i) {
            float rad = dirRad - ((num / 2) * angleStepRad) + i * angleStepRad;
            var lineDir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad));
            var hitInfo = Physics2D.Linecast(pos, pos + lineDir * visionLength);
            if (hitInfo.collider) {
                Debug.DrawLine(pos, pos + lineDir * hitInfo.distance, Color.red);
                if (hitInfo.collider.gameObject.layer == LayerMask.NameToLayer("Player")) {
                    playerInVision = true;
                    if (hitInfo.distance < alertVisionLength) {
                        playerInAlertVision = true;
                    }

                    //cast again without player
                    hitInfo = Physics2D.Linecast(pos, pos + lineDir * visionLength, ~(1 << LayerMask.NameToLayer("Player")));
                }
            }

            if (hitInfo.collider) {
                endPoints[i] = pos + lineDir * hitInfo.distance;
                alertEndPoints[i] = pos + lineDir * Mathf.Min(hitInfo.distance, alertVisionLength);
            }
            else {
                Debug.DrawLine(pos, pos + lineDir * visionLength, Color.red);
                endPoints[i] = pos + lineDir * visionLength;
                alertEndPoints[i] = pos + lineDir * alertVisionLength;
            }
        }

        visionCone.SetEndPoints(endPoints);
        alertVisionCone.SetEndPoints(alertEndPoints);
    }

    //return true if started chasing
    bool ActOnPlayerVision(float dt) {
        if (playerInVision) {
            alertVisionLength = Mathf.Min(visionLength, alertVisionLength + alertSpeed * dt);
            pointToInvestigate = player.transform.position;
            if (playerInAlertVision) {
                return true;
            }
            else {
                susIcon.SetActive(true);
            }
        }
        else {
            alertVisionLength = Mathf.Max(0, alertVisionLength - alertSpeedDecrease * dt);
            if (state != EnemyState.Investigating && state != EnemyState.LookingAround) {
                susIcon.SetActive(false);
            }
        }

        return false;
    }

    private void OnDrawGizmos() {
        Handles.Label(transform.position, currentPoint + ", " + state.ToString() + ", " + pointTimer.ToString("F1"));
    }

    public void Init(Player player, Corpses corpses) {
        this.player = player;
        this.corpses = corpses;
    }

    public bool CanKill() {
        return state != EnemyState.Chasing && state != EnemyState.Attacking;
    }
}