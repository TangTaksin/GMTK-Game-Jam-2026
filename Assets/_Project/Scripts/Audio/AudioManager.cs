using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Music Settings")]
    public AudioClip defaultMusicClip;

    [Header("Pitch Randomization (SFX)")]
    [SerializeField] private float minPitch = 0.95f;
    [SerializeField] private float maxPitch = 1.05f;

    [Header("Fade Durations")]
    [SerializeField] private float fadeOutDuration = 1.0f;
    [SerializeField] private float fadeInDuration = 1.0f;

    [Header("SFX Library")]
    public List<SoundClip> sfxLibrary = new List<SoundClip>();
    private Dictionary<string, AudioClip> sfxDictionary = new Dictionary<string, AudioClip>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // เตรียม Dictionary สำหรับ SFX
            foreach (var item in sfxLibrary)
            {
                if (item.clip != null && !sfxDictionary.ContainsKey(item.name))
                    sfxDictionary.Add(item.name, item.clip);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // เล่นเพลงเริ่มต้นถ้ามีการใส่ไฟล์ไว้
        if (defaultMusicClip != null) 
            PlayMusic(defaultMusicClip);
    }

    // ─── Music Logic ───

    public void PlayMusic(AudioClip clip, float volume = 0.5f)
    {
        if (clip == null) return;
        
        StopCoroutine("FadeInAudio");
        musicSource.clip = clip;
        StartCoroutine(FadeInAudio(musicSource, volume));
    }

    public void StopMusic()
    {
        StartCoroutine(FadeOutAudio(musicSource));
    }

    // ─── SFX Logic ───

    public void PlaySFX(string soundName)
    {
        if (sfxDictionary.TryGetValue(soundName, out AudioClip clip))
        {
            sfxSource.pitch = Random.Range(minPitch, maxPitch);
            sfxSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning($"[AudioManager] ไม่พบเสียงชื่อ: {soundName}");
        }
    }

    // ─── Fading Coroutines ───

    private IEnumerator FadeInAudio(AudioSource source, float targetVolume)
    {
        source.volume = 0;
        source.Play();
        while (source.volume < targetVolume)
        {
            source.volume += Time.deltaTime / fadeInDuration;
            yield return null;
        }
        source.volume = targetVolume;
    }

    private IEnumerator FadeOutAudio(AudioSource source)
    {
        float startVolume = source.volume;
        while (source.volume > 0)
        {
            source.volume -= startVolume * Time.deltaTime / fadeOutDuration;
            yield return null;
        }
        source.Stop();
        source.volume = startVolume; 
    }
}

[System.Serializable]
public class SoundClip
{
    public string name;
    public AudioClip clip;
}