using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private int poolSize = 16;

    private AudioSource[] pool;
    private int next;
    
    public static AudioManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        pool = new AudioSource[poolSize];
            
        for (int i = 0; i < poolSize; i++)
        {
            var go = new GameObject($"AudioSource_{i}");
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.outputAudioMixerGroup = sfxGroup;
            pool[i] = src;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Plays a "one shot" sound, defined as a single sound that plays once.
    /// </summary>
    /// <param name="soundDefinition">A ScriptableObject class containing pitch range, sound variations, distance data for the given sound to play.</param>
    /// <param name="soundOrigin">Where, in world space, should the sound originate from.</param>
    public void PlayOneShot(SoundDefinition soundDefinition, Vector2 soundOrigin)
    {
        AudioSource src = GetFreeSource(); // pick an available channel to play the one shot on

        src.transform.position = soundOrigin;
        src.clip = soundDefinition.PickRandomClip();
        src.pitch = soundDefinition.PickRandomPitch();
        src.volume = soundDefinition.volume;
        src.spatialBlend = 1f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = soundDefinition.minDistance;
        src.maxDistance = soundDefinition.maxDistance;
        src.Play();
    }

    /// <summary>
    /// Finds an idle AudioSource. If none is available, steals the oldest one.
    /// </summary>
    /// <returns>The AudioSource to play a sound effect through.</returns>
    private AudioSource GetFreeSource()
    {
        for (int i = 0; i < pool.Length; i++)
        {
            int idx = (next + i) % pool.Length;

            if (!pool[idx].isPlaying)
            {
                next = (idx + 1) % pool.Length;
                return pool[idx];
            }
        }

        AudioSource stolen = pool[next];
        next = (next + 1) % pool.Length;
        return stolen;
    }
}
