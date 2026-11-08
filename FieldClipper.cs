using System.Collections.Generic;

using UnityEngine;

namespace HomeBubble;

/// <summary>
/// Cuts away the parts of a bubble's sphere that lie inside other bubbles, so overlapping bubbles show one
/// merged outer surface instead of walls inside each other. Where two spheres meet, the cut is always a flat
/// plane, so the trimming is plain plane clipping of the sphere's triangles.
/// </summary>
internal static class FieldClipper
{
    /// <summary>The vertex data of a mesh we can clip, read once per mesh.</summary>
    private sealed class Source
    {
        public Vector3[] Positions;
        public Vector3[] Normals;
        public Vector4[] Tangents;
        public Color[] Colors;
        public Vector2[] Uv0;
        public Vector2[] Uv1;
        public int[][] SubMeshes;
        public Bounds Bounds;
    }

    private static readonly Dictionary<Mesh, Source> Sources = [];

    /// <summary>Scratch buffers, reused between builds (everything runs on the main thread).</summary>
    private static readonly List<Vector3> OutPositions = [];
    private static readonly List<Vector3> OutNormals = [];
    private static readonly List<Vector4> OutTangents = [];
    private static readonly List<Color> OutColors = [];
    private static readonly List<Vector2> OutUv0 = [];
    private static readonly List<Vector2> OutUv1 = [];
    private static readonly List<int> OutIndices = [];
    private static readonly Dictionary<long, int> EdgeVertices = [];
    private static List<int> _polygon = [];
    private static List<int> _clipped = [];
    private static int[] _sourceToOut = [];

    /// <summary>
    /// Fills <paramref name="target"/> with <paramref name="mesh"/> minus everything on the positive side of
    /// any of <paramref name="cuts"/> (planes in the mesh's local space).
    /// </summary>
    public static void Build(Mesh mesh, List<Plane> cuts, Mesh target)
    {
        Source source = GetSource(mesh);

        OutPositions.Clear();
        OutNormals.Clear();
        OutTangents.Clear();
        OutColors.Clear();
        OutUv0.Clear();
        OutUv1.Clear();
        EdgeVertices.Clear();
        if (_sourceToOut.Length < source.Positions.Length)
        {
            _sourceToOut = new int[source.Positions.Length];
        }
        for (int i = 0; i < source.Positions.Length; i++)
        {
            _sourceToOut[i] = -1;
        }

        target.Clear();
        target.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        int[][] subMeshIndices = new int[source.SubMeshes.Length][];
        for (int s = 0; s < source.SubMeshes.Length; s++)
        {
            OutIndices.Clear();
            int[] triangles = source.SubMeshes[s];
            for (int t = 0; t + 2 < triangles.Length; t += 3)
            {
                ClipTriangle(source, triangles[t], triangles[t + 1], triangles[t + 2], cuts);
            }
            subMeshIndices[s] = [.. OutIndices];
        }

        target.SetVertices(OutPositions);
        if (source.Normals != null)
        {
            target.SetNormals(OutNormals);
        }
        if (source.Tangents != null)
        {
            target.SetTangents(OutTangents);
        }
        if (source.Colors != null)
        {
            target.SetColors(OutColors);
        }
        if (source.Uv0 != null)
        {
            target.SetUVs(0, OutUv0);
        }
        if (source.Uv1 != null)
        {
            target.SetUVs(1, OutUv1);
        }
        target.subMeshCount = subMeshIndices.Length;
        for (int s = 0; s < subMeshIndices.Length; s++)
        {
            target.SetTriangles(subMeshIndices[s], s, false);
        }
        target.bounds = source.Bounds; // same culling as the whole sphere
    }

    private static void ClipTriangle(Source source, int a, int b, int c, List<Plane> cuts)
    {
        _polygon.Clear();
        _polygon.Add(MapSource(source, a));
        _polygon.Add(MapSource(source, b));
        _polygon.Add(MapSource(source, c));

        for (int k = 0; k < cuts.Count && _polygon.Count >= 3; k++)
        {
            Plane plane = cuts[k];
            _clipped.Clear();
            int previous = _polygon[_polygon.Count - 1];
            float previousDistance = plane.GetDistanceToPoint(OutPositions[previous]);
            foreach (int current in _polygon)
            {
                float currentDistance = plane.GetDistanceToPoint(OutPositions[current]);
                bool currentKept = currentDistance <= 0f;
                bool previousKept = previousDistance <= 0f;
                if (currentKept != previousKept)
                {
                    _clipped.Add(EdgeVertex(previous, previousDistance, current, currentDistance, k));
                }
                if (currentKept)
                {
                    _clipped.Add(current);
                }
                previous = current;
                previousDistance = currentDistance;
            }
            (_polygon, _clipped) = (_clipped, _polygon);
        }

        for (int i = 1; i + 1 < _polygon.Count; i++)
        {
            OutIndices.Add(_polygon[0]);
            OutIndices.Add(_polygon[i]);
            OutIndices.Add(_polygon[i + 1]);
        }
    }

    private static int MapSource(Source source, int index)
    {
        int mapped = _sourceToOut[index];
        if (mapped >= 0)
        {
            return mapped;
        }

        mapped = OutPositions.Count;
        OutPositions.Add(source.Positions[index]);
        if (source.Normals != null)
        {
            OutNormals.Add(source.Normals[index]);
        }
        if (source.Tangents != null)
        {
            OutTangents.Add(source.Tangents[index]);
        }
        if (source.Colors != null)
        {
            OutColors.Add(source.Colors[index]);
        }
        if (source.Uv0 != null)
        {
            OutUv0.Add(source.Uv0[index]);
        }
        if (source.Uv1 != null)
        {
            OutUv1.Add(source.Uv1[index]);
        }
        _sourceToOut[index] = mapped;
        return mapped;
    }

