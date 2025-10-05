using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Flags]
public enum CornerTag
{
    None = 0,

    // Y axis
    Top = 1 << 0,
    MiddleY = 1 << 1,
    Bottom = 1 << 2,

    // X axis
    Left = 1 << 3,
    MiddleX = 1 << 4,
    Right = 1 << 5,

    // Z axis
    Front = 1 << 6,
    MiddleZ = 1 << 7,
    Back = 1 << 8,

    Center = MiddleX | MiddleY | MiddleZ,

    All = Top | MiddleY | Bottom |
      Left | MiddleX | Right |
      Front | MiddleZ | Back
}

public struct BoundingPoint
{
    public Vector3 Position;
    public CornerTag Tags;

    public BoundingPoint(Vector3 pos, CornerTag tags)
    {
        Position = pos;
        Tags = tags;
    }

    public bool Has(CornerTag tag) => (Tags & tag) != 0;

    public override string ToString() => $"{Tags}: {Position}";
}

public class BoundingBox
{
    private readonly GameObject _go;
    private readonly List<BoundingPoint> _points = new List<BoundingPoint>();
    public IEnumerable<BoundingPoint> Points => _points;
    private List<GameObject> _debugMarkers = new List<GameObject>();
    public Vector3 Center { get; private set; }

    public BoundingBox(GameObject go)
    {
        _go = go;
        Refresh();
    }

    /// <summary>
    /// Recalculate all points based on the current transform
    /// </summary>
    public void Refresh()
    {
        var originalRotation = _go.transform.rotation;
        _go.transform.rotation = Quaternion.identity;

        _points.Clear();

        Transform rootT = _go.transform;

        // Collect all bounds sources
        List<Bounds> allBounds = new List<Bounds>();

        // MeshFilters
        foreach (var mf in _go.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.mesh == null) continue;
            Bounds b = mf.mesh.bounds;
            b.center = rootT.InverseTransformPoint(mf.transform.TransformPoint(b.center));
            allBounds.Add(b);
        }

        // Renderers
        foreach (var rend in _go.GetComponentsInChildren<Renderer>())
        {
            Bounds b = rend.bounds; // world-space bounds
            b.center = rootT.InverseTransformPoint(b.center);
            allBounds.Add(b);
        }

        // Colliders
        foreach (var col in _go.GetComponentsInChildren<Collider>())
        {
            Bounds b = col.bounds; // world-space bounds
            b.center = rootT.InverseTransformPoint(b.center);
            allBounds.Add(b);
        }

        if (allBounds.Count == 0)
        {
            // --- Fallback: no geometry found ---
            Vector3 pos = rootT.position;
            Center = pos;

            for (int xi = 0; xi < 3; xi++)
                for (int yi = 0; yi < 3; yi++)
                    for (int zi = 0; zi < 3; zi++)
                    {
                        CornerTag tag = CornerTag.None;
                        tag |= xi == 0 ? CornerTag.Left : xi == 1 ? CornerTag.MiddleX : CornerTag.Right;
                        tag |= yi == 0 ? CornerTag.Bottom : yi == 1 ? CornerTag.MiddleY : CornerTag.Top;
                        tag |= zi == 0 ? CornerTag.Back : zi == 1 ? CornerTag.MiddleZ : CornerTag.Front;

                        _points.Add(new BoundingPoint(pos, tag));
                    }

            _go.transform.rotation = originalRotation;
            return;
        }
        // Combine all bounds
        Bounds combinedBounds = allBounds[0];
        foreach (var b in allBounds.Skip(1))
        {
            combinedBounds.Encapsulate(b.min);
            combinedBounds.Encapsulate(b.max);
        }

        Center = rootT.TransformPoint(combinedBounds.center);

        Vector3 c = combinedBounds.center;
        Vector3 e = combinedBounds.extents;

        float[] xs = { -e.x, 0, e.x };
        float[] ys = { -e.y, 0, e.y };
        float[] zs = { -e.z, 0, e.z };

