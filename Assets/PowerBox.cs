using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerBox : MonoBehaviour {
    [SerializeField] private PowerConsumer consumer;
    [SerializeField] private GameObject powerLine;
    [SerializeField] private bool powerOn = true;
    [SerializeField] private Color onColor;
    [SerializeField] private Color offColor;

    private bool toggled = false;

    public void Start() {
        UpdatePowerLineGfx();
    }

    public bool CanBeToggled() {
        return !toggled;
    }

    public void Toggle() {
        if (toggled) {
            return;
        }

        toggled = true;
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