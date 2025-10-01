using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public static class Builder
{
    public static readonly Vector2 collectTuleFileProgress = new Vector2(0, 0.2f);

    private static readonly Profiler ms_BuildProfiler = new Profiler(nameof(Builder));
    private static readonly Profiler ms_LoadBuildSettingProfiler = ms_BuildProfiler.CreateChild(nameof(LoadSetting));
    private static readonly Profiler ms_SwitchPlayformProfiler = ms_BuildProfiler.CreateChild(nameof(SwitchPlatform));
    private static readonly Profiler ms_CollectProfiler = ms_BuildProfiler.CreateChild(nameof(Collect));
    private static readonly Profiler ms_CollectBuildSettingFileProfiler = ms_CollectProfiler.CreateChild("CollectBuildSettingFile");

#if UNITY_IOS
    private const string PLATFORM = "iOS";
#elif UNITY_ANDROID
    private const string PLATFORM = "Android";
#else
    private const string PLATFORM = "Windows";
#endif

    public static BuildSetting buildSetting { get; private set; } // 打包设置

    public static string buildPath { get; private set; } // 打包目录

    public static readonly string BuildSettingPath = Path.GetFullPath("BuildSetting.xml").Replace("\\", "/"); // 打包配置

    #region Build MenuItem

    [MenuItem("Tools/ResBuild/Windows")]
    public static void BuildWindows()
    {
        Debug.Log("执行了BuildWindows");
        Build();
    }

    public static void SwitchPlatform()
    {
        string platform = PLATFORM;

        switch (platform)
        {
            case "Windows":
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
                break;
            case "Android":
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                break;
            case "iOS":
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
                break;
        }
    }

    private static BuildSetting LoadSetting(string settingPath)
    {
        buildSetting = XmlUtility.Read<BuildSetting>(settingPath);
        if (buildSetting == null)
        {
            throw new Exception($"Load buildSetting failed, SettingPath:{settingPath}");
        }

        (buildSetting as ISupportInitialize)?.EndInit();

        buildPath = Path.GetFullPath(buildSetting.buildRoot).Replace("\\", "/");
        if (buildPath.Length > 0 && buildPath[buildPath.Length - 1] != '/')
        {
            buildPath += "/";
        }

        buildPath += $"{PLATFORM}/";

        return buildSetting;
    }

    private static void Build()
    {
        ms_BuildProfiler.Start();

        ms_SwitchPlayformProfiler.Start();
        SwitchPlatform();
        ms_SwitchPlayformProfiler.Stop();

        ms_LoadBuildSettingProfiler.Start();
        buildSetting = LoadSetting(BuildSettingPath);
        ms_LoadBuildSettingProfiler.Stop();

        // 搜集Bundle信息
        ms_CollectProfiler.Start();
        Dictionary<string, List<string>> bundleDic = Collect();
        ms_CollectProfiler.Stop();

        ms_BuildProfiler.Stop();

        Debug.Log($"打包完成{ms_BuildProfiler}");
    }

    /// <summary>
    /// 搜集打包bundle的信息
    /// </summary>
    /// <returns></returns>
    private static Dictionary<string, List<string>> Collect()
    {
        // 获取所有打包设置的文件列表
        ms_CollectBuildSettingFileProfiler.Start();
        HashSet<string> files = buildSetting.Collect();
        ms_CollectBuildSettingFileProfiler.Stop();
    }

    /// <summary>
    /// 获取指定路径的文件
    /// </summary>
    /// <param name="path"></param>
    /// <param name="prefix"></param>
    /// <param name="suffixes"></param>
    /// <returns></returns>
    public static List<string> GetFiles(string path, string prefix, params string[] suffixes)
    {
        List<string> result = new List<string>();

        // TODO:

        return result;
    }

    #endregion
}
