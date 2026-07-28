using UnityEngine;
using System.Collections;

public class AmbientAudioManager : MonoBehaviour
{
    [SerializeField] private AudioSource ambientSource;
    [SerializeField] private AudioClip eveningBirds;
    [SerializeField] private float fadeDuration = 2f;
    [SerializeField] private float targetVolume = 0.6f;

    void OnEnable() => TimeOfDay.OnSunrise += PlayMorningAmbience;
    void OnDisable() => TimeOfDay.OnSunrise -= PlayMorningAmbience;

    private void PlayMorningAmbience()
    {
        StartCoroutine(FadeIn(ambientSource, eveningBirds, fadeDuration, targetVolume));
    }

    private IEnumerator FadeIn(AudioSource source, AudioClip clip, float duration, float target)
    {
        source.clip = clip;
        source.volume = 0f;
        source.Play();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(0f, target, elapsed / duration);
            yield return null;
        }
        source.volume = target;
    }
}