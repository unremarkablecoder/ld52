using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Enemies : MonoBehaviour {
    [SerializeField] private Player player;
    [SerializeField] private Corpses corpses;
    private List<Guard> guards;

    private AudioManager audioManager;

    private void Awake() {
        audioManager = FindObjectOfType<AudioManager>();
    }

    void Update() {
        if (guards == null) {
            return;
        }
        foreach (var guard in guards) {
            if (guard.IsAlert()) {
                audioManager.SetAlert(true);
                return;

            }
        }
        audioManager.SetAlert(false);
    }

    public void OnLevelLoaded() {
        guards = GameObject.FindObjectsOfType<Guard>().ToList();
        foreach (var guard in guards) {
            guard.Init(player, corpses);
        }
    }

    public List<Guard> GetGuards() {
        return guards;
    }

    public void RemoveGuard(Guard guard) {
        guards.Remove(guard);
        Destroy(guard.gameObject);
    }
}
