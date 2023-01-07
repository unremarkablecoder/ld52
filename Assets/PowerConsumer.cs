using UnityEngine;

public class PowerConsumer : MonoBehaviour {
    protected bool powerOn;
    public virtual void OnPowerToggle(bool on) {
        powerOn = on;
    }
}
