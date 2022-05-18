using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu_LevelLockManager : MonoBehaviour
{
    #region Singleton
    private static MainMenu_LevelLockManager _instance;
    public static MainMenu_LevelLockManager Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance != null && _instance != this) Destroy(this.gameObject);
        else { _instance = this; DontDestroyOnLoad(gameObject); }
    }
    #endregion

    [SerializeField] private Animator[] anim_levels;
    [SerializeField] private Image[] levels_locked;
    private bool[] isLevelLocked;

    private string pre_levelscore = "score_level";
    [SerializeField] private string animInteractable = "interactable";

    private void Start()
    {
        isLevelLocked = new bool[levels_locked.Length];

        UpdateLevelTiles();
    }

    public bool LevelEndScoreUpdate(int level, float timeleft)
    {
        float cur = PlayerPrefs.GetFloat(pre_levelscore + level, -1);

        if (cur < timeleft)
        {
            PlayerPrefs.SetFloat(pre_levelscore + level, timeleft);
            return true;
        }
        else
        {
            return false;
        }
    }

    public void UpdateLevelTiles()
    {
        for (int i = 2; i < levels_locked.Length + 2; i++)
        {
            float score = PlayerPrefs.GetFloat(pre_levelscore + (i - 1), -1);
            bool isLocked = isLevelLocked[i - 2] = (score == -1);
            levels_locked[i - 2].gameObject.SetActive(isLocked);
            anim_levels[i - 2].SetBool(animInteractable, !isLocked);
        }
    }

    public bool IsLevelLocked(int levelnum)
    {
        if (levelnum < 2 || (levelnum - 2) >= isLevelLocked.Length) return false;
        else return isLevelLocked[levelnum - 2];
    }
}