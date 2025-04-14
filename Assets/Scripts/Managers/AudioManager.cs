using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    #region 싱글톤
    public static AudioManager Instance { get; private set; }
    #endregion

    [Header("오디오 믹서")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("오디오 설정")]
    [SerializeField] private SoundLibrary soundLibrary;
    [SerializeField] private int sfxPoolSize = 10;
    [SerializeField] private float sfxMax3DDistance = 20f;

    [Header("볼륨 설정")]
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float bgmVolume = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float uiVolume = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float ambientVolume = 1f;

    // BGM 플레이어 (항상 하나만 존재)
    private AudioSource bgmPlayer;
    private AudioSource ambientPlayer;
    
    // SFX 플레이어 풀
    private List<AudioSource> sfxPool = new List<AudioSource>();
    
    // UI 사운드 전용 플레이어
    private AudioSource uiPlayer;

    private string currentBGM = string.Empty;
    private string currentAmbient = string.Empty;

    private void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAudioSources()
    {
        // BGM 플레이어 설정
        bgmPlayer = gameObject.AddComponent<AudioSource>();
        bgmPlayer.loop = true;
        bgmPlayer.playOnAwake = false;
        bgmPlayer.spatialBlend = 0f; // 2D 사운드

        // Ambient 플레이어 설정
        ambientPlayer = gameObject.AddComponent<AudioSource>();
        ambientPlayer.loop = true;
        ambientPlayer.playOnAwake = false;
        ambientPlayer.spatialBlend = 0f; // 2D 사운드

        // UI 플레이어 설정
        uiPlayer = gameObject.AddComponent<AudioSource>();
        uiPlayer.loop = false;
        uiPlayer.playOnAwake = false;
        uiPlayer.spatialBlend = 0f; // 2D 사운드

        // SFX 풀 초기화
        GameObject sfxPoolObj = new GameObject("SFX_Pool");
        sfxPoolObj.transform.SetParent(transform);

        for (int i = 0; i < sfxPoolSize; i++)
        {
            GameObject sfxObj = new GameObject($"SFX_{i}");
            sfxObj.transform.SetParent(sfxPoolObj.transform);
            
            AudioSource source = sfxObj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 1f; // 3D 사운드
            source.maxDistance = sfxMax3DDistance;
            source.rolloffMode = AudioRolloffMode.Linear;
            
            sfxPool.Add(source);
        }

        // 볼륨 초기화
        ApplyVolume();
    }

    // 마스터 볼륨 설정
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        ApplyVolume();
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.Save();
    }

    // BGM 볼륨 설정
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        ApplyVolume();
        PlayerPrefs.SetFloat("BGMVolume", bgmVolume);
        PlayerPrefs.Save();
    }

    // SFX 볼륨 설정
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        ApplyVolume();
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.Save();
    }

    // UI 볼륨 설정
    public void SetUIVolume(float volume)
    {
        uiVolume = Mathf.Clamp01(volume);
        ApplyVolume();
        PlayerPrefs.SetFloat("UIVolume", uiVolume);
        PlayerPrefs.Save();
    }

    // Ambient 볼륨 설정
    public void SetAmbientVolume(float volume)
    {
        ambientVolume = Mathf.Clamp01(volume);
        ApplyVolume();
        PlayerPrefs.SetFloat("AmbientVolume", ambientVolume);
        PlayerPrefs.Save();
    }

    // 볼륨 설정 불러오기
    public void LoadVolumeSettings()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        uiVolume = PlayerPrefs.GetFloat("UIVolume", 1f);
        ambientVolume = PlayerPrefs.GetFloat("AmbientVolume", 1f);
        ApplyVolume();
    }

    // 볼륨 적용
    private void ApplyVolume()
    {
        if (audioMixer != null)
        {
            // 데시벨 변환 (-80dB ~ 0dB)
            float masterDB = masterVolume > 0.001f ? Mathf.Log10(masterVolume) * 20 : -80f;
            float bgmDB = bgmVolume > 0.001f ? Mathf.Log10(bgmVolume) * 20 : -80f;
            float sfxDB = sfxVolume > 0.001f ? Mathf.Log10(sfxVolume) * 20 : -80f;
            float uiDB = uiVolume > 0.001f ? Mathf.Log10(uiVolume) * 20 : -80f;
            float ambientDB = ambientVolume > 0.001f ? Mathf.Log10(ambientVolume) * 20 : -80f;

            audioMixer.SetFloat("MasterVolume", masterDB);
            audioMixer.SetFloat("BGMVolume", bgmDB);
            audioMixer.SetFloat("SFXVolume", sfxDB);
            audioMixer.SetFloat("UIVolume", uiDB);
            audioMixer.SetFloat("AmbientVolume", ambientDB);
        }
        else
        {
            // 믹서가 없는 경우 직접 볼륨 설정
            bgmPlayer.volume = masterVolume * bgmVolume;
            ambientPlayer.volume = masterVolume * ambientVolume;
            uiPlayer.volume = masterVolume * uiVolume;
            
            foreach (var source in sfxPool)
            {
                source.volume = masterVolume * sfxVolume;
            }
        }
    }

    // BGM 재생
    public void PlayBGM(string soundName, float fadeTime = 1.0f)
    {
        if (string.IsNullOrEmpty(soundName) || currentBGM == soundName) return;

        AudioClip clip = soundLibrary.GetClip(SoundType.BGM, soundName);
        if (clip == null)
        {
            Debug.LogWarning($"BGM 클립을 찾을 수 없음: {soundName}");
            return;
        }

        StartCoroutine(FadeBGM(clip, fadeTime));
        currentBGM = soundName;
    }

    // BGM 페이드
    private IEnumerator FadeBGM(AudioClip newClip, float fadeTime)
    {
        // 현재 재생 중인 BGM이 있으면 페이드 아웃
        if (bgmPlayer.isPlaying && fadeTime > 0)
        {
            float startVolume = bgmPlayer.volume;
            float timer = 0;

            while (timer < fadeTime)
            {
                timer += Time.deltaTime;
                bgmPlayer.volume = masterVolume * bgmVolume * Mathf.Lerp(startVolume, 0, timer / fadeTime);
                yield return null;
            }
        }

        // 새 BGM 설정 및 재생
        bgmPlayer.clip = newClip;
        bgmPlayer.volume = 0;
        bgmPlayer.Play();

        // 페이드 인
        if (fadeTime > 0)
        {
            float timer = 0;
            while (timer < fadeTime)
            {
                timer += Time.deltaTime;
                bgmPlayer.volume = masterVolume * bgmVolume * Mathf.Lerp(0, 1, timer / fadeTime);
                yield return null;
            }
        }
        else
        {
            bgmPlayer.volume = masterVolume * bgmVolume;
        }
    }

    // BGM 정지
    public void StopBGM(float fadeTime = 1.0f)
    {
        if (!bgmPlayer.isPlaying) return;
        
        if (fadeTime > 0)
            StartCoroutine(FadeOutBGM(fadeTime));
        else
        {
            bgmPlayer.Stop();
            currentBGM = string.Empty;
        }
    }

    // BGM 페이드 아웃
    private IEnumerator FadeOutBGM(float fadeTime)
    {
        float startVolume = bgmPlayer.volume;
        float timer = 0;

        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            bgmPlayer.volume = Mathf.Lerp(startVolume, 0, timer / fadeTime);
            yield return null;
        }

        bgmPlayer.Stop();
        currentBGM = string.Empty;
    }

    // Ambient 사운드 재생
    public void PlayAmbient(string soundName, float fadeTime = 1.0f)
    {
        if (string.IsNullOrEmpty(soundName) || currentAmbient == soundName) return;

        AudioClip clip = soundLibrary.GetClip(SoundType.Ambient, soundName);
        if (clip == null)
        {
            Debug.LogWarning($"Ambient 클립을 찾을 수 없음: {soundName}");
            return;
        }

        StartCoroutine(FadeAmbient(clip, fadeTime));
        currentAmbient = soundName;
    }

    // Ambient 페이드
    private IEnumerator FadeAmbient(AudioClip newClip, float fadeTime)
    {
        // 현재 재생 중인 Ambient가 있으면 페이드 아웃
        if (ambientPlayer.isPlaying && fadeTime > 0)
        {
            float startVolume = ambientPlayer.volume;
            float timer = 0;

            while (timer < fadeTime)
            {
                timer += Time.deltaTime;
                ambientPlayer.volume = masterVolume * ambientVolume * Mathf.Lerp(startVolume, 0, timer / fadeTime);
                yield return null;
            }
        }

        // 새 Ambient 설정 및 재생
        ambientPlayer.clip = newClip;
        ambientPlayer.volume = 0;
        ambientPlayer.Play();

        // 페이드 인
        if (fadeTime > 0)
        {
            float timer = 0;
            while (timer < fadeTime)
            {
                timer += Time.deltaTime;
                ambientPlayer.volume = masterVolume * ambientVolume * Mathf.Lerp(0, 1, timer / fadeTime);
                yield return null;
            }
        }
        else
        {
            ambientPlayer.volume = masterVolume * ambientVolume;
        }
    }

    // SFX 재생 (3D 포지션)
    public void PlaySFX(string soundName, Vector3 position = default)
    {
        AudioClip clip = soundLibrary.GetClip(SoundType.SFX, soundName);
        if (clip == null)
        {
            Debug.LogWarning($"SFX 클립을 찾을 수 없음: {soundName}");
            return;
        }

        // 사용 가능한 오디오 소스 찾기
        AudioSource source = GetAvailableSFXSource();
        if (source == null) return; // 모든 소스가 사용 중

        // 소스 설정
        source.transform.position = position;
        source.clip = clip;
        source.volume = masterVolume * sfxVolume;
        source.Play();
    }

    // UI 사운드 재생
    public void PlayUISound(string soundName)
    {
        AudioClip clip = soundLibrary.GetClip(SoundType.UI, soundName);
        if (clip == null)
        {
            Debug.LogWarning($"UI 클립을 찾을 수 없음: {soundName}");
            return;
        }

        // UI 사운드는 다른 UI 사운드를 중단하고 재생
        uiPlayer.clip = clip;
        uiPlayer.volume = masterVolume * uiVolume;
        uiPlayer.Play();
    }

    // 사용 가능한 SFX 오디오 소스 반환
    private AudioSource GetAvailableSFXSource()
    {
        foreach (var source in sfxPool)
        {
            if (!source.isPlaying)
                return source;
        }

        // 모든 소스가 사용 중인 경우 가장 오래된 소스 반환
        return sfxPool[0];
    }

    // 모든 사운드 정지
    public void StopAllSounds()
    {
        StopBGM(0);
        StopAmbient(0);
        
        foreach (var source in sfxPool)
        {
            source.Stop();
        }
        
        uiPlayer.Stop();
    }

    // Ambient 정지
    public void StopAmbient(float fadeTime = 1.0f)
    {
        if (!ambientPlayer.isPlaying) return;
        
        if (fadeTime > 0)
            StartCoroutine(FadeOutAmbient(fadeTime));
        else
        {
            ambientPlayer.Stop();
            currentAmbient = string.Empty;
        }
    }

    // Ambient 페이드 아웃
    private IEnumerator FadeOutAmbient(float fadeTime)
    {
        float startVolume = ambientPlayer.volume;
        float timer = 0;

        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            ambientPlayer.volume = Mathf.Lerp(startVolume, 0, timer / fadeTime);
            yield return null;
        }

        ambientPlayer.Stop();
        currentAmbient = string.Empty;
    }
}

// 사운드 타입 열거형
public enum SoundType
{
    BGM,
    SFX,
    UI,
    Ambient
} 