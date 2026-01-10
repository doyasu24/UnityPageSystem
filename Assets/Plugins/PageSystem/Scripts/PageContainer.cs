using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using PageSystem.Internal;
using UnityEngine;
using UnityEngine.Assertions;
using VContainer;
using VContainer.Unity;

namespace PageSystem
{
    /// <summary>
    /// Manages the page navigation stack and handles page loading, transitions, and lifecycle.
    /// Add this component to a GameObject in your scene hierarchy.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Pages are instantiated as children of this GameObject.
    /// </para>
    /// <para>
    /// For most use cases, use <see cref="PagePublisher"/> for navigation instead of calling
    /// PageContainer methods directly.
    /// </para>
    /// </remarks>
    public sealed class PageContainer : MonoBehaviour, IPageHistoryProvider
    {
        private readonly struct PageEntry : IDisposable
        {
            public readonly PageHistoryEntry HistoryEntry;
            private readonly AddressableAssetLoader<GameObject> _assetLoader;
            private readonly bool _isPreloaded;

            public PageEntry(PageHistoryEntry historyEntry, AddressableAssetLoader<GameObject> assetLoader,
                bool isPreloaded)
            {
                HistoryEntry = historyEntry;
                _assetLoader = assetLoader;
                _isPreloaded = isPreloaded;
            }

            public void Dispose()
            {
                Destroy(HistoryEntry.Page.gameObject);
                if (!_isPreloaded)
                    _assetLoader.Dispose();
            }
        }

        private readonly List<PageEntry> _pages = new();
        private readonly Dictionary<string, AddressableAssetLoader<GameObject>> _preloadedAssets = new();
        private CanvasGroup _canvasGroup;
        private CanvasGroup CanvasGroup => _canvasGroup ??= gameObject.GetOrAddComponent<CanvasGroup>();
        private bool _isInTransition;
        private bool _isActivePageStacked;

        // Context用のビュー（実データは_pagesから取得）
        private List<string> OrderedPageIds => _pages.Select(p => p.HistoryEntry.PageId).ToList();

        private Dictionary<string, Page> PagesDictionary =>
            _pages.ToDictionary(p => p.HistoryEntry.PageId, p => p.HistoryEntry.Page);

        /// <summary>
        /// Gets all currently loaded pages.
        /// </summary>
        public IEnumerable<Page> Pages => _pages.Select(p => p.HistoryEntry.Page);

        /// <inheritdoc />
        public PageHistoryEntry CurrentPage => _pages.Count == 0
            ? throw new InvalidOperationException("No pages are loaded.")
            : _pages[^1].HistoryEntry;

        /// <inheritdoc />
        public IReadOnlyList<PageHistoryEntry> History => _pages.Select(entry => entry.HistoryEntry).ToList();

        private IObjectResolver _objectResolver = null!;

        /// <summary>
        /// Injects the VContainer object resolver. Called automatically by VContainer.
        /// </summary>
        /// <param name="objectResolver">The object resolver for dependency injection.</param>
        [Inject]
        public void Inject(IObjectResolver objectResolver)
        {
            _objectResolver = objectResolver;
        }

        private void OnDestroy()
        {
            foreach (var entry in _pages)
                entry.Dispose();
            _pages.Clear();

            foreach (var loader in _preloadedAssets.Values)
                loader.Dispose();
            _preloadedAssets.Clear();
        }

        /// <summary>
        /// Pops pages until the specified destination page is reached.
        /// </summary>
        /// <param name="playAnimation">Whether to play transition animations.</param>
        /// <param name="destinationPageId">The page ID to pop to.</param>
        /// <returns>A task representing the asynchronous pop operation.</returns>
        /// <exception cref="Exception">Thrown when the destination page is not found in the stack.</exception>
        public async UniTask PopAsync(bool playAnimation, string destinationPageId)
        {
            var popCount = 0;
            for (var i = _pages.Count - 1; i >= 0; i--)
            {
                var page = _pages[i];
                if (page.HistoryEntry.PageId == destinationPageId)
                    break;
                popCount++;
            }

            if (popCount == _pages.Count)
                throw new Exception($"The page with id '{destinationPageId}' is not found.");

            await PopAsync(playAnimation, popCount);
        }

