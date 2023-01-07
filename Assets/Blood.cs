using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Blood : MonoBehaviour {
    [SerializeField] private GameObject prefab;

    private List<GameObject> blood = new List<GameObject>();

    public void SpawnBlood(Vector3 pos, float radius, float minSize, float maxSize) {
        var obj = Instantiate(prefab, transform);
        obj.transform.position = pos + new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f), 0).normalized * Random.Range(0.0f, radius);
        float scale = Random.Range(minSize, maxSize);
        obj.transform.localScale = Vector3.one * scale;
        blood.Add(obj);
    }

    public void Clear() {
        foreach (var o in blood) {
            Destroy(o);
        }
        blood.Clear();
    }
}
