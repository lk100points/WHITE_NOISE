using UnityEngine;

namespace WhiteNoise
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        public KeyCode forwardKey = KeyCode.W;
        public KeyCode backKey = KeyCode.S;
        public KeyCode leftKey = KeyCode.A;
        public KeyCode rightKey = KeyCode.D;
        public KeyCode sprintKey = KeyCode.LeftShift;
        public KeyCode interactKey = KeyCode.E;
        public KeyCode cameraKey = KeyCode.C;
        public KeyCode pauseKey = KeyCode.Escape;

        private CharacterController controller;
        private Transform cameraTransform;
        private Transform cameraPivot;
        private GameObject body;
        private float yaw;
        private float pitch;
        private bool inputEnabled;
        private bool thirdPerson;
        private float bobTime;
        private Vector3 cameraOffset;

        public bool ThirdPerson { get { return thirdPerson; } }
        public KeyCode InteractKey { get { return interactKey; } }
        public KeyCode CameraKey { get { return cameraKey; } }
        public KeyCode PauseKey { get { return pauseKey; } }
        public KeyCode ForwardKey { get { return forwardKey; } }
        public KeyCode BackKey { get { return backKey; } }
        public KeyCode LeftKey { get { return leftKey; } }
        public KeyCode RightKey { get { return rightKey; } }
        public KeyCode SprintKey { get { return sprintKey; } }

        public void Initialize()
        {
            controller = GetComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.34f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.stepOffset = 0.32f;
            controller.slopeLimit = 48f;

            body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Player silhouette";
            body.transform.SetParent(transform, false);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localScale = new Vector3(0.72f, 0.95f, 0.72f);
            Collider bodyCollider = body.GetComponent<Collider>();
            if (bodyCollider != null) Destroy(bodyCollider);
            Renderer bodyRenderer = body.GetComponent<Renderer>();
            if (bodyRenderer != null) bodyRenderer.material = WorldBuilder.MakeMaterial(new Color(0.19f, 0.20f, 0.22f));

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head silhouette";
            head.transform.SetParent(transform, false);
            head.transform.localPosition = new Vector3(0f, 1.72f, 0f);
            head.transform.localScale = new Vector3(0.38f, 0.38f, 0.38f);
            Collider headCollider = head.GetComponent<Collider>();
            if (headCollider != null) Destroy(headCollider);
            Renderer headRenderer = head.GetComponent<Renderer>();
            if (headRenderer != null) headRenderer.material = WorldBuilder.MakeMaterial(new Color(0.66f, 0.63f, 0.58f));

            cameraPivot = new GameObject("Camera pivot").transform;
            cameraPivot.SetParent(transform, false);
            cameraPivot.localPosition = new Vector3(0f, 1.62f, 0f);
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraTransform = cameraObject.transform;
            cameraTransform.SetParent(cameraPivot, false);
            cameraTransform.localPosition = Vector3.zero;
            cameraTransform.localRotation = Quaternion.identity;
            Camera cam = cameraObject.AddComponent<Camera>();
            cam.fieldOfView = 69f;
            cam.nearClipPlane = 0.06f;
            cam.farClipPlane = 260f;
            cam.allowMSAA = true;
            cameraObject.AddComponent<AudioListener>();
            ApplyCameraMode();
        }

        public void SetInputEnabled(bool value)
        {
            if (inputEnabled == value) return;
            inputEnabled = value;
            if (value)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public void Teleport(Vector3 position)
        {
            controller.enabled = false;
            transform.position = position;
            controller.enabled = true;
        }

        public void ToggleCamera()
        {
            thirdPerson = !thirdPerson;
            ApplyCameraMode();
        }

        public void Face(float degrees)
        {
            yaw = degrees;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        private void ApplyCameraMode()
        {
            if (cameraTransform == null) return;
            cameraOffset = thirdPerson ? new Vector3(-0.72f, 0.34f, -4.25f) : Vector3.zero;
            if (body != null) body.SetActive(thirdPerson);
            cameraTransform.localPosition = cameraOffset;
            cameraTransform.localRotation = Quaternion.identity;
        }

        private void Update()
        {
            if (!inputEnabled || controller == null) return;

            if (Input.GetKeyDown(cameraKey)) ToggleCamera();

            float mouseSensitivity = 2.2f;
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, -78f, 78f);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            float x = 0f;
            float z = 0f;
            if (Input.GetKey(forwardKey)) z += 1f;
            if (Input.GetKey(backKey)) z -= 1f;
            if (Input.GetKey(rightKey)) x += 1f;
            if (Input.GetKey(leftKey)) x -= 1f;
            Vector3 move = (transform.forward * z + transform.right * x).normalized;
            bool running = Input.GetKey(sprintKey);
            float speed = running ? 6.0f : 4.25f;
            Vector3 velocity = move * speed;
            velocity.y = -2.2f;
            controller.Move(velocity * Time.deltaTime);

            if (move.sqrMagnitude > 0.02f)
                bobTime += Time.deltaTime * (running ? 13f : 8f);
            else
                bobTime = Mathf.Lerp(bobTime, 0f, Time.deltaTime * 4f);

            if (!thirdPerson)
            {
                cameraTransform.localPosition = new Vector3(0f, Mathf.Sin(bobTime) * 0.018f, 0f);
            }
            else
            {
                Vector3 desired = cameraOffset + new Vector3(0f, Mathf.Sin(bobTime) * 0.012f, 0f);
                cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, desired, Time.deltaTime * 8f);
                Vector3 focus = cameraPivot.position + transform.forward * 1.15f + Vector3.up * 0.1f;
                cameraTransform.LookAt(focus);
            }
        }

        public void SetBindings(KeyCode f, KeyCode b, KeyCode l, KeyCode r, KeyCode sprint, KeyCode interact, KeyCode camera, KeyCode pause)
        {
            forwardKey = f;
            backKey = b;
            leftKey = l;
            rightKey = r;
            sprintKey = sprint;
            interactKey = interact;
            cameraKey = camera;
            pauseKey = pause;
        }
    }
}
