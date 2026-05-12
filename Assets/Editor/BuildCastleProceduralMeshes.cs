#nullable enable
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

            CreateIntactMesh();
            CreateCrackedMesh();
            CreateRuinedMesh();
            CreateCriticalMesh();

            AssetDatabase.Refresh();
            Debug.Log("[BuildCastleProceduralMeshes] All 4 castle meshes created successfully.");
        }

        private static void CreateIntactMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "castle_intact";

            // Base cube + 4 corner towers
            Vector3[] vertices = new Vector3[40];
            int vIdx = 0;

            // Base cube (0-23)
            float bx = 0.5f, bz = 0.5f, bh = 0.8f;
            vertices[vIdx++] = new Vector3(-bx, 0, -bz);      // 0
            vertices[vIdx++] = new Vector3(bx, 0, -bz);       // 1
            vertices[vIdx++] = new Vector3(bx, 0, bz);        // 2
            vertices[vIdx++] = new Vector3(-bx, 0, bz);       // 3
            vertices[vIdx++] = new Vector3(-bx, bh, -bz);     // 4
            vertices[vIdx++] = new Vector3(bx, bh, -bz);      // 5
            vertices[vIdx++] = new Vector3(bx, bh, bz);       // 6
            vertices[vIdx++] = new Vector3(-bx, bh, bz);      // 7

            // Corner towers (simple cylinders = cubes)
            float tr = 0.15f;
            float[] txArr = { -bx + tr, bx - tr, bx - tr, -bx + tr };
            float[] tzArr = { -bz + tr, -bz + tr, bz - tr, bz - tr };
            for (int i = 0; i < 4; i++)
            {
                float tx = txArr[i], tz = tzArr[i];
                vertices[vIdx++] = new Vector3(tx - tr, 0, tz - tr);       // tower base NW
                vertices[vIdx++] = new Vector3(tx + tr, 0, tz - tr);       // tower base NE
                vertices[vIdx++] = new Vector3(tx + tr, 0, tz + tr);       // tower base SE
                vertices[vIdx++] = new Vector3(tx - tr, 0, tz + tr);       // tower base SW
                vertices[vIdx++] = new Vector3(tx - tr, 1.2f, tz - tr);    // tower top NW
                vertices[vIdx++] = new Vector3(tx + tr, 1.2f, tz - tr);    // tower top NE
                vertices[vIdx++] = new Vector3(tx + tr, 1.2f, tz + tr);    // tower top SE
                vertices[vIdx++] = new Vector3(tx - tr, 1.2f, tz + tr);    // tower top SW
            }

            mesh.vertices = vertices;

            // Triangles for base cube (12 tris)
            int[] triangles = new int[108];
            int tIdx = 0;

            // Base cube faces
            // Bottom
            triangles[tIdx++] = 0; triangles[tIdx++] = 2; triangles[tIdx++] = 1;
            triangles[tIdx++] = 0; triangles[tIdx++] = 3; triangles[tIdx++] = 2;
            // Top
            triangles[tIdx++] = 4; triangles[tIdx++] = 5; triangles[tIdx++] = 6;
            triangles[tIdx++] = 4; triangles[tIdx++] = 6; triangles[tIdx++] = 7;
            // Front
            triangles[tIdx++] = 0; triangles[tIdx++] = 1; triangles[tIdx++] = 5;
            triangles[tIdx++] = 0; triangles[tIdx++] = 5; triangles[tIdx++] = 4;
            // Back
            triangles[tIdx++] = 2; triangles[tIdx++] = 3; triangles[tIdx++] = 7;
            triangles[tIdx++] = 2; triangles[tIdx++] = 7; triangles[tIdx++] = 6;
            // Left
            triangles[tIdx++] = 3; triangles[tIdx++] = 0; triangles[tIdx++] = 4;
            triangles[tIdx++] = 3; triangles[tIdx++] = 4; triangles[tIdx++] = 7;
            // Right
            triangles[tIdx++] = 1; triangles[tIdx++] = 2; triangles[tIdx++] = 6;
            triangles[tIdx++] = 1; triangles[tIdx++] = 6; triangles[tIdx++] = 5;

            // Corner tower faces (each tower = 12 tris)
            for (int t = 0; t < 4; t++)
            {
                int baseIdx = 8 + t * 8;
                // Bottom
                triangles[tIdx++] = baseIdx; triangles[tIdx++] = baseIdx + 2; triangles[tIdx++] = baseIdx + 1;
                triangles[tIdx++] = baseIdx; triangles[tIdx++] = baseIdx + 3; triangles[tIdx++] = baseIdx + 2;
                // Top
                triangles[tIdx++] = baseIdx + 4; triangles[tIdx++] = baseIdx + 5; triangles[tIdx++] = baseIdx + 6;
                triangles[tIdx++] = baseIdx + 4; triangles[tIdx++] = baseIdx + 6; triangles[tIdx++] = baseIdx + 7;
                // Sides (4 faces)
                triangles[tIdx++] = baseIdx; triangles[tIdx++] = baseIdx + 1; triangles[tIdx++] = baseIdx + 5;
                triangles[tIdx++] = baseIdx; triangles[tIdx++] = baseIdx + 5; triangles[tIdx++] = baseIdx + 4;
                triangles[tIdx++] = baseIdx + 1; triangles[tIdx++] = baseIdx + 2; triangles[tIdx++] = baseIdx + 6;
                triangles[tIdx++] = baseIdx + 1; triangles[tIdx++] = baseIdx + 6; triangles[tIdx++] = baseIdx + 5;
            }

            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            SaveMeshAsset(mesh, "castle_intact");
        }

        private static void CreateCrackedMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "castle_cracked";

            // Same as intact but with slight displacement on vertices
            Vector3[] vertices = new Vector3[40];
            int vIdx = 0;

            float bx = 0.5f, bz = 0.5f, bh = 0.8f;
            vertices[vIdx++] = new Vector3(-bx, 0, -bz);
            vertices[vIdx++] = new Vector3(bx, 0, -bz);
            vertices[vIdx++] = new Vector3(bx, 0, bz);
            vertices[vIdx++] = new Vector3(-bx, 0, bz);
            vertices[vIdx++] = new Vector3(-bx, bh * 0.95f, -bz);
            vertices[vIdx++] = new Vector3(bx, bh * 0.98f, -bz);
            vertices[vIdx++] = new Vector3(bx, bh * 0.92f, bz);
            vertices[vIdx++] = new Vector3(-bx, bh * 0.95f, bz);

            float tr = 0.15f;
            float[] txArr = { -bx + tr, bx - tr, bx - tr, -bx + tr };
            float[] tzArr = { -bz + tr, -bz + tr, bz - tr, bz - tr };
            for (int i = 0; i < 4; i++)
            {
                float tx = txArr[i], tz = tzArr[i];
                vertices[vIdx++] = new Vector3(tx - tr, 0, tz - tr);
                vertices[vIdx++] = new Vector3(tx + tr, 0, tz - tr);
                vertices[vIdx++] = new Vector3(tx + tr, 0, tz + tr);
                vertices[vIdx++] = new Vector3(tx - tr, 0, tz + tr);
                vertices[vIdx++] = new Vector3(tx - tr, 1.1f, tz - tr);    // slightly shorter
                vertices[vIdx++] = new Vector3(tx + tr, 1.15f, tz - tr);
                vertices[vIdx++] = new Vector3(tx + tr, 1.05f, tz + tr);
                vertices[vIdx++] = new Vector3(tx - tr, 1.1f, tz + tr);
            }

            mesh.vertices = vertices;
            mesh.triangles = CreateStandardTriangles();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            SaveMeshAsset(mesh, "castle_cracked");
        }

        private static void CreateRuinedMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "castle_ruined";

            // Partial collapse - reduce height and lower some towers
            Vector3[] vertices = new Vector3[40];
            int vIdx = 0;

            float bx = 0.5f, bz = 0.5f, bh = 0.5f;   // Reduced height
            vertices[vIdx++] = new Vector3(-bx, 0, -bz);
            vertices[vIdx++] = new Vector3(bx, 0, -bz);
            vertices[vIdx++] = new Vector3(bx, 0, bz);
            vertices[vIdx++] = new Vector3(-bx, 0, bz);
            vertices[vIdx++] = new Vector3(-bx, bh, -bz);
            vertices[vIdx++] = new Vector3(bx, bh * 0.85f, -bz);    // damaged
            vertices[vIdx++] = new Vector3(bx, bh * 0.7f, bz);      // more damaged
            vertices[vIdx++] = new Vector3(-bx, bh, bz);

            float tr = 0.15f;
            float[] txArr = { -bx + tr, bx - tr, bx - tr, -bx + tr };
            float[] tzArr = { -bz + tr, -bz + tr, bz - tr, bz - tr };
            for (int i = 0; i < 4; i++)
            {
                float tx = txArr[i], tz = tzArr[i];
                vertices[vIdx++] = new Vector3(tx - tr, 0, tz - tr);
                vertices[vIdx++] = new Vector3(tx + tr, 0, tz - tr);
                vertices[vIdx++] = new Vector3(tx + tr, 0, tz + tr);
                vertices[vIdx++] = new Vector3(tx - tr, 0, tz + tr);
                vertices[vIdx++] = new Vector3(tx - tr, 0.8f, tz - tr);    // severely reduced
                vertices[vIdx++] = new Vector3(tx + tr, 0.75f, tz - tr);
                vertices[vIdx++] = new Vector3(tx + tr, 0.6f, tz + tr);
                vertices[vIdx++] = new Vector3(tx - tr, 0.7f, tz + tr);
            }

            mesh.vertices = vertices;
            mesh.triangles = CreateStandardTriangles();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            SaveMeshAsset(mesh, "castle_ruined");
        }

        private static void CreateCriticalMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "castle_critical";

            // Mostly rubble - irregular scattered cubes
            Vector3[] vertices = new Vector3[24];
            int vIdx = 0;

            // 3 rubble chunks at various positions
            Vector3[] rubblePos = {
                new Vector3(-0.2f, 0, -0.1f),
                new Vector3(0.3f, 0, 0.2f),
                new Vector3(0, 0, -0.4f)
            };
            float[] rubbleScale = { 0.4f, 0.35f, 0.3f };

            for (int r = 0; r < 3; r++)
            {
                float s = rubbleScale[r];
                Vector3 pos = rubblePos[r];
                vertices[vIdx++] = pos + new Vector3(-s, 0, -s);
                vertices[vIdx++] = pos + new Vector3(s, 0, -s);
                vertices[vIdx++] = pos + new Vector3(s, 0, s);
                vertices[vIdx++] = pos + new Vector3(-s, 0, s);
                vertices[vIdx++] = pos + new Vector3(-s, s * 0.8f, -s);
                vertices[vIdx++] = pos + new Vector3(s, s * 0.7f, -s);
                vertices[vIdx++] = pos + new Vector3(s, s * 0.6f, s);
                vertices[vIdx++] = pos + new Vector3(-s, s * 0.75f, s);
            }

            mesh.vertices = vertices;

            int[] triangles = new int[108];
            int tIdx = 0;
            for (int r = 0; r < 3; r++)
            {
                int baseIdx = r * 8;
                // Standard cube triangles
                triangles[tIdx++] = baseIdx; triangles[tIdx++] = baseIdx + 2; triangles[tIdx++] = baseIdx + 1;
                triangles[tIdx++] = baseIdx; triangles[tIdx++] = baseIdx + 3; triangles[tIdx++] = baseIdx + 2;
                triangles[tIdx++] = baseIdx + 4; triangles[tIdx++] = baseIdx + 5; triangles[tIdx++] = baseIdx + 6;
                triangles[tIdx++] = baseIdx + 4; triangles[tIdx++] = baseIdx + 6; triangles[tIdx++] = baseIdx + 7;
                triangles[tIdx++] = baseIdx; triangles[tIdx++] = baseIdx + 1; triangles[tIdx++] = baseIdx + 5;
                triangles[tIdx++] = baseIdx; triangles[tIdx++] = baseIdx + 5; triangles[tIdx++] = baseIdx + 4;
                triangles[tIdx++] = baseIdx + 1; triangles[tIdx++] = baseIdx + 2; triangles[tIdx++] = baseIdx + 6;
                triangles[tIdx++] = baseIdx + 1; triangles[tIdx++] = baseIdx + 6; triangles[tIdx++] = baseIdx + 5;
                triangles[tIdx++] = baseIdx + 2; triangles[tIdx++] = baseIdx + 3; triangles[tIdx++] = baseIdx + 7;
                triangles[tIdx++] = baseIdx + 2; triangles[tIdx++] = baseIdx + 7; triangles[tIdx++] = baseIdx + 6;
                triangles[tIdx++] = baseIdx + 3; triangles[tIdx++] = baseIdx; triangles[tIdx++] = baseIdx + 4;
                triangles[tIdx++] = baseIdx + 3; triangles[tIdx++] = baseIdx + 4; triangles[tIdx++] = baseIdx + 7;
            }

            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            SaveMeshAsset(mesh, "castle_critical");
        }

        private static int[] CreateStandardTriangles()
        {
            int[] triangles = new int[108];
            int tIdx = 0;

            // Base cube
            triangles[tIdx++] = 0; triangles[tIdx++] = 2; triangles[tIdx++] = 1;
            triangles[tIdx++] = 0; triangles[tIdx++] = 3; triangles[tIdx++] = 2;
            triangles[tIdx++] = 4; triangles[tIdx++] = 5; triangles[tIdx++] = 6;
            triangles[tIdx++] = 4; triangles[tIdx++] = 6; triangles[tIdx++] = 7;
            triangles[tIdx++] = 0; triangles[tIdx++] = 1; triangles[tIdx++] = 5;
            triangles[tIdx++] = 0; triangles[tIdx++] = 5; triangles[tIdx++] = 4;
            triangles[tIdx++] = 2; triangles[tIdx++] = 3; triangles[tIdx++] = 7;
            triangles[tIdx++] = 2; triangles[tIdx++] = 7; triangles[tIdx++] = 6;
            triangles[tIdx++] = 3; triangles[tIdx++] = 0; triangles[tIdx++] = 4;
            triangles[tIdx++] = 3; triangles[tIdx++] = 4; triangles[tIdx++] = 7;
            triangles[tIdx++] = 1; triangles[tIdx++] = 2; triangles[tIdx++] = 6;
            triangles[tIdx++] = 1; triangles[tIdx++] = 6; triangles[tIdx++] = 5;

            // 4 corner towers
            for (int t = 0; t < 4; t++)
            {
                int baseIdx = 8 + t * 8;
                triangles[tIdx++] = baseIdx; triangles[tIdx++] = baseIdx + 2; triangles[tIdx++] = baseIdx + 1;
                triangles[tIdx++] = baseIdx; triangles[tIdx++] = baseIdx + 3; triangles[tIdx++] = baseIdx + 2;
                triangles[tIdx++] = baseIdx + 4; triangles[tIdx++] = baseIdx + 5; triangles[tIdx++] = baseIdx + 6;
                triangles[tIdx++] = baseIdx + 4; triangles[tIdx++] = baseIdx + 6; triangles[tIdx++] = baseIdx + 7;
                triangles[tIdx++] = baseIdx; triangles[tIdx++] = baseIdx + 1; triangles[tIdx++] = baseIdx + 5;
                triangles[tIdx++] = baseIdx; triangles[tIdx++] = baseIdx + 5; triangles[tIdx++] = baseIdx + 4;
                triangles[tIdx++] = baseIdx + 1; triangles[tIdx++] = baseIdx + 2; triangles[tIdx++] = baseIdx + 6;
                triangles[tIdx++] = baseIdx + 1; triangles[tIdx++] = baseIdx + 6; triangles[tIdx++] = baseIdx + 5;
            }

            return triangles;
        }

        private static void SaveMeshAsset(Mesh mesh, string name)
        {
            string assetPath = $"{CastleDir}/{name}.asset";
            AssetDatabase.CreateAsset(mesh, assetPath);
            Debug.Log($"[BuildCastleProceduralMeshes] Created: {assetPath}");
        }
    }
}
