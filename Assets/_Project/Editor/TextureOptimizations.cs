using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

public class TextureOptimizations : EditorWindow
{
    // --- Common Settings ---
    private string selectedPath = "";
    private bool includeSubfolders = true;
    private int maxSize = 512;
    private TextureResizeAlgorithm resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
    private TextureImporterFormat format = TextureImporterFormat.Automatic;
    private int compressionQuality = 50; // 0-100 or -1 for None
    private bool useCrunchCompression = false;

    // --- Platform Overrides ---
    private bool overrideWindows = false;
    private int winMaxSize = 512;
    private TextureResizeAlgorithm winResize = TextureResizeAlgorithm.Mitchell;
    private TextureImporterFormat winFormat = TextureImporterFormat.Automatic;

    private bool overrideMac = false;
    private int macMaxSize = 512;
    private TextureResizeAlgorithm macResize = TextureResizeAlgorithm.Mitchell;
    private TextureImporterFormat macFormat = TextureImporterFormat.Automatic;

    private bool overrideLinux = false;
    private int linuxMaxSize = 512;
    private TextureResizeAlgorithm linuxResize = TextureResizeAlgorithm.Mitchell;
    private TextureImporterFormat linuxFormat = TextureImporterFormat.Automatic;

    private bool overrideAndroid = false;
    private int androidMaxSize = 512;
    private TextureResizeAlgorithm androidResize = TextureResizeAlgorithm.Mitchell;
    private TextureImporterFormat androidFormat = TextureImporterFormat.Automatic;
    private int androidCompressionQuality = 50;
    private AndroidETC2Fallback androidETC2Fallback = AndroidETC2Fallback.Quality16Bit;

    private bool overrideIOS = false;
    private int iosMaxSize = 512;
    private TextureResizeAlgorithm iosResize = TextureResizeAlgorithm.Mitchell;
    private TextureImporterFormat iosFormat = TextureImporterFormat.Automatic;
    private int iosCompressionQuality = 50;

    private bool overrideWeb = false;
    private int webMaxSize = 512;
    private TextureResizeAlgorithm webResize = TextureResizeAlgorithm.Mitchell;
    private TextureImporterFormat webFormat = TextureImporterFormat.Automatic;

    private bool overrideUWP = false;
    private int uwpMaxSize = 512;
    private TextureResizeAlgorithm uwpResize = TextureResizeAlgorithm.Mitchell;
    private TextureImporterFormat uwpFormat = TextureImporterFormat.Automatic;

    private const string LogFileName = "TextureCompressionLogs.txt";

    private Vector2 scrollPos;

    // --- Progress Control ---
    private bool isProcessing = false;
    private bool cancelRequested = false;
    private float progress = 0f;
    private string currentFile = "";
    private Queue<string> processingQueue;
    private int totalFiles = 0;
    private int processedCount = 0;
    private string logPath;

    private Stopwatch stopwatch = new Stopwatch();

    private readonly string[] sizeLabels = { "32", "64", "128", "256", "512", "1024", "2048", "4096", "8192" };
    private readonly int[] sizeValues = { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 };

