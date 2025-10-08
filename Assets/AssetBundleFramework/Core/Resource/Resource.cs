using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal class Resource : AResource
{
    public override bool keepWaiting => !done;
}
