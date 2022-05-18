using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

public enum Powerup
{
    None = 0,
    Boost = 1,
    Grip = 2,
    Unbreakable = 3,
}

public class GameManager : MonoBehaviour
{
    #region Singleton
    private static GameManager _instance;
    public static GameManager Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance != null && _instance != this) Destroy(this.gameObject);
        else { _instance = this; SetQualitySettings(); }
    }
    #endregion

    private int currentLevelNum;

    public GameObject[] playerPrefab;
    public Transform holder_StartEndPositions;
    private List<Holder_StartEndPositions> startEndPositions;
    private GameObject currPlayer;
    private Player_Movement currPlayer_pm;
    private List<MovementRecordingHandler> playersCompleted;
    private MovementRecordingHandler currPlayer_Recorder;

    public int currPlayerNumber { get; private set; }

    private CameraFollow cam;

    public bool isRoundStarted = false;

    private bool isInBackwardsTime = false;

    public int timeDelayPerCar;

    [Space(10)]
    public float time_roundStartTime;
    private float time_left;
    private float time_current;
    private float time_extraCollected;
    private List<GameObject> timeCollectibles;

    private Powerup currentPowerup;
    private bool isPractice = false;

    [Space(10)]

    private bool wreckStarted = false;
    public float wreckTime;
    private float wreckTimer;

    private bool wrecked_reset = false;
    public float wrecked_resetTime;
    private float wrecked_resetTimer;

    [Space(10)]

    public float MapBoundsY;

    [Space(10)]
    [Header("UI")]
    public GameObject MAINCANVAS;
    public GameObject preRoundMenu;
    public GameObject resetButton;
    public GameObject pauseHideCanvas;
    public GameObject pauseMenu;
    public GameObject pauseMenu_btn;
    [Space(10)]
    public EventTrigger pm_LeftButton;
    public EventTrigger pm_RightButton;
    [HideInInspector] public bool pm_isLeftPressed = false;
    [HideInInspector] public bool pm_isRightPressed = false;
    [Space(10)]
    public TextMeshProUGUI text_Time;
    [Space(10)]
    public Image fillimg_wreckTimeLeft;
    [Space(10)]
    public GameObject levelEndScreen_timedOut;
    public GameObject levelEndScreen_success;
    public GameObject levelEndScreen_wrecked;

    public GameObject levelEndScreen_outOfBounds;
    public GameObject levelEndScreen_outOfBounds_pressToStart;
    [Space(10)]
    [Header("powerups")]
    public Button pwr_Boost;
    public Button pwr_Grip;
    public Button pwr_Unbreakable;
    private GameObject pwr_Boost_ON, pwr_Grip_ON, pwr_Unbreakable_ON;
    public Toggle practice;
    [Space(10)]
    [Header("QUALITY")]
    public bool enable_LUT = true;
    [Space(10)]
    [Header("DEBUG")]
    public TextMeshProUGUI fpscounter;

    private void SetQualitySettings()
    {
        if (enable_LUT) Shader.EnableKeyword("LUT_CC");
        else Shader.DisableKeyword("LUT_CC");
    }

    private void Start()
    {
        currentLevelNum = MainMenuManager.Instance.SetSceneActive();

        // INIT
        cam = CameraFollow.Instance;
        playersCompleted = new List<MovementRecordingHandler>();
        startEndPositions = new List<Holder_StartEndPositions>();
        for (int i = 0; i < holder_StartEndPositions.childCount; i++) startEndPositions.Add(holder_StartEndPositions.GetChild(i).GetComponent<Holder_StartEndPositions>());

        // INIT and Instantiate First Player
        currPlayerNumber = 0;
        InstantiateNewPlayer();
        InitPreRound(true);         // SET Player num rotation ...
        ActivatePreRoundUI(true);   // ENABLE preroundmenu/resetbutton
        levelEndScreen_outOfBounds.SetActive(false);
        cam.SetPreRoundCamera(startEndPositions[currPlayerNumber].startPosition, startEndPositions[currPlayerNumber].directionArrow.transform);     // SET camera to show start/end

        // SET global timer
        time_left = time_roundStartTime;
        time_current = 0;
        time_extraCollected = 0;
        timeCollectibles = new List<GameObject>();

        // SET player attributes
        currentPowerup = Powerup.None;
        practice.isOn = isPractice = false;

        // INIT player attribute buttons
        pwr_Boost.onClick.AddListener(() => SelectPowerup(Powerup.Boost));
        pwr_Grip.onClick.AddListener(() => SelectPowerup(Powerup.Grip));
        pwr_Unbreakable.onClick.AddListener(() => SelectPowerup(Powerup.Unbreakable));
        pwr_Boost_ON = pwr_Boost.transform.GetChild(0).gameObject;
        pwr_Grip_ON = pwr_Grip.transform.GetChild(0).gameObject;
        pwr_Unbreakable_ON = pwr_Unbreakable.transform.GetChild(0).gameObject;
        pwr_Boost_ON.SetActive(false);
        pwr_Grip_ON.SetActive(false);
        pwr_Unbreakable_ON.SetActive(false);

        // INIT player movement buttons
        EventTrigger.Entry entry;
        entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerDown;
        entry.callback.AddListener((data) => { pm_isLeftPressed = true; });
        pm_LeftButton.triggers.Add(entry);
        entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerUp;
        entry.callback.AddListener((data) => { pm_isLeftPressed = false; });
        pm_LeftButton.triggers.Add(entry);
        entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerDown;
        entry.callback.AddListener((data) => { pm_isRightPressed = true; });
        pm_RightButton.triggers.Add(entry);
        entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerUp;
        entry.callback.AddListener((data) => { pm_isRightPressed = false; });
        pm_RightButton.triggers.Add(entry);

        // DISABLE UI not needed at start
        fillimg_wreckTimeLeft.gameObject.SetActive(false);
        pauseMenu.SetActive(false);
        pauseMenu_btn.SetActive(true);

        // DEBUG / DisplayFPS
        StartCoroutine(fpscounterdisplay());
    }

    private IEnumerator fpscounterdisplay()
    {
        while (true)
        {
            fpscounter.text = ((int)(1f / Time.unscaledDeltaTime)).ToString();
            yield return null; yield return null; yield return null; yield return null; yield return null;
            yield return null; yield return null; yield return null; yield return null; yield return null;
            yield return null; yield return null; yield return null; yield return null; yield return null;
        }
    }

    private void Update()
    {
        // UPDATE global timer
        if (isRoundStarted)
        {
            time_current += Time.deltaTime;
            if ((time_left + time_extraCollected - time_current) <= 0) LevelEnd(false);
        }
        text_Time.text = Mathf.CeilToInt(time_left + time_extraCollected - time_current).ToString();

        // Player Just Wrecked But Still Playable
        if (wreckStarted)
        {
            wreckTimer += Time.deltaTime;
            fillimg_wreckTimeLeft.fillAmount = (wreckTime - wreckTimer) / wreckTime;
            if (wreckTimer > wreckTime)
            {
                wreckStarted = false;
                fillimg_wreckTimeLeft.gameObject.SetActive(false);
                currPlayer_pm.wreckComplete = true;
                LevelEnd(false, true);
            }
        }

        // Player Wreck Timer Elasped, GOING to reset player
        if (wrecked_reset)
        {
            wrecked_resetTimer += Time.deltaTime;
            if (wrecked_resetTimer > wrecked_resetTime) ResetPlayer();
        }
    }

    // ADD Collectible Time
    public void OnTrigger_Collectible(int t, GameObject g)
    {
        if (!isRoundStarted) return;

        time_extraCollected += t;
        timeCollectibles.Add(g);
        g.SetActive(false);
        AudioManager.Instance.Collectible();
    }

    // PLAYER Reached ENDTRIGGER
    public void OnTrigger_End(int colliderNum)
    {
        if (colliderNum == currPlayerNumber)
        {
            if (isPractice)
            {
                ResetPlayer();
                return;
            }

            if (!isRoundStarted) return;

            wreckStarted = false;
            fillimg_wreckTimeLeft.gameObject.SetActive(false);
            wrecked_reset = false;
            playersCompleted.Add(currPlayer_Recorder);
            for (int i = 0; i < playersCompleted.Count; i++) { playersCompleted[i].StopRigidbodyVelocity(); playersCompleted[i].ResetPlayback(playersCompleted.Count - i); }

            SkidMarksManager.Instance.ResetMesh();

            currPlayerNumber++;
            if (currPlayerNumber < Mathf.Min(playerPrefab.Length, startEndPositions.Count))
            {
                InstantiateNewPlayer();
                SelectPowerup(Powerup.None);
                InitPreRound(true);
                ActivatePreRoundUI(true);
                cam.SetPreRoundCamera(startEndPositions[currPlayerNumber].startPosition, startEndPositions[currPlayerNumber].directionArrow.transform);

                time_left -= time_current - time_extraCollected;
                time_extraCollected = 0;
                time_current = 0;
                timeCollectibles.Clear();
            }
            else
            {
                //ADD CameraTransition to UP
                LevelEnd(true);
            }
        }
    }

    // RESET all players
    public void ResetPlayer()
    {
        Time.timeScale = 1;
        AudioManager.Instance.MuteSFX(false);
        pauseMenu_btn.SetActive(true);

        isRoundStarted = false;
        wreckStarted = false;
        fillimg_wreckTimeLeft.gameObject.SetActive(false);
        wrecked_reset = false;
        currPlayer_pm.wreckComplete = false;
        currPlayer_pm.ToggleWreckSmoke(false);
        time_extraCollected = 0;
        for (int i = 0; i < timeCollectibles.Count; i++) timeCollectibles[i].SetActive(true);
        timeCollectibles.Clear();

        SkidMarksManager.Instance.ResetMesh();

        AudioManager.Instance.BackwardsTime();

        for (int i = 0; i < playersCompleted.Count; i++) { playersCompleted[i].StopRigidbodyVelocity(); playersCompleted[i].ResetPlayback(playersCompleted.Count - i, true); }
        currPlayer_Recorder.ResetPlayback(0, true, true);
        cam.SetPreRoundCamera(startEndPositions[currPlayerNumber].startPosition, startEndPositions[currPlayerNumber].directionArrow.transform);
        ActivatePreRoundUI(true);

        isInBackwardsTime = true;

        if (!isPractice) time_left -= 1f;
        time_current = 0;
    }

    // only called for CURRENT PLAYER after reset
    public void ResetPlayer_ReverseCallback()
    {
        currPlayer_Recorder.StopRigidbodyVelocity();
        currPlayer_pm.StopSphereRigidbodyVelocity();
        currPlayer.transform.position = startEndPositions[currPlayerNumber].startPosition.position;
        currPlayer.transform.rotation = Quaternion.identity;
        currPlayer.transform.GetChild(0).localPosition = currPlayer.transform.GetChild(0).GetChild(0).localPosition = currPlayer.transform.GetChild(0).GetChild(0).GetChild(0).localPosition = currPlayer.transform.GetChild(1).localPosition = Vector3.zero;
        currPlayer.transform.GetChild(0).localRotation = currPlayer.transform.GetChild(0).GetChild(0).localRotation = currPlayer.transform.GetChild(0).GetChild(0).GetChild(0).localRotation = currPlayer.transform.GetChild(1).localRotation = Quaternion.identity;
        InitPreRound(true);
        levelEndScreen_outOfBounds.SetActive(false);
        levelEndScreen_outOfBounds_pressToStart.SetActive(true);

        isInBackwardsTime = false;
    }

    // START Player Engine
    public void Button_PressToStart()
    {
        if (isRoundStarted) return;
        if (isInBackwardsTime) return;

        InitPreRound(false);
        ActivatePreRoundUI(false);
        cam.StartRoundCamera(currPlayer.transform.GetChild(0));
        for (int i = 0; i < playersCompleted.Count; i++) playersCompleted[i].StartPlayback(playersCompleted.Count - i);
        currPlayer_Recorder.StartRecording();
        currPlayer_pm.SetPowerup(currentPowerup);
        currPlayer_pm.StartStopRound(true);
        isRoundStarted = true;
    }

    private void InstantiateNewPlayer()
    {
        currPlayer = Instantiate(playerPrefab[currPlayerNumber], startEndPositions[currPlayerNumber].startPosition.position, Quaternion.identity, null);
        currPlayer.name = "Player" + currPlayerNumber;
        currPlayer_Recorder = currPlayer.GetComponent<MovementRecordingHandler>();
        currPlayer_pm = currPlayer.GetComponent<Player_Movement>();
    }

    // INIT round
    //      SET CurrPlayer num rotation wreck/smoke/dirarrow
    //      SET StartEndPosition
    //      COROUTINE cameraLERP till engine starts
    private void InitPreRound(bool active)
    {
        if (active)
        {
            currPlayer_pm.Init(currPlayerNumber, startEndPositions[currPlayerNumber].startPosition.localEulerAngles.y, startEndPositions[currPlayerNumber].directionArrow.transform);


            for (int i = 0; i < startEndPositions.Count; i++) startEndPositions[i].gameObject.SetActive(false);
            startEndPositions[currPlayerNumber].gameObject.SetActive(true);

            currPlayer_pm.StartStopRound(false);
        }
        isRoundStarted = !active;
    }

    // ENABLE preroundmenu OR resetbutton
    private void ActivatePreRoundUI(bool active)
    {
        levelEndScreen_timedOut.SetActive(false);
        levelEndScreen_success.SetActive(false);
        levelEndScreen_wrecked.SetActive(false);

        preRoundMenu.SetActive(active);
        resetButton.SetActive(!active);
        // add for powerups
    }

    // LEVELEND: completed / wrecked / timed out
    public void LevelEnd(bool success, bool wrecked = false)
    {
        isRoundStarted = false;
        AudioManager.Instance.MuteSFX(true);
        pauseMenu_btn.SetActive(false);

        preRoundMenu.SetActive(false);
        resetButton.SetActive((!success && (time_left > 3f)) || isPractice);

        wrecked_reset = (!success && wrecked);
        wrecked_resetTimer = 0;
        levelEndScreen_wrecked.SetActive(wrecked_reset);

        bool timedout = !success && !wrecked;
        levelEndScreen_timedOut.SetActive(timedout);
        if (timedout) Time.timeScale = 0;

        if (success)
        {
            bool highscore = MainMenu_LevelLockManager.Instance.LevelEndScoreUpdate(currentLevelNum, time_left + time_extraCollected - time_current);
            CameraFollow.Instance.EndLevelCameraAnim();
            levelEndScreen_success.SetActive(true);
        }
    }

    public void SelectPowerup(Powerup pwr)
    {
        pwr_Boost_ON.SetActive(false);
        pwr_Grip_ON.SetActive(false);
        pwr_Unbreakable_ON.SetActive(false);

        if (pwr == currentPowerup)
        {
            currentPowerup = Powerup.None;
        }
        else
        {
            currentPowerup = pwr;
            if (pwr == Powerup.Boost) pwr_Boost_ON.SetActive(true);
            else if (pwr == Powerup.Grip) pwr_Grip_ON.SetActive(true);
            else if (pwr == Powerup.Unbreakable) pwr_Unbreakable_ON.SetActive(true);
        }
    }

    public void SwitchPractice(bool isOn)
    {
        isPractice = isOn;
    }

    // PLAYER Just Wrecked But Still Playable
    public void StartWreckTimer()
    {
        if (wreckStarted || wrecked_reset) return;

        wreckStarted = true;
        fillimg_wreckTimeLeft.fillAmount = 1f;
        fillimg_wreckTimeLeft.gameObject.SetActive(true);
        wreckTimer = 0;
    }

    public void isOutOfBounds()
    {
        if (!isRoundStarted) return;

        levelEndScreen_outOfBounds.SetActive(true);
        levelEndScreen_outOfBounds_pressToStart.SetActive(false);
        ResetPlayer();
    }

    public void PAUSEMENU()
    {
        Time.timeScale = 0;
        pauseMenu.SetActive(true);
        pauseMenu_btn.SetActive(false);
        pauseHideCanvas.SetActive(false);
        MainMenuManager.Instance.PauseVolumeShow(true);

        AudioManager.Instance.MuteSFX(true);
    }

    public void PAUSEMENU_Resume()
    {
        Time.timeScale = 1;
        pauseMenu.SetActive(false);
        pauseMenu_btn.SetActive(true);
        pauseHideCanvas.SetActive(true);
        MainMenuManager.Instance.PauseVolumeShow(false);

        AudioManager.Instance.MuteSFX(false);
    }

    public void PAUSEMENU_MainMenu()
    {
        Time.timeScale = 1;
        pauseMenu.SetActive(false);
        pauseMenu_btn.SetActive(false);
        MainMenuManager.Instance.PauseVolumeShow(false);

        AudioManager.Instance.MuteSFX(false);

        MAINCANVAS.SetActive(false);
        MainMenuManager.Instance.UnloadGoToMainMenu();
    }
}