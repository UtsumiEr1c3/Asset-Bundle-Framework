using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 控制ab粒度
/// </summary>
public enum EBundleType
{
    File, // 以文件作为ab名字(最小粒度)
    Directory, // 以目录作为ab名字
    All // 以上所有的
}
