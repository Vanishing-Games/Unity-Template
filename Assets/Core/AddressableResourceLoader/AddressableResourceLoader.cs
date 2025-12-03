using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Core
{
    public static class AddressableResourceLoader
    {
        /// <summary>
        /// 加载一整个资源组 (基于Label), 适用于如关卡初始化时加载关卡需要的资源
        /// </summary>
        /// <param name="labelName">Addressable Group Label</param>
        public static async void LoadGroup(string labelName)
        {
            if (mLoadedGroupHandles.ContainsKey(labelName))
            {
                Logger.LogInfo($"Group {labelName} is already loaded.", LogTag.Addressables);
                return;
            }

            Logger.LogInfo($"Start loading group: {labelName}", LogTag.Addressables);

            var handle = Addressables.LoadAssetsAsync<object>(labelName, null);

            mLoadedGroupHandles.Add(labelName, handle);

            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                Logger.LogInfo(
                    $"Successfully loaded group: {labelName}. Count: {handle.Result.Count}",
                    LogTag.Addressables
                );
            }
            else
            {
                Logger.LogError($"Failed to load group: {labelName}", LogTag.Addressables);
                // 加载失败，移除句柄以免无法重试
                if (mLoadedGroupHandles.ContainsKey(labelName))
                    mLoadedGroupHandles.Remove(labelName);
            }
        }

        /// <summary>
        /// 卸载一整个资源组, 适用于如关卡结束时卸载关卡使用的资源
        /// </summary>
        /// <param name="labelName"></param>
        public static void UnloadGroup(string labelName)
        {
            if (mLoadedGroupHandles.TryGetValue(labelName, out var handle))
            {
                Addressables.Release(handle);
                mLoadedGroupHandles.Remove(labelName);
                Logger.LogInfo($"Unloaded group: {labelName}", LogTag.Addressables);
            }
            else
            {
                Logger.LogWarn(
                    $"Attempted to unload group {labelName} but it was not tracked.",
                    LogTag.Addressables
                );
            }
        }

        /// <summary>
        /// 加载单个资源, 优先从内存中(即已经加载到addressable资源)获取, fallback 到磁盘加载.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="addressableName"></param>
        /// <param name="fuzzySearch">是否开启模糊搜索</param>
        /// <returns></returns>
        public static async Task<T> GetAsset<T>(string addressableName, bool fuzzySearch = true)
            where T : class
        {
            // 1. 尝试从缓存句柄中获取 (即内存中)
            if (mLoadedAssetHandles.TryGetValue(addressableName, out var cachedHandle))
            {
                if (cachedHandle.IsValid() && cachedHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    Logger.LogInfo(
                        $"Resource found in memory: {addressableName}",
                        LogTag.Addressables
                    );
                    return cachedHandle.Result as T;
                }
            }

            // 2. 确定最终加载的Key名称
            string finalKey = addressableName;
            if (fuzzySearch)
            {
                string fuzzyResult = FuzzySearchAddressableName(addressableName);
                if (!string.IsNullOrEmpty(fuzzyResult))
                {
                    finalKey = fuzzyResult;
                    // 如果模糊搜索找到了不同的名字，再次检查缓存
                    if (
                        finalKey != addressableName
                        && mLoadedAssetHandles.TryGetValue(finalKey, out var fuzzyCachedHandle)
                    )
                    {
                        if (
                            fuzzyCachedHandle.IsValid()
                            && fuzzyCachedHandle.Status == AsyncOperationStatus.Succeeded
                        )
                            return fuzzyCachedHandle.Result as T;
                    }
                }
                else
                {
                    Logger.LogError(
                        $"Fuzzy search failed for: {addressableName}, aborting load.",
                        LogTag.Addressables
                    );
                    return null;
                }
            }

            Logger.LogInfo(
                $"Started loading addressable resource : {finalKey}",
                LogTag.Addressables
            );

            // 3. 开始异步加载
            var handle = Addressables.LoadAssetAsync<T>(finalKey);

            // 在任务完成前也可以缓存句柄，防止并发重复加载
            if (!mLoadedAssetHandles.ContainsKey(finalKey))
            {
                mLoadedAssetHandles.Add(finalKey, handle);
            }

            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                Logger.LogInfo(
                    $"Successfully loaded addressable resource : {finalKey}",
                    LogTag.Addressables
                );
                return handle.Result;
            }
            else
            {
                Logger.LogError(
                    $"Failed to load addressable resource : {finalKey}",
                    LogTag.Addressables
                );
                // 失败要清理缓存，允许下次重试
                if (mLoadedAssetHandles.ContainsKey(finalKey))
                    mLoadedAssetHandles.Remove(finalKey);

                return null;
            }
        }

        /// <summary>
        /// 从内存中(即已经加载到addressable资源)获取已经加载的资源, 若未加载则返回空
        /// </summary>
        public static T GetAssetInMemory<T>(string addressableName)
            where T : class
        {
            if (mLoadedAssetHandles.TryGetValue(addressableName, out var handle))
            {
                if (handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded)
                {
                    return handle.Result as T;
                }
            }
            // 可以在这里添加模糊搜索逻辑，但通常 GetAssetInMemory 用于精确获取
            return null;
        }

        /// <summary>
        /// 释放单个资源
        /// </summary>
        /// <param name="addressableName">资源的Addressable Name</param>
        public static void ReleaseAsset(string addressableName)
        {
            if (mLoadedAssetHandles.TryGetValue(addressableName, out var handle))
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
                mLoadedAssetHandles.Remove(addressableName);
                Logger.LogInfo($"Released asset: {addressableName}", LogTag.Addressables);
            }
            else
            {
                Logger.LogWarn(
                    $"Try to release asset {addressableName} but handle not found.",
                    LogTag.Addressables
                );
            }
        }

        /// <summary>
        /// 模糊搜索, 从所有的addressablename 中寻找对应的资源
        /// </summary>
        public static string FuzzySearchAddressableName(string fuzzyName)
        {
            if (string.IsNullOrEmpty(fuzzyName))
                return null;

            var allKeys = new HashSet<string>();
            foreach (var locator in Addressables.ResourceLocators)
            {
                foreach (var key in locator.Keys)
                {
                    // Addressables 的 Key 可能是 string 也可能是 Type 或其他对象，必须转 string
                    if (key is string keyStr && !Guid.TryParse(keyStr, out _)) // 过滤掉GUID类型的Key，只保留可读名称
                    {
                        allKeys.Add(keyStr);
                    }
                }
            }

            if (allKeys.Contains(fuzzyName))
                return fuzzyName;

            var caseInsensitiveMatch = allKeys.FirstOrDefault(k =>
                k.Equals(fuzzyName, StringComparison.OrdinalIgnoreCase)
            );
            if (caseInsensitiveMatch != null)
                return caseInsensitiveMatch;

            var partialMatches = allKeys.Where(k => k.Contains(fuzzyName)).ToList();
            if (partialMatches.Count == 1)
                return partialMatches[0];
            if (partialMatches.Count > 1)
                return null;

            var partialInsensitiveMatches = allKeys
                .Where(k => k.IndexOf(fuzzyName, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
            if (partialInsensitiveMatches.Count == 1)
                return partialInsensitiveMatches[0];

            return null;
        }

        public static void PrintAddressableInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Addressable Resource Loader Info:");
            sb.AppendLine($"  - BuildPath:{Addressables.BuildPath}");
            sb.AppendLine($"  - LibraryPath:{Addressables.LibraryPath}");
            sb.AppendLine($"  - RuntimePath:{Addressables.RuntimePath}");
            sb.AppendLine($"  - BuildReportPath:{Addressables.BuildReportPath}");
            sb.AppendLine($"  - PlayerBuildDataPath:{Addressables.PlayerBuildDataPath}");
            sb.AppendLine(
                $"  - kAddressablesRuntimeDataPath:{Addressables.kAddressablesRuntimeDataPath}"
            );
            sb.AppendLine(
                $"  - kAddressablesRuntimeBuildLogPath:{Addressables.kAddressablesRuntimeBuildLogPath}"
            );
            Logger.LogInfo(sb.ToString(), LogTag.Addressables);
        }

        // 用于缓存单个资源的句柄，Key是Addressable Name (PrimaryKey)
        private static readonly Dictionary<string, AsyncOperationHandle> mLoadedAssetHandles =
            new();

        // 用于缓存资源组的句柄，Key是Group Name (Label)
        private static readonly Dictionary<
            string,
            AsyncOperationHandle<IList<object>>
        > mLoadedGroupHandles = new();
    }
}
