using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

[Serializable]
public class MusicStruct
{
    public AudioClip stream;
    public string artist;
    public string song;
}

public class AudioManager : MonoBehaviour
{
    #region Singleton
    private static AudioManager _instance;
    public static AudioManager Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance != null && _instance != this) Destroy(this.gameObject);
        else { _instance = this; DontDestroyOnLoad(gameObject); }
    }
    #endregion

    [SerializeField] private AudioMixer audioMasterMix;

    [Space(10)]
    [Header("MUSIC")]
    [SerializeField] private Toggle[] tog_music;
    [SerializeField] private string musicVolume = "MusicVolume";
    [SerializeField] private AudioSource audsrc_music;
    //[SerializeField] private AudioClip[] music;
    [SerializeField] private MusicStruct[] music;
    [SerializeField] private int[] levels_music;
    private int musicVolLevel;
    private int music_itr;
    [Space(10)]
    [SerializeField] private TextMeshProUGUI artistname;
    [SerializeField] private TextMeshProUGUI songname;

    [Space(10)]
    [Header("SFX")]
    [SerializeField] private Toggle[] tog_sfx;
    [SerializeField] private string sfxVolume = "SFXVolume";
    [SerializeField] private int[] levels_sfx;
    private int sfxVolLevel;
    [Space(10)]
    [SerializeField] private AudioSource audsrc_collectible;
    [SerializeField] private AudioClip c_Collectible;
    [Space(10)]
    [SerializeField] private AudioSource audsrc_backwardstime;
    [SerializeField] private AudioClip c_BackwardsTime;

    [Space(10)]
    [Header("UI")]
    [SerializeField] private AudioSource audsrc_ui;
    [SerializeField] private AudioClip c_uiClick;


    private void ShuffleMusic()
    {
        MusicStruct tmp;

        for (int i = 0; i < music.Length - 1; i++)
        {
            int rnd = UnityEngine.Random.Range(i, music.Length);
            tmp = music[rnd];
            music[rnd] = music[i];
            music[i] = tmp;
        }
    }

    private void Start()
    {
        if (audsrc_music == null) audsrc_music = transform.GetChild(0).GetComponent<AudioSource>();
        if (audsrc_ui == null) audsrc_ui = transform.GetChild(1).GetComponent<AudioSource>();
        if (audsrc_collectible == null) audsrc_collectible = transform.GetChild(2).GetComponent<AudioSource>();
        if (audsrc_backwardstime == null) audsrc_backwardstime = transform.GetChild(3).GetComponent<AudioSource>();

        // INIT music
        ShuffleMusic();
        music_itr = 0;
        audsrc_music.mute = false;
        audsrc_music.clip = music[music_itr].stream;
        audsrc_music.Play();
        artistname.text = music[music_itr].artist;
        songname.text = music[music_itr].song;
        musicVolLevel = levels_music.Length - 1;
        audioMasterMix.SetFloat(musicVolume, levels_music[musicVolLevel]);

        // INIT sfx
        sfxVolLevel = levels_sfx.Length - 1;
        audioMasterMix.SetFloat(sfxVolume, levels_sfx[sfxVolLevel]);

        // INIT Toggles
        tog_music[0].isOn = musicVolLevel > 0;
        tog_music[1].isOn = musicVolLevel > 1;
        tog_music[2].isOn = musicVolLevel > 2;
        tog_sfx[0].isOn = sfxVolLevel > 0;
        tog_sfx[1].isOn = sfxVolLevel > 1;
        tog_sfx[2].isOn = sfxVolLevel > 2;
    }

    private void LateUpdate()
    {
        // MUSIC is STOPPED
        if (!audsrc_music.mute && !audsrc_music.isPlaying)
        {
            music_itr = (music_itr + 1) % music.Length;
            audsrc_music.clip = music[music_itr].stream;
            audsrc_music.Play();
            artistname.text = music[music_itr].artist;
            songname.text = music[music_itr].song;
        }
    }

    public void ChangeMusicSFXVolume(bool isMusic)
    {
        if (isMusic)
        {
            musicVolLevel = (musicVolLevel + 1) % levels_music.Length;
            audioMasterMix.SetFloat(musicVolume, levels_music[musicVolLevel]);

            tog_music[0].isOn = musicVolLevel > 0;
            tog_music[1].isOn = musicVolLevel > 1;
            tog_music[2].isOn = musicVolLevel > 2;
        }
        else
        {
            sfxVolLevel = (sfxVolLevel + 1) % levels_sfx.Length;
            audioMasterMix.SetFloat(sfxVolume, levels_sfx[sfxVolLevel]);

            tog_sfx[0].isOn = sfxVolLevel > 0;
            tog_sfx[1].isOn = sfxVolLevel > 1;
            tog_sfx[2].isOn = sfxVolLevel > 2;
        }
    }

    public void MuteSFX(bool pause)
    {
        audioMasterMix.SetFloat(sfxVolume, pause ? -80 : levels_sfx[sfxVolLevel]);
    }

    public void UIClick()
    {
        audsrc_ui.PlayOneShot(c_uiClick);
    }

    public void UIClick(AudioClip c)
    {
        audsrc_ui.PlayOneShot(c);
    }

    public void Collectible()
    {
        audsrc_collectible.PlayOneShot(c_Collectible);
    }

    public void BackwardsTime()
    {
        audsrc_backwardstime.PlayOneShot(c_BackwardsTime);
    }
}