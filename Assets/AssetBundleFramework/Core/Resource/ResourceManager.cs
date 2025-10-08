using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ResourceManager
{
    private const string MANIFEST_BUNDLE = "manifest.ab";
    private const string RESOURCE_ASSET_NAME = "Assets/Temp/Resource.bytes";
    private const string BUNDLE_ASSET_NAME = "Assets/Temp/Bundle.bytes";
    private const string DEPENDENCY_ASSET_NAME = "Assets/Temp/Dependency.bytes";

    /// <summary>
    /// 单例
    /// </summary>
    public static ResourceManager instance { get; } = new ResourceManager();

    /// <summary>
    /// 保存资源对应的bundle
    /// </summary>
    internal Dictionary<string, string> ResourceBundleDic = new Dictionary<string, string>();

    /// <summary>
    /// 保存资源的依赖关系
    /// </summary>
    internal Dictionary<string, List<string>> ResourceDependencyDic = new Dictionary<string, List<string>>();

    /// <summary>
    /// 所有资源集合
    /// </summary>
    private Dictionary<string, AResource> m_ResourceDic = new Dictionary<string, AResource>();

    /// <summary>
    /// 需要释放的资源
    /// </summary>
    private LinkedList<AResource> m_NeedUnloadList = new LinkedList<AResource>();

    /// <summary>
    /// 异步加载集合
    /// </summary>
    private List<AResourceAsync> m_AsyncList = new List<AResourceAsync>();

    /// <summary>
    /// 是否使用AssetDatabase加载
    /// </summary>
    private bool m_Editor; 

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="platform">平台</param>
    /// <param name="getFileCallback">获取资源真实路径回调</param>
    /// <param name="editor">是否使用AssetDatabase加载</param>
    /// <param name="offset">获取bundle的偏移</param>
    public void Initialize(string platform, Func<string, string> getFileCallback, bool editor, ulong offset)
    {
        m_Editor = editor;

        if (m_Editor)
        {
            return;
        }

        BundleManager.instance.Initialize(platform, getFileCallback, offset);

        string manifestBundleFile = getFileCallback.Invoke(MANIFEST_BUNDLE);
        AssetBundle manifestAssetBundle = AssetBundle.LoadFromFile(manifestBundleFile, 0, offset);

        TextAsset resourceTextAsset = manifestAssetBundle.LoadAsset(RESOURCE_ASSET_NAME) as TextAsset;
        TextAsset bundleTextAsset = manifestAssetBundle.LoadAsset(BUNDLE_ASSET_NAME) as TextAsset;
        TextAsset dependencyTextAsset = manifestAssetBundle.LoadAsset(DEPENDENCY_ASSET_NAME) as TextAsset;

        byte[] resourseBytes = resourceTextAsset.bytes;
        byte[] bundleBytes = bundleTextAsset.bytes;
        byte[] dependencyBytes = dependencyTextAsset.bytes;

        manifestAssetBundle.Unload(true);
        manifestAssetBundle = null;

        // 保存id对应的assetUrl
        Dictionary<ushort, string> assetUrlDic = new Dictionary<ushort, string>();

        #region 读取资源信息

        {
            MemoryStream resourceMemoryStream = new MemoryStream(resourseBytes);
            BinaryReader resourceBinaryReader = new BinaryReader(resourceMemoryStream);
            // 获取资源个数
            ushort resourseCount = resourceBinaryReader.ReadUInt16();
            for (ushort i = 0; i < resourseCount; i++)
            {
                string assetUrl = resourceBinaryReader.ReadString();
                assetUrlDic.Add(i, assetUrl);
            }
        }

        #endregion

        #region 读取bundle信息

        {
            ResourceBundleDic.Clear();
            MemoryStream bundleMemoryStream = new MemoryStream(bundleBytes);
            BinaryReader bundleBinaryReader = new BinaryReader(bundleMemoryStream);
            // 获取bundle个数
            ushort bundleCount = bundleBinaryReader.ReadUInt16();
            for (int i = 0; i < bundleCount; i++)
            {
                string bundleUrl = bundleBinaryReader.ReadString();
                string bundleFileUrl = bundleUrl;
                ushort resourceCount = bundleBinaryReader.ReadUInt16();
                for (int ii = 0; ii < resourceCount; ii++)
                {
                    ushort assetId = bundleBinaryReader.ReadUInt16();
                    string assetUrl = assetUrlDic[assetId];
                    ResourceBundleDic.Add(assetUrl, bundleFileUrl);
                }
            }
        }

        #endregion

        #region 读取资源依赖信息

        {
            ResourceDependencyDic.Clear();
            MemoryStream dependencyMemoryStream = new MemoryStream(dependencyBytes);
            BinaryReader dependencyBinaryReader = new BinaryReader(dependencyMemoryStream);
            // 获取依赖链个数
            ushort dependencyCount = dependencyBinaryReader.ReadUInt16();
            for (int i = 0; i < dependencyCount; i++)
            {
                // 获取资源个数
                ushort resourceCount = dependencyBinaryReader.ReadUInt16();
                ushort assetId = dependencyBinaryReader.ReadUInt16();
                string assetUrl = assetUrlDic[assetId];
                List<string> dependencyList = new List<string>(resourceCount);
                for (int ii = 0; ii < resourceCount; ii++)
                {
                    ushort dependencyAssetId = dependencyBinaryReader.ReadUInt16();
                    string dependencyUrl = assetUrlDic[dependencyAssetId];
                    dependencyList.Add(dependencyUrl);
                }
                ResourceDependencyDic.Add(assetUrl, dependencyList);
            }
        }

        #endregion
    }

    /// <summary>
    /// 加载资源
    /// </summary>
    /// <param name="url">资源url</param>
    /// <param name="async">是否异步</param>
    /// <param name="callback">加载完成回调</param>
    public void LoadWithCallback(string url, bool async, Action<IResource> callback)
    {

    }

    /// <summary>
    /// 内部加载资源
    /// </summary>
    /// <param name="url">资源url</param>
    /// <param name="async">是否异步</param>
    /// <param name="dependency">是否依赖</param>
    /// <returns></returns>
    private AResource LoadInternal(string url, bool async, bool dependency)
    {
        AResource resource = null;
        if (m_ResourceDic.TryGetValue(url, out resource))
        {
            // 从需要释放的列表中移除
            if (resource.reference == 0)
            {
                m_NeedUnloadList.Remove(resource);
            }

            resource.AddReference();

            return resource;
        }

        if (m_Editor)
        {
            resource = new EditorResource();
        }
        else if (async)
        {
            ResourceAsync resourceAsync = new ResourceAsync();
            m_AsyncList.Add(resourceAsync);
            resource = resourceAsync;
        }
        else
        {
            resource = new Resource();
        }

        resource.url = url;
        m_ResourceDic.Add(url, resource);

        // 加载依赖
        List<string> dependencies = null;
        ResourceDependencyDic.TryGetValue(url, out dependencies);
        if (dependencies != null && dependencies.Count > 0)
        {
            resource.dependencies = new AResource[dependencies.Count];
            for (int i = 0; i < dependencies.Count; i++)
            {
                string dependencyUrl = dependencies[i];
                AResource dependencyResource = LoadInternal(dependencyUrl, async, true);
                resource.dependencies[i] = dependencyResource;
            }
        }

        resource.AddReference();
        resource.Load();

        return resource;
    }
}
