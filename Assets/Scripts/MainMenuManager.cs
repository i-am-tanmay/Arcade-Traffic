using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    #region Singleton
    private static MainMenuManager _instance;
    public static MainMenuManager Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance != null && _instance != this) Destroy(this.gameObject);
        else { _instance = this; DontDestroyOnLoad(gameObject); }
    }
    #endregion

    [SerializeField] private string levelNamePrefix;

    [SerializeField] private GameObject UI_levelButtons;
    [SerializeField] private GameObject UI_loadingScreen;
    [SerializeField] private GameObject UI_volumeButtons;

    [SerializeField] private GameObject mainCam;

    private string currentLevelName;
    private int curLevelNum;

    private bool isLoadingLevel = false;

    private void Start()
    {
        currentLevelName = SceneManager.GetActiveScene().name;
        UI_levelButtons.SetActive(true);
        UI_volumeButtons.SetActive(true);
        UI_loadingScreen.SetActive(false);
        mainCam.SetActive(true);
    }

    public void LoadLevel(int levelnum)
    {
        if (isLoadingLevel) return;
        isLoadingLevel = true;
        StartCoroutine(LoadLevelAsync(levelNamePrefix + levelnum));
        curLevelNum = levelnum;
    }

    private IEnumerator LoadLevelAsync(string levelname)
    {
        AsyncOperation loadlevel = SceneManager.LoadSceneAsync(levelname, LoadSceneMode.Additive);
        loadlevel.allowSceneActivation = false;

        UI_levelButtons.SetActive(false);
        UI_volumeButtons.SetActive(false);
        UI_loadingScreen.SetActive(true);

        float t = 0;
        while (t < 2f || loadlevel.progress < .88f)
        {
            t += Time.deltaTime;
            yield return null;
        }

        UI_levelButtons.SetActive(false);
        UI_volumeButtons.SetActive(false);
        UI_loadingScreen.SetActive(false);
        mainCam.SetActive(false);
        loadlevel.allowSceneActivation = true;
        currentLevelName = levelname;
    }

    public int SetSceneActive()
    {
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(currentLevelName));
        return curLevelNum;
    }

    public void UnloadGoToMainMenu()
    {
        UI_levelButtons.SetActive(false);
        UI_volumeButtons.SetActive(false);
        UI_loadingScreen.SetActive(true);
        mainCam.SetActive(true);

        StartCoroutine(UnloadGoToMainMenuAsync());
        
    }

    private IEnumerator UnloadGoToMainMenuAsync()
    {
        AsyncOperation deloadcurrscene = SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene(), UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
        currentLevelName = SceneManager.GetSceneAt(0).name;
        SceneManager.SetActiveScene(SceneManager.GetSceneAt(0));

        while(!deloadcurrscene.isDone)
        {
            yield return null;
        }

        UI_loadingScreen.SetActive(false);
        UI_levelButtons.SetActive(true);
        UI_volumeButtons.SetActive(true);
        MainMenu_LevelLockManager.Instance.UpdateLevelTiles();
        isLoadingLevel = false;
    }

    public void PauseVolumeShow(bool pause)
    {
        UI_volumeButtons.SetActive(pause);
    }
}