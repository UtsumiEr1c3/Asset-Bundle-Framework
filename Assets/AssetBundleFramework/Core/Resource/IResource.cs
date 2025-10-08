using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IResource
{
    Object GetAsset();
    GameObject Instantiate();
    GameObject Instantiate(Transform parent, bool instantiateInWorldSpace);
}
