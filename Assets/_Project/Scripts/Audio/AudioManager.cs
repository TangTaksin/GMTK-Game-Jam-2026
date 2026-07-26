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

    private Coroutine activeMusicRoutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // เตรียม Dictionary สำหรับ SFX
            foreach (var item in sfxLibrary)
            {
                if (item != null && item.clip != null && !string.IsNullOrEmpty(item.name) && !sfxDictionary.ContainsKey(item.name))
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
        if (clip == null || musicSource == null) return;
        
        if (activeMusicRoutine != null)
        {
            StopCoroutine(activeMusicRoutine);
        }
        activeMusicRoutine = StartCoroutine(FadeInAudio(clip, volume));
    }

    public void StopMusic()
    {
        if (musicSource == null) return;

        if (activeMusicRoutine != null)
        {
            StopCoroutine(activeMusicRoutine);
        }
        activeMusicRoutine = StartCoroutine(FadeOutAudio(musicSource));
    }

    [Header("Looping SFX Source")]
    [SerializeField] private AudioSource loopSFXSource;

    // ─── SFX Logic ───

    private AudioClip GetClip(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return null;

        if (sfxDictionary.TryGetValue(soundName, out AudioClip clip))
            return clip;

        // Fallback: search without spaces or case-insensitive
        string cleanName = soundName.Replace(" ", "").ToLower();
        foreach (var kvp in sfxDictionary)
        {
            if (kvp.Key.Replace(" ", "").ToLower() == cleanName)
            {
                return kvp.Value;
            }
        }

        return null;
    }

    public void PlaySFX(string soundName)
    {
        if (sfxSource == null) return;

        AudioClip clip = GetClip(soundName);
        if (clip != null)
        {
            sfxSource.pitch = Random.Range(minPitch, maxPitch);
            sfxSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning($"[AudioManager] ไม่พบเสียงชื่อ: {soundName}");
        }
    }

    public void PlayLoopingSFX(string soundName, float pitch = 1.0f)
    {
        AudioClip clip = GetClip(soundName);
        if (clip != null)
        {
            if (loopSFXSource == null)
            {
                loopSFXSource = gameObject.AddComponent<AudioSource>();
                loopSFXSource.loop = true;
                if (sfxSource != null)
                {
                    loopSFXSource.outputAudioMixerGroup = sfxSource.outputAudioMixerGroup;
                }
            }

            loopSFXSource.pitch = pitch;

            if (loopSFXSource.clip == clip && loopSFXSource.isPlaying) return;

            loopSFXSource.clip = clip;
            loopSFXSource.loop = true;
            loopSFXSource.Play();
        }
        else
        {
            Debug.LogWarning($"[AudioManager] ไม่พบเสียง Loop ชื่อ: {soundName}");
        }
    }

    public void StopLoopingSFX()
    {
        if (loopSFXSource != null && loopSFXSource.isPlaying)
        {
            loopSFXSource.Stop();
            loopSFXSource.clip = null;
        }
    }

    // ─── Fading Coroutines ───

    private IEnumerator FadeInAudio(AudioClip clip, float targetVolume)
    {
        if (fadeInDuration <= 0f)
        {
            musicSource.clip = clip;
            musicSource.volume = targetVolume;
            musicSource.Play();
            activeMusicRoutine = null;
            yield break;
        }

        musicSource.clip = clip;
        musicSource.volume = 0;
        musicSource.Play();

        while (musicSource.volume < targetVolume)
        {
            musicSource.volume += Time.unscaledDeltaTime / fadeInDuration;
            yield return null;
        }
        musicSource.volume = targetVolume;
        activeMusicRoutine = null;
    }

    private IEnumerator FadeOutAudio(AudioSource source)
    {
        float startVolume = source.volume;

        if (fadeOutDuration <= 0f)
        {
            source.Stop();
            activeMusicRoutine = null;
            yield break;
        }

        while (source.volume > 0)
        {
            source.volume -= startVolume * Time.unscaledDeltaTime / fadeOutDuration;
            yield return null;
        }
        source.Stop();
        source.volume = startVolume;
        activeMusicRoutine = null;
    }
}

[System.Serializable]
public class SoundClip
{
    public string name;
    public AudioClip clip;
}