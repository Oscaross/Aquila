using UnityEngine;

[CreateAssetMenu(fileName = "SoundDefinition", menuName = "Scriptable Objects/Audio/SoundDefinition")]
public class SoundDefinition : ScriptableObject
{
    public AudioClip[] clips;
    [Range(0f, 1f)] public float volume = 1f;
    public Vector2 pitchRange = new Vector2(0.95f, 1.05f);
    public float minDistance = 2f;
    public float maxDistance = 25f;

    public AudioClip PickRandomClip()
    {
        Debug.Assert(clips != null && clips.Length > 0);
        return clips[Random.Range(0, clips.Length)];
    }
    public float PickRandomPitch() => Random.Range(pitchRange.x, pitchRange.y);
}
