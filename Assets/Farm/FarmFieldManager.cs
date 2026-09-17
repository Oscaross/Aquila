using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class FarmFieldManager : MonoBehaviour
{
    [SerializeField] private FarmField fieldPrefab;
    private readonly List<FarmField> fields = new();

    public void Register(FarmField field) => fields.Add(field);
    public void Unregister(FarmField field) => fields.Remove(field);

    private void Update()
    {
        float delta = Time.deltaTime * GameClock.TimeMultiplier;
        foreach (var field in fields)
        {
            field.Tick(delta);
        }
    }
}
