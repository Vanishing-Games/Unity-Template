using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Core
{
    public class PrintAddressableInfoCommand : ITriggerCommand
    {
        public bool Execute()
        {
            AddressableResourceLoader.PrintAddressableInfo();
            return true;
        }
    }

    public class LoadAddressableCommand<T> : IAsyncCommand<T>
        where T : Object
    {
        public LoadAddressableCommand(string addressableName)
        {
            mAddress = addressableName;
        }

        public Task<T> ExecuteAsync()
        {
            return AddressableResourceLoader.GetAsset<T>(mAddress);
        }

        public T Execute()
        {
            var handle = AddressableResourceLoader.GetAsset<T>(mAddress);
            return handle.Result;
        }

        private string mAddress;
    }
}
