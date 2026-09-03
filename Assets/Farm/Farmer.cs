using System;
using UnityEngine;

public class Farmer : MonoBehaviour
{
    private Pathfinder pathfinder;
    private float morale;
    
    private void Awake()
    {
        pathfinder = GetComponent<Pathfinder>();
    }
}

enum FarmerState
{
    Idling,
    Harvesting,
    Transporting
}
