using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Sampler))]
[RequireComponent(typeof(ExportDataToTexture))]
public class DrawOnMesh : MonoBehaviour
{
    [SerializeField]
    [Range(0, 0.1f)]
    float gizumoSphereSize = 0.008f;

    PointData[] points;
    //PointData[] pointDatas;

    Sampler sampler;
    Mesh mesh;

    SkinnedMeshRenderer skinRenderer;
    MeshFilter mf;
    float scale = 1;

    [SerializeField]
    Texture2D dataTex;

    [SerializeField]
    Texture2D distributionTex;

    ExportDataToTexture dataExporter;

    Vector3[] worldPositions;
    Vector3[] distributionDataList;

    private void Awake()
    {
        skinRenderer = this.GetComponentInChildren<SkinnedMeshRenderer>();
    }

    public void SetDistributionTexture(Texture2D tex)
    {
        distributionTex = tex;
        skinRenderer.material.SetTexture("_MainTex", tex);
    }

    // Use this for initialization
    public void Begin()
    {
        if (distributionTex == null)
            return;

        sampler = this.GetComponent<Sampler>();
        mf = this.GetComponent<MeshFilter>();
        dataExporter = this.GetComponent<ExportDataToTexture>();

        if (mf != null)
            mesh = mf.mesh;
        else
        {
            mesh = new Mesh();
            skinRenderer.BakeMesh(mesh);
        }

        scale = this.transform.localScale.magnitude / this.transform.lossyScale.magnitude;
        
        if (dataTex == null)
        {
            this.points = sampler.Sampling();

            Vector3[] positionDataList = new Vector3[this.points.Length];

            distributionDataList = new Vector3[positionDataList.Length];
            worldPositions = new Vector3[positionDataList.Length];

            for (int i = 0; i < positionDataList.Length; i++)
            {
                var p = points[i];
                positionDataList[i] = new Vector3(p.index, p.x, p.y);
                worldPositions[i] = GetWorldPosition(p.x, p.y, p.index);

                if(distributionTex != null)
                {
                    var uv = GetCoordinate(p.x, p.y, p.index);
                    var distributionColor = distributionTex.GetPixel((int)(uv.x * distributionTex.width), (int)(uv.y * distributionTex.height));
                    distributionDataList[i] = new Vector3(distributionColor.r, distributionColor.g, distributionColor.b);
                }
            }

            dataExporter.SetTargetData(positionDataList, distributionDataList);
        }
        else
        {
            int length = dataTex.width;
            distributionDataList = new Vector3[length];
            worldPositions = new Vector3[length];

            var pixels = dataTex.GetPixels();
            for (int i = 0; i < length; i++)
            {
                var p = dataTex.GetPixel(i, 0);
                if(p != null)
                worldPositions[i] = GetWorldPosition(p.g, p.b, (int)p.r);

                var d = dataTex.GetPixel(i, 1);
                distributionDataList[i] = new Vector3(d.r, d.g, d.b);
            }

            //positions = new Vector3[pixels.Length];
            //for (int i = 0; i < pixels.Length; i++)
            //{
            //    var col = pixels[i];

            //    var p = new PointData();
            //    p.index = (int)col.r;
            //    p.u = col.g;
            //    p.v = col.b;

            //    //Debug.Log("<color=green>" + col + "</color>");

            //    //positions[i] = UvToWorldPosition(p.u, p.v, p.index);
            //}
        }
    }


    // Update is called once per frame
    void Update()
    {

    }

    Vector2 GetCoordinate(float x, float y, int index)
    {
        var t1 = mesh.triangles[index * 3];
        var t2 = mesh.triangles[index * 3 + 1];
        var t3 = mesh.triangles[index * 3 + 2];

        Vector2 uv = Interpolate(x, y, mesh.uv[t1], mesh.uv[t2], mesh.uv[t3]);

        return uv;
    }

    Vector3 GetWorldPosition(float x, float y, int index)
    {
        var t1 = mesh.triangles[index * 3];
        var t2 = mesh.triangles[index * 3 + 1];
        var t3 = mesh.triangles[index * 3 + 2];

        Vector3 localPos = Interpolate(x, y, mesh.vertices[t1], mesh.vertices[t2], mesh.vertices[t3]);
        localPos *= scale;

        return transform.TransformPoint(localPos);
    }

    private Vector2 Interpolate(float x, float y, Vector2 t1, Vector2 t2, Vector2 t3)
    {
        float aa = x;
        float bb = y;
        float cc = 1 - x - y;

        Vector2 p = aa * t1 + bb * t2 + cc * t3;
        return p;
    }
    private Vector3 Interpolate(float x, float y, Vector3 t1, Vector3 t2, Vector3 t3)
    {
        float aa = x;
        float bb = y;
        float cc = 1 - x - y;

        Vector3 p = aa * t1 + bb * t2 + cc * t3;
        return p;
    }

    private void OnDrawGizmos()
    {
        if (worldPositions != null)
        {
            for (int i = 0; i < worldPositions.Length; i++)
            {
                var p = worldPositions[i];
                if(distributionDataList != null)
                {
                    var col = distributionDataList[i];
                    if (col == Vector3.zero)
                        continue;

                        Gizmos.color = new Color(col.x, col.y, col.z);
                }

                Gizmos.DrawSphere(p, gizumoSphereSize);
            }
        }
    }
}
