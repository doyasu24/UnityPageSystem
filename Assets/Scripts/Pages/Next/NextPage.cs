using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace PageSystem.Examples.Pages.Next
{
    [ResourceKey("NextPage")]
    public class NextPage : Page
    {
        [SerializeField] private Button _backButton = null!;

        public IUniTaskAsyncEnumerable<AsyncUnit> OnBackClickedAsync(CancellationToken cancellationToken) =>
            _backButton.OnClickAsAsyncEnumerable(cancellationToken);
    }
}
