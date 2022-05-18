using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class Player_AudioManager : MonoBehaviour
{
    [SerializeField] private AudioMixer mainmixer;
    [SerializeField] private MovementRecordingHandler movrechandler;

    [Space(10)]

    [SerializeField] private AudioSource src_Engine;
    [SerializeField] private AudioSource src_Drift;
    [SerializeField] private AudioSource src_Horn;
    [SerializeField] private AudioSource src_Collision;

    [SerializeField] private AudioSource src_OTHER_Drift;
    [SerializeField] private AudioSource src_OTHER_Horn;
    [SerializeField] private AudioSource src_OTHER_Collision;
    private bool isPlayback = false;

    [Space(10)]

    [SerializeField] private AudioClip c_engineStart;
    [SerializeField] private AudioClip c_engine;
    private bool isEngineStarted = true;
    private bool isEngineStartComplete = false;
    private float playerSpeed;
    [SerializeField] [Range(-3, 3)] private float enginePitch_Min;
    [SerializeField] [Range(-3, 3)] private float enginePitch_Max;

    [Space(10)]

    [SerializeField] private AudioClip c_drift;
    public bool isDrifting { get; private set; }

    [Space(10)]

    [SerializeField] private AudioClip c_horn;
    private bool canPlayHorn = true;
    private float volscale_horn;

    [Space(10)]

    [SerializeField] private AudioClip c_collision;
    [SerializeField] private AudioClip c_collisionWreck;
    private bool canPlayCollision = true, canPlayWreck = true;
    private float volscale_collision;

    private void Start()
    {
        if (src_Engine == null) src_Engine = transform.GetChild(1).GetComponent<AudioSource>();
        if (src_Drift == null) src_Drift = transform.GetChild(2).GetComponent<AudioSource>();
        if (src_Horn == null) src_Horn = transform.GetChild(3).GetComponent<AudioSource>();
        if (src_Collision == null) src_Collision = transform.GetChild(4).GetComponent<AudioSource>();

        if (src_OTHER_Drift == null) src_OTHER_Drift = transform.GetChild(0).GetChild(0).GetComponent<AudioSource>();
        if (src_OTHER_Horn == null) src_OTHER_Horn = transform.GetChild(0).GetChild(1).GetComponent<AudioSource>();
        if (src_OTHER_Collision == null) src_OTHER_Collision = transform.GetChild(0).GetChild(2).GetComponent<AudioSource>();

        if (movrechandler == null) movrechandler = transform.root.GetComponent<MovementRecordingHandler>();

        src_Drift.clip = c_drift;
        src_Drift.loop = true;
        src_Drift.Play();
        src_Drift.Pause();
        src_Drift.volume = 0;

        src_Horn.loop = false;
        volscale_horn = src_Horn.volume;

        src_Collision.loop = false;
        volscale_collision = src_Collision.volume;
    }

    private void Update()
    {
        if (!isPlayback)
        {
            if (isEngineStarted)
            {
                if (!isEngineStartComplete && !src_Engine.isPlaying)
                {
                    isEngineStartComplete = true;
                    src_Engine.clip = c_engine;
                    src_Engine.loop = true;
                    src_Engine.pitch = enginePitch_Min;
                    src_Engine.Play();
                }

                if (isEngineStartComplete)
                {
                    src_Engine.pitch = Mathf.Lerp(enginePitch_Min, enginePitch_Max, playerSpeed);
                }
            }

            if (isDrifting && !src_Drift.isPlaying) { src_Drift.UnPause(); src_Drift.volume = 0; }
            else if (!isDrifting && src_Drift.isPlaying)
            {
                src_Drift.volume -= 6 * Time.deltaTime;
                if (src_Drift.volume <= 0)
                    src_Drift.Pause();
            }

            if (isDrifting) src_Drift.volume += 2 * Time.deltaTime;
        }
        else
        {
            if (isDrifting && !src_OTHER_Drift.isPlaying) { src_OTHER_Drift.UnPause(); src_OTHER_Drift.volume = 0; }
            else if (!isDrifting && src_OTHER_Drift.isPlaying)
            {
                src_OTHER_Drift.volume -= 6 * Time.deltaTime;
                if (src_OTHER_Drift.volume <= 0)
                    src_OTHER_Drift.Pause();
            }

            if (isDrifting) src_OTHER_Drift.volume += 2 * Time.deltaTime;
        }
    }

    public void StartStopEngine(bool isStart)
    {
        if (isPlayback) return;

        if (isStart)
        {
            src_Engine.loop = false;
            src_Engine.pitch = 1;
            src_Engine.PlayOneShot(c_engineStart, .5f);
            isEngineStarted = true;
            isEngineStartComplete = false;
        }
        else
        {
            src_Engine.Stop();
            isEngineStarted = isEngineStartComplete = false;
        }
    }

    public void SetPlayerSpeed(float speed)
    {
        playerSpeed = Mathf.Abs(speed);
    }

    public void SetDrifting(bool isDrifting)
    {
        this.isDrifting = isDrifting;
    }

    public void PlayHorn()
    {
        if (!canPlayHorn) return;

        canPlayHorn = false;
        StartCoroutine(WaitForHornSound());

        if (isPlayback) src_OTHER_Horn.PlayOneShot(c_horn, volscale_horn);
        else src_Horn.PlayOneShot(c_horn);

        movrechandler.RecordHornAudio();
    }

    private IEnumerator WaitForHornSound()
    {
        if (canPlayHorn) yield break;

        yield return new WaitForSeconds(.3f);

        canPlayHorn = true;
    }

    public void PlayCollision(bool isWreck)
    {
        if (isWreck)
        {
            if (!canPlayWreck) return;

            canPlayWreck = false;
            StartCoroutine(WaitForCollisionSound(true));

            if (isPlayback) src_OTHER_Collision.PlayOneShot(c_collisionWreck, volscale_collision);
            else src_Collision.PlayOneShot(c_collisionWreck);
        }
        else
        {
            if (!canPlayCollision) return;

            canPlayCollision = false;
            StartCoroutine(WaitForCollisionSound(false));

            if (isPlayback) src_OTHER_Collision.PlayOneShot(c_collision, volscale_collision);
            else src_Collision.PlayOneShot(c_collision);
        }

        movrechandler.RecordCollisionAudio(isWreck);
    }

    private IEnumerator WaitForCollisionSound(bool isWreck)
    {
        if ((isWreck && canPlayWreck) || (!isWreck && canPlayCollision)) yield break;

        yield return new WaitForSeconds(.3f);

        if (isWreck) canPlayWreck = true;
        else canPlayCollision = true;
    }

    public void ConvertToPlayback()
    {
        if (src_Engine != null) Destroy(src_Engine.gameObject);
        if (src_Drift != null) Destroy(src_Drift.gameObject);
        if (src_Horn != null) Destroy(src_Horn.gameObject);
        if (src_Collision != null) Destroy(src_Collision.gameObject);
        isPlayback = true;
        isDrifting = false;

        src_OTHER_Drift.clip = c_drift;
        src_OTHER_Drift.loop = true;
        src_OTHER_Drift.Play();
        src_OTHER_Drift.Pause();
        src_OTHER_Drift.volume = 0;

        canPlayHorn = canPlayWreck = canPlayCollision = true;
    }
}