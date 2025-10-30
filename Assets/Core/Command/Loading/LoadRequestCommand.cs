using Cysharp.Threading.Tasks;

namespace Core
{
    public class LoadRequestCommand : AbastractManagerCommand<LoadManager>
    {
        public LoadRequestEvent LoadEvent { get; set; }

        public LoadRequestCommand(LoadRequestEvent loadEvent)
        {
            LoadEvent = loadEvent;
        }

        public override bool Execute()
        {
            if (m_Manager == null)
            {
                Logger.LogError(
                    "[LoadCommand] LoadManager is not initialized, making excution for LoadEvent failed"
                );

                return false;
            }

            m_Manager.PrepareForLoad(LoadEvent);
            MessageBroker.Global.Publish(LoadEvent);

            return true;
        }

        public override async UniTask<bool> ExecuteAsync()
        {
            bool completed = false;
            bool succeed = false;

            void OnLoadComplete(R3.Result loadEvent)
            {
                completed = true;
                succeed = loadEvent.IsSuccess;
            }

            using var subscription = MessageBroker.Global.Subscribe<LoadRequestEvent>(
                _ => { },
                OnLoadComplete
            );
            if (!Execute())
                return false;

            await UniTask.WaitUntil(() => completed);

            return succeed;
        }
    }
}