    [MenuItem("Tools/Texture Optimizer")]
    public static void ShowWindow()
    {
        var window = GetWindow<TextureOptimizations>("Texture Optimizer");
        window.minSize = new Vector2(420, 600);
        window.Show();
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        GUILayout.Label("Select Texture Folder or File", EditorStyles.boldLabel);
        if (GUILayout.Button("Browse"))
        {
            string path = EditorUtility.OpenFolderPanel("Select Texture Folder", "Assets", "");
            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith(Application.dataPath))
                    path = "Assets" + path.Substring(Application.dataPath.Length);
                selectedPath = path;
            }
        }

        EditorGUILayout.LabelField("Selected Path", selectedPath);
        includeSubfolders = EditorGUILayout.Toggle("Include Subfolders", includeSubfolders);

        GUILayout.Space(10);
        GUILayout.Label("Default Texture Settings", EditorStyles.boldLabel);

        maxSize = EditorGUILayout.IntPopup("Max Size", maxSize, sizeLabels, sizeValues);
        resizeAlgorithm = (TextureResizeAlgorithm)EditorGUILayout.EnumPopup("Resize Algorithm", resizeAlgorithm);
        format = (TextureImporterFormat)EditorGUILayout.EnumPopup("Texture Format", format);

        string[] compOptions = { "None", "Low", "Normal", "High" };
        int[] compValues = { -1, 0, 50, 100 };
        int compIndex = Array.IndexOf(compValues, compressionQuality);
        if (compIndex < 0) compIndex = 2;
        compIndex = EditorGUILayout.Popup("Compression Preset", compIndex, compOptions);
        compressionQuality = compValues[compIndex];

        useCrunchCompression = EditorGUILayout.Toggle("Use Crunch Compression", useCrunchCompression);
        compressionQuality = EditorGUILayout.IntSlider("Compressor Quality", compressionQuality, 0, 100);

        GUILayout.Space(10);
        DrawPlatformOverrides();

        GUILayout.Space(20);

        if (!isProcessing)
        {
            if (GUILayout.Button("Optimize Textures", GUILayout.Height(40)))
            {
                if (string.IsNullOrEmpty(selectedPath))
                {
                    EditorUtility.DisplayDialog("No Path Selected", "Please select a folder or file.", "OK");
                    return;
                }
                bool confirm = EditorUtility.DisplayDialog(
                    "Confirm Optimization",
                    $"All textures under path:\n{selectedPath}\n\nwill be permanently modified with new settings.\nProceed?",
                    "Yes", "Cancel");
                if (confirm) StartProcessing();
            }
        }
        else
        {
            GUILayout.Label("Processing: " + Path.GetFileName(currentFile));
            Rect rect = GUILayoutUtility.GetRect(50, 20);
            EditorGUI.ProgressBar(rect, progress, $"{Mathf.RoundToInt(progress * 100)}%");

            // --- ETA and Speed Info ---
            double elapsed = stopwatch.Elapsed.TotalSeconds;
            double speed = processedCount / Math.Max(elapsed, 0.01);
            double remainingTime = (totalFiles - processedCount) / Math.Max(speed, 0.01);

            GUILayout.Label($"Speed: {speed:F2} files/sec | ETA: {FormatTime(remainingTime)}");

            if (GUILayout.Button("Cancel")) cancelRequested = true;
        }

        EditorGUILayout.EndScrollView();
    }

    private void StartProcessing()
    {
        string[] files = GetTextureFiles(selectedPath);
        processingQueue = new Queue<string>(files);
        totalFiles = files.Length;
        processedCount = 0;
        logPath = Path.Combine(Application.dataPath, LogFileName);
        File.WriteAllText(logPath, $"Texture Optimization Log - {DateTime.Now}\n\n");

        isProcessing = true;
        cancelRequested = false;
        stopwatch.Restart();

        EditorApplication.update += ProcessNextFile;
    }

    private void ProcessNextFile()
    {
        if (cancelRequested || processingQueue.Count == 0)
        {
            EndProcessing(cancelRequested);
            return;
        }

        string file = processingQueue.Dequeue();
        currentFile = file;
        progress = (float)processedCount / totalFiles;

        string relativePath = file.StartsWith(Application.dataPath)
            ? "Assets" + file.Substring(Application.dataPath.Length)
            : file;

        var importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
        if (importer != null)
        {
            string oldSettings = $"Old → Size:{importer.maxTextureSize}, Format:{importer.textureCompression}, Crunch:{importer.crunchedCompression}";

            importer.maxTextureSize = maxSize;

            var resizeProp = typeof(TextureImporter).GetProperty("resizeAlgorithm");
            resizeProp?.SetValue(importer, resizeAlgorithm);

            TextureImporterFormat finalFormat = GetCrunchFormat(importer, format);
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings()
            {
                name = "Default",
                maxTextureSize = maxSize,
                format = format,
                overridden = true,
                compressionQuality = compressionQuality < 0 ? 50 : compressionQuality,
                crunchedCompression = useCrunchCompression && IsCrunchSupported(finalFormat)
            });

            ApplyPlatformOverrides(importer);
            importer.SaveAndReimport();

            string newSettings = $"New → Size:{importer.maxTextureSize}, Format:{format}, Crunch:{useCrunchCompression}, Compression:{compressionQuality}";
            string logMsg = $"{relativePath}\n{oldSettings}\n{newSettings}\n";
            UnityEngine.Debug.Log(logMsg);
            File.AppendAllText(logPath, logMsg + "\n");
        }

        processedCount++;
        Repaint();
    }

    private void EndProcessing(bool canceled)
    {
        EditorApplication.update -= ProcessNextFile;
        isProcessing = false;
        stopwatch.Stop();
        progress = 1f;

        string result = canceled
            ? $"Operation canceled! Processed {processedCount}/{totalFiles} textures."
            : $"Processed {processedCount}/{totalFiles} textures.";
        EditorUtility.DisplayDialog("Done", $"{result}\nLogs at:\n{logPath}", "OK");

        AssetDatabase.Refresh();
        Repaint();
    }

    private string FormatTime(double seconds)
    {
        TimeSpan t = TimeSpan.FromSeconds(seconds);
        if (t.TotalHours >= 1) return $"{(int)t.TotalHours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";
        return $"{t.Minutes:D2}:{t.Seconds:D2}";
    }

    private void DrawPlatformOverrides()
    {
        GUILayout.Label("Platform Overrides", EditorStyles.boldLabel);

        overrideWindows = EditorGUILayout.BeginToggleGroup("Windows Override", overrideWindows);
        winMaxSize = EditorGUILayout.IntPopup("Max Size", winMaxSize, sizeLabels, sizeValues);
        winResize = (TextureResizeAlgorithm)EditorGUILayout.EnumPopup("Resize Algorithm", winResize);
        winFormat = (TextureImporterFormat)EditorGUILayout.EnumPopup("Format", winFormat);
        EditorGUILayout.EndToggleGroup();

        overrideMac = EditorGUILayout.BeginToggleGroup("Mac Override", overrideMac);
        macMaxSize = EditorGUILayout.IntPopup("Max Size", macMaxSize, sizeLabels, sizeValues);
        macResize = (TextureResizeAlgorithm)EditorGUILayout.EnumPopup("Resize Algorithm", macResize);
        macFormat = (TextureImporterFormat)EditorGUILayout.EnumPopup("Format", macFormat);
        EditorGUILayout.EndToggleGroup();

        overrideLinux = EditorGUILayout.BeginToggleGroup("Linux Override", overrideLinux);
        linuxMaxSize = EditorGUILayout.IntPopup("Max Size", linuxMaxSize, sizeLabels, sizeValues);
        linuxResize = (TextureResizeAlgorithm)EditorGUILayout.EnumPopup("Resize Algorithm", linuxResize);
        linuxFormat = (TextureImporterFormat)EditorGUILayout.EnumPopup("Format", linuxFormat);
        EditorGUILayout.EndToggleGroup();

        overrideAndroid = EditorGUILayout.BeginToggleGroup("Android Override", overrideAndroid);
        androidMaxSize = EditorGUILayout.IntPopup("Max Size", androidMaxSize, sizeLabels, sizeValues);
        androidResize = (TextureResizeAlgorithm)EditorGUILayout.EnumPopup("Resize Algorithm", androidResize);
        androidFormat = (TextureImporterFormat)EditorGUILayout.EnumPopup("Format", androidFormat);
        androidCompressionQuality = EditorGUILayout.IntSlider("Compressor Quality", androidCompressionQuality, 0, 100);
        androidETC2Fallback = (AndroidETC2Fallback)EditorGUILayout.EnumPopup("ETC2 Fallback", androidETC2Fallback);
        EditorGUILayout.EndToggleGroup();

        overrideIOS = EditorGUILayout.BeginToggleGroup("iOS Override", overrideIOS);
        iosMaxSize = EditorGUILayout.IntPopup("Max Size", iosMaxSize, sizeLabels, sizeValues);
        iosResize = (TextureResizeAlgorithm)EditorGUILayout.EnumPopup("Resize Algorithm", iosResize);
        iosFormat = (TextureImporterFormat)EditorGUILayout.EnumPopup("Format", iosFormat);
        iosCompressionQuality = EditorGUILayout.IntSlider("Compressor Quality", iosCompressionQuality, 0, 100);
        EditorGUILayout.EndToggleGroup();

        overrideWeb = EditorGUILayout.BeginToggleGroup("Web Override", overrideWeb);
        webMaxSize = EditorGUILayout.IntPopup("Max Size", webMaxSize, sizeLabels, sizeValues);
        webResize = (TextureResizeAlgorithm)EditorGUILayout.EnumPopup("Resize Algorithm", webResize);
        webFormat = (TextureImporterFormat)EditorGUILayout.EnumPopup("Format", webFormat);
        EditorGUILayout.EndToggleGroup();

        overrideUWP = EditorGUILayout.BeginToggleGroup("UWP Override", overrideUWP);
        uwpMaxSize = EditorGUILayout.IntPopup("Max Size", uwpMaxSize, sizeLabels, sizeValues);
        uwpResize = (TextureResizeAlgorithm)EditorGUILayout.EnumPopup("Resize Algorithm", uwpResize);
        uwpFormat = (TextureImporterFormat)EditorGUILayout.EnumPopup("Format", uwpFormat);
        EditorGUILayout.EndToggleGroup();
    }

    private void ApplyPlatformOverrides(TextureImporter importer)
    {
        if (overrideWindows)
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings()
            {
                name = "Standalone",
                maxTextureSize = winMaxSize,
                format = winFormat,
                overridden = true
            });

        if (overrideMac)
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings()
            {
                name = "Standalone",
                maxTextureSize = macMaxSize,
                format = macFormat,
                overridden = true
            });

        if (overrideLinux)
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings()
            {
                name = "Standalone",
                maxTextureSize = linuxMaxSize,
                format = linuxFormat,
                overridden = true
            });

        if (overrideAndroid)
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings()
            {
                name = "Android",
                maxTextureSize = androidMaxSize,
                format = androidFormat,
                overridden = true,
                compressionQuality = androidCompressionQuality,
                androidETC2FallbackOverride = (AndroidETC2FallbackOverride)androidETC2Fallback
            });

        if (overrideIOS)
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings()
            {
                name = "iPhone",
                maxTextureSize = iosMaxSize,
                format = iosFormat,
                overridden = true,
                compressionQuality = iosCompressionQuality
            });

        if (overrideWeb)
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings()
            {
                name = "WebGL",
                maxTextureSize = webMaxSize,
                format = webFormat,
                overridden = true
            });

        if (overrideUWP)
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings()
            {
                name = "WindowsStoreApps",
                maxTextureSize = uwpMaxSize,
                format = uwpFormat,
                overridden = true
            });
    }

    private string[] GetTextureFiles(string path)
    {
        string fullPath = path.StartsWith("Assets") ? Application.dataPath + path.Substring(6) : path;
        return Directory.GetFiles(fullPath, "*.*", includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
            .Where(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".tga", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".psd", StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private TextureImporterFormat GetCrunchFormat(TextureImporter importer, TextureImporterFormat requested)
    {
        if (!useCrunchCompression)
            return requested;

        // If Automatic or AutomaticCompressed, switch to crunchable format
        if (requested == TextureImporterFormat.Automatic || requested == TextureImporterFormat.Automatic)
        {
            bool hasAlpha = importer.DoesSourceTextureHaveAlpha();
            return hasAlpha ? TextureImporterFormat.DXT5Crunched : TextureImporterFormat.DXT1Crunched;
        }

        // Keep requested if it's already crunchable
        if (requested == TextureImporterFormat.DXT1Crunched ||
            requested == TextureImporterFormat.DXT5Crunched ||
            requested == TextureImporterFormat.ETC2_RGBA8Crunched)
            return requested;

        return requested; // If unsupported, fallback to requested
    }

    private bool IsCrunchSupported(TextureImporterFormat format)
    {
        return format == TextureImporterFormat.DXT1Crunched ||
               format == TextureImporterFormat.DXT5Crunched ||
               format == TextureImporterFormat.ETC2_RGBA8Crunched;
    }
}
