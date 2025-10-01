using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 引用类型
/// </summary>
public enum EResourceType
{
    Direct = 1, // 在打包设置中分析到的资源
    Dependency = 2, // 依赖资源
    Generate = 3 // 生成的文件
}
