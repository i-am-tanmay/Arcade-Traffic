using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct PlayerAttributes
{
    public float accel;
    public float speed;
    public float rotation;
    [Space(10)]
    public float grip;
}

public class Player_Movement : MonoBehaviour
{
    public int playerNum { get; private set; }

    [SerializeField] private PlayerAttributes attr_default, attr_boost, attr_grip, attr_wreck;
    private PlayerAttributes currAttr;
    [SerializeField] [Range(0.1f, 1f)] private float val_bushSlowMul = .55f;
    public bool isBushSlowed;

    [Range(1f, 100f)] [SerializeField] private float val_gravity = 15f;
    [Range(1f, -1f)] [SerializeField] private float val_collisionAngle = .45f;

    [SerializeField] private Transform sphereCollider;
    private Rigidbody sphere;
    [SerializeField] private Transform player_Container;
    [SerializeField] private Transform player_Normal;
    [SerializeField] private Transform player_Rotation;

    private float accel;
    private float rotation;
    private float curr_Accel;
    private float curr_Rotation;

    [HideInInspector] public bool wreckComplete = false;
    [SerializeField] private ParticleSystem ps_WreckSmoke;

    [SerializeField] private LayerMask layer_Ground;
    [SerializeField] private float rayground_Length = .6f;
    private RaycastHit ray_Ground;

    private Vector3 accel_Dir;
    private RaycastHit ray_Accel;

    private bool pwr_Unbreakable = false;

    private float radius;

    private bool engineStarted = false;
    [SerializeField] private float engineStartTime = 1f;
    private float engineStartTimer;

    private bool isRoundStarted = false;

    [SerializeField] private Transform directionarrow;
    private Transform directionarrow_target;
    private SpriteRenderer dirarrowsprite;
    private Vector2 dirarrowsprite_size;
    private Color dirarrowsprite_color;
    private float dirarrowsprite_heightmul;
    private float dirarrowanim_timer;
    [SerializeField] private float dirarrowanim_time = 3f;
    [SerializeField] private AnimationCurve dirarrowanim_curve;
    private float dirarrowanim_currH;
    [SerializeField] private float dirarrowanim_HeightReduce = 3f;
    [SerializeField] private Vector2 dirarrowdist_minmax;
    private bool stopDirectionArrowAnim = false;

    [SerializeField] private Transform[] skidmarks_Pos;
    [SerializeField] private float skidmarks_RayLenght = .6f;
    private int[] skidmarks_index = { -1, -1, -1, -1 };
    private RaycastHit ray_skidmarks;
    [Range(1f, 180f)] [SerializeField] private float skidmarks_LimitAngle = 20f;

    [Space(10f)]
    [SerializeField] private Player_AudioManager pAudio;
    [SerializeField] [Range(.1f, 1f)] private float pDriftingCutoff;

    [Space(10f)]
    [Header("UNITY")]
    [SerializeField] [Range(1, 60)] private int solverIterations = 60;
    [SerializeField] [Range(1, 60)] private int solverIterations_vel = 60;

    private void Start()
    {
        sphere = sphereCollider.GetComponent<Rigidbody>();

        Rigidbody rb = player_Container.GetComponent<Rigidbody>();
        sphere.solverIterations = rb.solverIterations = solverIterations;
        sphere.solverVelocityIterations = rb.solverVelocityIterations = solverIterations_vel;


        accel_Dir = player_Rotation.forward;
        radius = sphereCollider.GetComponent<SphereCollider>().radius;

        dirarrowsprite = directionarrow.GetChild(0).GetComponent<SpriteRenderer>();
        dirarrowsprite_color = dirarrowsprite.color;
        dirarrowsprite_color.a = 1f;
        dirarrowsprite_size = dirarrowsprite.size;
        dirarrowanim_currH = dirarrowsprite_size.y;
        dirarrowsprite_heightmul = 1 / dirarrowsprite.transform.localScale.y;
        dirarrowanim_timer = 0;

        engineStarted = false;
        isRoundStarted = false;
        engineStartTimer = 0;
        pAudio.StartStopEngine(false);

        ToggleWreckSmoke(false);

        currAttr = attr_default;
    }

