using System;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Map
{
    public class AppearanceObj : MonoBehaviour
    {
        [SerializeField] private GameObject[] objs;

        // private void Start()
        // {
        //     TryHide();
        // }

        public void TryAppearance()
        {
            if (objs == null) return;
            foreach (GameObject obj in objs)
            {
                obj.SetActive(true);
            }
        }

        public void TryHide()
        {
            if (objs == null) return;
            foreach (GameObject obj in objs)
            {
                obj.SetActive(false);
            }
        }
        
    }
}