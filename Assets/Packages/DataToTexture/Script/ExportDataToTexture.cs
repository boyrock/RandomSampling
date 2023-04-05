using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

public class ExportDataToTexture : MonoBehaviour
{
    [SerializeField]
    ComputeShader texGen;

    string folderPath;

    Data[] _data;
    ComputeBuffer _dataBuffer;

    public string outputPath;

    public string textureName;

    void Start() { }

    void Update() { }

    [SerializeField]
    bool isShowTexture = true;

    Rect rec = new Rect(0, 0, 100, 100);
    Texture2D tex;
    private void OnGUI()
    {
        if (tex == null)
            return;

        if (isShowTexture == true &&  Event.current.type.Equals(EventType.Repaint))
            Graphics.DrawTexture(rec, tex);
    }

    public void SetTargetData(Vector3[] pointDataList, Vector3[] distributionDataList)
    {
        _data = new Data[pointDataList.Length];

        for (int i = 0; i < _data.Length; i++)
        {
            var d = new Data();
            d.point = pointDataList[i];
            d.distribution = distributionDataList[i];

            if (d.distribution != Vector3.zero)
            {
                _data[i] = d;
            }
            else
            {
                //Debug.Log("d.distribution : " + d.distribution);
            }
        }
    }

    public void Vector3ToTex()
    {
        if (string.IsNullOrEmpty(outputPath))
            return;

        if(_data == null || _data.Length == 0)
        {
            Debug.Log("<color=red>target data is empty</color>");
            return;
        }

        if(string.IsNullOrEmpty(outputPath))
        {
            folderPath = outputPath;
        }
        else
        {
            folderPath = Path.Combine("Assets", outputPath);
        }

        int texWidth = Mathf.NextPowerOfTwo(_data.Length);
        int texHeight = 2;

        Debug.Log("positionData.Length : " + _data.Length);
        Debug.Log("texWidth : " + texWidth);

        RenderTexture rt = new RenderTexture(texWidth, texHeight, 0, RenderTextureFormat.ARGBFloat);
        rt.name = textureName;
        rt.enableRandomWrite = true;
        rt.Create();
        RenderTexture.active = rt;
        GL.Clear(true, true, Color.clear);


        //for (int i = 0; i < _data.Length; i++)
        //{
        //}
        _dataBuffer = new ComputeBuffer(_data.Length, Marshal.SizeOf(typeof(Data)));
        //positionDataBuf.SetData(_positionData);
        _dataBuffer.SetData(_data);
        texGen.SetTexture(0, "output", rt);
        //texGen.SetBuffer(0, "positionBuf", positionDataBuf);
        texGen.SetBuffer(0, "dataBuf", _dataBuffer);

        //texGen.Dispatch(0, _positionData.Length / 8 + 1, 1, 1);
        texGen.Dispatch(0, _data.Length / 8 + 1, 1, 1);

        tex = RenderTextureToTexture2D.Convert(rt);

        for (int i = 0; i < tex.width; i++)
        {
            var pos = tex.GetPixel(i, 0);
            var distrubution = tex.GetPixel(i, 1);
            //Debug.Log("<color=green>" + pos + "</color>");
            //Debug.Log("<color=red>" + distrubution + "</color>");
        }

        string fileName = rt.name + ".asset";
        string assetFilePath = Path.Combine("Assets/", fileName);
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.CreateAsset(tex, assetFilePath);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
#endif

        //Copy file to destination
        string toPath = Path.Combine(folderPath, fileName);

        Debug.Log("toPath : " + toPath);
        File.Copy(assetFilePath, toPath, true);

        if (rt != null)
            rt.Release();

        if (_dataBuffer != null)
            _dataBuffer.Release();

        Debug.Log("<color=green>bake completed</color>");
    }

    private void OnDestroy()
    {
        if(_dataBuffer != null)
        {
            _dataBuffer.Release();
        }
    }

    //void Read()
    //{
    //    Debug.Log("baked_tex width " + baked_tex.width);
    //    Debug.Log("baked_tex height " + baked_tex.height);
    //    var dataList = baked_tex.GetPixels();

    //    for (int i = 0; i < dataList.Length; i++)
    //    {
    //        Debug.Log(dataList[i]);
    //    }
    //}
}

public struct Data
{
    public Vector3 point;
    public Vector3 distribution;
}
