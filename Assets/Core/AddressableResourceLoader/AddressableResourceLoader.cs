using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Core
{
    public interface IResourceLoader
    {
        public Task<T> GetAsset<T>(string name)
            where T : Object;
    }

    public class ResourceDirLoader : IResourceLoader
    {
        public async Task<T> GetAsset<T>(string name)
            where T : Object
        {
            var res = Resources.Load<T>(name);

            if (res == null)
                Logger.LogError(
                    $"ResourceDirLoader: Failed to load resource '{name}'",
                    LogTag.Addressables
                );

            return res;
        }
    }

    public class AddressableLoader : IResourceLoader
    {
        public Task<T> GetAsset<T>(string name)
            where T : Object
        {
            var handle = Addressables.LoadAssetAsync<T>(name);
            return handle.Task;
        }
    }

    public static class AddressableResourceLoader
    {
        public static void Initialize(IResourceLoader loader = null)
        {
            if (sIsInitialized)
                return;

            sLoader = loader ?? new ResourceDirLoader();
            sIsInitialized = true;
        }

        public static async Task<T> GetAsset<T>(string addressableName)
            where T : Object
        {
            Initialize();

            var task = sLoader.GetAsset<T>(addressableName);
            return await task;
        }

        public static void PrintAddressableInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Addressable Asset Info:");
            var locations = Addressables
                .ResourceLocators.SelectMany(locator =>
                    locator.Locate("", typeof(object), out var locs)
                        ? locs
                        : Enumerable.Empty<IResourceLocation>()
                )
                .Distinct();
            foreach (var location in locations)
            {
                sb.AppendLine(
                    $"- Address: {location.PrimaryKey}, InternalId: {location.InternalId}, ProviderId: {location.ProviderId}"
                );
            }
            Logger.LogInfo(sb.ToString(), LogTag.Addressables);
        }

        private static bool sIsInitialized = false;
        private static IResourceLoader sLoader = null;
    }
}