    /// <summary>The vertex where a cut crosses an edge, shared by both triangles on that edge.</summary>
    private static int EdgeVertex(int i, float di, int j, float dj, int cut)
    {
        // Always interpolate from the lower index so both triangles get the exact same point.
        if (j < i)
        {
            (i, j) = (j, i);
            (di, dj) = (dj, di);
        }
        long key = ((long)cut << 48) ^ ((long)i << 24) ^ j;
        if (EdgeVertices.TryGetValue(key, out int existing))
        {
            return existing;
        }

        float t = di / (di - dj);
        int index = OutPositions.Count;
        OutPositions.Add(Vector3.LerpUnclamped(OutPositions[i], OutPositions[j], t));
        if (OutNormals.Count > 0)
        {
            OutNormals.Add(Vector3.LerpUnclamped(OutNormals[i], OutNormals[j], t).normalized);
        }
        if (OutTangents.Count > 0)
        {
            Vector4 ti = OutTangents[i];
            Vector3 direction = Vector3.LerpUnclamped(ti, OutTangents[j], t).normalized;
            OutTangents.Add(new Vector4(direction.x, direction.y, direction.z, ti.w));
        }
        if (OutColors.Count > 0)
        {
            OutColors.Add(Color.LerpUnclamped(OutColors[i], OutColors[j], t));
        }
        if (OutUv0.Count > 0)
        {
            OutUv0.Add(Vector2.LerpUnclamped(OutUv0[i], OutUv0[j], t));
        }
        if (OutUv1.Count > 0)
        {
            OutUv1.Add(Vector2.LerpUnclamped(OutUv1[i], OutUv1[j], t));
        }
        EdgeVertices[key] = index;
        return index;
    }

    private static Source GetSource(Mesh mesh)
    {
        if (Sources.TryGetValue(mesh, out Source source))
        {
            return source;
        }

        if (mesh.isReadable)
        {
            source = new Source
            {
                Positions = mesh.vertices,
                Normals = NullIfEmpty(mesh.normals),
                Tangents = NullIfEmpty(mesh.tangents),
                Colors = NullIfEmpty(mesh.colors),
                Uv0 = NullIfEmpty(mesh.uv),
                Uv1 = NullIfEmpty(mesh.uv2),
                SubMeshes = new int[mesh.subMeshCount][],
                Bounds = mesh.bounds,
            };
            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                source.SubMeshes[s] = mesh.GetTriangles(s);
            }
        }
        else
        {
            // The game ships the mesh without CPU access, so stand in a sphere of the same size and place.
            HomeBubblePlugin.Log.LogInfo("The force field mesh '" + mesh.name + "' is not readable; overlapping bubbles use a generated sphere.");
            source = BuildSphere(mesh.bounds, mesh.subMeshCount);
        }

        Sources[mesh] = source;
        return source;
    }

    private static T[] NullIfEmpty<T>(T[] values)
    {
        return values != null && values.Length > 0 ? values : null;
    }

    /// <summary>A latitude/longitude sphere with outward normals and spherical UVs.</summary>
    private static Source BuildSphere(Bounds bounds, int subMeshCount)
    {
        const int longitudes = 64;
        const int latitudes = 40;
        float radius = bounds.extents.x;
        Vector3 center = bounds.center;

        const int count = (longitudes + 1) * (latitudes + 1);
        Vector3[] positions = new Vector3[count];
        Vector3[] normals = new Vector3[count];
        Vector2[] uvs = new Vector2[count];
        for (int lat = 0; lat <= latitudes; lat++)
        {
            float v = (float)lat / latitudes;
            float polar = v * Mathf.PI;
            for (int lon = 0; lon <= longitudes; lon++)
            {
                float u = (float)lon / longitudes;
                float azimuth = u * 2f * Mathf.PI;
                Vector3 normal = new(Mathf.Sin(polar) * Mathf.Cos(azimuth), Mathf.Cos(polar), Mathf.Sin(polar) * Mathf.Sin(azimuth));
                int index = (lat * (longitudes + 1)) + lon;
                positions[index] = center + (normal * radius);
                normals[index] = normal;
                uvs[index] = new Vector2(u, 1f - v);
            }
        }

        List<int> triangles = [];
        for (int lat = 0; lat < latitudes; lat++)
        {
            for (int lon = 0; lon < longitudes; lon++)
            {
                int a = (lat * (longitudes + 1)) + lon;
                int b = a + longitudes + 1;
                // Wound so the faces point outward.
                triangles.Add(a);
                triangles.Add(a + 1);
                triangles.Add(b);
                triangles.Add(a + 1);
                triangles.Add(b + 1);
                triangles.Add(b);
            }
        }

        // Every material slot gets the full sphere, as the original mesh's slots presumably do.
        int[][] subMeshes = new int[Mathf.Max(1, subMeshCount)][];
        int[] all = [.. triangles];
        for (int s = 0; s < subMeshes.Length; s++)
        {
            subMeshes[s] = all;
        }

        return new Source
        {
            Positions = positions,
            Normals = normals,
            Uv0 = uvs,
            SubMeshes = subMeshes,
            Bounds = bounds,
        };
    }
}
