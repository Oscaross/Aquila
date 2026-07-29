using UnityEngine;

/**
 * Visually updates the number of tool sprites outside a building based on the number of workers that building has assigned to it. 
*/

public class ToolDisplay : MonoBehaviour
{
    [SerializeField] private SpriteRenderer target;
    [SerializeField] private Sprite[] byCount;

    public void SetCount(int n)
    {
        n = Mathf.Clamp(n, 0, byCount.Length - 1); // has to be between the number of byCount sprites (worker slots) and zero
        target.sprite = byCount[n];
        target.enabled = byCount[n] != null;
    }
}
