using System.Threading;
using Cysharp.Threading.Tasks;
using PageSystem.Examples.Pages.Next;
using VContainer;
using VContainer.Unity;

namespace PageSystem.Examples.Pages.Initial
{
    public class InitialPageLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<InitialPage>().UnderTransform(transform);
            builder.RegisterEntryPoint<InitialPageEntryPoint>();
        }
    }

    public class InitialPageEntryPoint : IAsyncStartable
    {
        private readonly PagePublisher _pagePublisher;
        private readonly InitialPage _initialPage;

        [Inject]
        public InitialPageEntryPoint(PagePublisher pagePublisher, InitialPage initialPage)
        {
            _pagePublisher = pagePublisher;
            _initialPage = initialPage;
        }

        public async UniTask StartAsync(CancellationToken cancellationToken)
        {
            await foreach (var _ in _initialPage.OnNextClickedAsync(cancellationToken))
            {
                // Push: Add new page to stack (keeps current page underneath)
                _pagePublisher.Push<NextPage>();

                // Push without stacking: Replace current page (destroys current page)
                // _pagePublisher.Push<NextPage>(isStack: false);

                // Pop: Remove current page and return to previous
                // _pagePublisher.Pop();
            }
        }
    }
}
