using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityObject : MonoBehaviour
{
    public EntityComponentVolume volume;
    public List<EntityComponent> components;
    private void Awake()
    {
        if (volume != null) 
        {
            components = volume.Components;
        }
        foreach (var item in components)
        {
            item.Init(); 
        }
    }
    private void Start()
    {
        if(components != null)
        {
            foreach (var item in components)
            {
                item.Execute("test");
            }
        }
    }
}
