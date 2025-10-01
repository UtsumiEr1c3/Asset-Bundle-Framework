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
    private static readonly Profiler ms_BuildProfiler = new Profiler(nameof(Builder));
    private static readonly Profiler ms_LoadBuildSettingProfiler = ms_BuildProfiler.CreateChild(nameof(LoadSetting));
    private static readonly Profiler ms_SwitchPlayformProfiler = ms_BuildProfiler.CreateChild(nameof(SwitchPlatform));

#if UNITY_IOS
    private const string PLATFORM = "iOS";
#elif UNITY_ANDROID
    private const string PLATFORM = "Android";
#else
    private const string PLATFORM = "Windows";
#endif

    public static BuildSetting buildSetting { get; private set; } // 打包设置

    public static string buildPath { get; private set; } // 打包目录

    #region Build MenuItem

    [MenuItem("Tools/ResBuild/Windows")]
    public static void BuildWindows()
    {
        Debug.Log("执行了BuildWindows");
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
    

    #endregion
}
