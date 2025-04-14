using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SoundLibrary", menuName = "Audio/Sound Library")]
public class SoundLibrary : ScriptableObject
{
    [System.Serializable]
    public class SoundGroup
    {
        public string groupName;
        public List<SoundData> sounds = new List<SoundData>();
    }

    [System.Serializable]
    public class SoundData
    {
        public string soundName;
        public AudioClip clip;
        [Range(0f, 2f)]
        public float volume = 1f;
        [Range(0.1f, 3f)]
        public float pitch = 1f;
    }

    [Header("BGM")]
    public SoundGroup bgmSounds;

    [Header("SFX")]
    public List<SoundGroup> sfxCategories = new List<SoundGroup>();

    [Header("UI")]
    public SoundGroup uiSounds;

    [Header("Ambient")]
    public SoundGroup ambientSounds;

    // 소리 클립 가져오기
    public AudioClip GetClip(SoundType type, string soundName)
    {
        SoundData sound = GetSoundData(type, soundName);
        return sound?.clip;
    }

    // 소리 데이터 가져오기
    public SoundData GetSoundData(SoundType type, string soundName)
    {
        switch (type)
        {
            case SoundType.BGM:
                return FindSound(bgmSounds, soundName);
            
            case SoundType.SFX:
                foreach (var category in sfxCategories)
                {
                    SoundData sound = FindSound(category, soundName);
                    if (sound != null) return sound;
                }
                return null;
            
            case SoundType.UI:
                return FindSound(uiSounds, soundName);
            
            case SoundType.Ambient:
                return FindSound(ambientSounds, soundName);
            
            default:
                return null;
        }
    }

    // 그룹에서 사운드 찾기
    private SoundData FindSound(SoundGroup group, string soundName)
    {
        if (group == null || string.IsNullOrEmpty(soundName)) return null;
        
        foreach (var sound in group.sounds)
        {
            if (sound.soundName == soundName) return sound;
        }
        
        return null;
    }
} 