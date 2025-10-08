using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal class EditorResource : AResource
{
    public override bool keepWaiting => !done;
}
