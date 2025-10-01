using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using UnityEngine;

public class BuildItem
{
    [DisplayName("资源路径")]
    [XmlAttribute("AssetPath")]
    public string assetPath { get; set; }

    [DisplayName("资源类型")]
    [XmlAttribute("ResourceType")]
    public EResourceType resourceType { get; set; } = EResourceType.Direct;

    [DisplayName("ab粒度")]
    [XmlAttribute("BundleType")]
    public EBundleType bundleType { get; set; } = EBundleType.File;

    [DisplayName("资源后缀")]
    [XmlAttribute("Suffix")]
    public string suffix { get; set; } = ".prefab";

    [XmlIgnore] 
    public List<string> ignorePaths { get; set; } = new List<string>();

    [XmlIgnore] 
    public List<string> suffixes { get; set; } = new List<string>();

    // 配合该打包设置的个数
    [XmlIgnore]
    public int count { get; set; }
}
