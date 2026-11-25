#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class WhiteNoiseBuild
{
    [MenuItem("WHITE NOISE/Build Windows x64")]
    public static void BuildWindows()
    {
        string root = Directory.GetCurrentDirectory();
        string output = Path.Combine(root, "Build", "Windows");
        Directory.CreateDirectory(output);
        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0)
        {
            Debug.LogError("WHITE NOISE: no enabled scenes in Build Settings.");
            return;
        }

        BuildPlayerOptions options = new BuildPlayerOptions();
        options.scenes = scenes;
        options.locationPathName = Path.Combine(output, "WHITE_NOISE.exe");
        options.target = BuildTarget.StandaloneWindows64;
        options.options = BuildOptions.None;

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == BuildResult.Succeeded)
            Debug.Log("WHITE NOISE Windows build completed: " + options.locationPathName);
        else
            Debug.LogError("WHITE NOISE build failed: " + report.summary.result);
    }
}
#endif
