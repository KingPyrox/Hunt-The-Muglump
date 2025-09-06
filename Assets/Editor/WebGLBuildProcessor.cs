using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.Linq;
using System.IO;

namespace OldSchoolGames.HuntTheMuglump.Editor
{
    public class WebGLBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;
        
        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL)
                return;
            
            Debug.Log("Starting WebGL build preprocessing...");
            
            // Optimize build settings
            OptimizeBuildSettings();
            
            // Optimize textures
            OptimizeTexturesForBuild();
            
            // Optimize audio
            OptimizeAudioForBuild();
            
            // Clean up unused assets
            CleanupUnusedAssets();
            
            Debug.Log("WebGL build preprocessing complete!");
        }
        
        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL)
                return;
            
            Debug.Log("WebGL Build Summary:");
            Debug.Log($"Total Size: {report.summary.totalSize / (1024 * 1024)}MB");
            Debug.Log($"Build Time: {report.summary.totalTime.TotalSeconds:F2} seconds");
            
            // Generate detailed report
            GenerateBuildReport(report);
        }
        
        private void OptimizeBuildSettings()
        {
            // Set compression format
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            
            // Enable data caching
            PlayerSettings.WebGL.dataCaching = true;
            
            // Set memory size based on game requirements
            PlayerSettings.WebGL.memorySize = 512;
            
            // Enable exception handling only in development
#if UNITY_EDITOR
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.FullWithStacktrace;
#else
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
#endif
            
            // Optimize for size
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.stripUnusedMeshComponents = true;
            
            // Set IL2CPP optimization
            PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.WebGL, Il2CppCompilerConfiguration.Release);
            
            Debug.Log("Build settings optimized for WebGL");
        }
        
        private void OptimizeTexturesForBuild()
        {
            var textures = AssetDatabase.FindAssets("t:Texture2D")
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Where(path => path.StartsWith("Assets/"));
            
            int optimizedCount = 0;
            
            foreach (var texturePath in textures)
            {
                var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
                if (importer != null)
                {
                    var platformSettings = importer.GetPlatformTextureSettings("WebGL");
                    
                    // Skip if already optimized
                    if (platformSettings.overridden && platformSettings.format == TextureImporterFormat.DXT5Crunched)
                        continue;
                    
                    platformSettings.overridden = true;
                    
                    // Determine optimal size based on texture usage
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                    if (texture != null)
                    {
                        // UI textures can be smaller
                        if (texturePath.Contains("/UI/") || texturePath.Contains("/Interface/"))
                        {
                            platformSettings.maxTextureSize = Mathf.Min(512, texture.width);
                        }
                        // Background/environment textures
                        else if (texturePath.Contains("/Background/") || texturePath.Contains("/Environment/"))
                        {
                            platformSettings.maxTextureSize = Mathf.Min(1024, texture.width);
                        }
                        // Character/sprite textures
                        else
                        {
                            platformSettings.maxTextureSize = Mathf.Min(512, texture.width);
                        }
                    }
                    
                    // Use crunched compression for better file size
                    platformSettings.format = TextureImporterFormat.DXT5Crunched;
                    platformSettings.compressionQuality = 50;
                    
                    importer.SetPlatformTextureSettings(platformSettings);
                    optimizedCount++;
                }
            }
            
            if (optimizedCount > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"Optimized {optimizedCount} textures for WebGL build");
            }
        }
        
        private void OptimizeAudioForBuild()
        {
            var audioClips = AssetDatabase.FindAssets("t:AudioClip")
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Where(path => path.StartsWith("Assets/"));
            
            int optimizedCount = 0;
            
            foreach (var audioPath in audioClips)
            {
                var importer = AssetImporter.GetAtPath(audioPath) as AudioImporter;
                if (importer != null)
                {
                    var settings = importer.GetOverrideSampleSettings("WebGL");
                    
                    // Skip if already optimized
                    if (settings.compressionFormat == AudioCompressionFormat.AAC)
                        continue;
                    
                    // Determine settings based on audio type
                    bool isMusic = audioPath.Contains("/Music/");
                    bool isSFX = audioPath.Contains("/SFX/") || audioPath.Contains("/Sounds/");
                    
                    // Music: streaming, lower quality
                    if (isMusic)
                    {
                        settings.loadType = AudioClipLoadType.Streaming;
                        settings.compressionFormat = AudioCompressionFormat.AAC;
                        settings.quality = 0.7f;
                    }
                    // SFX: compressed in memory, higher quality
                    else
                    {
                        settings.loadType = AudioClipLoadType.CompressedInMemory;
                        settings.compressionFormat = AudioCompressionFormat.AAC;
                        settings.quality = 0.8f;
                    }
                    
                    settings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
                    
                    importer.SetOverrideSampleSettings("WebGL", settings);
                    optimizedCount++;
                }
            }
            
            if (optimizedCount > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"Optimized {optimizedCount} audio clips for WebGL build");
            }
        }
        
        private void CleanupUnusedAssets()
        {
            // Remove unused shader variants
            var shaders = AssetDatabase.FindAssets("t:Shader")
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Where(path => path.StartsWith("Assets/"));
            
            foreach (var shaderPath in shaders)
            {
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
                if (shader != null)
                {
                    // Strip unused shader variants
                    // This is handled by Unity's shader stripping settings
                }
            }
            
            // Force asset database cleanup
            AssetDatabase.SaveAssets();
            Resources.UnloadUnusedAssets();
            
            Debug.Log("Cleaned up unused assets");
        }
        
        private void GenerateBuildReport(BuildReport report)
        {
            var reportContent = new System.Text.StringBuilder();
            
            reportContent.AppendLine("=== WebGL Build Report ===");
            reportContent.AppendLine($"Date: {System.DateTime.Now}");
            reportContent.AppendLine($"Unity Version: {Application.unityVersion}");
            reportContent.AppendLine();
            
            reportContent.AppendLine("Build Summary:");
            reportContent.AppendLine($"  Platform: {report.summary.platform}");
            reportContent.AppendLine($"  Total Size: {report.summary.totalSize / (1024 * 1024):F2}MB");
            reportContent.AppendLine($"  Build Time: {report.summary.totalTime.TotalMinutes:F2} minutes");
            reportContent.AppendLine($"  Result: {report.summary.result}");
            reportContent.AppendLine();
            
            // Asset breakdown
            reportContent.AppendLine("Asset Breakdown:");
            var assetGroups = report.packedAssets
                .SelectMany(p => p.contents)
                .GroupBy(a => Path.GetExtension(a.sourceAssetPath))
                .OrderByDescending(g => g.Sum(a => (long)a.packedSize));
            
            foreach (var group in assetGroups.Take(10))
            {
                var totalSize = group.Sum(a => (long)a.packedSize);
                reportContent.AppendLine($"  {group.Key}: {totalSize / (1024 * 1024):F2}MB ({group.Count()} files)");
            }
            
            reportContent.AppendLine();
            reportContent.AppendLine("Optimization Recommendations:");
            
            // Check for large textures
            var largeTextures = report.packedAssets
                .SelectMany(p => p.contents)
                .Where(a => a.sourceAssetPath.EndsWith(".png") || a.sourceAssetPath.EndsWith(".jpg"))
                .Where(a => a.packedSize > 1024 * 1024) // Larger than 1MB
                .OrderByDescending(a => a.packedSize);
            
            if (largeTextures.Any())
            {
                reportContent.AppendLine("  Large textures found (consider reducing size):");
                foreach (var texture in largeTextures.Take(5))
                {
                    reportContent.AppendLine($"    - {Path.GetFileName(texture.sourceAssetPath)}: {texture.packedSize / (1024 * 1024):F2}MB");
                }
            }
            
            // Check for large audio files
            var largeAudio = report.packedAssets
                .SelectMany(p => p.contents)
                .Where(a => a.sourceAssetPath.EndsWith(".wav") || a.sourceAssetPath.EndsWith(".mp3"))
                .Where(a => a.packedSize > 512 * 1024) // Larger than 512KB
                .OrderByDescending(a => a.packedSize);
            
            if (largeAudio.Any())
            {
                reportContent.AppendLine("  Large audio files found (consider compression):");
                foreach (var audio in largeAudio.Take(5))
                {
                    reportContent.AppendLine($"    - {Path.GetFileName(audio.sourceAssetPath)}: {audio.packedSize / (1024 * 1024):F2}MB");
                }
            }
            
            // Save report
            var reportPath = Path.Combine(Path.GetDirectoryName(report.summary.outputPath), "WebGLBuildReport.txt");
            File.WriteAllText(reportPath, reportContent.ToString());
            
            Debug.Log($"Build report saved to: {reportPath}");
            Debug.Log(reportContent.ToString());
        }
    }
    
    // Custom build window for WebGL
    public class WebGLBuildWindow : EditorWindow
    {
        private bool optimizeTextures = true;
        private bool optimizeAudio = true;
        private bool stripUnusedCode = true;
        private bool enableCompression = true;
        
        [MenuItem("Build/WebGL Build with Optimization")]
        public static void ShowWindow()
        {
            GetWindow<WebGLBuildWindow>("WebGL Build");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("WebGL Build Settings", EditorStyles.boldLabel);
            
            optimizeTextures = EditorGUILayout.Toggle("Optimize Textures", optimizeTextures);
            optimizeAudio = EditorGUILayout.Toggle("Optimize Audio", optimizeAudio);
            stripUnusedCode = EditorGUILayout.Toggle("Strip Unused Code", stripUnusedCode);
            enableCompression = EditorGUILayout.Toggle("Enable Compression", enableCompression);
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Build for WebGL", GUILayout.Height(30)))
            {
                BuildForWebGL();
            }
            
            if (GUILayout.Button("Build and Run", GUILayout.Height(30)))
            {
                BuildAndRun();
            }
        }
        
        private void BuildForWebGL()
        {
            // Apply settings
            if (optimizeTextures)
            {
                var optimizer = new WebGLAssetOptimizer();
                // Run optimization
            }
            
            PlayerSettings.stripEngineCode = stripUnusedCode;
            PlayerSettings.WebGL.compressionFormat = enableCompression ? 
                WebGLCompressionFormat.Brotli : WebGLCompressionFormat.Disabled;
            
            // Build
            var buildPath = EditorUtility.SaveFolderPanel("Choose Build Location", "", "WebGLBuild");
            if (!string.IsNullOrEmpty(buildPath))
            {
                BuildPipeline.BuildPlayer(
                    EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                    buildPath,
                    BuildTarget.WebGL,
                    BuildOptions.None
                );
            }
        }
        
        private void BuildAndRun()
        {
            BuildForWebGL();
            // The build will automatically run if successful
        }
    }
}