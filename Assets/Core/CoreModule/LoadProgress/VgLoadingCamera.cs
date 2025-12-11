using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core
{
    public class VgLoadingCamera : MonoProgressable
    {
        private CancellationTokenSource _cancellationTokenSource;

        public override void Hide()
        {
            HideAsync().Forget();
        }

        public override void Show()
        {
            ShowAsync().Forget();
        }

        private async UniTaskVoid HideAsync()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                await WaitForTransitionsToComplete(_cancellationTokenSource.Token);
                gameObject.SetActive(false);
            }
            catch (OperationCanceledException) { }
            finally
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private async UniTaskVoid ShowAsync()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                await WaitForTransitionsToComplete(_cancellationTokenSource.Token);
                gameObject.SetActive(true);
            }
            catch (OperationCanceledException) { }
            finally
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private async UniTask WaitForTransitionsToComplete(CancellationToken cancellationToken)
        {
            if (!HasActiveTransitions())
                return;

            await WaitForAllTransitionsHidden(cancellationToken);
        }

        private async UniTask WaitForAllTransitionsHidden(CancellationToken cancellationToken)
        {
            var tcs = new UniTaskCompletionSource();
            var isCompleted = false;

            void OnTransitionsHidden()
            {
                if (!isCompleted)
                {
                    isCompleted = true;
                    VgLoadingTransition.OnAllTransitionsHidden -= OnTransitionsHidden;
                    tcs.TrySetResult();
                }
            }

            VgLoadingTransition.OnAllTransitionsHidden += OnTransitionsHidden;

            try
            {
                await tcs.Task.AttachExternalCancellation(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                if (!isCompleted)
                {
                    VgLoadingTransition.OnAllTransitionsHidden -= OnTransitionsHidden;
                }
                throw;
            }
        }

        private bool HasActiveTransitions()
        {
            VgLoadingTransition[] transitions = FindObjectsByType<VgLoadingTransition>(
                FindObjectsSortMode.None
            );
            return transitions.Length > 0;
        }

        public override void UpdateProgress(float progress) { }
    }
}
