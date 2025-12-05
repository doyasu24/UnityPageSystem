using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace PageSystem.Internal
{
    internal class AddressableAssetLoader<T> : IDisposable where T : Object
    {
        private readonly string _addressableKey;
        private T _asset;

        public AddressableAssetLoader(string addressableKey)
        {
            _addressableKey = addressableKey;
        }

        public async UniTask LoadAssetAsync(CancellationToken cancellationToken)
        {
            if (_asset) return;
            var asset = await Addressables.LoadAssetAsync<T>(_addressableKey)
                .ToUniTask(cancellationToken: cancellationToken);
            if (!asset) throw new InvalidOperationException($"Failed to load asset: {_addressableKey}");
            _asset = asset;
        }

        public T GetAsset()
        {
            if (!_asset) throw new InvalidOperationException($"Asset not loaded: {_addressableKey}");
            return _asset!;
        }

        public async UniTask<T> LoadAssetAndGetAsync(CancellationToken cancellationToken)
        {
            if (_asset) return GetAsset();
            await LoadAssetAsync(cancellationToken);
            return GetAsset();
        }

        public void Dispose()
        {
            if (_asset)
            {
                Addressables.Release(_asset);
                _asset = null;
            }

            Resources.UnloadUnusedAssets();
        }
    }
}
