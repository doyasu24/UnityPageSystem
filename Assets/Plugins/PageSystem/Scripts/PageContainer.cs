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
    public sealed class PageContainer : MonoBehaviour, IPageStackProvider
    {
        private readonly List<PageStackItem> _pageStack = new();
        private readonly Dictionary<string, AddressableAssetLoader<GameObject>> _preloadedAssets = new();
        private CanvasGroup _canvasGroup;
        private CanvasGroup CanvasGroup => _canvasGroup ??= gameObject.GetOrAddComponent<CanvasGroup>();
        private bool _isInTransition;

        private PageStackInfo[] _stackCache;
        private bool _isStackCacheDirty = true;

        /// <inheritdoc />
        public PageStackInfo CurrentPage => _pageStack.Count == 0
            ? throw new InvalidOperationException("No pages are loaded.")
            : _pageStack[^1].ToStackInfo();

        /// <inheritdoc />
        public IReadOnlyList<PageStackInfo> Stack
        {
            get
            {
                if (!_isStackCacheDirty) return _stackCache;
                _stackCache = _pageStack.Select(e => e.ToStackInfo()).ToArray();
                _isStackCacheDirty = false;
                return _stackCache;
            }
        }

        /// <inheritdoc />
        public int PageCount => _pageStack.Count;

        /// <summary>
        /// Gets all currently loaded page instances.
        /// </summary>
        public IEnumerable<Page> Pages => _pageStack.Select(e => e.PageInstance);

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
            foreach (var entry in _pageStack)
                entry.Dispose();
            _pageStack.Clear();

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
            for (var i = _pageStack.Count - 1; i >= 0; i--)
            {
                var entry = _pageStack[i];
                if (entry.PageId == destinationPageId)
                    break;
                popCount++;
            }

            if (popCount == _pageStack.Count)
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

            var (page, entry) = await LoadPageAsync<TPage>(resourceKey, pageId, stack, cancellationToken);

            var currentEntry = _pageStack.Count > 0 ? _pageStack[^1] : default;
            var exitPage = currentEntry.PageInstance;
            var shouldRemoveExitPage = exitPage != null && !currentEntry.Stacked;
            await ExecutePushTransitionAsync(page, exitPage, shouldRemoveExitPage, playAnimation, cancellationToken);

            PageStackItem? removedEntry = null;
            if (shouldRemoveExitPage)
            {
                removedEntry = _pageStack[^1];
                _pageStack.RemoveAt(_pageStack.Count - 1);
            }

            _pageStack.Add(entry);
            _isStackCacheDirty = true;

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
            Assert.IsTrue(_pageStack.Count >= popCount,
                "Cannot transition because the page count is less than the pop count.");
            Assert.IsFalse(_isInTransition,
                "Cannot transition because the screen is already in transition.");

            _isInTransition = true;
            CanvasGroup.interactable = false;

            var removedEntries = new List<PageStackItem>(popCount);
            for (var i = 0; i < popCount; i++)
                removedEntries.Add(_pageStack[^(i + 1)]);

            var enterIndex = _pageStack.Count - popCount - 1;
            var enterPage = enterIndex >= 0 ? _pageStack[enterIndex].PageInstance : null;

            await ExecutePopTransitionAsync(removedEntries, enterPage, playAnimation, cancellationToken);

            _pageStack.RemoveRange(_pageStack.Count - popCount, popCount);
            _isStackCacheDirty = true;

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

        private async UniTask<(TPage page, PageStackItem entry)> LoadPageAsync<TPage>(
            string resourceKey, string pageId, bool stacked, CancellationToken cancellationToken)
            where TPage : Page
        {
            var isPreloaded = _preloadedAssets.TryGetValue(resourceKey, out var loader);
            if (!isPreloaded)
                loader = new AddressableAssetLoader<GameObject>(resourceKey);

            var prefab = await loader.LoadAssetAndGetAsync(cancellationToken);
            var instance = _objectResolver.Instantiate(prefab, transform);
            if (!instance.TryGetComponent<TPage>(out var page))
                page = instance.AddComponent<TPage>();

            var entry = new PageStackItem(resourceKey, pageId, page, stacked, loader, isPreloaded);
            return (page, entry);
        }

        private async UniTask ExecutePushTransitionAsync(
            Page enterPage,
            Page exitPage,
            bool shouldDeactivateExitPage,
            bool playAnimation,
            CancellationToken cancellationToken)
        {
            enterPage.AfterLoad((RectTransform)transform);
            if (exitPage != null) exitPage.BeforeExit();
            enterPage.BeforeEnter();
            await UniTask.WhenAll(
                exitPage != null
                    ? exitPage.ExitAsync(true, playAnimation, cancellationToken)
                    : UniTask.CompletedTask,
                enterPage.EnterAsync(true, playAnimation, cancellationToken)
            );
            if (exitPage != null) exitPage.AfterExit();
            if (shouldDeactivateExitPage) exitPage.gameObject.SetActive(false);
        }

        private async UniTask ExecutePopTransitionAsync(
            IReadOnlyList<PageStackItem> exitEntries,
            Page enterPage,
            bool playAnimation,
            CancellationToken cancellationToken)
        {
            foreach (var entry in exitEntries) entry.PageInstance.BeforeExit();
            if (enterPage != null) enterPage.BeforeEnter();
            await UniTask.WhenAll(
                exitEntries[0].PageInstance.ExitAsync(false, playAnimation, cancellationToken),
                enterPage != null
                    ? enterPage.EnterAsync(false, playAnimation, cancellationToken)
                    : UniTask.CompletedTask
            );
            foreach (var entry in exitEntries)
            {
                entry.PageInstance.AfterExit();
                entry.PageInstance.gameObject.SetActive(false);
            }
        }
    }
}
