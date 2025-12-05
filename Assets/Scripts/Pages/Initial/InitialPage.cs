using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace PageSystem.Examples.Pages.Initial
{
    // [ResourceKey] is REQUIRED for PagePublisher.Push<InitialPage>()
    [ResourceKey("InitialPage")]
    public class InitialPage : Page
    {
        [SerializeField] private Button _nextPageButton = null!;

        public IUniTaskAsyncEnumerable<AsyncUnit> OnNextClickedAsync(CancellationToken cancellationToken) =>
            _nextPageButton.OnClickAsAsyncEnumerable(cancellationToken);
    }
}
