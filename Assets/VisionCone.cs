using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisionCone : MonoBehaviour {
    private Mesh mesh;

    private const int numEndPoints = 15;
    private Vector3[] vertices = new Vector3[numEndPoints+1];

    private int[] triangles = new int[(numEndPoints-1)*3];
    
    // Start is called before the first frame update
    void Start() {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        for (int i = 0; i < numEndPoints-1; ++i) {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i+2;
            triangles[i * 3 + 2] = i+1;
        }
        mesh.vertices = vertices;
        mesh.triangles = triangles;

    }

    public void SetEndPoints(Vector3[] endPoints) {
        for (int i = 1; i <= numEndPoints; ++i) {
            vertices[i] = transform.worldToLocalMatrix.MultiplyPoint(endPoints[i - 1]);
        }

        mesh.vertices = vertices;
    }
}
