using System;
using UnityEngine;

namespace CHG._02_Scripts.CoreSystem.FeedbackSystem
{
    public class FeedbackPlayer : MonoBehaviour
    {
        private IFeedback[] _feedbacks;

        private void Awake() => _feedbacks = GetComponentsInChildren<IFeedback>();

        public void PlayFeedback(FeedbackContext context)
        {
            foreach (IFeedback feedback in _feedbacks)
                feedback.PlayFeedback(context);
        }
    }
}