using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class WebGLAssetOptimizer : EditorWindow
{
    private Vector2 scrollPosition;
    private bool showTextureSettings = true;
    private bool showAudioSettings = true;
    private bool showBuildSettings = true;
    
    // Texture settings
    private int maxTextureSize = 1024;
    private int compressionQuality = 50;
    
    // Audio settings  
    private float audioQuality = 0.7f;
    
    // Progress tracking
    private float progress = 0f;
    private string progressMessage = "";
    
    [MenuItem("Tools/WebGL Asset Optimizer")]
    public static void ShowWindow()
    {
        var window = GetWindow<WebGLAssetOptimizer>("WebGL Asset Optimizer");
        window.minSize = new Vector2(400, 500);
    }
    
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        EditorGUILayout.LabelField("WebGL Asset Optimization", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // Texture Settings
        showTextureSettings = EditorGUILayout.Foldout(showTextureSettings, "Texture Settings", true);
        if (showTextureSettings)
        {
            EditorGUI.indentLevel++;
            maxTextureSize = EditorGUILayout.IntPopup("Max Texture Size", maxTextureSize,
                new string[] { "256", "512", "1024", "2048" },
                new int[] { 256, 512, 1024, 2048 });
            
            compressionQuality = EditorGUILayout.IntSlider("Compression Quality", compressionQuality, 0, 100);
            
            if (GUILayout.Button("Optimize All Textures"))
            {
                OptimizeAllTextures();
            }
            
            if (GUILayout.Button("Create Texture Atlas Guide"))
            {
                CreateTextureAtlasGuide();
            }
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // Audio Settings
        showAudioSettings = EditorGUILayout.Foldout(showAudioSettings, "Audio Settings", true);
        if (showAudioSettings)
        {
            EditorGUI.indentLevel++;
            audioQuality = EditorGUILayout.Slider("Audio Quality", audioQuality, 0f, 1f);
            
            if (GUILayout.Button("Optimize All Audio"))
            {
                OptimizeAllAudio();
            }
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // Build Settings
        showBuildSettings = EditorGUILayout.Foldout(showBuildSettings, "Build Settings", true);
        if (showBuildSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.HelpBox("Recommended WebGL Build Settings", MessageType.Info);
            
            if (GUILayout.Button("Apply Recommended Build Settings"))
            {
                ApplyRecommendedBuildSettings();
            }
            
            if (GUILayout.Button("Analyze Asset Usage"))
            {
                AnalyzeAssetUsage();
            }
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        
        // Batch Operations
        EditorGUILayout.LabelField("Batch Operations", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Run Full Optimization", GUILayout.Height(30)))
        {
            RunFullOptimization();
        }
        
        if (GUILayout.Button("Generate Optimization Report"))
        {
            GenerateOptimizationReport();
        }
        
        // Progress bar
        if (progress > 0)
        {
            EditorGUILayout.Space();
            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(GUILayout.Height(20)), progress, progressMessage);
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void OptimizeAllTextures()
    {
        var textures = AssetDatabase.FindAssets("t:Texture2D")
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .Where(path => path.StartsWith("Assets/"))
            .ToList();
        
        int current = 0;
        int total = textures.Count;
        int optimized = 0;
        
        EditorUtility.DisplayProgressBar("Optimizing Textures", "Starting...", 0);
        
        try
        {
            foreach (var texturePath in textures)
            {
                current++;
                EditorUtility.DisplayProgressBar("Optimizing Textures", 
                    $"Processing {Path.GetFileName(texturePath)} ({current}/{total})", 
                    (float)current / total);
                
                var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
                if (importer != null)
                {
                    bool changed = false;
                    
                    // Get or create WebGL platform settings
                    var platformSettings = importer.GetPlatformTextureSettings("WebGL");
                    
                    // Always override for WebGL
                    if (!platformSettings.overridden)
                    {
                        platformSettings.overridden = true;
                        changed = true;
                    }
                    
                    // Set max texture size
                    if (platformSettings.maxTextureSize != maxTextureSize)
                    {
                        platformSettings.maxTextureSize = maxTextureSize;
                        changed = true;
                    }
                    
                    // Use Automatic compression for WebGL
                    platformSettings.format = TextureImporterFormat.Automatic;
                    
                    // Set compression quality
                    if (platformSettings.compressionQuality != compressionQuality)
                    {
                        platformSettings.compressionQuality = compressionQuality;
                        changed = true;
                    }
                    
                    // Additional optimizations
                    if (importer.textureCompression != TextureImporterCompression.Compressed)
                    {
                        importer.textureCompression = TextureImporterCompression.Compressed;
                        changed = true;
                    }
                    
                    if (importer.crunchedCompression != true)
                    {
                        importer.crunchedCompression = true;
                        importer.compressionQuality = compressionQuality;
                        changed = true;
                    }
                    
                    // Disable unnecessary features for 2D games
                    if (importer.isReadable)
                    {
                        importer.isReadable = false;
                        changed = true;
                    }
                    
                    if (importer.mipmapEnabled)
                    {
                        importer.mipmapEnabled = false;
                        changed = true;
                    }
                    
                    if (changed)
                    {
                        importer.SetPlatformTextureSettings(platformSettings);
                        importer.SaveAndReimport();
                        optimized++;
                    }
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
        
        AssetDatabase.Refresh();
        Debug.Log($"Optimized {optimized} out of {total} textures for WebGL");
        EditorUtility.DisplayDialog("Texture Optimization Complete", 
            $"Successfully optimized {optimized} textures.\nTotal textures processed: {total}", "OK");
    }
    
    private void CreateTextureAtlasGuide()
    {
        var sprites = AssetDatabase.FindAssets("t:Sprite")
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .Where(path => path.StartsWith("Assets/"))
            .ToList();
        
        var report = new System.Text.StringBuilder();
        report.AppendLine("=== Texture Atlas Creation Guide ===");
        report.AppendLine();
        report.AppendLine("To create Sprite Atlases in Unity:");
        report.AppendLine("1. Go to: Assets > Create > 2D > Sprite Atlas");
        report.AppendLine("2. Name your atlas appropriately (e.g., UI_Atlas, Gameplay_Atlas)");
        report.AppendLine("3. Drag sprites into the 'Objects for Packing' list");
        report.AppendLine("4. Configure settings:");
        report.AppendLine("   - Pack Preview: Click to see the packed result");
        report.AppendLine("   - Max Texture Size: 2048 or 4096");
        report.AppendLine("   - Format: Automatic");
        report.AppendLine();
        
        // Group sprites by folder
        var spriteGroups = sprites.GroupBy(s => Path.GetDirectoryName(s))
            .OrderByDescending(g => g.Count());
        
        report.AppendLine("Suggested Atlas Groups:");
        foreach (var group in spriteGroups.Take(10))
        {
            report.AppendLine($"\n{Path.GetFileName(group.Key)} ({group.Count()} sprites)");
            foreach (var sprite in group.Take(5))
            {
                report.AppendLine($"  - {Path.GetFileName(sprite)}");
            }
            if (group.Count() > 5)
                report.AppendLine($"  ... and {group.Count() - 5} more");
        }
        
        var reportPath = "Assets/TextureAtlasGuide.txt";
        File.WriteAllText(reportPath, report.ToString());
        AssetDatabase.Refresh();
        
        Debug.Log($"Texture Atlas guide saved to: {reportPath}");
        EditorUtility.RevealInFinder(reportPath);
        EditorUtility.DisplayDialog("Atlas Guide Created", 
            $"Texture atlas guide has been created at:\n{reportPath}", "OK");
    }
    
    private void OptimizeAllAudio()
    {
        var audioClips = AssetDatabase.FindAssets("t:AudioClip")
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .Where(path => path.StartsWith("Assets/"))
            .ToList();
        
        int current = 0;
        int total = audioClips.Count;
        int optimized = 0;
        
        EditorUtility.DisplayProgressBar("Optimizing Audio", "Starting...", 0);
        
        try
        {
            foreach (var audioPath in audioClips)
            {
                current++;
                EditorUtility.DisplayProgressBar("Optimizing Audio", 
                    $"Processing {Path.GetFileName(audioPath)} ({current}/{total})", 
                    (float)current / total);
                
                var importer = AssetImporter.GetAtPath(audioPath) as AudioImporter;
                if (importer != null)
                {
                    var settings = importer.GetOverrideSampleSettings("WebGL");
                    bool changed = false;
                    
                    // Determine optimal settings based on file type
                    bool isMusic = audioPath.Contains("/Music/");
                    bool isSFX = audioPath.Contains("/SFX/") || audioPath.Contains("/Sounds/");
                    
                    var newLoadType = isMusic ? AudioClipLoadType.Streaming : AudioClipLoadType.CompressedInMemory;
                    if (settings.loadType != newLoadType)
                    {
                        settings.loadType = newLoadType;
                        changed = true;
                    }
                    
                    if (settings.compressionFormat != AudioCompressionFormat.AAC)
                    {
                        settings.compressionFormat = AudioCompressionFormat.AAC;
                        changed = true;
                    }
                    
                    if (settings.quality != audioQuality)
                    {
                        settings.quality = audioQuality;
                        changed = true;
                    }
                    
                    if (settings.sampleRateSetting != AudioSampleRateSetting.OptimizeSampleRate)
                    {
                        settings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
                        changed = true;
                    }
                    
                    if (changed)
                    {
                        importer.SetOverrideSampleSettings("WebGL", settings);
                        importer.SaveAndReimport();
                        optimized++;
                    }
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
        
        AssetDatabase.Refresh();
        Debug.Log($"Optimized {optimized} out of {total} audio clips for WebGL");
        EditorUtility.DisplayDialog("Audio Optimization Complete", 
            $"Successfully optimized {optimized} audio files.\nTotal audio files processed: {total}", "OK");
    }
    
    private void ApplyRecommendedBuildSettings()
    {
        // Player settings for WebGL
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.memorySize = 512;
        
        // Strip engine code
        PlayerSettings.stripEngineCode = true;
        
        // Graphics settings
        PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });
        
        // Disable unnecessary features
        PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
        PlayerSettings.WebGL.nameFilesAsHashes = true;
        
        Debug.Log("Applied recommended WebGL build settings");
        EditorUtility.DisplayDialog("Settings Applied", 
            "Recommended WebGL build settings have been applied successfully.", "OK");
    }
    
    private void AnalyzeAssetUsage()
    {
        var usedAssets = new HashSet<string>();
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path);
        
        foreach (var scenePath in scenes)
        {
            var dependencies = AssetDatabase.GetDependencies(scenePath, true);
            foreach (var dep in dependencies)
            {
                usedAssets.Add(dep);
            }
        }
        
        var allAssets = AssetDatabase.FindAssets("")
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .Where(path => path.StartsWith("Assets/") && 
                          !path.EndsWith(".cs") && 
                          !path.EndsWith(".meta") &&
                          !path.Contains("/Editor/"));
        
        var unusedAssets = allAssets.Where(a => !usedAssets.Contains(a)).ToList();
        
        var report = new System.Text.StringBuilder();
        report.AppendLine("=== Asset Usage Analysis ===");
        report.AppendLine($"Total Assets: {allAssets.Count()}");
        report.AppendLine($"Used Assets: {usedAssets.Count}");
        report.AppendLine($"Potentially Unused: {unusedAssets.Count}");
        report.AppendLine();
        report.AppendLine("Potentially Unused Assets (review before deleting):");
        
        foreach (var asset in unusedAssets.Take(100))
        {
            var fileInfo = new FileInfo(asset);
            if (fileInfo.Exists)
            {
                report.AppendLine($"  {asset} ({fileInfo.Length / 1024}KB)");
            }
        }
        
        if (unusedAssets.Count > 100)
        {
            report.AppendLine($"  ... and {unusedAssets.Count - 100} more");
        }
        
        var reportPath = "Assets/AssetUsageAnalysis.txt";
        File.WriteAllText(reportPath, report.ToString());
        AssetDatabase.Refresh();
        
        Debug.Log($"Asset usage analysis saved to: {reportPath}");
        EditorUtility.RevealInFinder(reportPath);
    }
    
    private void RunFullOptimization()
    {
        if (EditorUtility.DisplayDialog("Run Full Optimization", 
            "This will optimize all textures and audio files for WebGL.\nThis may take several minutes.\n\nContinue?", 
            "Yes", "Cancel"))
        {
            Debug.Log("Starting full WebGL optimization...");
            
            OptimizeAllTextures();
            OptimizeAllAudio();
            ApplyRecommendedBuildSettings();
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("Full optimization complete!");
            EditorUtility.DisplayDialog("Optimization Complete", 
                "All assets have been optimized for WebGL.\nCheck the console for details.", "OK");
        }
    }
    
    private void GenerateOptimizationReport()
    {
        var report = new System.Text.StringBuilder();
        report.AppendLine("WebGL Asset Optimization Report");
        report.AppendLine("================================");
        report.AppendLine($"Generated: {System.DateTime.Now}");
        report.AppendLine();
        
        // Texture analysis
        var textures = AssetDatabase.FindAssets("t:Texture2D")
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .Where(path => path.StartsWith("Assets/"));
        
        long totalTextureSize = 0;
        int unoptimizedTextures = 0;
        
        foreach (var path in textures)
        {
            var fileInfo = new FileInfo(path);
            if (fileInfo.Exists)
            {
                totalTextureSize += fileInfo.Length;
            }
            
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                var settings = importer.GetPlatformTextureSettings("WebGL");
                if (!settings.overridden)
                {
                    unoptimizedTextures++;
                }
            }
        }
        
        report.AppendLine("TEXTURES:");
        report.AppendLine($"  Total: {textures.Count()} files");
        report.AppendLine($"  Size: {totalTextureSize / (1024 * 1024):F2}MB");
        report.AppendLine($"  Unoptimized for WebGL: {unoptimizedTextures}");
        report.AppendLine();
        
        // Audio analysis
        var audioClips = AssetDatabase.FindAssets("t:AudioClip")
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .Where(path => path.StartsWith("Assets/"));
        
        long totalAudioSize = 0;
        int unoptimizedAudio = 0;
        
        foreach (var path in audioClips)
        {
            var fileInfo = new FileInfo(path);
            if (fileInfo.Exists)
            {
                totalAudioSize += fileInfo.Length;
            }
            
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer != null)
            {
                var settings = importer.GetOverrideSampleSettings("WebGL");
                if (settings.compressionFormat != AudioCompressionFormat.AAC)
                {
                    unoptimizedAudio++;
                }
            }
        }
        
        report.AppendLine("AUDIO:");
        report.AppendLine($"  Total: {audioClips.Count()} files");
        report.AppendLine($"  Size: {totalAudioSize / (1024 * 1024):F2}MB");
        report.AppendLine($"  Unoptimized for WebGL: {unoptimizedAudio}");
        report.AppendLine();
        
        // Recommendations
        report.AppendLine("RECOMMENDATIONS:");
        if (unoptimizedTextures > 0)
        {
            report.AppendLine($"  - Optimize {unoptimizedTextures} textures using 'Optimize All Textures'");
        }
        if (unoptimizedAudio > 0)
        {
            report.AppendLine($"  - Optimize {unoptimizedAudio} audio files using 'Optimize All Audio'");
        }
        if (totalTextureSize > 50 * 1024 * 1024)
        {
            report.AppendLine("  - Consider creating Sprite Atlases to reduce draw calls");
        }
        if (totalAudioSize > 20 * 1024 * 1024)
        {
            report.AppendLine("  - Consider reducing audio quality or removing unused audio files");
        }
        
        var reportPath = "Assets/WebGLOptimizationReport.txt";
        File.WriteAllText(reportPath, report.ToString());
        AssetDatabase.Refresh();
        
        Debug.Log($"Optimization report saved to: {reportPath}");
        EditorUtility.RevealInFinder(reportPath);
        EditorUtility.DisplayDialog("Report Generated", 
            $"Optimization report has been saved to:\n{reportPath}", "OK");
    }
}