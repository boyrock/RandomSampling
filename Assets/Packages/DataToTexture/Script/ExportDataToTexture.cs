using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ExportDataToTexture : MonoBehaviour
{
    ComputeBuffer positionDataBuf;
    ComputeBuffer colorDataBuf;

    [SerializeField]
    ComputeShader texGen;

    string folderPath;

    Vector3[] _positionData;

    [SerializeField]
    string dirPath;
    public string textureName;

    void Start() { }

    void Update() { }

    [SerializeField]
    bool isShowTexture = true;

    public Rect rec = new Rect(0, 0, 100, 100);
    Texture2D tex;
    private void OnGUI()
    {
        if (tex == null)
            return;

        if (isShowTexture == true &&  Event.current.type.Equals(EventType.Repaint))
            Graphics.DrawTexture(rec, tex);
    }

    public void SetTargetData(Vector3[] positionData)
    {
        _positionData = positionData;
    }

    public void Vector3ToTex()
    {
        if(_positionData == null || _positionData.Length == 0)
        {
            Debug.Log("<color=red>target data is empty</color>");
            return;
        }

        if(string.IsNullOrEmpty(dirPath))
        {
            folderPath = dirPath;
        }
        else
        {
            folderPath = Path.Combine("Assets", dirPath);
        }

        int texWidth = Mathf.NextPowerOfTwo(_positionData.Length);
        int texHeight = 1;

        Debug.Log("positionData.Length : " + _positionData.Length);
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
        positionDataBuf = new ComputeBuffer(_positionData.Length, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector3)));
        positionDataBuf.SetData(_positionData);

        texGen.SetTexture(0, "output", rt);
        texGen.SetBuffer(0, "positionBuf", positionDataBuf);

        texGen.Dispatch(0, _positionData.Length / 8 + 1, 1, 1);

        tex = RenderTextureToTexture2D.Convert(rt);

        var pixels = tex.GetPixels();
        //for (int i = 0; i < pixels.Length; i++)
        //{
        //    Debug.Log("<color=green>" + pixels[i] + "</color>");
        //    Debug.Log("<color=red>" + _data[i] + "</color>");
        //}

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

        if(positionDataBuf != null)
            positionDataBuf.Release();

        if (colorDataBuf != null)
            colorDataBuf.Release();

        Debug.Log("<color=green>bake completed</color>");
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
