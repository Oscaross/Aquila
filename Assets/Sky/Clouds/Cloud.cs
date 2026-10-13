using System;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Cloud : MonoBehaviour
{
    public SpriteRenderer sr;
    public float localX, xMax, xMin, yNorm;

    void Awake() => sr = GetComponent<SpriteRenderer>();

    public static Cloud Create(Transform parent, Material mat, Sprite sprite, string sortingLayer, CloudType cloudType)
    {
        var go = new GameObject("Cloud");
        go.transform.SetParent(parent, false);
        var cloud = go.AddComponent<Cloud>();

        cloud.sr.sprite = sprite;
        cloud.sr.sharedMaterial = mat;
        cloud.sr.sortingLayerName = sortingLayer;
        cloud.gameObject.layer = cloud.transform.parent.gameObject.layer; // make sure the cloud shares the same layer as it's band so we cull it properly in e.g. the river renderer

        cloud.sr.sortingOrder = OrderFor(cloudType);
        return cloud;
    }

    private static int OrderFor(CloudType type) => type switch
    {
        CloudType.Near => 50,
        CloudType.Mid => 40,
        CloudType.Far => -100,
        _ => 0
    };
}