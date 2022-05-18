using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_PlayClickAudio : MonoBehaviour
{
    [SerializeField] private AudioClip audioClip;

    private void Start()
    {
        Button uibtn = GetComponent<Button>();
        if (uibtn) uibtn.onClick.AddListener(() => { if (audioClip == null) AudioManager.Instance.UIClick(); else AudioManager.Instance.UIClick(audioClip); });
    }
}
