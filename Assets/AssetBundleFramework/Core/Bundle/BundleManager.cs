using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;

internal class BundleManager
{
    private Func<string, string> m_GetFileCallback; // 获取资源真实路径回调

    internal ulong offset { get; private set; } // 加载bundle开始的偏移地址

    private AssetBundleManifest m_AssetBundleManifest; // bundle依赖管理信息

    public static readonly BundleManager instance = new BundleManager();

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="platform">平台</param>
    /// <param name="getFileCallback">获取资源真实路径回调</param>
    /// <param name="offset">加载bundle的偏移</param>
    internal void Initialize(string platform, Func<string, string> getFileCallback, ulong offset)
    {
        m_GetFileCallback = getFileCallback;
        this.offset = offset;

        string assetBundleManifestFile = getFileCallback.Invoke(platform);
        AssetBundle manifestAssetBundle = AssetBundle.LoadFromFile(assetBundleManifestFile);
        Object[] objs = manifestAssetBundle.LoadAllAssets();
        if (objs.Length == 0)
        {
            throw new Exception($"{nameof(BundleManager)}.{nameof(Initialize)}() AssetBundleManifest load fail");
        }

        m_AssetBundleManifest = objs[0] as AssetBundleManifest;
    }
}
