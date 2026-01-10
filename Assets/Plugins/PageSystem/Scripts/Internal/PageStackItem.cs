using System;
using UnityEngine;

namespace PageSystem.Internal
{
    /// <summary>
    /// Internal structure representing a page entry in the navigation stack.
    /// Manages the page instance and its associated resources.
    /// </summary>
    internal readonly struct PageStackItem : IDisposable
    {
        /// <summary>
        /// The Addressable resource key for the page prefab.
        /// </summary>
        public readonly string ResourceKey;

        /// <summary>
        /// The unique identifier for this page instance.
        /// </summary>
        public readonly string PageId;

        /// <summary>
        /// The instantiated page component.
        /// </summary>
        public readonly Page PageInstance;

        /// <summary>
        /// Indicates whether this page should be retained in the stack when a new page is pushed.
        /// </summary>
        public readonly bool Stacked;

        private readonly AddressableAssetLoader<GameObject> _assetLoader;
        private readonly bool _isPreloaded;

        /// <summary>
        /// Initializes a new instance of the <see cref="PageStackItem"/> struct.
        /// </summary>
        /// <param name="resourceKey">The Addressable resource key.</param>
        /// <param name="pageId">The unique page identifier.</param>
        /// <param name="pageInstance">The instantiated page component.</param>
        /// <param name="stacked">Whether the page should be retained when a new page is pushed.</param>
        /// <param name="assetLoader">The asset loader used to load the page prefab.</param>
        /// <param name="isPreloaded">Whether the asset was preloaded.</param>
        public PageStackItem(
            string resourceKey,
            string pageId,
            Page pageInstance,
            bool stacked,
            AddressableAssetLoader<GameObject> assetLoader,
            bool isPreloaded)
        {
            ResourceKey = resourceKey;
            PageId = pageId;
            PageInstance = pageInstance;
            Stacked = stacked;
            _assetLoader = assetLoader;
            _isPreloaded = isPreloaded;
        }

        /// <summary>
        /// Converts this entry to a <see cref="PageStackInfo"/> for external consumers.
        /// </summary>
        /// <returns>A read-only view of this page entry.</returns>
        public PageStackInfo ToStackInfo() => new(ResourceKey, PageId, Stacked);

        /// <summary>
        /// Releases the resources associated with this page entry.
        /// Destroys the page GameObject and releases the asset loader if not preloaded.
        /// </summary>
        public void Dispose()
        {
            UnityEngine.Object.Destroy(PageInstance.gameObject);
            if (!_isPreloaded)
                _assetLoader.Dispose();
        }
    }
}
