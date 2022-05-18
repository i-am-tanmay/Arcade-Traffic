using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonAnimateOnClick : MonoBehaviour
{
    [SerializeField] private float timeDelay = 1f;
    [SerializeField] private string triggerName = "btnPress";

    [Space(10f)]

    [SerializeField] private bool isLevel = false;
    [SerializeField] private int levelNum;

    void Start()
    {
        Button btn = GetComponent<Button>();
        Animator anim = GetComponent<Animator>();

        if (btn == null || anim == null) return;

        Button.ButtonClickedEvent clickevent = btn.onClick;

        btn.onClick = new Button.ButtonClickedEvent();
        btn.onClick.AddListener(() =>
        {
            anim.SetTrigger(triggerName);
            if (!isLevel || (isLevel && !MainMenu_LevelLockManager.Instance.IsLevelLocked(levelNum)))
                StartCoroutine(beforeonclick(clickevent, timeDelay));
        });

    }

    private IEnumerator beforeonclick(Button.ButtonClickedEvent ev, float t)
    {
        yield return new WaitForSecondsRealtime(t);
        ev.Invoke();
    }
}