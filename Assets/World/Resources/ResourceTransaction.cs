using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public struct ResourceTransaction
{
    public ResourceTransaction(Resource resource, int amount)
    {
        if (amount <= 0) Debug.LogError("Can't have a resource transaction that is a negative or zero transaction!");
        this.resource = resource;
        this.amount = amount;
    }
    
    public Resource resource;
    public int amount;
}