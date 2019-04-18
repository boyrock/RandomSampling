using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class MeshHelper
{
    static List<Vector3> vertices;
    static List<Vector3> normals;
    static List<Color> colors;
    static List<Vector2> uv;
    static List<Vector2> uv1;
    static List<Vector2> uv2;

    static List<int> indices;
    static Dictionary<uint, int> newVectices;

    static void InitArrays(Mesh mesh)
    {
        vertices = new List<Vector3>(mesh.vertices);
        normals = new List<Vector3>(mesh.normals);
        colors = new List<Color>(mesh.colors);
        uv = new List<Vector2>(mesh.uv);
        uv1 = new List<Vector2>(mesh.uv2);
        uv2 = new List<Vector2>(mesh.uv3);
        indices = new List<int>();
        //indices = new List<int>(mesh.triangles);
    }
    static void CleanUp()
    {
        vertices = null;
        normals = null;
        colors = null;
        uv = null;
        uv1 = null;
        uv2 = null;
        indices = null;
    }

    #region Subdivide4 (2x2)
    static int GetNewVertex4(int i1, int i2)
    {
        int newIndex = vertices.Count;
        uint t1 = ((uint)i1 << 16) | (uint)i2;
        uint t2 = ((uint)i2 << 16) | (uint)i1;
        if (newVectices.ContainsKey(t2))
            return newVectices[t2];
        if (newVectices.ContainsKey(t1))
            return newVectices[t1];

        newVectices.Add(t1, newIndex);

        vertices.Add((vertices[i1] + vertices[i2]) * 0.5f);
        if (normals.Count > 0)
            normals.Add((normals[i1] + normals[i2]).normalized);
        if (colors.Count > 0)
            colors.Add((colors[i1] + colors[i2]) * 0.5f);
        if (uv.Count > 0)
            uv.Add((uv[i1] + uv[i2]) * 0.5f);
        if (uv1.Count > 0)
            uv1.Add((uv1[i1] + uv1[i2]) * 0.5f);
        if (uv2.Count > 0)
            uv2.Add((uv2[i1] + uv2[i2]) * 0.5f);

        return newIndex;
    }

    public static int[] Subdivide(int i1, int i2, int i3)
    {
        int[] t_indices = new int[12];

        int a = GetNewVertex4(i1, i2);
        int b = GetNewVertex4(i2, i3);
        int c = GetNewVertex4(i3, i1);
        t_indices[0] = i1;
        t_indices[1] = a;
        t_indices[2] = c;

        t_indices[3] = i2;
        t_indices[4] = b;
        t_indices[5] = a;

        t_indices[6] = i3;
        t_indices[7] = c;
        t_indices[8] = b;

        t_indices[9] = a;
        t_indices[10] = b;
        t_indices[11] = c;

        return t_indices;
    }

    public static void Subdivide(Mesh mesh, int[] levels)
    {
        newVectices = new Dictionary<uint, int>();

        InitArrays(mesh);

        int[] triangles = mesh.triangles;
        int count = triangles.Length / 3;

        int i1, i2, i3;

        for (int i = 0; i < count; i++)
        {
            i1 = triangles[i * 3];
            i2 = triangles[i * 3 + 1];
            i3 = triangles[i * 3 + 2];

            int suvdivisionLevel = levels[i];

            if(suvdivisionLevel > 0)
            {
                var temp_indices_1 = Subdivide(i1, i2, i3);

                indices.AddRange(temp_indices_1);
                if (suvdivisionLevel == 1)
                    indices.AddRange(temp_indices_1);

                for (int l2 = 0; suvdivisionLevel >= 2 && l2 < temp_indices_1.Length / 3; l2++)
                {
                    i1 = temp_indices_1[l2 * 3];
                    i2 = temp_indices_1[l2 * 3 + 1];
                    i3 = temp_indices_1[l2 * 3 + 2];

                    var temp_indices_2 = Subdivide(i1, i2, i3);

                    if (suvdivisionLevel == 2)
                        indices.AddRange(temp_indices_2);

                    for (int l3 = 0; suvdivisionLevel >= 3 && l3 < temp_indices_2.Length / 3; l3++)
                    {
                        i1 = temp_indices_2[l3 * 3];
                        i2 = temp_indices_2[l3 * 3 + 1];
                        i3 = temp_indices_2[l3 * 3 + 2];

                        var temp_indices_3 = Subdivide(i1, i2, i3);

                        if (suvdivisionLevel == 3)
                            indices.AddRange(temp_indices_3);

                        for (int l4 = 0; suvdivisionLevel >= 3 && l4 < temp_indices_3.Length / 3; l4++)
                        {
                            i1 = temp_indices_3[l4 * 3];
                            i2 = temp_indices_3[l4 * 3 + 1];
                            i3 = temp_indices_3[l4 * 3 + 2];

                            var temp_indices_4 = Subdivide(i1, i2, i3);

                            if (suvdivisionLevel == 4)
                                indices.AddRange(temp_indices_4);
                        }
                    }
                }
            }
            else
            {
                indices.Add(i1);
                indices.Add(i2);
                indices.Add(i3);
            }

        }

        //Debug.Log("mesh.vertices : " + vertices.Count);
        //Debug.Log("mesh.triangles : " + indices.Count);

        mesh.vertices = vertices.ToArray();
        mesh.triangles = indices.ToArray();
        if (normals.Count > 0)
            mesh.normals = normals.ToArray();
        if (colors.Count > 0)
            mesh.colors = colors.ToArray();
        if (uv.Count > 0)
            mesh.uv = uv.ToArray();
        if (uv1.Count > 0)
            mesh.uv2 = uv1.ToArray();
        if (uv2.Count > 0)
            mesh.uv2 = uv2.ToArray();

    }


    #endregion Subdivide

    public static Mesh DuplicateMesh(Mesh mesh)
    {
        return (Mesh)UnityEngine.Object.Instantiate(mesh);
    }
}