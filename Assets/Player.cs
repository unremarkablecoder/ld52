using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerState {
    Idle,
    Walking,
    Killing,
    PickingUpCorpse,
    IdleWithCorpse,
    WalkingWithCorpse,
    DroppingCorpse,
    Dying,
    Win
}

public class Player : MonoBehaviour {
    [SerializeField] private GameLoop gameLoop;
    [SerializeField] private Enemies enemies;
    [SerializeField] private Blood blood;
    [SerializeField] private Corpses corpses;
    [SerializeField] private GameObject killPrompt;
    [SerializeField] private GameObject grabPrompt;
    [SerializeField] private GameObject dropPrompt;
    [SerializeField] private GameObject togglePrompt;
    private float radius = 0.5f;
    private float maxSpeed = 4.5f;
    private float maxSpeedWithCorpse = 2.5f;
    private float killRange = 0.8f;
    private float accel = 20;
    private float decel = 25;
    private float rotateSpeed = 720;
    private float harvestDuration = 1.167f;
    private float corpsePickupRadius = 1.1f;
    private Vector3 vel;
    private float targetRot;

    private PlayerState currentState = PlayerState.Idle;
    private float stateTimer;

    private Camera cam;

    private Guard currentKillTarget = null;
    private GuardCorpse currentCorpseTarget = null;
    private Animator animator;

    private PowerBox[] powerBoxes;
    private GameObject goal;

    void Awake() {
        cam = Camera.main;
        animator = GetComponent<Animator>();
    }

    // Start is called before the first frame update
    void Start() {
    }

    // Update is called once per frame
    void FixedUpdate() {
        if (!animator) {
            return;
        }
        float dt = Time.fixedDeltaTime;
        var pos = transform.position;

        togglePrompt.SetActive(false);
        UpdateState(dt);

        float rot = Mathf.Atan2(transform.right.y, transform.right.x);
        rot = Mathf.MoveTowardsAngle(rot * Mathf.Rad2Deg, targetRot * Mathf.Rad2Deg, rotateSpeed * dt) * Mathf.Deg2Rad;
        transform.right = new Vector3(Mathf.Cos(rot), Mathf.Sin(rot));
    }

    void UpdateState(float dt) {
        switch (currentState) {
            case PlayerState.Idle:
            case PlayerState.Walking:
                DoIdleOrWalking(dt);
                break;
            case PlayerState.Killing:
                DoKilling(dt);
                break;
            case PlayerState.PickingUpCorpse:
                DoPickingUpCorpse(dt);
                break;
            case PlayerState.DroppingCorpse:
                DoDroppingCorpse(dt);
                break;
            case PlayerState.IdleWithCorpse:
            case PlayerState.WalkingWithCorpse:
                DoIdleOrWalkingWithCorpse(dt);
                break;
        }

        stateTimer += dt;
    }


    void UpdateVelocityFromInput(float dt) {
        if (Input.GetKey(KeyCode.A)) {
            vel.x -= accel * dt;
        }
        else if (Input.GetKey(KeyCode.D)) {
            vel.x += accel * dt;
        }
        else {
            vel.x -= Mathf.Min(Mathf.Abs(vel.x), decel * dt) * Mathf.Sign(vel.x);
        }

        if (Input.GetKey(KeyCode.W)) {
            vel.y += accel * dt;
        }
        else if (Input.GetKey(KeyCode.S)) {
            vel.y -= accel * dt;
        }
        else {
            vel.y -= Mathf.Min(Mathf.Abs(vel.y), decel * dt) * Mathf.Sign(vel.y);
        }

        vel = Vector3.ClampMagnitude(vel, HasCorpse() ? maxSpeedWithCorpse : maxSpeed);
        
    }

