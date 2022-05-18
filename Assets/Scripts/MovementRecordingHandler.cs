using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementRecordingHandler : MonoBehaviour
{
    private List<Vector3> pos;
    private List<Quaternion> rot;
    private List<bool> drift;
    private List<int> collision;
    private List<bool> collision_iswreck;
    private List<int> horn;

    [SerializeField] private Transform target_Record;
    [SerializeField] private Rigidbody target_Playback;
    [SerializeField] private Transform[] keeprotationzero;
    [SerializeField] private GameObject movementSphere;
    [SerializeField] private GameObject directionalArrow;
    [SerializeField] private ParticleSystem ps_WreckSmoke;
    private bool isRecording = false;
    private bool isPlayback = false;

    private int playback_iterator;

    private Vector3 pos_curr, pos_next;
    private Quaternion rot_curr, rot_next;

    private int nextitr_horn, nextitr_collision;

    private bool isConvertedToPlayback = false;

    private bool isBackwardsTime = false;

    [Space(10f)]
    [SerializeField] private Player_AudioManager pAudio;

    private void FixedUpdate()
    {
        // UPDATE fixed PLAYBACK position/rotation
        if (target_Playback != null && isPlayback)
        {
            if (pos.Count > playback_iterator)
            {
                if (pos.Count > playback_iterator + 1)
                {
                    pos_curr = pos[playback_iterator];
                    pos_next = pos[playback_iterator + 1];
                    rot_curr = rot[playback_iterator];
                    rot_next = rot[playback_iterator + 1];
                }

                target_Playback.position = pos_curr;
                target_Playback.rotation = rot_curr;

                playback_iterator++;
                if (pos.Count <= playback_iterator) { isPlayback = false; gameObject.SetActive(false); }
                else
                {
                    if (horn.Count > 0 && horn.Count > nextitr_horn && playback_iterator >= horn[nextitr_horn])
                    {
                        if (playback_iterator == horn[nextitr_horn]) { pAudio.PlayHorn(); }
                        nextitr_horn++;
                    }

                    if (collision.Count > 0 && collision.Count > nextitr_collision && playback_iterator >= collision[nextitr_collision])
                    {
                        if (playback_iterator == collision[nextitr_collision])
                        {
                            bool iswreck = collision_iswreck[nextitr_collision];
                            pAudio.PlayCollision(iswreck);
                            if (iswreck) ToggleWreckSmoke(true);
                        }
                        nextitr_collision++;
                    }
                }
            }
            else
            {
                isPlayback = false;
                gameObject.SetActive(false);
            }

        }
        // RECORD position rotation
        else if (target_Record != null && isRecording)
        {
            pos.Add(target_Record.position);
            rot.Add(target_Record.rotation.normalized);

            drift.Add(pAudio.isDrifting);

            playback_iterator++;
        }
        else if (isConvertedToPlayback && !isBackwardsTime)
        {
            target_Playback.position = pos_curr;
            target_Playback.rotation = rot_curr;
        }
    }

    private void Update()
    {
        // PLAYBACK player pos every frame
        if (target_Playback != null && isPlayback)
        {
            /** OLD FOR SYNCING UPDATE TO FIXEDUPDATE
            if (prev_iterator == playback_iterator)
            {
                del_Time += Time.deltaTime;
            }
            else
            {
                prev_iterator = playback_iterator;
                del_Time = (del_Time + Time.deltaTime - Time.fixedDeltaTime);
                if (del_Time <= 0) del_Time = 0;
                else del_Time = del_Time % Time.fixedDeltaTime;
            }

            float t = Mathf.Clamp01(del_Time / Time.fixedDeltaTime);
            if (t == 0)
            {
                target_Record.position = pos_curr;
                target_Record.rotation = rot_curr;
            }
            else
            {
                target_Record.position = Vector3.Lerp(pos_curr, pos_next, t);
                if (rot_curr.Equals(rot_next)) target_Record.rotation = rot_curr;
                else target_Record.rotation = Quaternion.Lerp(rot_curr, rot_next, t);
            }
            */

            for (int ii = 0; ii < keeprotationzero.Length; ii++) keeprotationzero[ii].localRotation = Quaternion.identity;
            target_Playback.velocity = pos_next - target_Playback.position;
            target_Playback.rotation = rot_curr;
            pAudio.SetDrifting(drift[playback_iterator]);

        }

    }

    // SET player to RECORD
    public void StartRecording()
    {
        pos = new List<Vector3>();
        rot = new List<Quaternion>();
        drift = new List<bool>();
        collision = new List<int>();
        collision_iswreck = new List<bool>();
        horn = new List<int>();
        playback_iterator = 0;
        StopRigidbodyVelocity();
        isPlayback = false;
        isRecording = true;
    }

    // SET player to PLAYBACK
    public void StartPlayback(int i)
    {
        if (pos.Count <= i * GameManager.Instance.timeDelayPerCar) { isPlayback = false; gameObject.SetActive(false); return; }

        StopRigidbodyVelocity();

        playback_iterator = i * GameManager.Instance.timeDelayPerCar;
        target_Playback.position = pos_curr = pos[playback_iterator];
        pos_next = pos[playback_iterator + 1];
        target_Playback.rotation = rot_curr = rot[playback_iterator];
        rot_next = rot[playback_iterator + 1];
        isPlayback = true;
        pAudio.ConvertToPlayback();
        ToggleWreckSmoke(false);

        nextitr_horn = nextitr_collision = 0;
    }

    // RESET Player Position
    //      REVERSE false when TRIGGER END
    public void ResetPlayback(int i, bool reverse = false, bool isCurrentPlayer = false)
    {
        if (isCurrentPlayer)
        {
            gameObject.SetActive(true);
            StartCoroutine(BackwardsTime(0, true));
        }
        else if (pos.Count > i * GameManager.Instance.timeDelayPerCar)
        {
            gameObject.SetActive(true);
            if (reverse) StartCoroutine(BackwardsTime(i, false));
            else ResetPlaybackFunc(i, false);
        }
        else gameObject.SetActive(false);

        pAudio.SetDrifting(false);
        pAudio.StartStopEngine(false);
        ToggleWreckSmoke(false);
        isRecording = false;
        isPlayback = false;
    }

    // REVERSE player to appropriate position
    private IEnumerator BackwardsTime(int i, bool isCurrentPlayer)
    {
        isBackwardsTime = true;
        gameObject.SetActive(true);
        float target = CameraFollow.Instance.roundRestartCamLerpTime;
        float t = 0;
        int iterator;
        if (isCurrentPlayer)
        {
            movementSphere.SetActive(false); GetComponent<Player_Movement>().enabled = false; directionalArrow.SetActive(false);
            for (int ii = 0; ii < keeprotationzero.Length; ii++) keeprotationzero[ii].localRotation = Quaternion.identity;
            StopRigidbodyVelocity();
        }

        int min = isCurrentPlayer ? 0 : i * GameManager.Instance.timeDelayPerCar;
        int max = isCurrentPlayer ? pos.Count - 1 : playback_iterator;

        while (t < target)
        {
            iterator = Mathf.FloorToInt(Mathf.SmoothStep(max, min, t / target));
            if (iterator >= pos.Count) iterator = pos.Count - 1;
            target_Playback.position = pos[iterator];
            target_Playback.rotation = rot[iterator];
            t += Time.deltaTime;
            yield return null;
        }

        if (isCurrentPlayer)
        {
            movementSphere.SetActive(true); GetComponent<Player_Movement>().enabled = true; directionalArrow.SetActive(true);
            target_Playback.transform.GetComponent<ConfigurableJoint>().connectedBody = movementSphere.GetComponent<Rigidbody>();
        }
        isBackwardsTime = false;
        ResetPlaybackFunc(i, isCurrentPlayer);
    }

    // INIT player positions to ZERO
    //      CONVERT player to PLAYBACK
    private void ResetPlaybackFunc(int i, bool isCurrentPlayer)
    {
        gameObject.SetActive(true);
        playback_iterator = i * GameManager.Instance.timeDelayPerCar;
        if (!isCurrentPlayer && !isConvertedToPlayback)
        {
            if (movementSphere) Destroy(movementSphere);
            if (GetComponent<Player_Movement>()) Destroy(GetComponent<Player_Movement>());
            if (target_Playback.GetComponent<ConfigurableJoint>()) Destroy(target_Playback.GetComponent<ConfigurableJoint>());
            if (directionalArrow) Destroy(directionalArrow);
            pAudio.ConvertToPlayback();

            target_Playback.solverIterations = Physics.defaultSolverIterations;
            target_Playback.solverVelocityIterations = Physics.defaultSolverVelocityIterations;

            isConvertedToPlayback = true;
        }

        for (int ii = 0; ii < keeprotationzero.Length; ii++) keeprotationzero[ii].localRotation = Quaternion.identity;
        StopRigidbodyVelocity();
        target_Playback.position = pos_curr = pos[playback_iterator];
        pos_next = pos[playback_iterator + 1];
        target_Playback.rotation = rot_curr = rot[playback_iterator];
        rot_next = rot[playback_iterator + 1];
        if (isCurrentPlayer) GameManager.Instance.ResetPlayer_ReverseCallback();
    }

    public void StopRigidbodyVelocity()
    {
        target_Playback.velocity = Vector3.zero;
        target_Playback.angularVelocity = Vector3.zero;
    }

    public void RecordHornAudio()
    {
        if (target_Record != null && isRecording)
            horn.Add(playback_iterator);
    }

    public void RecordCollisionAudio(bool isWreck)
    {
        if (target_Record != null && isRecording)
        {
            collision.Add(playback_iterator);
            collision_iswreck.Add(isWreck);
        }
    }

    public void ToggleWreckSmoke(bool enable)
    {
        if (enable)
        {
            ps_WreckSmoke.gameObject.SetActive(true);
            ps_WreckSmoke.Play();
        }
        else
        {
            ps_WreckSmoke.Stop();
            ps_WreckSmoke.gameObject.SetActive(false);
        }
    }

}