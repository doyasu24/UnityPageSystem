using System;
using System.Collections.Generic;
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
    public sealed class PageContainer : MonoBehaviour
    {
        private readonly Dictionary<string, AddressableAssetLoader<GameObject>> _assetLoaders = new();
        private readonly List<string> _orderedPageIds = new();
        private readonly Dictionary<string, Page> _pages = new();
        private CanvasGroup _canvasGroup;
        private CanvasGroup CanvasGroup => _canvasGroup ??= gameObject.GetOrAddComponent<CanvasGroup>();
        private bool _isInTransition;
        private bool _isActivePageStacked;

        /// <summary>
        /// Gets all currently loaded pages.
        /// </summary>
        public IEnumerable<Page> Pages => _pages.Values;

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
            foreach (var pageId in _orderedPageIds)
            {
                var page = _pages[pageId];
                var loader = _assetLoaders[pageId];
                Destroy(page.gameObject);
                loader.Dispose();
            }

            _assetLoaders.Clear();
            _pages.Clear();
            _orderedPageIds.Clear();
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
            for (var i = _orderedPageIds.Count - 1; i >= 0; i--)
            {
                var pageId = _orderedPageIds[i];
                if (pageId == destinationPageId)
                    break;

                popCount++;
            }

            if (popCount == _orderedPageIds.Count)
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

            var (page, loader) = await LoadPageAsync<TPage>(resourceKey, cancellationToken);
            _assetLoaders.Add(pageId, loader);

            var context = PagePushContext.Create(pageId, page, _orderedPageIds, _pages, _isActivePageStacked);
            await context.PushAsync(playAnimation, (RectTransform)transform, cancellationToken);

            if (context.ShouldRemoveExitPage)
            {
                context.ExitPage.gameObject.SetActive(false);
                _pages.Remove(context.ExitPageId);
                _orderedPageIds.Remove(context.ExitPageId);
            }

            _pages.Add(context.EnterPageId, context.EnterPage);
            _orderedPageIds.Add(context.EnterPageId);
            _isActivePageStacked = stack;

            _isInTransition = false;
            CanvasGroup.interactable = true;

            AfterPush(context);

            return (pageId, page);
        }

        private void AfterPush(PagePushContext context)
        {
            if (!context.ShouldRemoveExitPage) return;
            Destroy(context.ExitPage.gameObject);
            var loader = _assetLoaders[context.ExitPageId];
            loader.Dispose();
            _assetLoaders.Remove(context.ExitPageId);
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

            var context = PagePopContext.Create(_orderedPageIds, _pages, popCount);
            await context.PopAsync(playAnimation, cancellationToken);
            for (var i = 0; i < context.ExitPageIds.Count; i++)
            {
                var exitPage = context.ExitPages[i];
                var exitPageId = context.ExitPageIds[i];
                exitPage.gameObject.SetActive(false);
                _pages.Remove(exitPageId);
                _orderedPageIds.RemoveAt(_orderedPageIds.Count - 1);
            }

            _isActivePageStacked = true;

            _isInTransition = false;
            CanvasGroup.interactable = true;

            AfterPop(context);
        }

        private void AfterPop(PagePopContext context)
        {
            for (var i = 0; i < context.ExitPageIds.Count; i++)
            {
                var unusedPageId = context.ExitPageIds[i];
                var unusedPage = context.ExitPages[i];
                Destroy(unusedPage.gameObject);
                var loader = _assetLoaders[unusedPageId];
                loader.Dispose();
                _assetLoaders.Remove(unusedPageId);
            }
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
            if (_assetLoaders.ContainsKey(resourceKey))
                throw new InvalidOperationException(
                    $"The resource with key \"${resourceKey}\" has already been preloaded.");

            var loader = new AddressableAssetLoader<GameObject>(resourceKey);
            _assetLoaders.Add(resourceKey, loader);
            await loader.LoadAssetAsync(cancellationToken);
        }

        private async UniTask<(TPage, AddressableAssetLoader<GameObject>)> LoadPageAsync<TPage>(
            string resourceKey, CancellationToken cancellationToken)
            where TPage : Page
        {
            var loader = new AddressableAssetLoader<GameObject>(resourceKey);
            var prefab = await loader.LoadAssetAndGetAsync(cancellationToken);
            var instance = _objectResolver.Instantiate(prefab, transform);
            if (!instance.TryGetComponent<TPage>(out var page))
                page = instance.AddComponent<TPage>();
            return (page, loader);
        }
    }
}