    void DoIdleOrWalking(float dt) {
        var pos = transform.position;

        UpdateVelocityFromInput(dt);

        if (vel.sqrMagnitude > float.Epsilon) {
            SetState(PlayerState.Walking);
            targetRot = Mathf.Atan2(vel.normalized.y, vel.normalized.x);
        }
        else {
            SetState(PlayerState.Idle);
        }

        pos += vel * dt;

        pos = HandleCollision(pos);
        pos = CheckEnemies(pos);

        transform.position = pos;

        if (CheckForGoal()) {
            return;
        }

        dropPrompt.SetActive(false);

        killPrompt.SetActive(currentKillTarget);
        if (currentKillTarget) {
            killPrompt.transform.position = currentKillTarget.transform.position;
            if (Input.GetKey(KeyCode.J)) {
                SetState(PlayerState.Killing);
                currentKillTarget.SetState(EnemyState.BeingHarvested);
                return;
            }
        }

        UpdateCorpseTarget();
        grabPrompt.SetActive(currentCorpseTarget);
        if (currentCorpseTarget) {
            grabPrompt.transform.position = currentCorpseTarget.transform.position;
            if (Input.GetKey(KeyCode.K)) {
                SetState(PlayerState.PickingUpCorpse);
                return;
            }
        }
        
        CheckPowerBoxes();
    }

    void DoIdleOrWalkingWithCorpse(float dt) {
        var pos = transform.position;

        UpdateVelocityFromInput(dt);

        if (vel.sqrMagnitude > float.Epsilon) {
            SetState(PlayerState.WalkingWithCorpse);
            targetRot = Mathf.Atan2(vel.normalized.y, vel.normalized.x);
        }
        else {
            SetState(PlayerState.IdleWithCorpse);
        }


        pos += vel * dt;

        pos = HandleCollision(pos);
        pos = CheckEnemies(pos);
        
        transform.position = pos;
        
        if (CheckForGoal()) {
            return;
        }

        grabPrompt.SetActive(false);
        killPrompt.SetActive(false);
        dropPrompt.SetActive(true);
        dropPrompt.transform.position = currentCorpseTarget.transform.position;

        if (Input.GetKey(KeyCode.K)) {
            SetState(PlayerState.DroppingCorpse);
        }
    }

    bool HasCorpse() {
        return currentState == PlayerState.IdleWithCorpse || currentState == PlayerState.WalkingWithCorpse;
    }

    void DoKilling(float dt) {
        animator.SetTrigger("kill");
        var pos = transform.position;
        var toTarget = currentKillTarget.transform.position - pos;
        var toTargetDir = toTarget.normalized;
        pos += ((currentKillTarget.transform.position - toTargetDir * (radius + currentKillTarget.Radius)) - pos) * 0.1f;
        targetRot = Mathf.Atan2(toTargetDir.y, toTargetDir.x);
        transform.position = pos;

        if (Random.Range(0.0f, 1.0f) < 0.15f) {
            blood.SpawnBlood(currentKillTarget.transform.position, 1.5f, 0.05f, 0.2f);
        }

        if (stateTimer >= harvestDuration) {
            for (int i = 0; i < 3; ++i) {
                blood.SpawnBlood(currentKillTarget.transform.position, 1.0f, 0.3f, 0.8f);
            }

            corpses.SpawnCorpse(currentKillTarget.transform.position, currentKillTarget.transform.right);

            enemies.RemoveGuard(currentKillTarget);
            currentKillTarget = null;
            SetState(PlayerState.Idle);
        }
    }

    void DoPickingUpCorpse(float dt) {
        animator.SetTrigger("pickingUpCorpse");
        var pos = transform.position;
        var toTarget = currentCorpseTarget.transform.position - pos;
        var toTargetDir = toTarget.normalized;
        pos += ((currentCorpseTarget.transform.position - toTargetDir * (corpsePickupRadius)) - pos) * 0.1f;
        targetRot = Mathf.Atan2(toTargetDir.y, toTargetDir.x);
        transform.position = pos;
        if (stateTimer >= 0.5f) {
            currentCorpseTarget.transform.parent = transform;
            SetState(PlayerState.IdleWithCorpse);
        }
    }

    private void DoDroppingCorpse(float dt) {
        animator.SetTrigger("pickingUpCorpse");
        if (stateTimer >= 0.5f) {
            currentCorpseTarget.transform.parent = corpses.transform;
            SetState(PlayerState.Idle);
        }
    }

