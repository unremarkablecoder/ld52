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
    Backtrack,
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
    [SerializeField] private float lookingForPlayerAbortTime = 5.0f;
    [SerializeField] private float lookingAroundAbortTime = 5.0f;
    private float rotateSpeed = 720;
    private float targetRot;

    public float Radius => radius;

    private EnemyState state = EnemyState.WalkingToPoint;

    private int currentPoint = 0;
    private float alertVisionLength = 0;
    private Player player;
    private Corpses corpses;
    private Vector3 pointToInvestigate;
    private float lookAroundTimer = 0;
    private float lookAroundVel = 0;
    private float stateTimer = 0;
    private bool playerInVision;
    private bool playerInAlertVision;
    private bool playedHuhSound = false;

    private List<GuardCorpse> seenCorpses = new List<GuardCorpse>();
    private List<Vector3> backtrackPoints = new List<Vector3>();
    private Vector3 prevPos;
    private AudioManager audioManager;

    public void SetState(EnemyState newState) {
        if (newState == state) {
            return;
        }

        state = newState;
        lookAroundTimer = 0;
        stateTimer = 0;

        switch (newState) {
            case EnemyState.Investigating:
                audioManager.Play(audioManager.huh);
                break;
            case EnemyState.Chasing:
                audioManager.Play(audioManager.alertSound);
                audioManager.Play(audioManager.overThere);
                break;
        }
    }

    void Awake() {
        if (patrolPoints.Length == 0) {
            patrolPoints = new[] { new PatrolPoint { pos = transform.position, lookDir = transform.right, timeToStay = 10 } };
        }

        prevPos = transform.position;
        audioManager = FindObjectOfType<AudioManager>();
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
            case EnemyState.Backtrack:
                DoBacktrack(dt);
                break;
        }

        stateTimer += dt;
        prevPos = transform.position;
    }

    void DoStandingAtPoint(float dt) {
        backtrackPoints.Clear();
        alertIcon.SetActive(false);
        var lookDir = patrolPoints[currentPoint].lookDir.normalized;
        targetRot = Mathf.Atan2(lookDir.y, lookDir.x);
        if (stateTimer >= patrolPoints[currentPoint].timeToStay) {
            if (patrolPoints.Length > 1) {
                currentPoint = (currentPoint + 1) % patrolPoints.Length;
                SetState(EnemyState.WalkingToPoint);
                return;
            }
            else {
                stateTimer = 0;
            }
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
        backtrackPoints.Clear();
        alertIcon.SetActive(false);
        var targetPos = patrolPoints[currentPoint].pos;
        var pos = transform.position;
        var toTarget = targetPos - pos;
        var dir = toTarget.normalized;
        targetRot = Mathf.Atan2(dir.y, dir.x);
        pos += dir * Mathf.Min(toTarget.magnitude, walkSpeed * dt);
        transform.position = pos;

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
        UpdateBacktracking();
        var toPlayer = player.transform.position - pos;
        if (toPlayer.magnitude < 1.1f) {
            SetState(EnemyState.Attacking);
            return;
        }

        if (toTarget.sqrMagnitude < 0.01f) {
            SetState(EnemyState.LookingForPlayer);
            return;
        }
    }

    void DoInvestigating(float dt) {
        alertIcon.SetActive(false);
        susIcon.SetActive(true);
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
        //just look at first
        if (stateTimer > 1.0f) {
            pos += toTargetDir * (walkSpeed * dt);
            transform.position = pos;
            if (toTarget.sqrMagnitude < 0.01f) {
                audioManager.Play(audioManager.body);
                SetState(EnemyState.LookingAround);
                return;
            }
        }

        UpdateBacktracking();

        if (ActOnPlayerVision(dt)) {
            SetState(EnemyState.Chasing);
            return;
        }
    }

    void DoLookingAround(float dt) {
        alertIcon.SetActive(false);
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
        
        if (CheckForCorpses(out var corpse)) {
            pointToInvestigate = corpse.transform.position;
            seenCorpses.Add(corpse);
            SetState(EnemyState.Investigating);
            return;
        }
        
        if (stateTimer >= lookingAroundAbortTime) {
            audioManager.Play(audioManager.ohWell);
            SetState(EnemyState.Backtrack);
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
        
        if (CheckForCorpses(out var corpse)) {
            pointToInvestigate = corpse.transform.position;
            seenCorpses.Add(corpse);
            SetState(EnemyState.Investigating);
            return;
        }

        if (stateTimer >= lookingForPlayerAbortTime) {
            audioManager.Play(audioManager.disappeared);
            SetState(EnemyState.Backtrack);
            return;
        }
    }

    void DoAttacking(float dt) {
        alertIcon.SetActive(true);
        susIcon.SetActive(false);
        if (stateTimer > 0.1f) {
            //game over
            player.Die();
        }
    }

    void DoBacktrack(float dt) {
        if (ActOnPlayerVision(dt)) {
            SetState(EnemyState.Chasing);
            return;
        }
        alertIcon.SetActive(false);
        susIcon.SetActive(false);
        
        var pos = transform.position;
        Vector3 targetPos = pos;
        //can I reach any patrol points?
        int patrolPointIndexFound = 0;
        bool canReachPatrolPoints = false;
        for (int i = 0; i < patrolPoints.Length; ++i) {
            var patrolPoint = patrolPoints[i];
            var toPoint = patrolPoint.pos - pos;
            var hitInfo = Physics2D.CircleCast(pos, radius-0.01f, toPoint.normalized, toPoint.magnitude, 1 << LayerMask.NameToLayer("Walls"));
            if (!hitInfo.collider) {
                canReachPatrolPoints = true;
                targetPos = patrolPoint.pos;
                patrolPointIndexFound = i;
                break;
            }
        }

        if (!canReachPatrolPoints) {
            //check backtrack points
            bool canReachBacktrackPoints = false;
            foreach (var backtrackPoint in backtrackPoints) {
                var toPoint = backtrackPoint - pos;
                var hitInfo = Physics2D.CircleCast(pos, radius-0.01f, toPoint.normalized, toPoint.magnitude, 1 << LayerMask.NameToLayer("Walls"));
                if (!hitInfo.collider) {
                    canReachBacktrackPoints = true;
                    targetPos = backtrackPoint;
                    break;
                }
            }

            if (!canReachBacktrackPoints) {
                Debug.LogWarning("can't backtrack");
                return;
            }
        }

        var toTarget = targetPos - pos;
        var dir = toTarget.normalized;
        targetRot = Mathf.Atan2(dir.y, dir.x);
        pos += dir * Mathf.Min(toTarget.magnitude, walkSpeed * dt);
        toTarget = targetPos - pos;
        transform.position = pos;
        if (toTarget.sqrMagnitude < 0.01f) {
            transform.position = targetPos;
            if (canReachPatrolPoints) {
                currentPoint = patrolPointIndexFound;
                SetState(EnemyState.WalkingToPoint);
                return;
            }
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

        const int num = VisionCone.numEndPoints;
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
                if (!playedHuhSound) {
                    playedHuhSound = true;
                    audioManager.Play(audioManager.huh);
                }
            }
        }
        else {
            playedHuhSound = false;
            alertVisionLength = Mathf.Max(0, alertVisionLength - alertSpeedDecrease * dt);
            if (state != EnemyState.Investigating && state != EnemyState.LookingAround) {
                susIcon.SetActive(false);
            }
        }

        return false;
    }

    #if UNITY_EDITOR
    private void OnDrawGizmos() {
        Handles.Label(transform.position, currentPoint + ", " + state.ToString() + ", " + stateTimer.ToString("F1"));
    }

    private void OnDrawGizmosSelected() {
        foreach (var backtrackPoint in backtrackPoints) {
            Handles.PositionHandle(backtrackPoint, Quaternion.identity);
        }
    }
    #endif

    public void Init(Player player, Corpses corpses) {
        this.player = player;
        this.corpses = corpses;
    }

    public bool CanKill() {
        return state != EnemyState.Chasing && state != EnemyState.Attacking;
    }

    void UpdateBacktracking() {
        var pos = transform.position;
        //can I reach any patrol points?
        bool canReachPatrolPoints = false;
        foreach (var patrolPoint in patrolPoints) {
            var toPoint = patrolPoint.pos - pos;
            var hitInfo = Physics2D.CircleCast(pos, radius, toPoint.normalized, toPoint.magnitude, 1 << LayerMask.NameToLayer("Walls"));
            if (!hitInfo.collider) {
                canReachPatrolPoints = true;
                break;
            }
        }

        if (canReachPatrolPoints) {
            return;
        }

        //check backtrack points
        bool canReachBacktrackPoints = false;
        foreach (var backtrackPoint in backtrackPoints) {
            var toPoint = backtrackPoint - pos;
            var hitInfo = Physics2D.CircleCast(pos, radius, toPoint.normalized, toPoint.magnitude, 1 << LayerMask.NameToLayer("Walls"));
            if (!hitInfo.collider) {
                canReachBacktrackPoints = true;
                break;
            }
        }

        if (canReachBacktrackPoints) {
            return;
        }

        //should have been able to reach it last frame, so add that position
        backtrackPoints.Add(prevPos);
    }

    public bool IsAlert() {
        return state == EnemyState.Chasing || state == EnemyState.Attacking;
    }
}