using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Corpses : MonoBehaviour {
    [SerializeField] private GuardCorpse guardCorpsePrefab;

    private List<GuardCorpse> corpses = new List<GuardCorpse>();
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SpawnCorpse(Vector3 pos, Vector3 dir) {
        var obj = Instantiate(guardCorpsePrefab, transform);
        obj.transform.position = pos;
        obj.transform.right = dir;
        corpses.Add(obj);
    }

    public List<GuardCorpse> GetCorpses() {
        return corpses;
    }
    
    public void RemoveCorpse(GuardCorpse corpse) {
        corpses.Remove(corpse);
        Destroy(corpse.gameObject);
    }

    public void Clear() {
        foreach (var guardCorpse in corpses) {
            Destroy(guardCorpse.gameObject);
        }
        corpses.Clear();
    }
}
