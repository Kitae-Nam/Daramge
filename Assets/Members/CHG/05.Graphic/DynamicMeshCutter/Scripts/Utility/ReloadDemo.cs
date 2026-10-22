using UnityEngine;
using UnityEngine.SceneManagement;

namespace Members.CHG.Graphic.DynamicMeshCutter.Scripts.Utility
{
    public class ReloadDemo : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }
    }

}
