using UnityEngine;
using UnityEngine.SceneManagement;

namespace CHG._05.Graphic.Asset.DynamicMeshCutter.Scripts.Utility
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