    private void FixedUpdate()
    {
        Debug.DrawRay(sphereCollider.position, Vector3.down * rayground_Length, Color.red);
        if (Input.GetKeyDown(KeyCode.H)) pAudio.PlayHorn();
        pAudio.SetDrifting(false);

        if (isRoundStarted && engineStarted)
        {
            if (wreckComplete)
            {
                sphere.velocity = Vector3.Lerp(sphere.velocity, Vector3.zero, Time.fixedDeltaTime * 5f);
                sphere.angularVelocity = Vector3.Lerp(sphere.angularVelocity, Vector3.zero, Time.fixedDeltaTime * 5f);
                player_Container.localRotation = Quaternion.identity;
                player_Rotation.localEulerAngles = new Vector3(0, player_Rotation.localEulerAngles.y, 0);

                if (Physics.Raycast(sphereCollider.position, Vector3.down, out ray_Ground, rayground_Length, layer_Ground, QueryTriggerInteraction.Ignore))
                    player_Normal.up = Vector3.Lerp(player_Normal.up, ray_Ground.normal, Time.fixedDeltaTime * 8f);
                else player_Normal.up = Vector3.Lerp(player_Normal.up, Vector3.up, Time.fixedDeltaTime);

                return;
            }

            if (Physics.Raycast(sphereCollider.position, Vector3.down, out ray_Ground, rayground_Length, layer_Ground, QueryTriggerInteraction.Ignore))
            {
                player_Rotation.localEulerAngles = new Vector3(0, Mathf.Lerp(player_Rotation.localEulerAngles.y, player_Rotation.localEulerAngles.y + curr_Rotation, Time.fixedDeltaTime * 5f), 0);
                sphere.AddForceAtPosition(accel_Dir * curr_Accel, sphereCollider.position + Vector3.up * radius, ForceMode.Acceleration);
                player_Normal.up = Vector3.Lerp(player_Normal.up, ray_Ground.normal, Time.fixedDeltaTime * 8f);
                sphere.AddForce(player_Rotation.TransformDirection(-Vector3.right * player_Rotation.InverseTransformDirection(sphere.velocity).x) * currAttr.grip, ForceMode.Acceleration);

                float intensity = Vector3.Angle(player_Rotation.forward, sphere.velocity);
                if (intensity > 90f) intensity = 90f;
                if (intensity < skidmarks_LimitAngle) intensity = 0;
                else intensity = (intensity - skidmarks_LimitAngle) / (90 - skidmarks_LimitAngle);
                for (int i = 0; i < 4; i++)
                    if (Physics.Raycast(skidmarks_Pos[i].position, Vector3.down, out ray_skidmarks, skidmarks_RayLenght, layer_Ground, QueryTriggerInteraction.Ignore))
                    {
                        skidmarks_index[i] = SkidMarksManager.Instance.AddSkidMark(ray_skidmarks.point + sphere.velocity * Time.fixedDeltaTime, ray_skidmarks.normal, intensity, skidmarks_index[i]);
                        if (intensity > 0) pAudio.SetDrifting(true);
                    }
                    else skidmarks_index[i] = -1;
            }
            else
            {
                player_Normal.up = Vector3.Lerp(player_Normal.up, Vector3.up, Time.fixedDeltaTime);
                for (int i = 0; i < 4; i++) skidmarks_index[i] = -1;
            }

            if (Physics.Raycast(sphereCollider.position, Vector3.down, out ray_Accel, 50f, layer_Ground, QueryTriggerInteraction.Ignore))
                accel_Dir = Vector3.Cross(player_Rotation.right, ray_Accel.normal);


            sphere.velocity = Vector3.ClampMagnitude(sphere.velocity, currAttr.speed * (isBushSlowed ? val_bushSlowMul : 1f));
            float pSpeedFrac = sphere.velocity.magnitude / currAttr.speed;
            pAudio.SetPlayerSpeed(pSpeedFrac);
            if (pSpeedFrac < pDriftingCutoff) pAudio.SetDrifting(false);
            player_Container.localRotation = Quaternion.identity;

            if (GameManager.Instance.MapBoundsY > sphere.position.y) GameManager.Instance.isOutOfBounds();
        }
        else
        {
            sphere.velocity = Vector3.zero;
            sphere.angularVelocity = Vector3.zero;

            if (Physics.Raycast(sphereCollider.position, Vector3.down, out ray_Ground, rayground_Length, layer_Ground, QueryTriggerInteraction.Ignore))
                player_Normal.up = Vector3.Lerp(player_Normal.up, ray_Ground.normal, Time.fixedDeltaTime * 8f);
            else player_Normal.up = Vector3.Lerp(player_Normal.up, Vector3.up, Time.fixedDeltaTime);

            if (isRoundStarted)
            {
                engineStartTimer += Time.fixedDeltaTime;
                if (engineStartTimer > engineStartTime) engineStarted = true;
            }
        }

        sphere.AddForce(Vector3.down * val_gravity, ForceMode.Acceleration);

        isBushSlowed = false;
    }