        /// <summary>
        /// Pushes a new page onto the navigation stack.
        /// </summary>
        /// <typeparam name="TPage">The type of page to push.</typeparam>
        /// <param name="resourceKey">The Addressable key for the page prefab.</param>
        /// <param name="playAnimation">Whether to play transition animations.</param>
        /// <param name="stack">If true, keeps the current page in memory; if false, destroys it.</param>
        /// <param name="pageId">Optional custom page ID. If null, a GUID is generated.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A tuple containing the page ID and the instantiated page component.</returns>
        public async UniTask<(string pageId, TPage page)> PushAsync<TPage>(
            string resourceKey,
            bool playAnimation,
            bool stack = true,
            string pageId = null,
            CancellationToken cancellationToken = default
        ) where TPage : Page
        {
            Assert.IsNotNull(resourceKey);
            Assert.IsFalse(_isInTransition, "Cannot transition because the screen is already in transition.");

            _isInTransition = true;
            CanvasGroup.interactable = false;

            pageId ??= Guid.NewGuid().ToString();

            var (page, entry) = await LoadPageAsync<TPage>(resourceKey, pageId, cancellationToken);

            var context = PagePushContext.Create(pageId, page, OrderedPageIds, PagesDictionary, _isActivePageStacked);
            await context.PushAsync(playAnimation, (RectTransform)transform, cancellationToken);

            PageEntry? removedEntry = null;
            if (context.ShouldRemoveExitPage)
            {
                context.ExitPage.gameObject.SetActive(false);
                removedEntry = _pages[^1];
                _pages.RemoveAt(_pages.Count - 1);
            }

            _pages.Add(entry);
            _isActivePageStacked = stack;

            _isInTransition = false;
            CanvasGroup.interactable = true;

            removedEntry?.Dispose();

            return (pageId, page);
        }

        /// <summary>
        /// Pops the specified number of pages from the navigation stack.
        /// </summary>
        /// <param name="playAnimation">Whether to play transition animations.</param>
        /// <param name="popCount">The number of pages to pop. Default is 1.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous pop operation.</returns>
        public async UniTask PopAsync(bool playAnimation, int popCount = 1,
            CancellationToken cancellationToken = default)
        {
            Assert.IsTrue(popCount >= 1);
            Assert.IsTrue(_pages.Count >= popCount,
                "Cannot transition because the page count is less than the pop count.");
            Assert.IsFalse(_isInTransition,
                "Cannot transition because the screen is already in transition.");

            _isInTransition = true;
            CanvasGroup.interactable = false;

            var context = PagePopContext.Create(OrderedPageIds, PagesDictionary, popCount);
            await context.PopAsync(playAnimation, cancellationToken);

            var removedEntries = new List<PageEntry>(popCount);
            for (var i = 0; i < popCount; i++)
            {
                var entry = _pages[^1];
                entry.HistoryEntry.Page.gameObject.SetActive(false);
                removedEntries.Add(entry);
                _pages.RemoveAt(_pages.Count - 1);
            }

            _isActivePageStacked = true;

            _isInTransition = false;
            CanvasGroup.interactable = true;

            foreach (var entry in removedEntries)
                entry.Dispose();
        }

        /// <summary>
        /// Preloads a page asset into memory without displaying it.
        /// Useful for reducing load times on frequently accessed pages.
        /// </summary>
        /// <param name="resourceKey">The Addressable key for the page prefab to preload.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous preload operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the resource has already been preloaded.</exception>
        public async UniTask PreloadAsync(string resourceKey, CancellationToken cancellationToken = default)
        {
            if (_preloadedAssets.ContainsKey(resourceKey))
                throw new InvalidOperationException(
                    $"The resource with key \"{resourceKey}\" has already been preloaded.");

            var loader = new AddressableAssetLoader<GameObject>(resourceKey);
            await loader.LoadAssetAsync(cancellationToken);
            _preloadedAssets.Add(resourceKey, loader);
        }

        private async UniTask<(TPage page, PageEntry entry)> LoadPageAsync<TPage>(
            string resourceKey, string pageId, CancellationToken cancellationToken)
            where TPage : Page
        {
            var isPreloaded = _preloadedAssets.TryGetValue(resourceKey, out var loader);
            if (!isPreloaded)
                loader = new AddressableAssetLoader<GameObject>(resourceKey);

            var prefab = await loader.LoadAssetAndGetAsync(cancellationToken);
            var instance = _objectResolver.Instantiate(prefab, transform);
            if (!instance.TryGetComponent<TPage>(out var page))
                page = instance.AddComponent<TPage>();

            var historyEntry = new PageHistoryEntry(resourceKey, pageId, page);
            var entry = new PageEntry(historyEntry, loader, isPreloaded);
            return (page, entry);
        }
    }
}