    Vector3 HandleCollision(Vector3 pos) {
        var oldPos = transform.position;
        var hitInfo = Physics2D.CircleCast(oldPos, radius, (pos - oldPos).normalized, 0.1f, ~(1 << LayerMask.NameToLayer("Player")));
        if (hitInfo.collider) {
            return hitInfo.point + hitInfo.normal * (radius + 0.01f);
        }

        return pos;
    }

    Vector3 CheckEnemies(Vector3 pos) {
        var guards = enemies.GetGuards();
        currentKillTarget = null;
        foreach (var guard in guards) {
            var toGuard = guard.transform.position - pos;
            float dist = toGuard.magnitude;

            if (dist < radius + guard.Radius) {
                pos = guard.transform.position - toGuard.normalized * (radius + guard.Radius);
            }

            if (dist < killRange + radius + guard.Radius && guard.CanKill()) {
                //make sure no wall in way
                var hitInfo = Physics2D.CircleCast(pos, radius, toGuard.normalized, dist, 1 << LayerMask.NameToLayer("Walls"));
                if (!hitInfo.collider) {
                    currentKillTarget = guard;
                }
            }
        }

        return pos;
    }

    void SetState(PlayerState newState) {
        currentState = newState;
        stateTimer = 0;
        switch (currentState) {
            case PlayerState.Idle:
                animator.SetTrigger("idle");
                break;
            case PlayerState.Walking:
                animator.SetTrigger("walking");
                break;
            case PlayerState.Killing:
                animator.SetTrigger("kill");
                break;
            case PlayerState.IdleWithCorpse:
                animator.SetTrigger("idleWithCorpse");
                break;
            case PlayerState.WalkingWithCorpse:
                animator.SetTrigger("walkingWithCorpse");
                break;
            case PlayerState.PickingUpCorpse:
            case PlayerState.DroppingCorpse:
                animator.SetTrigger("pickingUpCorpse");
                break;
        }
    }

    void UpdateCorpseTarget() {
        var pos = transform.position;
        var corpseList = corpses.GetCorpses();
        currentCorpseTarget = null;
        foreach (var corpse in corpseList) {
            var toCorpse = corpse.transform.position - pos;
            float dist = toCorpse.magnitude;

            if (dist < corpsePickupRadius) {
                currentCorpseTarget = corpse;
            }
        }
    }

    public void OnLevelLoaded(Vector3 spawnPos, GameObject goal) {
        this.goal = goal;
        gameObject.SetActive(true);
        powerBoxes = GameObject.FindObjectsOfType<PowerBox>();
        transform.position = spawnPos;
        SetState(PlayerState.Idle);
        currentCorpseTarget = null;
        currentKillTarget = null;

    }

    public void Die() {
        SetState(PlayerState.Dying);
        gameLoop.Die();
        dropPrompt.SetActive(false);
        killPrompt.SetActive(false);
        dropPrompt.SetActive(false);
        togglePrompt.SetActive(false);
        gameObject.SetActive(false);
    }

    void CheckPowerBoxes() {
        togglePrompt.SetActive(false);
        foreach (var powerBox in powerBoxes) {
            var toBox = powerBox.transform.position - transform.position;
            if (toBox.sqrMagnitude < 1 && Vector3.Angle(-powerBox.transform.right, toBox.normalized) < 80) {
                togglePrompt.SetActive(true);
                togglePrompt.transform.position = powerBox.transform.position;
                if (Input.GetKey(KeyCode.L)) {
                    powerBox.Toggle();
                }
            }
        }
    }

    bool CheckForGoal() {
        var pos = transform.position;
        if ((goal.transform.position - pos).sqrMagnitude < 2) {
            SetState(PlayerState.Win);
            gameLoop.Win();
            dropPrompt.SetActive(false);
            killPrompt.SetActive(false);
            dropPrompt.SetActive(false);
            togglePrompt.SetActive(false);
            gameObject.SetActive(false);
            return true;
        }

        return false;
    }
}