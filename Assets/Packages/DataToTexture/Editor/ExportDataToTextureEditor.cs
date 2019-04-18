using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ExportDataToTexture))]
public class ExportDataToTextureEditor : Editor {

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        ExportDataToTexture baker = target as ExportDataToTexture;
        //ボタンを表示
        if (GUILayout.Button("Bake Vector3ToTex"))
        {
            baker.Vector3ToTex();
        }
    }
}
