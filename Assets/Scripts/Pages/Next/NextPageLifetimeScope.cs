using System.Threading;
using Cysharp.Threading.Tasks;
using VContainer;
using VContainer.Unity;

namespace PageSystem.Examples.Pages.Next
{
    public class NextPageLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<NextPage>().UnderTransform(transform);
            builder.RegisterEntryPoint<NextPageEntryPoint>();
        }
    }

    public class NextPageEntryPoint : IAsyncStartable
    {
        private readonly PagePublisher _pagePublisher;
        private readonly NextPage _nextPage;

        [Inject]
        public NextPageEntryPoint(PagePublisher pagePublisher, NextPage nextPage)
        {
            _pagePublisher = pagePublisher;
            _nextPage = nextPage;
        }

        public async UniTask StartAsync(CancellationToken cancellationToken)
        {
            await foreach (var _ in _nextPage.OnBackClickedAsync(cancellationToken))
            {
                _pagePublisher.Pop();
            }
        }
    }
}
