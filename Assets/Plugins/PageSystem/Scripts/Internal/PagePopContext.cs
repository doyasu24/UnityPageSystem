using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace PageSystem.Internal
{
    internal readonly struct PagePopContext
    {
        public IReadOnlyList<string> ExitPageIds { get; }
        public IReadOnlyList<Page> ExitPages { get; }
        public Page ExitPage => ExitPages[0];
        public Page EnterPage { get; }

        private PagePopContext(
            IReadOnlyList<string> exitPageIds,
            IReadOnlyList<Page> exitPages,
            Page enterPage
        )
        {
            ExitPageIds = exitPageIds;
            ExitPages = exitPages;
            EnterPage = enterPage;
        }

        public async UniTask PopAsync(bool playAnimation, CancellationToken cancellationToken)
        {
            foreach (var page in ExitPages) page.BeforeExit();
            if (EnterPage != null) EnterPage.BeforeEnter();
            await UniTask.WhenAll(
                ExitPage.ExitAsync(false, playAnimation, cancellationToken),
                EnterPage != null
                    ? EnterPage.EnterAsync(false, playAnimation, cancellationToken)
                    : UniTask.CompletedTask
            );
            foreach (var exitModal in ExitPages) exitModal.AfterExit();
        }

        public static PagePopContext Create(
            IReadOnlyList<string> orderedPageIds,
            IReadOnlyDictionary<string, Page> pages,
            int popCount
        )
        {
            var exitPageIds = new List<string>();
            var exitPages = new List<Page>();

            for (var i = orderedPageIds.Count - 1; i >= orderedPageIds.Count - popCount; i--)
            {
                var id = orderedPageIds[i];
                exitPageIds.Add(id);
                exitPages.Add(pages[id]);
            }

            var enterIndex = orderedPageIds.Count - popCount - 1;
            var enterId = enterIndex >= 0 ? orderedPageIds[enterIndex] : null;
            var enter = enterId != null ? pages[enterId] : null;

            return new PagePopContext(exitPageIds, exitPages, enter);
        }
    }
}
