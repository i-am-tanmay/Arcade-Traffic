using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    #region Singleton
    private static CameraFollow _instance;
    public static CameraFollow Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance != null && _instance != this) Destroy(this.gameObject);
        else { _instance = this; }
    }
    #endregion

    private Camera cam;

    [HideInInspector] public Transform Target;

    Bounds cambound;

    [SerializeField] private Vector3 Offset;

    private bool multipleTargets;
    private Vector3 multipleTargetsPos;

    private bool isRoundStartCameraLerping = false;
    public float roundStartCamLerpTime = 1f;
    private float roundStartCamLerpTimer = 0;

    private bool isRoundRestartCameraLerping = false;
    public float roundRestartCamLerpTime = 1f;
    private float roundRestartCamLerpTimer = 0;

    private bool firsttime = true;
    private Vector3 viewworlddist;
    [Range(0f, .5f)] public float x = .1f;
    [Range(0f, .5f)] public float y = .1f;

    /*
    public Vector2 TopBot;
    public Vector2 LeftRight;

    float topdist;
    float botdist;
    float rightdist;
    float leftdist;
    float padding = 1.5f;

    RaycastHit hittest;
    public LayerMask testmask;
    */

    private bool isEndLevelCameraAnim = false;

    private void Start()
    {
        cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (isEndLevelCameraAnim) return;

        if (!multipleTargets)
        {
            if (Target != null)
            {
                //Vector3 targetpos = Target.position + (globalFollow ? (Vector3.forward * Offset.z + Vector3.right * Offset.x) : (Target.forward * Offset.z + Target.right * Offset.x)) + Vector3.up * Offset.y;
                Vector3 targetpos = Target.position + Offset;

                // LERP Camera TO player
                if (isRoundStartCameraLerping)
                {
                    roundStartCamLerpTimer += Time.deltaTime;
                    transform.position = Vector3.Slerp(multipleTargetsPos, targetpos, roundStartCamLerpTimer / roundStartCamLerpTime);
                    //if (transform.position.z < Target.position.z) transform.LookAt(Target);
                    transform.forward = Vector3.Slerp(Vector3.down, -Offset, roundStartCamLerpTimer / roundStartCamLerpTime);
                    if (roundStartCamLerpTimer > roundStartCamLerpTime) isRoundStartCameraLerping = false;

                    firsttime = true;
                }
                else
                {
                    // UPDATE Camera POS when round started
                    if (firsttime)
                    {
                        transform.position = targetpos;
                        transform.forward = -Offset;

                        viewworlddist = (cam.ViewportToWorldPoint(new Vector3(1f, 1f, Offset.y)) - cam.ViewportToWorldPoint(new Vector3(0f, 0f, Offset.y)));

                        /*
                        topdist = botdist = viewworlddist.z / 2f + padding * 2.5f;
                        rightdist = viewworlddist.x / 2f + padding;
                        leftdist = viewworlddist.x / 2f + padding;
                        Vector3 center = Target.position;
                        float xdif = 0f;
                        bool hittop = false, hitbot = false;
                        if (Physics.Raycast(transform.position, transform.forward, out hittest, 100f, testmask, QueryTriggerInteraction.Ignore))
                            center = hittest.point;
                        else Debug.Log("(center) CAMRAY DIDN't HIT GROUND");
                        //BOT
                        if (Physics.Raycast(transform.position, Quaternion.AngleAxis(cam.fieldOfView / 2f, transform.right) * transform.forward, out hittest, 100f, testmask, QueryTriggerInteraction.Ignore))
                        { botdist = center.z - hittest.point.z + padding * 2.5f; xdif = -hittest.point.x; hitbot = true; }
                        else Debug.Log("(bot) CAMRAY DIDN't HIT GROUND");
                        //TOP
                        if (Physics.Raycast(transform.position, Quaternion.AngleAxis(-cam.fieldOfView / 2f, transform.right) * transform.forward, out hittest, 100f, testmask, QueryTriggerInteraction.Ignore))
                        { topdist = hittest.point.z - center.z + padding * 2.5f; xdif += hittest.point.x; hittop = true; }
                        else Debug.Log("(top) CAMRAY DIDN't HIT GROUND");
                        //RIGHT
                        if (Physics.Raycast(transform.position, Quaternion.AngleAxis(Camera.VerticalToHorizontalFieldOfView(cam.fieldOfView, cam.aspect) / 2f, transform.up) * transform.forward, out hittest, 100f, testmask, QueryTriggerInteraction.Ignore))
                        { leftdist = hittest.point.x - center.x + ((hittop && hitbot) ? xdif : padding) + padding; rightdist = leftdist + ((hittop && hitbot) ? xdif : padding) + padding; }
                        else Debug.Log("(right) CAMRAY DIDN't HIT GROUND");
                        */

                        firsttime = false;
                    }
                    else
                    {
                        Vector3 delta = Vector3.zero;

                        if (targetpos.x > transform.position.x + viewworlddist.x * x) delta.x = targetpos.x - (transform.position.x + viewworlddist.x * x);
                        else if (targetpos.x < transform.position.x - viewworlddist.x * x) delta.x = targetpos.x - (transform.position.x - viewworlddist.x * x);

                        if (targetpos.z > transform.position.z + viewworlddist.z * y) delta.z = targetpos.z - (transform.position.z + viewworlddist.z * y);
                        else if (targetpos.z < transform.position.z - viewworlddist.z * y) delta.z = targetpos.z - (transform.position.z - viewworlddist.z * y);

                        transform.position += delta;
                        /*transform.position = new Vector3(Mathf.Clamp(transform.position.x, LeftRight.x + leftdist + Offset.x, LeftRight.y - rightdist + Offset.x),
                            transform.position.y,
                            Mathf.Clamp(transform.position.z, TopBot.y + botdist + Offset.z, TopBot.x - topdist + Offset.z));*/
                    }
                }
            }
            else firsttime = true;
        }
        // LERP Camera TO Top Showing both start/end positions
        else if (isRoundRestartCameraLerping)
        {
            roundRestartCamLerpTimer += Time.deltaTime;
            transform.position = Vector3.Slerp(transform.position, multipleTargetsPos, roundRestartCamLerpTimer / roundRestartCamLerpTime);
            //if (transform.position.z < Target.position.z) transform.LookAt(Target);
            //transform.forward = Vector3.Slerp(transform.forward, Vector3.down, roundRestartCamLerpTimer / roundRestartCamLerpTime);
            transform.localEulerAngles = Vector3.Slerp(transform.localEulerAngles, Vector3.right * 90f, roundRestartCamLerpTimer / roundRestartCamLerpTime);
            if (roundRestartCamLerpTimer > roundRestartCamLerpTime) { isRoundStartCameraLerping = false; transform.position = multipleTargetsPos; transform.localEulerAngles = Vector3.right * 90f; }

            firsttime = true;
        }
        else firsttime = true;
    }

    // SET Camera on top showing both start and end positions
    public void SetPreRoundCamera(Transform t1, Transform t2)
    {
        multipleTargets = true;

        if (t1.GetComponentInChildren<BoxCollider>()) cambound = new Bounds(t1.GetComponentInChildren<BoxCollider>().bounds.center, t1.GetComponentInChildren<BoxCollider>().bounds.size);
        else cambound = new Bounds(t1.position, Vector3.zero);
        if (t2.GetComponent<BoxCollider>()) cambound.Encapsulate(t2.GetComponent<BoxCollider>().bounds);
        else cambound.Encapsulate(t2.position);

        float horfovsize = cambound.size.z;
        float fov = cam.fieldOfView;
        if (cambound.size.x > horfovsize * cam.aspect)
        {
            horfovsize = cambound.size.x;
            fov = Camera.VerticalToHorizontalFieldOfView(fov, cam.aspect);
        }

        float wd = 1.1f * horfovsize * (1 / Mathf.Tan(Mathf.Deg2Rad * fov * .5f)) / 2f;
        //transform.position = 
        multipleTargetsPos = new Vector3(cambound.center.x, wd, cambound.center.z);

        roundRestartCamLerpTimer = 0;
        isRoundRestartCameraLerping = true;
    }

    public void StartRoundCamera(Transform target)
    {
        Target = target;
        roundStartCamLerpTimer = 0;
        multipleTargets = false;
        isRoundStartCameraLerping = true;
    }

    public void EndLevelCameraAnim()
    {
        isEndLevelCameraAnim = true;
        StartCoroutine(EndLevelCameraAnimMOVE());
    }

    private IEnumerator EndLevelCameraAnimMOVE()
    {
        float t = 0;
        Quaternion currot = transform.rotation;
        Quaternion newrot = Quaternion.Euler(-45f, 0, 0);

        while (t <= 1f)
        {
            transform.rotation = Quaternion.Slerp(currot, newrot, t);
            t += Time.deltaTime;
            yield return null;
        }

        transform.rotation = newrot;
    }
}