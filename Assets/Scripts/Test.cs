using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] // This attribute makes the struct serializable so that it can be edited in the Inspector.
public struct NameColorPair
{
    public string name;
    public Color color;
}

public class Test : MonoBehaviour
{
    public List<NameColorPair> nameColorPairs = new List<NameColorPair>();

    // You can add more functionality to manipulate the list if needed.
}