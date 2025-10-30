namespace Core
{
    public class Singleton<T>
        where T : new()
    {
        private static readonly object lockObject = new();
        private static T instance;

        public static T Instance
        {
            get
            {
                lock (lockObject)
                {
                    instance ??= new T();
                    return instance;
                }
            }
        }
    }
}
