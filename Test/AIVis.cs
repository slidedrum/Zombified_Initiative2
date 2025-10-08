using UnityEngine;
using System.Collections.Generic;

public static class VisibilityUtility
{
    /// <summary>
    /// Returns true if the target is visible from the observer. Uses a horizontal FOV
    /// and does at most 100 random raycasts to candidate triangles.
    /// </summary>
    public static bool IsTargetVisible(GameObject target, GameObject observer, float horizontalFOVDegrees = 90f, int maxRaycasts = 100)
    {
        if (target == null || observer == null)
            return false;

        Vector3 fromPoint = observer.transform.position;
        Vector3 viewDir = observer.transform.forward;

        // Convert horizontal FOV to facingDotThreshold
        float halfFovRad = (horizontalFOVDegrees * 0.5f) * Mathf.Deg2Rad;
        float facingDotThreshold = Mathf.Cos(halfFovRad);

        List<TriangleData> candidateTris = new();

        // Collect triangles from MeshRenderers
        foreach (var mr in target.GetComponentsInChildren<MeshRenderer>())
        {
            var mf = mr.GetComponent<MeshFilter>();
            if (!mf || !mf.sharedMesh) continue;

            AddMeshTriangles(mf.sharedMesh, mr.transform, mr, fromPoint, viewDir, facingDotThreshold, candidateTris);
        }

        // Collect triangles from SkinnedMeshRenderers
        foreach (var smr in target.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            var baked = new Mesh();
            smr.BakeMesh(baked);
            AddMeshTriangles(baked, smr.transform, smr, fromPoint, viewDir, facingDotThreshold, candidateTris);
        }

        if (candidateTris.Count == 0)
            return false;

        // Shuffle list to pick random triangles
        for (int i = 0; i < candidateTris.Count; i++)
        {
            int j = Random.Range(i, candidateTris.Count);
            var temp = candidateTris[i];
            candidateTris[i] = candidateTris[j];
            candidateTris[j] = temp;
        }

        int raycastsDone = 0;
        int trisToCheck = Mathf.Min(maxRaycasts, candidateTris.Count);

        for (int i = 0; i < trisToCheck; i++)
        {
            var tri = candidateTris[i];

            Vector3 dir = tri.center - fromPoint;
            float dist = dir.magnitude;
            if (dist < 0.0001f) continue;

            dir /= dist;

            if (Physics.Raycast(fromPoint, dir, out RaycastHit hit, dist + 0.05f))
            {
                if (hit.collider != null && hit.collider.transform.IsChildOf(target.transform))
                {
                    // Visible triangle found
                    return true;
                }
            }

            raycastsDone++;
        }

        return false;
    }

    private struct TriangleData
    {
        public Vector3 v0, v1, v2;
        public Vector3 normal;
        public Vector3 center;
        public float distance;
        public Renderer renderer;
    }

    private static void AddMeshTriangles(Mesh mesh, Transform t, Renderer r,
                                         Vector3 fromPoint, Vector3 viewDir,
                                         float facingDotThreshold,
                                         List<TriangleData> list)
    {
        var verts = mesh.vertices;
        var tris = mesh.triangles;

        for (int i = 0; i < tris.Length; i += 3)
        {
            Vector3 v0 = t.TransformPoint(verts[tris[i]]);
            Vector3 v1 = t.TransformPoint(verts[tris[i + 1]]);
            Vector3 v2 = t.TransformPoint(verts[tris[i + 2]]);

            Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;
            Vector3 toObserver = (fromPoint - ((v0 + v1 + v2) / 3f)).normalized;

            float facingDot = Vector3.Dot(normal, toObserver);
            if (facingDot < facingDotThreshold)
                continue;

            Vector3 center = (v0 + v1 + v2) / 3f;
            float distance = Vector3.Dot(center - fromPoint, viewDir);

            list.Add(new TriangleData
            {
                v0 = v0,
                v1 = v1,
                v2 = v2,
                normal = normal,
                center = center,
                distance = distance,
                renderer = r
            });
        }
    }
}
