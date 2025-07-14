using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HotspotComponent : EntityComponent
{
    public override string name => "Hotspot";
    public Color color = Color.yellow;
    public float range = 1f;
    public override void Execute(object obj)
    {
        Debug.Log($"{name}  {obj}");
    }
}
