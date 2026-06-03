using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Press F5 to take a screenshot. Saved to Screenshots/ in the project root.
/// </summary>
public class RenderingScreenshotTool : MonoBehaviour
{
    [Tooltip("Supersize multiplier for higher resolution captures.")]
    [SerializeField] private int _superSize = 2;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame)
        {
            string folder = Path.Combine(Application.dataPath, "..", "Screenshots");
            Directory.CreateDirectory(folder);

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string path = Path.Combine(folder, $"{timestamp}.png");

            ScreenCapture.CaptureScreenshot(path, _superSize);
            Debug.Log($"[Screenshot] {path}");
        }
    }
}
