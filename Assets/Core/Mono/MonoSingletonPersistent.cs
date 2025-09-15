using UnityEngine;

namespace Core
{
    /// <summary>
    /// Persistent while application is running.
    /// Thread safe implementation of Singleton pattern using lasy initialization.
    /// </summary>
    public class MonoSingletonPersistent<T> : MonoBehaviour
        where T : MonoBehaviour
    {
        private static T instance;
        private static readonly object lockObject = new();

        public static T Instance
        {
            get
            {
                lock (lockObject)
                {
                    if (instance == null)
                    {
                        instance = FindObjectOfType<T>();

                        if (instance == null)
                        {
                            GameObject singletonObject = new(typeof(T).Name);
                            instance = singletonObject.AddComponent<T>();
                            DontDestroyOnLoad(singletonObject);
                        }
                    }
                    return instance;
                }
            }
        }

        protected virtual void Awake()
        {
            if (instance == null)
            {
                instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
