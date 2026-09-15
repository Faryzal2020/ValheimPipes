using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class BuildAssetBundle : MonoBehaviour {
    [MenuItem("Assets/Build AssetBundles")]
    private static void BuildAllAssetBundles() {
        // Automatically ensure all prefabs in Assets/Prefabs have the assetBundleName assigned
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });
        foreach (string guid in prefabGuids) {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AssetImporter importer = AssetImporter.GetAtPath(path);
            if (importer != null && string.IsNullOrEmpty(importer.assetBundleName)) {
                importer.SetAssetBundleNameAndVariant("valheimpipes_assetbundle", "");
            }
        }
        AssetDatabase.SaveAssets();

        const string assetBundleOutputPath = "AssetBundles/StandaloneWindows";
        string hopperAssetBundlePath = Path.Combine(assetBundleOutputPath, "valheimpipes_assetbundle");

        BuildPipeline.BuildAssetBundles(assetBundleOutputPath, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
        FileUtil.ReplaceFile(hopperAssetBundlePath, "../ValheimPipes/valheimpipes_assetbundle");
        Debug.Log("AssetBundle build completed and copied to ValheimPipes/valheimpipes_assetbundle");
    }

    [MenuItem("Assets/Create Procedural Mesh")]
    static void CreateMesh() {
        string filePath = EditorUtility.SaveFilePanelInProject("Create Procedural Mesh", "Mesh", "asset", "");
        if (filePath == "") return;
        AssetDatabase.CreateAsset(new Mesh(), filePath);
    }
}
