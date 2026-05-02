using UnityEngine;

namespace Core
{
    public interface IProgressable
    {
        void UpdateProgress(float progress);
        void Tick();
        void Show();
        void Hide();
    }

    public abstract class MonoProgressable : MonoBehaviour, IProgressable
    {
        public abstract void UpdateProgress(float progress);

        public virtual void Tick() { }

        public abstract void Show();

        public abstract void Hide();
    }
}
