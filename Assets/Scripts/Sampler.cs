using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Sampler : MonoBehaviour
{
    MeshFilter mf;

    [SerializeField]
    Texture2D densityTex;

    float texelSize;

    int[] subdivisionLevels;

    TriangleData[] triangleDatas;

    int triangle_totalCount;

    [SerializeField]
    int samplingCount;

    Vector3[] points;

    // Use this for initialization
    void Start()
    {
        PreProcessing();

        DrawRandomPoints();
    }

    private void PreProcessing()
    {
        mf = this.GetComponent<MeshFilter>();

        triangle_totalCount = mf.mesh.triangles.Length / 3;

        var total_triangle_size = CalcuteTriangleTotalSize();

        triangleDatas = new TriangleData[triangle_totalCount];

        for (int i = 0; i < triangle_totalCount; i++)
        {
            var t0 = mf.mesh.triangles[i * 3];
            var t1 = mf.mesh.triangles[i * 3 + 1];
            var t2 = mf.mesh.triangles[i * 3 + 2];

            var v1 = mf.mesh.vertices[t0];
            var v2 = mf.mesh.vertices[t1];
            var v3 = mf.mesh.vertices[t2];

            var uv1 = mf.mesh.uv[t0];
            var uv2 = mf.mesh.uv[t1];
            var uv3 = mf.mesh.uv[t2];

            float size = GetTriangleSize(v1, v2, v3);

            TriangleData td = new TriangleData();

            Vector2 centerPos = GetBarycentric(uv1, uv2, uv3);

            float density = GetDensityFromTex(uv1, uv2, uv3);

            float pdf = (density * size) / total_triangle_size;
            td.index = i;
            td.pdf = pdf;

            for (int j = 0; j < i; j++)
            {
                td.cdf += triangleDatas[j].pdf;
            }

            td.cdf += pdf;

            triangleDatas[i] = td;
        }

        #region mesh subdivide
        //texelSize = densityTex.texelSize.x * densityTex.texelSize.y;

        //subdivisionLevels = new int[mf.mesh.triangles.Length / 3];

        //for (int i = 0; i < mf.mesh.triangles.Length; i+=3)
        //{
        //    var t0 = mf.mesh.triangles[i];
        //    var t1 = mf.mesh.triangles[i + 1];
        //    var t2 = mf.mesh.triangles[i + 2];

        //    var v1 = mf.mesh.vertices[t0];
        //    var v2 = mf.mesh.vertices[t1];
        //    var v3 = mf.mesh.vertices[t2];

        //    Vector3 v = Vector3.Cross(v1 - v2, v1 - v3);
        //    float size = v.magnitude * 0.5f;

        //    int subdivisionLevel = 0;

        //    while (!checkSize(size))
        //    {
        //        ++subdivisionLevel;
        //        size /= Mathf.Pow(4.0f, subdivisionLevel);
        //    }

        //    subdivisionLevels[i / 3] = subdivisionLevel;

        //    Debug.Log("level : " + subdivisionLevel);
        //}

        //Mesh mesh = mf.mesh;
        //MeshHelper.Subdivide(mesh, subdivisionLevels);   // divides a single quad into 6x6 quads
        //mf.mesh = mesh;

        #endregion
    }

    private Vector2 GetBarycentric(Vector2 uv1, Vector2 uv2, Vector2 uv3)
    {
        Vector2 bary;
        bary = (uv1 + uv2 + uv3) / 3.0f;

        return bary;
    }
    private void DrawRandomPoints()
    {
        points = new Vector3[samplingCount];
        
        for (int i = 0; i < samplingCount; i++)
        {
            var triangle_index = Bisection(triangleDatas, Random.value);

            if (triangle_index.HasValue)
            {
                var rnd1 = Random.value;
                var rnd2 = Random.value;

                var u = 1 - Mathf.Sqrt(rnd1);
                var v = rnd2 * Mathf.Sqrt(rnd1);

                var pos = UvToWorldPosition(new Vector2(u, v), triangle_index.Value);
                points[i] = pos;
            }
        }
    }

    Vector3 UvToWorldPosition(Vector2 uv, int index)
    {
        var t1 = mf.mesh.triangles[index * 3];
        var t2 = mf.mesh.triangles[index * 3 + 1];
        var t3 = mf.mesh.triangles[index * 3 + 2];

        var u = uv.x;
        var v = uv.y;

        float aa = u;
        float bb = v;
        float cc = 1 - u - v;

        Vector3 p3D = aa * mf.mesh.vertices[t1] + bb * mf.mesh.vertices[t2] + cc * mf.mesh.vertices[t3];

        return transform.TransformPoint(p3D);
    }

    public int? Bisection(TriangleData[] array, float target)
    {
        if (array == null || array.Length == 0)
            return null;

        int left = 0;
        int right = array.Length - 1;

        while (left <= right)
        {
            float d = (right - left) / 2.0f;
            if (d <= 0.5f)
                return right;

            int middle = left + (right - left) / 2;

            if (array[middle].cdf > target)
            {
                right = middle;
            }
            else if (array[middle].cdf < target)
            {
                left = middle;
            }
            else
            {
                return middle;
            }
        }

        Debug.Log("Seek Fail!!");
        return null;
    }

    public struct TriangleData
    {
        public int index;
        public float pdf;
        public float cdf;
    }

    bool checkSize(float triangle_size)
    {
        if (triangle_size > texelSize)
            return false;

        return true;
    }

    float CalcuteTriangleTotalSize()
    {
        float total_size = 0;

        for (int i = 0; i < triangle_totalCount; i++)
        {
            var t1 = mf.mesh.triangles[i * 3];
            var t2 = mf.mesh.triangles[i * 3 + 1];
            var t3 = mf.mesh.triangles[i * 3 + 2];

            var v1 = mf.mesh.vertices[t1];
            var v2 = mf.mesh.vertices[t2];
            var v3 = mf.mesh.vertices[t3];

            var size = GetTriangleSize(v1, v2, v3);

            var uv1 = mf.mesh.uv[t1];
            var uv2 = mf.mesh.uv[t2];
            var uv3 = mf.mesh.uv[t3];

            float density = GetDensityFromTex(uv1, uv2, uv3);

            total_size += (density * size);
        }

        return total_size;
    }

    private float GetDensityFromTex(Vector2 uv1, Vector2 uv2, Vector2 uv3)
    {
        Vector2 barycentricPos = GetBarycentric(uv1, uv2, uv3);

        float density = 1;
        if (densityTex != null)
        {
            Color c = densityTex.GetPixel((int)(barycentricPos.x * densityTex.width), (int)(barycentricPos.y * densityTex.height));
            density = c.r;
        }

        return density;
    }

    private float GetTriangleSize(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        Vector3 v = Vector3.Cross(v1 - v2, v1 - v3);
        float size = v.magnitude * 0.5f;
        return Mathf.Abs(size);
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Q))
            DrawRandomPoints();
    }

    private void OnDrawGizmos()
    {
        if(points != null)
        {
            for (int i = 0; i < points.Length; i++)
            {
                var pos = points[i];
                Gizmos.DrawSphere(pos, 0.01f);
            }
        }
    }
}
