using System;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Map
{
    public class AppearanceWall : MonoBehaviour
    {
        [SerializeField] private GameObject[] walls;

        private void Start()
        {
            TryBroken();
        }

        public void TryAppearance()
        {
            if (walls == null) return;
            foreach (GameObject wall in walls)
            {
                wall.SetActive(true);
            }
        }

        public void TryBroken()
        {
            if (walls == null) return;
            foreach (GameObject wall in walls)
            {
                wall.SetActive(false);
            }
        }
        
    }
}