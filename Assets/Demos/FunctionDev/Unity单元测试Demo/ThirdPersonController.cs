using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("角色参数")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;

    [Header("摄像机参数")]
    public Transform cameraTransform;
    public float cameraDistance = 5f;
    public float cameraHeight = 2f;
    public float cameraSensitivity = 2f;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isRunning;

    private float cameraYaw;
    private float cameraPitch;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        cameraYaw = transform.eulerAngles.y;
        cameraPitch = 10f;
    }

    void Update()
    {
        // 检查是否在地面
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // 移动输入
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 move = transform.right * h + transform.forward * v;
        isRunning = Input.GetKey(KeyCode.LeftShift);

        float speed = isRunning ? runSpeed : walkSpeed;
        controller.Move(move.normalized * speed * Time.deltaTime);

        // 跳跃
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // 重力
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // 摄像机控制
        CameraControl();
    }

    void CameraControl()
    {
        if (cameraTransform == null)
            return;

        cameraYaw += Input.GetAxis("Mouse X") * cameraSensitivity;
        cameraPitch -= Input.GetAxis("Mouse Y") * cameraSensitivity;
        cameraPitch = Mathf.Clamp(cameraPitch, -30f, 60f);

        Quaternion camRot = Quaternion.Euler(cameraPitch, cameraYaw, 0);
        Vector3 camPos =
            transform.position
            - camRot * Vector3.forward * cameraDistance
            + Vector3.up * cameraHeight;

        cameraTransform.position = camPos;
        cameraTransform.rotation = camRot;

        // 角色朝向摄像机前方
        Vector3 lookDir = cameraTransform.forward;
        lookDir.y = 0;
        if (lookDir.sqrMagnitude > 0.01f)
            transform.forward = lookDir;
    }
}
