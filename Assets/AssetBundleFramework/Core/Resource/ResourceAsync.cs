using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal class ResourceAsync : AResourceAsync
{
    public override bool keepWaiting => !done;
}
