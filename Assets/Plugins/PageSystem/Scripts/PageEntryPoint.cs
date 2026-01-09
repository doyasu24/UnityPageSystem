using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using VContainer;
using VContainer.Unity;

namespace PageSystem
{
    public class PageEntryPoint : IInitializable, IDisposable
    {
        private class TransitionScope : IDisposable
        {
            private static bool _isTransition;
            private TransitionScope() => _isTransition = true;
            public void Dispose() => _isTransition = false;

            public static async UniTask<IDisposable> GetAsync(CancellationToken cancellationToken)
            {
                if (_isTransition)
                    await UniTask.WaitUntil(() => !_isTransition, cancellationToken: cancellationToken);
                _isTransition = true;
                return new TransitionScope();
            }
        }

        private readonly PageContainer _pageContainer;
        private readonly LifetimeScope _lifetimeScope;
        private readonly PagePublisher _pageSubscriber;
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        [Inject]
        public PageEntryPoint(
            PageContainer pageContainer,
            LifetimeScope lifetimeScope,
            PagePublisher pageSubscriber)
        {
            _pageContainer = pageContainer;
            _lifetimeScope = lifetimeScope;
            _pageSubscriber = pageSubscriber;
        }

        public void Initialize()
        {
            _pageSubscriber.PushMessage
                .Skip(1) // Skip the initial default value
                .ForEachAwaitAsync(m => PushAsync(m, _cancellationTokenSource.Token).SuppressCancellationThrow(),
                    _cancellationTokenSource.Token)
                .SuppressCancellationThrow()
                .Forget();

            _pageSubscriber.PopMessage
                .Skip(1) // Skip the initial default value
                .ForEachAwaitAsync(m => PopAsync(m, _cancellationTokenSource.Token).SuppressCancellationThrow(),
                    _cancellationTokenSource.Token)
                .SuppressCancellationThrow()
                .Forget();
        }

        private async UniTask PushAsync(PagePushMessage message, CancellationToken cancellationToken)
        {
            using var transitionScope = await TransitionScope.GetAsync(cancellationToken);
            using var lifetimeScope = LifetimeScope.EnqueueParent(_lifetimeScope);
            await _pageContainer.PushAsync<Page>(message.ResourceKey, message.PlayAnimation, message.Stack,
                null, cancellationToken);
        }

        private async UniTask PopAsync(PagePopMessage message, CancellationToken cancellationToken)
        {
            if (!_pageContainer.Pages.Any()) return;
            using var transitionScope = await TransitionScope.GetAsync(cancellationToken);
            await _pageContainer.PopAsync(message.PlayAnimation, cancellationToken: cancellationToken);
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }
    }
}
