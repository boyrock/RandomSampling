using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Events;

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

    SkinnedMeshRenderer skinRenderer;
    Mesh mesh;

    float scale;

    //PointData[] pointDatas;

    float[] sizeOfTriangles;

    // Use this for initialization
    void Start() { }

    public void Sampling(UnityAction<PointData[]> callback)
    {
        UnityAction callback_preProcessing = delegate
        {
            StartCoroutine(ChoiceRandomPoint(callback));
        };

        PreProcessing(callback_preProcessing);
    }

    public PointData[] Sampling()
    {
        PreProcessing();
        return ChoiceRandomPoint();
    }

    void PreProcessing(UnityAction callback)
    {
        mf = this.GetComponent<MeshFilter>();
        skinRenderer = this.GetComponentInChildren<SkinnedMeshRenderer>();

        if (mf != null)
            mesh = mf.mesh;
        else
        {
            mesh = new Mesh();
            skinRenderer.BakeMesh(mesh);
        }

        scale = this.transform.localScale.magnitude / this.transform.lossyScale.magnitude;

        triangle_totalCount = mesh.triangles.Length / 3;

        sizeOfTriangles = new float[triangle_totalCount];

        UnityAction<float> callback_calcuteTotalProbability = delegate(float totalProbability)
        {
            StartCoroutine(CalcuProbabilityOfTriangles(totalProbability, callback));
        };

        StartCoroutine(CalcuteTotalProbability(callback_calcuteTotalProbability));
    }

    void PreProcessing()
    {
        mf = this.GetComponent<MeshFilter>();
        skinRenderer = this.GetComponentInChildren<SkinnedMeshRenderer>();

        if (mf != null)
            mesh = mf.mesh;
        else
        {
            mesh = new Mesh();
            skinRenderer.BakeMesh(mesh);
        }

        scale = this.transform.localScale.magnitude / this.transform.lossyScale.magnitude;

        triangle_totalCount = mesh.triangles.Length / 3;

        sizeOfTriangles = new float[triangle_totalCount];

        var totalProbability = CalcuteTotalProbability();

        CalcuProbabilityOfTriangles(totalProbability);
    }

    IEnumerator CalcuProbabilityOfTriangles(float total_probability, UnityAction callback)
    {
        triangleDatas = new TriangleData[triangle_totalCount];

        Debug.Log("triangle_totalCount : " + triangle_totalCount);
        for (int i = 0; i < triangle_totalCount; i++)
        {
            var t0 = mesh.triangles[i * 3];
            var t1 = mesh.triangles[i * 3 + 1];
            var t2 = mesh.triangles[i * 3 + 2];

            var uv1 = mesh.uv[t0];
            var uv2 = mesh.uv[t1];
            var uv3 = mesh.uv[t2];

            float size = sizeOfTriangles[i];

            TriangleData td = new TriangleData();

            float density = GetDensityFromTex(uv1, uv2, uv3);

            float pdf = (density * size) / total_probability;
            td.index = i;
            td.pdf = pdf;

            for (int j = 0; j < i; j++)
            {
                td.cdf += triangleDatas[j].pdf;
            }

            td.cdf += pdf;

            triangleDatas[i] = td;

            yield return null;
        }

        callback();
    }

    void CalcuProbabilityOfTriangles(float total_probability)
    {
        triangleDatas = new TriangleData[triangle_totalCount];

        Debug.Log("triangle_totalCount : " + triangle_totalCount);
        for (int i = 0; i < triangle_totalCount; i++)
        {
            var t0 = mesh.triangles[i * 3];
            var t1 = mesh.triangles[i * 3 + 1];
            var t2 = mesh.triangles[i * 3 + 2];

            var uv1 = mesh.uv[t0];
            var uv2 = mesh.uv[t1];
            var uv3 = mesh.uv[t2];

            float size = sizeOfTriangles[i];

            TriangleData td = new TriangleData();

            float density = GetDensityFromTex(uv1, uv2, uv3);
            float pdf = (density * size) / total_probability;

            td.index = i;
            td.pdf = pdf;

            for (int j = 0; j < i; j++)
            {
                td.cdf += triangleDatas[j].pdf;
            }

            td.cdf += pdf;

            triangleDatas[i] = td;

        }
    }

    private void Subdivide()
    {
        //#region mesh subdivide

        //texelSize = densityTex.texelSize.x * densityTex.texelSize.y;

        //subdivisionLevels = new int[mesh.triangles.Length / 3];

        //for (int i = 0; i < mesh.triangles.Length; i += 3)
        //{
        //    var t0 = mesh.triangles[i];
        //    var t1 = mesh.triangles[i + 1];
        //    var t2 = mesh.triangles[i + 2];

        //    var v1 = mesh.vertices[t0];
        //    var v2 = mesh.vertices[t1];
        //    var v3 = mesh.vertices[t2];

        //    Vector3 v = Vector3.Cross(v1 - v2, v1 - v3);
        //    float size = v.magnitude * 0.5f;

        //    int subdivisionLevel = 0;

        //    while (!checkSize(size))
        //    {
        //        ++subdivisionLevel;
        //        size /= Mathf.Pow(4.0f, subdivisionLevel);
        //    }

        //    subdivisionLevels[i / 3] = subdivisionLevel - 1;

        //    //Debug.Log("level : " + subdivisionLevel);
        //}

        //Mesh mesh = mesh;

        //MeshHelper.Subdivide(mesh, subdivisionLevels);
        ////mesh = mesh;

        //#endregion
    }

    private Vector2 GetBarycentric(Vector2 uv1, Vector2 uv2, Vector2 uv3)
    {
        Vector2 bary;
        bary = (uv1 + uv2 + uv3) / 3.0f;

        return bary;
    }

    private IEnumerator ChoiceRandomPoint(UnityAction<PointData[]> callback)
    {
        Debug.Log("ChoiceRandomPoint!");

        var pointDatas = new PointData[samplingCount];

        for (int i = 0; i < samplingCount; i++)
        {
            PointData pointData = new PointData();

            var triangle_index = Bisection(triangleDatas, Random.value);

            if (triangle_index.HasValue)
            {
                var rnd1 = Random.value;
                var rnd2 = Random.value;

                var u = 1 - Mathf.Sqrt(rnd1);
                var v = rnd2 * Mathf.Sqrt(rnd1);

                pointData.index = triangle_index.Value;
                pointData.x = u;
                pointData.y = v;

                pointDatas[i] = pointData;
            }

            yield return null;
        }

        callback(pointDatas);
    }
    private PointData[] ChoiceRandomPoint()
    {
        Debug.Log("ChoiceRandomPoint!");

        var pointDatas = new List<PointData>();

        for (int i = 0; i < samplingCount; i++)
        {
            PointData pointData = new PointData();

            var triangle_index = Bisection(triangleDatas, Random.value);

            if (triangle_index.HasValue)
            {
                var rnd1 = Random.value;
                var rnd2 = Random.value;

                var u = 1 - Mathf.Sqrt(rnd1);
                var v = rnd2 * Mathf.Sqrt(rnd1);

                pointData.index = triangle_index.Value;
                pointData.x = u;
                pointData.y = v;

                pointDatas.Add(pointData);
            }
        }

        List<Vector3> worldPositions = ConvertToWorldPosition(pointDatas);

        var removeItems = GetRemoveIndex(worldPositions.ToArray());

        RemoveItem(pointDatas, removeItems);

        return pointDatas.ToArray();
    }

    private static void RemoveItem(List<PointData> source, List<int> removeItems)
    {
        var copy = source.ToList();
        for (int i = 0; i < removeItems.Count; i++)
        {
            var target = removeItems[i];

            var item = copy[target];
            source.Remove(item);
        }
    }

    List<Vector3> ConvertToWorldPosition(List<PointData> pList)
    {
        List<Vector3> worldPositions = new List<Vector3>();
        for (int i = 0; i < pList.Count; i++)
        {
            var p = pList[i];
            var worldPos = UvToWorldPosition(p.x, p.y, p.index);
            worldPositions.Add(worldPos);
        }

        return worldPositions;
    }

    [SerializeField]
    [Range(0, 10f)]
    float thresholdDist;

    List<int> GetRemoveIndex(Vector3[] positionDataList)
    {
        List<int> deleteIndies = new List<int>();

        for (int i = 0; i < positionDataList.Length; i++)
        {
            bool skip = false;

            for (int z = 0; z < deleteIndies.Count; z++)
            {
                if (deleteIndies[z] == i)
                    skip = true;
            }

            if (skip == true)
                continue;

            var p = positionDataList[i];

            for (int j = 0; j < positionDataList.Length; j++)
            {
                if (i == j)
                    continue;

                skip = false;

                var pp = positionDataList[j];
                var dist = Vector3.Distance(p, pp);

                if (dist <= thresholdDist)
                {
                    for (int z = 0; z < deleteIndies.Count; z++)
                    {
                        if (deleteIndies[z] == j)
                        {
                            skip = true;
                            break;
                        }
                    }

                    if (skip == false)
                        deleteIndies.Add(j);
                }
            }
        }
        //Debug.Log("deleteIndex : " + deleteIndex.Count);


        return deleteIndies;
    }
    Vector3 UvToWorldPosition(float u, float v, int index)
    {
        var t1 = mesh.triangles[index * 3];
        var t2 = mesh.triangles[index * 3 + 1];
        var t3 = mesh.triangles[index * 3 + 2];

        float aa = u;
        float bb = v;
        float cc = 1 - u - v;

        Vector3 p3D = aa * mesh.vertices[t1] + bb * mesh.vertices[t2] + cc * mesh.vertices[t3];

        p3D *= scale;

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


    bool checkSize(float triangle_size)
    {
        if (triangle_size > texelSize)
            return false;

        return true;
    }

    IEnumerator CalcuteTotalProbability(UnityAction<float> callback)
    {
        float total_probability = 0;

        for (int i = 0; i < triangle_totalCount; i++)
        {
            var t1 = mesh.triangles[i * 3];
            var t2 = mesh.triangles[i * 3 + 1];
            var t3 = mesh.triangles[i * 3 + 2];

            var v1 = mesh.vertices[t1];
            var v2 = mesh.vertices[t2];
            var v3 = mesh.vertices[t3];

            var size = GetTriangleSize(v1, v2, v3);

            sizeOfTriangles[i] = size;

            var uv1 = mesh.uv[t1];
            var uv2 = mesh.uv[t2];
            var uv3 = mesh.uv[t3];

            float density = GetDensityFromTex(uv1, uv2, uv3);

            total_probability += (density * size);

            yield return null;
        }

        callback(total_probability);
    }

    float CalcuteTotalProbability()
    {
        float total_probability = 0;

        for (int i = 0; i < triangle_totalCount; i++)
        {
            var t1 = mesh.triangles[i * 3];
            var t2 = mesh.triangles[i * 3 + 1];
            var t3 = mesh.triangles[i * 3 + 2];

            var v1 = mesh.vertices[t1];
            var v2 = mesh.vertices[t2];
            var v3 = mesh.vertices[t3];

            var size = GetTriangleSize(v1, v2, v3);

            sizeOfTriangles[i] = size;

            var uv1 = mesh.uv[t1];
            var uv2 = mesh.uv[t2];
            var uv3 = mesh.uv[t3];

            float density = GetDensityFromTex(uv1, uv2, uv3);

            total_probability += (density * size);
        }

        return total_probability;
    }

    private float GetDensityFromTex(Vector2 uv1, Vector2 uv2, Vector2 uv3)
    {
        Vector2 barycentricPos = GetBarycentric(uv1, uv2, uv3);

        float density = 1;
        if (densityTex != null)
        {
            var col1 = densityTex.GetPixel((int)(uv1.x * densityTex.width), (int)(uv1.y * densityTex.height));
            var col2 = densityTex.GetPixel((int)(uv2.x * densityTex.width), (int)(uv2.y * densityTex.height));
            var col3 = densityTex.GetPixel((int)(uv3.x * densityTex.width), (int)(uv3.y * densityTex.height));

            Color c = (col1 + col2 + col3) / 3f; //densityTex.GetPixel((int)(barycentricPos.x * densityTex.width), (int)(barycentricPos.y * densityTex.height));
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
    }
}
public struct TriangleData
{
    public int index;
    public float pdf;
    public float cdf;
}

public struct PointData
{
    public int index;
    public float x;
    public float y;
}
