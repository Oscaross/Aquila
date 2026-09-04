using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// 
/// </summary>

public class FarmFieldManager : MonoBehaviour
{
    [SerializeField] private FarmField fieldPrefab;
    private readonly List<FarmField> fields = new();

    public void Register(FarmField field) => fields.Add(field);
    public void Unregister(FarmField field) => fields.Remove(field);

    private void Update()
    {
        float delta = Time.deltaTime * TimeOfDay.Instance.TimeMultiplier;
        foreach (var field in fields)
        {
            field.Tick(delta);
        }
    }

    private void Awake()
    {
        for (int i = 0; i < 4; i++)
        {
            FarmField field = Instantiate(fieldPrefab, new Vector2(Random.Range(-10f, 10f), 0f), Quaternion.identity, transform);
            Register(field);
        }
    }
}
