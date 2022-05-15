using System;
using UnityEngine;

namespace Sample.Scripts
{
    // Most of the code is taken from this post: https://sharpcoderblog.com/blog/unity-3d-fps-controller
    [RequireComponent(typeof(CharacterController))]
    public class SimpleFPSController : MonoBehaviour
    {

        public float walkingSpeed = 7.5f;
        public float runningSpeed = 11.5f;
        public float jumpSpeed = 8.0f;
        public float gravity = 20.0f;
        public Camera playerCamera;
        public float lookSpeed = 2.0f;
        public float lookXLimit = 45.0f;
        
        private CharacterController m_CharacterController;
        private Vector3 m_MoveDirection = Vector3.zero;
        private float m_RotationX = 0f;

        [HideInInspector]
        public bool canMove = true;
        
        private void Start()
        {
            m_CharacterController = GetComponent<CharacterController>();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            Vector3 forward = transform.TransformDirection(Vector3.forward);
            Vector3 right = transform.TransformDirection(Vector3.right);
            
            bool isRunning = Input.GetKey(KeyCode.LeftShift);
            float curSpeedX = canMove ? (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Vertical") : 0;
            float curSpeedY = canMove ? (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Horizontal") : 0;
            float movementDirectionY = m_MoveDirection.y;
            m_MoveDirection = (forward * curSpeedX) + (right * curSpeedY);

            if (Input.GetButton("Jump") && canMove && m_CharacterController.isGrounded)
            {
                m_MoveDirection.y = jumpSpeed;
            }
            else
            {
                m_MoveDirection.y = movementDirectionY;
            }
            
            // Apply gravity. Gravity is multiplied by deltaTime twice (once here, and once below
            // when the moveDirection is multiplied by deltaTime). This is because gravity should be applied
            // as an acceleration (ms^-2)
            if (!m_CharacterController.isGrounded)
            {
                m_MoveDirection.y -= gravity * Time.deltaTime;
            }

            // Move the controller
            m_CharacterController.Move(m_MoveDirection * Time.deltaTime);

            // Player and Camera rotation
            if (canMove)
            {
                m_RotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
                m_RotationX = Mathf.Clamp(m_RotationX, -lookXLimit, lookXLimit);
                playerCamera.transform.localRotation = Quaternion.Euler(m_RotationX, 0, 0);
                transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
            }
            
        }
    }
}
