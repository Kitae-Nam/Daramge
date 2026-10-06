using System;
using DG.Tweening;
using UnityEngine;

namespace GGMLib.CoreLibrary
{
    public class FeedbackPlayer : MonoBehaviour
    {
        private AbstractFeedback[] _feedbacks;
        
        private void Awake()
        {
            _feedbacks = GetComponents<AbstractFeedback>();
        }

        public void PlayAllFeedbacks()
        {
            foreach (AbstractFeedback feedback in _feedbacks)
            {
                feedback.CreateFeedback();
            }
        }

        public void StopAllFeedbacks()
        {
            foreach (AbstractFeedback feedback in _feedbacks)
            {
                feedback.StopFeedback();
            }
        }
    }
}