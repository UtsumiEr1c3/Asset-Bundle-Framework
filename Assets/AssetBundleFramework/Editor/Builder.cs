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
    public static readonly Vector2 collectRuleFileProgress = new Vector2(0, 0.2f);
    public static readonly Vector2 ms_GetDependencyProgress = new Vector2(0.2f, 0.4f);

    private static readonly Profiler ms_BuildProfiler = new Profiler(nameof(Builder));
    private static readonly Profiler ms_LoadBuildSettingProfiler = ms_BuildProfiler.CreateChild(nameof(LoadSetting));
    private static readonly Profiler ms_SwitchPlayformProfiler = ms_BuildProfiler.CreateChild(nameof(SwitchPlatform));
    private static readonly Profiler ms_CollectProfiler = ms_BuildProfiler.CreateChild(nameof(Collect));
    private static readonly Profiler ms_CollectBuildSettingFileProfiler = ms_CollectProfiler.CreateChild("CollectBuildSettingFile");
    private static readonly Profiler ms_CollectDependencyProfiler = ms_CollectProfiler.CreateChild(nameof(CollectDependency));

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

        // 搜集所有文件的依赖关系
        ms_CollectDependencyProfiler.Start();
        Dictionary<string, List<string>> dependencyDic = CollectDependency(files);
        ms_CollectDependencyProfiler.Stop();
    }

    /// <summary>
    /// 收集指定文件集合的所有依赖信息
    /// </summary>
    /// <param name="files"></param>
    /// <returns></returns>
    private static Dictionary<string, List<string>> CollectDependency(ICollection<string> files)
    {
        float min = ms_GetDependencyProgress.x;
        float max = ms_GetDependencyProgress.y;

        Dictionary<string, List<string>> dependencyDic = new Dictionary<string, List<string>>();

        // 声明fileList后, 不需要递归了
        List<string> fileList = new List<string>(files);

        for (int i = 0; i < fileList.Count; i++)
        {
            string assetUrl = fileList[i];
            if (dependencyDic.ContainsKey(assetUrl))
            {
                continue;
            }

            if (i % 10 == 0)
            {
                // 大概模拟进度
                float progress = min + (max - min) * ((float)i / (files.Count * 3));
                EditorUtility.DisplayProgressBar($"{nameof(CollectDependency)}", "搜集依赖信息", progress);
            }

            string[] dependencies = AssetDatabase.GetDependencies(assetUrl, false);
            List<string> dependencyList = new List<string>(dependencies.Length);

            // 过滤掉不符合要求的asset
            for (int ii = 0; ii < dependencies.Length; ii++)
            {
                string tempAssetUrl = dependencies[ii];
                string extension = Path.GetExtension(tempAssetUrl).ToLower();
                if (string.IsNullOrEmpty(extension) || extension == ".cs" || extension == ".dll")
                {
                    continue;
                }
                dependencyList.Add(tempAssetUrl);
                if (!fileList.Contains(tempAssetUrl))
                {
                    fileList.Add(tempAssetUrl);
                }
            }

            dependencyDic.Add(assetUrl, dependencyList);
        }

        return dependencyDic;
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
        string[] files = Directory.GetFiles(path, $"*.*", SearchOption.AllDirectories);
        List<string> result = new List<string>(files.Length);

        for (int i = 0; i < files.Length; i++)
        {
            string file = files[i].Replace('\\', '/');

            if (prefix != null && file.StartsWith(prefix, StringComparison.InvariantCulture))
            {
                continue;
            }

            if (suffixes != null && suffixes.Length > 0)
            {
                bool exist = false;

                for (int ii = 0; ii < suffixes.Length; ii++)
                {
                    string suffix = suffixes[ii];
                    if (file.EndsWith(suffix, StringComparison.InvariantCulture))
                    {
                        exist = true;
                        break;
                    }
                }

                if (!exist)
                {
                    continue;
                }
            }
            result.Add(file);
        }

        return result;
    }

    #endregion
}
