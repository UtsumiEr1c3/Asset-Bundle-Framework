using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using log4net.Filter;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class BuildSetting : ISupportInitialize
{
    [DisplayName("项目名称")]
    [XmlAttribute("ProjectName")]
    public string projectName { get; set; }

    [DisplayName("后缀列表")]
    [XmlAttribute("SuffixList")]
    public List<string> suffixList { get; set; }

    [DisplayName("打包文件的目录文件夹")]
    [XmlAttribute("BuildRoot")]
    public string buildRoot { get; set; }

    [DisplayName("打包选项")]
    [XmlElement("BuildItem")]
    public List<BuildItem> items{ get; set; }

    [XmlIgnore]
    public Dictionary<string, BuildItem> itemDic = new Dictionary<string, BuildItem>();

    public void BeginInit()
    {

    }

    public void EndInit()
    {
        buildRoot = Path.GetFullPath(buildRoot).Replace("\\", "/");

        itemDic.Clear();

        for (int i = 0; i < items.Count; i++)
        {
            BuildItem buildItem = items[i];

            if (buildItem.bundleType == EBundleType.All || buildItem.bundleType == EBundleType.Directory)
            {
                if (!Directory.Exists(buildItem.assetPath))
                {
                    throw new Exception($"不存在资源路径: {buildItem.assetPath}");
                }
            }

            // 处理后缀
            string[] prefixes = buildItem.suffix.Split('|');
            for (int ii = 0; ii < prefixes.Length; ii++)
            {
                string prefix = prefixes[ii].Trim();
                if (!string.IsNullOrEmpty(prefix))
                {
                    buildItem.suffixes.Add(prefix);
                }
            }

            if (itemDic.ContainsKey(buildItem.assetPath))
            {
                throw new Exception($"重复的资源路径: {buildItem.assetPath}");
            }
            itemDic.Add(buildItem.assetPath, buildItem);
        }
    }

    /// <summary>
    /// 获取所有在打包设置的文件列表
    /// </summary>
    /// <returns></returns>
    public HashSet<string> Collect()
    {
        float min = Builder.collectRuleFileProgress.x;
        float max = Builder.collectRuleFileProgress.y;

        EditorUtility.DisplayProgressBar($"{nameof(Collect)}", "搜集打包规则资源", min);

        // 处理每个规则忽略的目录, 比如路径A/B/C. 需要忽略A/B
        for (int i = 0; i < items.Count; i++)
        {
            BuildItem buildItem_i = items[i];

            if (buildItem_i.resourceType != EResourceType.Direct)
            {
                continue;
            }
            buildItem_i.ignorePaths.Clear();
            for (int j = 0; j < items.Count; j++)
            {
                BuildItem buildItem_j = items[j];
                if (i != j && buildItem_j.resourceType == EResourceType.Direct)
                {
                    if (buildItem_j.assetPath.StartsWith(buildItem_i.assetPath, StringComparison.InvariantCulture))
                    {
                        buildItem_i.ignorePaths.Add(buildItem_j.assetPath);
                    }
                }
            }
        }

        // 存储被规则分析到的所有文件
        HashSet<string> files = new HashSet<string>();
        for (int i = 0; i < items.Count; i++)
        {
            BuildItem buildItem = items[i];

            EditorUtility.DisplayProgressBar($"{nameof(Collect)}", "搜集打包规则资源", min + (max - min) * ((float)i / items.Count - 1));

            if (buildItem.resourceType != EResourceType.Direct)
            {
                continue;
            }

            List<string> tempFiles = Builder.GetFiles(buildItem.assetPath, null, buildItem.suffixes.ToArray());
            for (int j = 0; j < tempFiles.Count; j++)
            {
                string file = tempFiles[j];

                // 过滤被忽略的
                if (IsIgnore(buildItem.ignorePaths, file))
                {
                    continue;
                }

                files.Add(file);
            }

            EditorUtility.DisplayProgressBar($"{nameof(Collect)}", "搜集打包设置资源", (float)(i + 1) / items.Count);
        }

        return files;
    }

    /// <summary>
    /// 文件是否在忽略列表里
    /// </summary>
    /// <param name="ignoreList"></param>
    /// <param name="file"></param>
    /// <returns></returns>
    public bool IsIgnore(List<string> ignoreList, string file)
    {
        for (int i = 0; i < ignoreList.Count; i++)
        {
            string ignorePath = ignoreList[i];

            if (string.IsNullOrEmpty(ignorePath))
            {
                continue;
            }

            if (file.StartsWith(ignorePath, StringComparison.InvariantCulture))
            {
                return true;
            }
        }

        return false;
    }
}
