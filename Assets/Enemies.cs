using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Enemies : MonoBehaviour {
    private List<Guard> guards;
    
    // Start is called before the first frame update
    void Start() {
        guards = GameObject.FindObjectsOfType<Guard>().ToList();
    }

    public List<Guard> GetGuards() {
        return guards;
    }

    public void RemoveGuard(Guard guard) {
        guards.Remove(guard);
        Destroy(guard.gameObject);
    }
}
