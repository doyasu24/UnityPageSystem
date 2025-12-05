using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace PageSystem.Internal
{
    internal readonly struct PagePushContext
    {
        public string EnterPageId { get; }
        public Page EnterPage { get; }
        public string ExitPageId { get; }
        public Page ExitPage { get; }
        private bool IsExitPageStacked { get; }
        public bool ShouldRemoveExitPage => ExitPage != null && !IsExitPageStacked;

        private PagePushContext(
            string enterPageId,
            Page enterPage,
            string exitPageId,
            Page exitPage,
            bool isExitPageStacked
        )
        {
            EnterPageId = enterPageId;
            EnterPage = enterPage;
            ExitPageId = exitPageId;
            ExitPage = exitPage;
            IsExitPageStacked = isExitPageStacked;
        }

        public async UniTask PushAsync(bool playAnimation, RectTransform containerTransform,
            CancellationToken cancellationToken)
        {
            EnterPage.AfterLoad(containerTransform);
            if (ExitPage != null) ExitPage.BeforeExit();
            EnterPage.BeforeEnter();
            await UniTask.WhenAll(
                ExitPage != null
                    ? ExitPage.ExitAsync(true, playAnimation, cancellationToken)
                    : UniTask.CompletedTask,
                EnterPage.EnterAsync(true, playAnimation, cancellationToken)
            );
            if (ExitPage != null) ExitPage.AfterExit();
        }

        public static PagePushContext Create(
            string pageId,
            Page enterPage,
            List<string> orderedPageIds,
            Dictionary<string, Page> pages,
            bool isExitPageStacked
        )
        {
            var hasExit = orderedPageIds.Count > 0;
            var exitPageId = hasExit ? orderedPageIds[^1] : null;
            var exitPage = hasExit ? pages[exitPageId] : null;
            return new PagePushContext(pageId, enterPage, exitPageId, exitPage, isExitPageStacked);
        }
    }
}
