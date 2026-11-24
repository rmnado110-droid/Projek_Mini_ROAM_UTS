using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 1.5f;
    public float runSpeed = 5f;
    public float turnSpeed = 10f;
    public float gravity = -20f;

    [Header("Animation")]
    public string speedParameter = "Speed";
    public float animDamping = 0.1f;
    [Tooltip("Harus sama persis dengan nama state di tab Animator")]
    public string sitStateName = "Sitting";
    public string idleStateName = "Idle";
    public float crossFadeTime = 0.2f;
    [Tooltip("Opsional: isi dengan playeranimator kalau Animator model belum punya Controller")]
    public RuntimeAnimatorController fallbackController;

    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.E;

    [Header("Debug")]
    public bool showDebug = true;

    private CharacterController controller;
    private Animator animator;
    private Transform cam;
    private float verticalVelocity;
    private int speedHash;
    private bool hasSpeedParam;

    private bool isSitting;
    private SitPoint currentSeat;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        cam = Camera.main.transform;
        speedHash = Animator.StringToHash(speedParameter);

        DisableAnimationViewer();
        SetupAnimator();
    }

    void DisableAnimationViewer()
    {
        foreach (var mb in GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb == null || mb == this) continue;
            if (mb.GetType().Name.Contains("UniversalAnimation"))
            {
                mb.enabled = false;
                if (showDebug) Debug.Log("PlayerController: " + mb.GetType().Name + " dimatikan.");
            }
        }
    }

    void SetupAnimator()
    {
        Animator[] all = GetComponentsInChildren<Animator>(true);

        if (all.Length == 0)
        {
            Debug.LogWarning("PlayerController: tidak ada Animator di Player atau child-nya.");
            return;
        }

        foreach (var a in all)
        {
            if (a.avatar != null && a.avatar.isValid && a.avatar.isHuman)
            {
                animator = a;
                break;
            }
        }
        if (animator == null)
        {
            foreach (var a in all)
            {
                if (a.avatar != null) { animator = a; break; }
            }
        }
        if (animator == null) animator = all[0];

        foreach (var a in all)
        {
            if (a != animator)
            {
                a.enabled = false;
                if (showDebug) Debug.Log("PlayerController: Animator di '" + a.gameObject.name + "' dimatikan.");
            }
        }

        if (animator.runtimeAnimatorController == null && fallbackController != null)
            animator.runtimeAnimatorController = fallbackController;

        animator.applyRootMotion = false;

        if (showDebug)
        {
            Debug.Log("Animator dipakai: " + animator.gameObject.name
                + " | Controller: " + (animator.runtimeAnimatorController ? animator.runtimeAnimatorController.name : "KOSONG")
                + " | Avatar: " + (animator.avatar ? animator.avatar.name : "KOSONG"));
        }

        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogWarning("PlayerController: Animator model belum punya Controller.");
            return;
        }

        foreach (var p in animator.parameters)
        {
            if (p.name == speedParameter && p.type == AnimatorControllerParameterType.Float) hasSpeedParam = true;
        }
        if (!hasSpeedParam)
            Debug.LogWarning("PlayerController: parameter Float '" + speedParameter + "' tidak ada di Controller.");

        CheckState(sitStateName);
        CheckState(idleStateName);
    }

    void CheckState(string stateName)
    {
        if (animator != null && !animator.HasState(0, Animator.StringToHash(stateName)))
            Debug.LogWarning("PlayerController: state '" + stateName + "' tidak ditemukan di Animator. Samakan namanya dengan di tab Animator.");
    }

    void Update()
    {
        if (Input.GetKeyDown(interactKey))
        {
            if (isSitting) StandUp();
            else TrySit();
        }

        if (isSitting)
        {
            if (animator != null && hasSpeedParam)
                animator.SetFloat(speedHash, 0f);
            return;
        }

        Move();
    }

    void Move()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 camForward = cam.forward; camForward.y = 0; camForward.Normalize();
        Vector3 camRight = cam.right; camRight.y = 0; camRight.Normalize();

        Vector3 dir = camForward * v + camRight * h;
        if (dir.magnitude > 1f) dir.Normalize();

        float speed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;

        if (controller.isGrounded && verticalVelocity < 0) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 move = dir * speed;
        move.y = verticalVelocity;
        controller.Move(move * Time.deltaTime);

        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * Time.deltaTime);
        }

        if (animator != null && hasSpeedParam)
        {
            float animSpeed = dir.sqrMagnitude > 0.01f ? speed : 0f;
            animator.SetFloat(speedHash, animSpeed, animDamping, Time.deltaTime);
        }
    }

    void TrySit()
    {
        SitPoint nearest = null;
        float best = float.MaxValue;
        float nearestAny = -1f;

        foreach (var sp in FindObjectsByType<SitPoint>(FindObjectsSortMode.None))
        {
            float d = Vector3.Distance(transform.position, sp.transform.position);
            if (nearestAny < 0 || d < nearestAny) nearestAny = d;

            if (d <= sp.interactRange && d < best)
            {
                best = d;
                nearest = sp;
            }
        }

        if (nearest == null)
        {
            if (showDebug)
                Debug.Log(nearestAny < 0
                    ? "Tidak ada objek SitPoint di scene."
                    : "SitPoint terdekat berjarak " + nearestAny.ToString("F2") + " m. Dekati sampai masuk Interact Range.");
            return;
        }

        currentSeat = nearest;
        isSitting = true;

        controller.enabled = false;
        transform.SetPositionAndRotation(nearest.transform.position, nearest.transform.rotation);

        if (animator != null) animator.CrossFadeInFixedTime(sitStateName, crossFadeTime);
    }

    void StandUp()
    {
        isSitting = false;

        if (animator != null) animator.CrossFadeInFixedTime(idleStateName, crossFadeTime);

        if (currentSeat != null)
            transform.position = currentSeat.StandPosition;

        controller.enabled = true;
        verticalVelocity = 0f;
        currentSeat = null;
    }
}
