using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Enemies : MonoBehaviour {
    [SerializeField] private Player player;
    [SerializeField] private Corpses corpses;
    private List<Guard> guards;
    
    // Start is called before the first frame update
    void Start() {
        
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
