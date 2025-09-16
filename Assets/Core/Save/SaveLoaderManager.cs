using Core;
using System;

public class SaveLoaderManager : CoreModuleManagerBase<SaveLoaderManager>
{
    protected override void CreateLoader(ILoadInfo loadInfo)
    {
        throw new NotImplementedException();
    }

    protected override void OnLoadingError(Exception exception)
    {
        throw new NotImplementedException();
    }

    protected override void OnReceiveLoadRequest(LoadRequestEvent loadEventInfo)
    {
        throw new NotImplementedException();
    }
}
