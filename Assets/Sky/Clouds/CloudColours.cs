using System;
using UnityEngine;

/// <summary>
/// A preset of keyed colours that the high parts (lit topside), mid parts (bulk) and low parts (underside) of a cloud should be matched to.
/// Create one of these to give clouds a colour to lerp towards, such as a white preset for midday or a dark gray preset for a thunderstorm, then assign them in SkyController.cs
/// </summary>

[Serializable]
public struct CloudColours
{
    public Color high, mid, low;
}
