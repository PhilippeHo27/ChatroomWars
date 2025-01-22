using System;
using Core.Singletons;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Core
{
    public class MoveableObject : MonoBehaviour
    {
        private Vector2 _movement;
        private float _moveSpeed = 5f;

        private void Start()
        {
            InputManager.Instance.InputActionHandlers["Move"].Performed += OnMovementInput;
            InputManager.Instance.InputActionHandlers["Move"].Canceled += OnMovementInput;        
        }

        private void OnEnable()
        {

        }

        private void OnDisable()
        {
            InputManager.Instance.InputActionHandlers["Move"].Performed -= OnMovementInput;
            InputManager.Instance.InputActionHandlers["Move"].Canceled -= OnMovementInput;
        }

        private void OnMovementInput(InputAction.CallbackContext context)
        {
            _movement = context.ReadValue<Vector2>();
        }

        private void Update()
        {
            Vector3 movement = new Vector3(_movement.x, _movement.y, 0) * _moveSpeed * Time.deltaTime;
            transform.position += movement;
        }
    }
}