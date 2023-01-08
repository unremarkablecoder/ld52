using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerBox : MonoBehaviour {
    [SerializeField] private PowerConsumer consumer;
    [SerializeField] private GameObject powerLine;
    [SerializeField] private bool powerOn = true;
    [SerializeField] private Color onColor;
    [SerializeField] private Color offColor;

    private float lastToggleTimer = 0.5f;
    private AudioManager audioManager;


    private void Awake() {
        audioManager = FindObjectOfType<AudioManager>();
    }

    public void Start() {
        
        consumer.OnPowerToggle(powerOn);
        UpdatePowerLineGfx();
    }

    private void FixedUpdate() {
        lastToggleTimer += Time.fixedDeltaTime;
    }

    public void Toggle() {
        if (lastToggleTimer < 0.5f) {
            return;
        }

        audioManager.Play(audioManager.powerToggle);
        audioManager.Play(audioManager.door);
        lastToggleTimer = 0;
        powerOn = !powerOn;
        consumer.OnPowerToggle(powerOn);
        UpdatePowerLineGfx();
    }

    void UpdatePowerLineGfx() {
        SpriteRenderer[] powerLineGfx = powerLine.GetComponentsInChildren<SpriteRenderer>();
        foreach (var spriteRenderer in powerLineGfx) {
            spriteRenderer.color = powerOn ? onColor : offColor;
        }
    }
}