        for (int xi = 0; xi < 3; xi++)
            for (int yi = 0; yi < 3; yi++)
                for (int zi = 0; zi < 3; zi++)
                {
                    Vector3 localPoint = c + new Vector3(xs[xi], ys[yi], zs[zi]);
                    Vector3 worldPoint = rootT.TransformPoint(localPoint);

                    CornerTag tag = CornerTag.None;
                    tag |= xi == 0 ? CornerTag.Left : xi == 1 ? CornerTag.MiddleX : CornerTag.Right;
                    tag |= yi == 0 ? CornerTag.Bottom : yi == 1 ? CornerTag.MiddleY : CornerTag.Top;
                    tag |= zi == 0 ? CornerTag.Back : zi == 1 ? CornerTag.MiddleZ : CornerTag.Front;

                    _points.Add(new BoundingPoint(worldPoint, tag));
                }

        // Restore rotation
        _go.transform.rotation = originalRotation;

        // Rotate all points by original rotation around center
        for (int i = 0; i < _points.Count; i++)
        {
            Vector3 localOffset = _points[i].Position - Center;
            Vector3 rotatedOffset = originalRotation * localOffset;
            Vector3 rotatedPos = Center + rotatedOffset;
            _points[i] = new BoundingPoint(rotatedPos, _points[i].Tags);
        }
    }
    public bool IsInside(Vector3 worldPoint)
    {
        // Use only the 8 true corners (no middle points)
        var corners = _points.Where(p =>
            !p.Has(CornerTag.MiddleX) &&
            !p.Has(CornerTag.MiddleY) &&
            !p.Has(CornerTag.MiddleZ)).ToArray();

        if (corners.Length == 0)
            return false;

        // Compute axis-aligned min/max from the 8 corners
        Vector3 min = corners[0].Position;
        Vector3 max = corners[0].Position;

        for (int i = 1; i < corners.Length; i++)
        {
            min = Vector3.Min(min, corners[i].Position);
            max = Vector3.Max(max, corners[i].Position);
        }

        // Check if the point is inside the bounding box
        return (worldPoint.x >= min.x && worldPoint.x <= max.x) &&
               (worldPoint.y >= min.y && worldPoint.y <= max.y) &&
               (worldPoint.z >= min.z && worldPoint.z <= max.z);
    }

    /// <summary>
    /// GetCorners all points matching include tags, optionally excluding tags
    /// </summary>
    public IEnumerable<Vector3> GetCorners(CornerTag include = CornerTag.All, CornerTag exclude = CornerTag.None)
    {
        foreach (var p in _points)
        {
            if (p.Has(include) && (exclude == CornerTag.None || !p.Has(exclude)))
                yield return p.Position;
        }
    }

    /// <summary>
    /// Only the 8 true corners
    /// </summary>
    public IEnumerable<Vector3> CornersOnly()
    {
        foreach (var p in _points)
        {
            if (!p.Has(CornerTag.MiddleX) &&
                !p.Has(CornerTag.MiddleY) &&
                !p.Has(CornerTag.MiddleZ))
                yield return p.Position;
        }
    }
    public void ToggleDebug()
    {
        if (_debugMarkers.Count == 0)
            ShowDebug();
        else
            ClearDebug();
    }
    /// <summary>
    /// Shows a simple TextMesh "-" at each point of the bounding box for debugging
    /// </summary>
    /// <param name="parent">Optional parent transform for the markers</param>
    /// <param name="size">Size of the TextMesh font</param>
    public void ShowDebug(Transform parent = null, float size = 0.2f)
    {
        ClearDebug();
        foreach (var point in GetCorners())
        {
            GameObject marker = new GameObject("BB_DebugPoint");
            if (parent != null)
                marker.transform.parent = parent;

            marker.transform.position = point;

            TextMesh tm = marker.AddComponent<TextMesh>();
            tm.text = "-";
            tm.characterSize = size;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.red;

            _debugMarkers.Add(marker);
        }
    }

    /// <summary>
    /// Removes any debug markers previously created
    /// </summary>
    public void ClearDebug()
    {
        foreach (var go in _debugMarkers)
        {
            if (go != null)
                GameObject.Destroy(go);
        }
        _debugMarkers.Clear();
    }
    // Convenience properties for faces
    public IEnumerable<Vector3> Front => GetCorners(CornerTag.Front);
    public IEnumerable<Vector3> Back => GetCorners(CornerTag.Back);
    public IEnumerable<Vector3> Top => GetCorners(CornerTag.Top);
    public IEnumerable<Vector3> Bottom => GetCorners(CornerTag.Bottom);
    public IEnumerable<Vector3> Left => GetCorners(CornerTag.Left);
    public IEnumerable<Vector3> Right => GetCorners(CornerTag.Right);
}
