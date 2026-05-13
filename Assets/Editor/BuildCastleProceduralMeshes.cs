#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace CrowdDefense.Editor
{
    public class BuildCastleProceduralMeshes
    {
        private const string CastleDir = "Assets/Models/Castle";

        [MenuItem("Tools/CrowdDefense/Build Castle Procedural Meshes")]
        public static void BuildAllCastleMeshes()
        {
            if (!Directory.Exists(CastleDir))
                Directory.CreateDirectory(CastleDir);

            SaveMeshAsset(BuildIntact(),   "castle_intact");
            SaveMeshAsset(BuildCracked(),  "castle_cracked");
            SaveMeshAsset(BuildRuined(),   "castle_ruined");
            SaveMeshAsset(BuildCritical(), "castle_critical");

            AssetDatabase.Refresh();
            Debug.Log("[BuildCastleProceduralMeshes] All 4 castle meshes rebuilt.");
        }

        // ─── Stage builders ────────────────────────────────────────────────────

        private static Mesh BuildIntact()
        {
            var b = new MeshBuilder();
            AddEnclosureWall(b, wallHeight: 0.55f, wallThick: 0.08f, missingMask: 0);
            AddTourelle(b, new Vector3(-0.45f, 0f, -0.45f), height: 1.1f, radius: 0.14f, roofH: 0.35f, roofScale: 1f, tiltZ: 0f);
            AddTourelle(b, new Vector3( 0.45f, 0f, -0.45f), height: 1.1f, radius: 0.14f, roofH: 0.35f, roofScale: 1f, tiltZ: 0f);
            AddTourelle(b, new Vector3( 0.45f, 0f,  0.45f), height: 1.1f, radius: 0.14f, roofH: 0.35f, roofScale: 1f, tiltZ: 0f);
            AddTourelle(b, new Vector3(-0.45f, 0f,  0.45f), height: 1.1f, radius: 0.14f, roofH: 0.35f, roofScale: 1f, tiltZ: 0f);
            AddCentralTower(b, height: 1.8f, radius: 0.22f, roofH: 0.55f, truncate: 1f);
            AddFlag(b, poleBase: new Vector3(0f, 1.8f, 0f), poleHeight: 0.5f);
            return b.Build("castle_intact");
        }

        private static Mesh BuildCracked()
        {
            var b = new MeshBuilder();
            AddEnclosureWall(b, wallHeight: 0.52f, wallThick: 0.08f, missingMask: 0);
            AddTourelle(b, new Vector3(-0.45f, 0f, -0.45f), height: 1.1f,  radius: 0.14f, roofH: 0.35f, roofScale: 1f,   tiltZ: 0f);
            AddTourelle(b, new Vector3( 0.45f, 0f, -0.45f), height: 1.05f, radius: 0.14f, roofH: 0.33f, roofScale: 0.95f, tiltZ: 5f);
            AddTourelle(b, new Vector3( 0.45f, 0f,  0.45f), height: 1.1f,  radius: 0.14f, roofH: 0.35f, roofScale: 1f,   tiltZ: 0f);
            AddTourelle(b, new Vector3(-0.45f, 0f,  0.45f), height: 1.08f, radius: 0.14f, roofH: 0.34f, roofScale: 0.98f, tiltZ: -3f);
            AddCentralTower(b, height: 1.75f, radius: 0.22f, roofH: 0.52f, truncate: 1f);
            AddFlag(b, poleBase: new Vector3(0f, 1.75f, 0f), poleHeight: 0.45f);
            return b.Build("castle_cracked");
        }

        private static Mesh BuildRuined()
        {
            var b = new MeshBuilder();
            // Front wall section missing (bit 0)
            AddEnclosureWall(b, wallHeight: 0.45f, wallThick: 0.08f, missingMask: 1);
            // Tourelle 0: no roof (decapitated)
            AddTourelle(b, new Vector3(-0.45f, 0f, -0.45f), height: 0.85f, radius: 0.14f, roofH: 0f,   roofScale: 1f,    tiltZ: 0f);
            AddTourelle(b, new Vector3( 0.45f, 0f, -0.45f), height: 0.9f,  radius: 0.14f, roofH: 0.2f, roofScale: 0.7f,  tiltZ: 8f);
            AddTourelle(b, new Vector3( 0.45f, 0f,  0.45f), height: 0.95f, radius: 0.14f, roofH: 0.25f, roofScale: 0.85f, tiltZ: 0f);
            AddTourelle(b, new Vector3(-0.45f, 0f,  0.45f), height: 0.55f, radius: 0.14f, roofH: 0f,   roofScale: 1f,    tiltZ: 0f);
            // Central tower: truncated roof 70%
            AddCentralTower(b, height: 1.6f, radius: 0.22f, roofH: 0.55f, truncate: 0.7f);
            return b.Build("castle_ruined");
        }

        private static Mesh BuildCritical()
        {
            var b = new MeshBuilder();
            // Front + right walls missing (bits 0+1)
            AddEnclosureWall(b, wallHeight: 0.32f, wallThick: 0.08f, missingMask: 3);
            // Only 2 tourelle stubs survive
            AddTourelle(b, new Vector3(-0.45f, 0f, -0.45f), height: 0.5f,  radius: 0.14f, roofH: 0f, roofScale: 1f, tiltZ: 0f);
            AddTourelle(b, new Vector3( 0.45f, 0f,  0.45f), height: 0.45f, radius: 0.14f, roofH: 0f, roofScale: 1f, tiltZ: 0f);
            // Central tower standing but no roof
            AddCentralTower(b, height: 1.3f, radius: 0.22f, roofH: 0f, truncate: 0f);
            // Rubble where destroyed tourelles were
            AddRubble(b, new Vector3( 0.45f, 0f, -0.45f), size: 0.18f);
            AddRubble(b, new Vector3(-0.45f, 0f,  0.45f), size: 0.15f);
            return b.Build("castle_critical");
        }

        // ─── Part builders ──────────────────────────────────────────────────────

        // 4 wall segments around centre. missingMask bitmask: bit0=front(+Z), bit1=right(+X), bit2=back(-Z), bit3=left(-X)
        private static void AddEnclosureWall(MeshBuilder b, float wallHeight, float wallThick, int missingMask)
        {
            float half = 0.45f;
            if ((missingMask & 1) == 0)
                b.AddBox(new Vector3(0f, wallHeight * 0.5f,  half), new Vector3(half * 2f - wallThick * 2f, wallHeight, wallThick));
            if ((missingMask & 2) == 0)
                b.AddBox(new Vector3( half, wallHeight * 0.5f, 0f), new Vector3(wallThick, wallHeight, half * 2f - wallThick * 2f));
            if ((missingMask & 4) == 0)
                b.AddBox(new Vector3(0f, wallHeight * 0.5f, -half), new Vector3(half * 2f - wallThick * 2f, wallHeight, wallThick));
            if ((missingMask & 8) == 0)
                b.AddBox(new Vector3(-half, wallHeight * 0.5f, 0f), new Vector3(wallThick, wallHeight, half * 2f - wallThick * 2f));
        }

        private static void AddCentralTower(MeshBuilder b, float height, float radius, float roofH, float truncate)
        {
            b.AddCylinder(Vector3.zero, radius, height, 8);
            if (roofH > 0f)
                b.AddCone(new Vector3(0f, height, 0f), radius * 1.1f, roofH, 8, truncate);
        }

        private static void AddTourelle(MeshBuilder b, Vector3 pos, float height, float radius, float roofH, float roofScale, float tiltZ)
        {
            b.AddCylinder(pos, radius, height, 6, tiltZ: tiltZ);
            if (roofH > 0f)
                b.AddCone(pos + new Vector3(0f, height, 0f), radius * 1.15f * roofScale, roofH * roofScale, 6, 1f, tiltZ: tiltZ);
        }

        private static void AddFlag(MeshBuilder b, Vector3 poleBase, float poleHeight)
        {
            b.AddCylinder(poleBase + new Vector3(0f, poleHeight * 0.5f, 0f), 0.025f, poleHeight, 4);
            Vector3 flagOrigin = poleBase + new Vector3(0f, poleHeight * 0.9f, 0f);
            b.AddQuad(flagOrigin,
                      flagOrigin + new Vector3(0.22f, 0f,    0f),
                      flagOrigin + new Vector3(0.22f, 0.12f, 0f),
                      flagOrigin + new Vector3(0f,    0.12f, 0f));
        }

        private static void AddRubble(MeshBuilder b, Vector3 centre, float size)
        {
            b.AddBox(centre + new Vector3(-size * 0.3f, size * 0.15f, 0f),        new Vector3(size, size * 0.3f, size * 0.8f));
            b.AddBox(centre + new Vector3( size * 0.4f, size * 0.1f, size * 0.2f), new Vector3(size * 0.6f, size * 0.25f, size * 0.6f));
        }

        // ─── Asset save ──────────────────────────────────────────────────────────

        private static void SaveMeshAsset(Mesh mesh, string name)
        {
            string path = $"{CastleDir}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(mesh, existing);
                EditorUtility.SetDirty(existing);
            }
            else
            {
                AssetDatabase.CreateAsset(mesh, path);
            }
            Debug.Log($"[BuildCastleProceduralMeshes] Saved: {path}");
        }
    }

    // ─── MeshBuilder ────────────────────────────────────────────────────────────

    internal class MeshBuilder
    {
        private readonly List<Vector3> _verts = new();
        private readonly List<int>     _tris  = new();
        private readonly List<Vector3> _norms = new();

        private int V => _verts.Count;

        public void AddBox(Vector3 centre, Vector3 size)
        {
            float hx = size.x * 0.5f, hy = size.y * 0.5f, hz = size.z * 0.5f;
            Vector3 v000 = centre + new Vector3(-hx, -hy, -hz);
            Vector3 v100 = centre + new Vector3( hx, -hy, -hz);
            Vector3 v110 = centre + new Vector3( hx,  hy, -hz);
            Vector3 v010 = centre + new Vector3(-hx,  hy, -hz);
            Vector3 v001 = centre + new Vector3(-hx, -hy,  hz);
            Vector3 v101 = centre + new Vector3( hx, -hy,  hz);
            Vector3 v111 = centre + new Vector3( hx,  hy,  hz);
            Vector3 v011 = centre + new Vector3(-hx,  hy,  hz);

            AddFace(v000, v100, v110, v010, Vector3.back);
            AddFace(v101, v001, v011, v111, Vector3.forward);
            AddFace(v001, v000, v010, v011, Vector3.left);
            AddFace(v100, v101, v111, v110, Vector3.right);
            AddFace(v001, v101, v100, v000, Vector3.down);
            AddFace(v010, v110, v111, v011, Vector3.up);
        }

        // bottomCentre = base of cylinder. tiltZ = lean degrees around Z.
        public void AddCylinder(Vector3 bottomCentre, float radius, float height, int segs, float tiltZ = 0f)
        {
            Quaternion tilt = Quaternion.Euler(0f, 0f, tiltZ);
            Vector3[] btm = new Vector3[segs];
            Vector3[] top = new Vector3[segs];
            for (int i = 0; i < segs; i++)
            {
                float a = Mathf.PI * 2f * i / segs;
                Vector3 offset = new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
                btm[i] = bottomCentre + tilt * offset;
                top[i] = bottomCentre + tilt * (offset + new Vector3(0f, height, 0f));
            }

            for (int i = 0; i < segs; i++)
            {
                int j = (i + 1) % segs;
                Vector3 outward = (btm[i] + btm[j]) * 0.5f - bottomCentre;
                outward.y = 0f;
                outward = outward.sqrMagnitude < 1e-6f ? Vector3.right : outward.normalized;
                AddFace(btm[i], btm[j], top[j], top[i], outward);
            }

            // Top cap
            int tc = V;
            Vector3 topC = bottomCentre + tilt * new Vector3(0f, height, 0f);
            _verts.Add(topC); _norms.Add(tilt * Vector3.up);
            for (int i = 0; i < segs; i++) { _verts.Add(top[i]); _norms.Add(tilt * Vector3.up); }
            for (int i = 0; i < segs; i++)
            {
                _tris.Add(tc); _tris.Add(tc + 1 + (i + 1) % segs); _tris.Add(tc + 1 + i);
            }

            // Bottom cap
            int bc = V;
            _verts.Add(bottomCentre); _norms.Add(tilt * Vector3.down);
            for (int i = 0; i < segs; i++) { _verts.Add(btm[i]); _norms.Add(tilt * Vector3.down); }
            for (int i = 0; i < segs; i++)
            {
                _tris.Add(bc); _tris.Add(bc + 1 + i); _tris.Add(bc + 1 + (i + 1) % segs);
            }
        }

        // baseCenter = base of cone. truncate=1 → full cone, <1 → flat-topped frustum.
        public void AddCone(Vector3 baseCenter, float baseRadius, float coneHeight, int segs, float truncate = 1f, float tiltZ = 0f)
        {
            if (coneHeight <= 0f) return;
            Quaternion tilt = Quaternion.Euler(0f, 0f, tiltZ);
            float topRadius = baseRadius * (1f - truncate);

            Vector3[] ring = new Vector3[segs];
            for (int i = 0; i < segs; i++)
            {
                float a = Mathf.PI * 2f * i / segs;
                ring[i] = baseCenter + tilt * new Vector3(Mathf.Cos(a) * baseRadius, 0f, Mathf.Sin(a) * baseRadius);
            }

            Vector3 apex = baseCenter + tilt * new Vector3(0f, coneHeight * truncate, 0f);
            int apexIdx = V; _verts.Add(apex); _norms.Add(tilt * Vector3.up);

            for (int i = 0; i < segs; i++)
            {
                int j = (i + 1) % segs;
                if (truncate >= 0.999f)
                {
                    int ri = V; _verts.Add(ring[i]); _norms.Add((ring[i] - baseCenter).normalized);
                    int rj = V; _verts.Add(ring[j]); _norms.Add((ring[j] - baseCenter).normalized);
                    _tris.Add(apexIdx); _tris.Add(ri); _tris.Add(rj);
                }
                else
                {
                    float a0 = Mathf.PI * 2f * i / segs, a1 = Mathf.PI * 2f * j / segs;
                    Vector3 tr0 = baseCenter + tilt * new Vector3(Mathf.Cos(a0) * topRadius, coneHeight * truncate, Mathf.Sin(a0) * topRadius);
                    Vector3 tr1 = baseCenter + tilt * new Vector3(Mathf.Cos(a1) * topRadius, coneHeight * truncate, Mathf.Sin(a1) * topRadius);
                    AddFace(ring[i], ring[j], tr1, tr0, (ring[i] - baseCenter).normalized);
                }
            }

            // Base cap
            int baseCtr = V;
            _verts.Add(baseCenter); _norms.Add(tilt * Vector3.down);
            for (int i = 0; i < segs; i++) { _verts.Add(ring[i]); _norms.Add(tilt * Vector3.down); }
            for (int i = 0; i < segs; i++)
            {
                _tris.Add(baseCtr); _tris.Add(baseCtr + 1 + i); _tris.Add(baseCtr + 1 + (i + 1) % segs);
            }

            // Top flat cap when truncated
            if (truncate < 0.999f && topRadius > 0.001f)
            {
                int topCtr = V;
                _verts.Add(apex); _norms.Add(tilt * Vector3.up);
                for (int i = 0; i < segs; i++)
                {
                    float a = Mathf.PI * 2f * i / segs;
                    _verts.Add(baseCenter + tilt * new Vector3(Mathf.Cos(a) * topRadius, coneHeight * truncate, Mathf.Sin(a) * topRadius));
                    _norms.Add(tilt * Vector3.up);
                }
                for (int i = 0; i < segs; i++)
                {
                    _tris.Add(topCtr); _tris.Add(topCtr + 1 + (i + 1) % segs); _tris.Add(topCtr + 1 + i);
                }
            }
        }

        public void AddQuad(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3)
            => AddFace(v0, v1, v2, v3, Vector3.Cross(v1 - v0, v3 - v0).normalized);

        public Mesh Build(string name)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(_verts);
            mesh.SetNormals(_norms);
            mesh.SetTriangles(_tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private void AddFace(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 normal)
        {
            int b = V;
            _verts.Add(v0); _norms.Add(normal);
            _verts.Add(v1); _norms.Add(normal);
            _verts.Add(v2); _norms.Add(normal);
            _verts.Add(v3); _norms.Add(normal);
            _tris.Add(b); _tris.Add(b + 1); _tris.Add(b + 2);
            _tris.Add(b); _tris.Add(b + 2); _tris.Add(b + 3);
        }
    }
}
