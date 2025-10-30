using Core;
public class SaveLoadInfo : ILoadInfo
{
    public LoaderType GetNeededLoaderType()
    {
        return LoaderType.SaveLoader;
    }
}
