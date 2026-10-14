using UnityEngine;
using UnityEngine.InputSystem;

namespace Memebers.CWH.SCRIPTS.TEST
{
    public class TestCameraController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;

        void Update()
        {
            Vector3 input = Vector3.zero;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed)
                    input.z += 1;

                if (Keyboard.current.sKey.isPressed)
                    input.z -= 1;

                if (Keyboard.current.aKey.isPressed)
                    input.x -= 1;

                if (Keyboard.current.dKey.isPressed)
                    input.x += 1;
            }

            Vector3 move = new Vector3(input.x, 0f, input.z);
            transform.position += move.normalized * moveSpeed * Time.deltaTime;
        }
    }
}