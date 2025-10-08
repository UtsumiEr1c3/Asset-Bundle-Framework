using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal abstract class AResource : CustomYieldInstruction, IResource
{
    /// <summary>
    /// Asset对应的Url
    /// </summary>
    public string url { get; set; }

    /// <summary>
    /// 加载完成的资源
    /// </summary>
    public virtual Object asset { get; protected set; }

    /// <summary>
    /// 依赖资源
    /// </summary>
    internal AResource[] dependencies { get; set; }

    /// <summary>
    /// 引用计数器
    /// </summary>
    internal int reference { get; set; }

    /// <summary>
    /// 是否加载完成
    /// </summary>
    internal bool done { get; set; }

    /// <summary>
    /// 增加引用
    /// </summary>
    /// <returns></returns>
    public void AddReference()
    {
        ++reference;
    }

    public Object GetAsset()
    {

        return asset;
    }

    public GameObject Instantiate()
    {
        Object obj = asset;

        if (!obj)
        {
            return null;
        }

        if (!(obj is GameObject))
        {
            return null;
        }

        return Object.Instantiate(obj) as GameObject;
    }

    public GameObject Instantiate(Transform parent, bool instantiateInWorldSpace)
    {
        Object obj = asset;

        if (!obj)
        {
            return null;
        }

        if (!(obj is GameObject))
        {
            return null;
        }

        return Object.Instantiate(obj, parent, instantiateInWorldSpace) as GameObject;
    }

    /// <summary>
    /// 加载资源
    /// </summary>
    internal abstract void Load();
}
