using UnityEditor;
using UnityEngine;

/// <summary>
/// Copies dialogue-master.csv from design/ to StreamingAssets/ on entering play mode.
/// Edit the CSV in design/, it auto-syncs when you hit Play.
/// </summary>
[InitializeOnLoad]
public static class DialogueCSVSync
{
    private const string SOURCE = "design/dialogue-master.csv";
    private const string DEST = "Assets/StreamingAssets/dialogue-master.csv";

    static DialogueCSVSync()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode) return;

        string sourcePath = System.IO.Path.Combine(Application.dataPath, "..", SOURCE);
        string destPath = System.IO.Path.Combine(Application.dataPath, "..", DEST);

        if (!System.IO.File.Exists(sourcePath))
        {
            Debug.LogWarning($"[DialogueCSVSync] Source not found: {sourcePath}");
            return;
        }

        string destDir = System.IO.Path.GetDirectoryName(destPath);
        if (!System.IO.Directory.Exists(destDir))
            System.IO.Directory.CreateDirectory(destDir);

        System.IO.File.Copy(sourcePath, destPath, overwrite: true);
        AssetDatabase.Refresh();
        Debug.Log("[DialogueCSVSync] Synced dialogue-master.csv to StreamingAssets.");
    }
}
