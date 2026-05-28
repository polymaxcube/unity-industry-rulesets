#if PIXYZ_PLUGIN_FOR_UNITY
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.PixyzPlugin4Unity.Actions;

public class ExportMetadataAndPrefab : ActionInOut<IList<GameObject>, IList<GameObject>> 
{
    [UserParameter]
    [Tooltip("Target folder path for saving files (e.g., Assets/CAD_Output)")]
    public string exportFolder = "Assets/CAD_Output";

    [HelperMethod]
    public void ResetParameters() 
    {
        exportFolder = "Assets/CAD_Output";
    }

    public override int Id => 286404160;
    public override string MenuPathRuleEngine => "Custom/ExportMetadataAndPrefab";
    public override string MenuPathToolbox =>  "Custom/ExportMetadataAndPrefab";
    public override string Tooltip => "Extracts Pixyz Metadata to JSON and saves the object as a Prefab.";
    public override string Icon => null;
    public override int Priority => 15001; 

    public override IList<GameObject> Run(IList<GameObject> input) 
    {
        if (!Directory.Exists(exportFolder))
        {
            Directory.CreateDirectory(exportFolder);
        }

        foreach (GameObject go in input) 
        {
            if (go == null) continue;

            string cleanName = go.name.Replace(" ", "_"); 
            cleanName = SanitizeFileName(cleanName);

            Component metadataComponent = go.GetComponent("Metadata"); 
            
            if (metadataComponent != null) 
            {
                var method = metadataComponent.GetType().GetMethod("getProperties");
                if (method != null)
                {
                    var properties = method.Invoke(metadataComponent, null) as Dictionary<string, string>;
                    
                    if (properties != null && properties.Count > 0) 
                    {
                        string jsonString = ConvertDictionaryToJson(properties, cleanName);
                        string jsonPath = Path.Combine(exportFolder, cleanName + "_metadata.json");
                        File.WriteAllText(jsonPath, jsonString);
                    }
                }
            }

            bool isPartOfPrefab = PrefabUtility.IsPartOfAnyPrefab(go);
            bool isOutermostRoot = PrefabUtility.IsOutermostPrefabInstanceRoot(go);

            if (!isPartOfPrefab || isOutermostRoot)
            {
                string prefabPath = Path.Combine(exportFolder, cleanName + ".prefab");
                PrefabUtility.SaveAsPrefabAssetAndConnect(go, prefabPath, InteractionMode.AutomatedAction);
            }
            else
            {
                Debug.Log($"[ATT] Skipped saving child object '{go.name}' as a separate prefab (Saved within parent).");
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"[ATT] Exported JSON and Prefabs successfully to: {exportFolder}");

        return input;
    }

    private string ConvertDictionaryToJson(Dictionary<string, string> dict, string objectName) 
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"  \"cadPartName\": \"{objectName}\",");
        sb.AppendLine("  \"properties\": {");
        int count = 0;
        foreach (var kvp in dict) 
        {
            count++;
            string comma = (count < dict.Count) ? "," : "";
            string cleanValue = kvp.Value.Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", "");
            sb.AppendLine($"    \"{kvp.Key}\": \"{cleanValue}\"{comma}");
        }
        sb.AppendLine("  }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private string SanitizeFileName(string name)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        
        foreach (char c in invalidChars)
        {
            name = name.Replace(c, '_');
        }
        
        return name;
    }
}
#endif
