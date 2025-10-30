using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Core
{
    public class VgLoadingTransition : MonoProgressable
    {
        public static event Action OnAllTransitionsHidden;
        public static event Action OnAllTransitionsShown;

        [Header("Animation Settings")]
        [SerializeField]
        private float animationDuration = 0.5f;

        [SerializeField]
        private Ease easeType = Ease.OutQuart;

        [SerializeField]
        private Vector3 showScale = Vector3.one;

        [SerializeField]
        private Vector3 hideScale = Vector3.zero;

        private Tween currentTween;
        private Queue<System.Action> animationQueue = new Queue<System.Action>();
        private bool isProcessingQueue = false;
        private static int activeHideTransitions = 0;
        private static int completedHideTransitions = 0;
        private static int activeShowTransitions = 0;
        private static int completedShowTransitions = 0;

        public override void Show()
        {
            ResetCounters();
            animationQueue.Enqueue(() => ExecuteShow());
            ProcessQueue();
        }

        public override void Hide()
        {
            ResetCounters();
            animationQueue.Enqueue(() => ExecuteHide());
            ProcessQueue();
        }

        private static void ResetCounters()
        {
            activeHideTransitions = 0;
            completedHideTransitions = 0;
            activeShowTransitions = 0;
            completedShowTransitions = 0;
        }

        private void ProcessQueue()
        {
            if (isProcessingQueue || animationQueue.Count == 0)
                return;

            isProcessingQueue = true;
            var nextAction = animationQueue.Dequeue();
            nextAction?.Invoke();
        }

        private void ExecuteShow()
        {
            transform.localScale = hideScale;

            if (currentTween != null && currentTween.IsActive())
                currentTween.Kill();

            gameObject.SetActive(true);
            activeShowTransitions++;

            currentTween = transform
                .DOScale(showScale, animationDuration)
                .SetEase(easeType)
                .OnComplete(() =>
                {
                    currentTween = null;
                    isProcessingQueue = false;
                    completedShowTransitions++;

                    if (completedShowTransitions >= activeShowTransitions)
                    {
                        OnAllTransitionsShown?.Invoke();
                        completedShowTransitions = 0;
                        activeShowTransitions = 0;
                    }

                    ProcessQueue();
                });
        }

        private void ExecuteHide()
        {
            if (currentTween != null && currentTween.IsActive())
                currentTween.Kill();

            activeHideTransitions++;

            currentTween = transform
                .DOScale(hideScale, animationDuration)
                .SetEase(easeType)
                .OnComplete(() =>
                {
                    gameObject.SetActive(false);
                    currentTween = null;
                    isProcessingQueue = false;
                    completedHideTransitions++;

                    if (completedHideTransitions >= activeHideTransitions)
                    {
                        OnAllTransitionsHidden?.Invoke();
                        completedHideTransitions = 0;
                        activeHideTransitions = 0;
                    }

                    ProcessQueue();
                });
        }

        public override void UpdateProgress(float progress) { }

        private void OnDestroy()
        {
            if (currentTween != null && currentTween.IsActive())
                currentTween.Kill();

            animationQueue.Clear();
        }
    }
}