    private void Update()
    {
        Vector3 dir = directionarrow_target.position - directionarrow.position;
        directionarrow.rotation = Quaternion.AngleAxis(-Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg, Vector3.up);
        dirarrowsprite_color.a = 1f;

        if (!stopDirectionArrowAnim && !isRoundStarted && !engineStarted)
        {
            // DIRECTION ARROW
            dirarrowanim_currH = Mathf.Lerp(dirarrowsprite_size.y, dir.magnitude * dirarrowsprite_heightmul - dirarrowanim_HeightReduce, dirarrowanim_curve.Evaluate(dirarrowanim_timer / dirarrowanim_time));
            dirarrowsprite.size = new Vector2(dirarrowsprite_size.x, dirarrowanim_currH);
            dirarrowanim_timer = (dirarrowanim_timer + Time.deltaTime) % dirarrowanim_time;
        }
        else if ((isRoundStarted || stopDirectionArrowAnim) && !engineStarted)
        {
            float h = Mathf.SmoothStep(dirarrowanim_currH, dirarrowsprite_size.y, dirarrowanim_timer / (CameraFollow.Instance.roundStartCamLerpTime * .7f));
            dirarrowsprite.size = new Vector2(dirarrowsprite_size.x, h);
            dirarrowanim_timer += Time.deltaTime;
        }
        else if (isRoundStarted && engineStarted)
        {
            dirarrowsprite.size = dirarrowsprite_size;
            float dirdist = dir.sqrMagnitude;
            if (dirdist > dirarrowdist_minmax.x && dirdist < dirarrowdist_minmax.y) dirarrowsprite_color.a = (dirdist - dirarrowdist_minmax.x) / dirarrowdist_minmax.y;
            else if (dirdist <= dirarrowdist_minmax.x) dirarrowsprite_color.a = 0;

            //if (Input.GetKey(KeyCode.W)) accel = currAttr.accel;
            //else if (Input.GetKey(KeyCode.S)) accel = -currAttr.accel * .8f;
            accel = currAttr.accel;

            //if (Input.GetKey(KeyCode.D)) rotation = currAttr.rotation;
            //else if (Input.GetKey(KeyCode.A)) rotation = -currAttr.rotation;
            if (GameManager.Instance.pm_isRightPressed || Input.GetAxis("Horizontal") > .3f) rotation = currAttr.rotation;
            else if (GameManager.Instance.pm_isLeftPressed || Input.GetAxis("Horizontal") < -.3f) rotation = -currAttr.rotation;

            curr_Accel = Mathf.SmoothStep(curr_Accel, accel, Time.deltaTime * 12f); accel = 0;
            curr_Rotation = Mathf.Lerp(curr_Rotation, rotation, Time.deltaTime * 4f); rotation = 0;
        }

        dirarrowsprite.color = dirarrowsprite_color;
    }

    public void StopSphereRigidbodyVelocity()
    {
        sphere.velocity = Vector3.zero;
        sphere.angularVelocity = Vector3.zero;
    }

    // INIT engine dirarrowANIM waitforcameraLERP
    public void StartStopRound(bool isStarted)
    {
        if (isStarted)
        {
            if (!isRoundStarted)
            {
                engineStartTimer = 0;
                stopDirectionArrowAnim = true;
                StartCoroutine(StartStopRound_WaitForCamera());
                pAudio.StartStopEngine(true);
            }
        }
        else
        {
            stopDirectionArrowAnim = false;
            isRoundStarted = false;
            engineStarted = false;
            pAudio.StartStopEngine(false);
        }

        dirarrowanim_timer = 0;
    }

    // WAIT for cameraLERP till engine starts
    private IEnumerator StartStopRound_WaitForCamera()
    {
        float t = CameraFollow.Instance.roundStartCamLerpTime - engineStartTime;
        if (t < 0) t = 0;
        yield return new WaitForSeconds(t + .1f);
        isRoundStarted = true;
    }

    // SET CURRENTPLAYER num rot wreck/smoke/dirarrow
    public void Init(int playerNumber, float initYRot, Transform target)
    {
        playerNum = playerNumber;
        player_Rotation.localEulerAngles = Vector3.up * initYRot;
        directionarrow_target = target;
        wreckComplete = false;
        ToggleWreckSmoke(false);
    }

    public void CollideCalc(Vector3 angle)
    {
        float dt = Vector3.Dot(angle, -Vector3.ProjectOnPlane(player_Rotation.forward, Vector3.up));
        if (!pwr_Unbreakable && dt > val_collisionAngle)
        {
            GameManager.Instance.StartWreckTimer();
            wreckComplete = false;
            ToggleWreckSmoke(true);
            currAttr = attr_wreck;
            pAudio.PlayCollision(true);
        }
        else
        {
            pAudio.PlayCollision(false);
        }
    }

    public void SetPowerup(Powerup pwr)
    {
        pwr_Unbreakable = false;

        switch (pwr)
        {
            case Powerup.None:
                currAttr = attr_default;
                break;

            case Powerup.Boost:
                currAttr = attr_boost;
                break;

            case Powerup.Grip:
                currAttr = attr_grip;
                break;

            case Powerup.Unbreakable:
                currAttr = attr_default;
                pwr_Unbreakable = true;
                break;
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