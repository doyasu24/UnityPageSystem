using System.Threading;
using Cysharp.Threading.Tasks;
using PageSystem.Internal;
using UnityEngine;

namespace PageSystem
{
    /// <summary>
    /// Abstract base class for all pages in the page system.
    /// Inherit from this class to create navigable pages.
    /// </summary>
    /// <remarks>
    /// Pages are loaded via Unity Addressables using the key specified by <see cref="ResourceKeyAttribute"/>.
    /// The page lifecycle is managed automatically by <see cref="PageContainer"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// [ResourceKey("MyPage")]
    /// public class MyPage : Page
    /// {
    ///     [SerializeField] private Button _button;
    ///     public Button Button => _button;
    /// }
    /// </code>
    /// </example>
    [DisallowMultipleComponent]
    public abstract class Page : MonoBehaviour
    {
        /// <summary>
        /// The rendering order for page layering. Lower values render behind higher values.
        /// Set this in the Unity Inspector.
        /// </summary>
        [SerializeField] private int _renderingOrder;

        private CanvasGroup _canvasGroup;
        private RectTransform _parentTransform;
        private RectTransform _rectTransform;

        internal void AfterLoad(RectTransform parentTransform)
        {
            _rectTransform = (RectTransform)transform;
            _canvasGroup = gameObject.GetOrAddComponent<CanvasGroup>();
            _parentTransform = parentTransform;
            _rectTransform.FillParent(_parentTransform);

            // Set order of rendering.
            var siblingIndex = 0;
            for (var i = 0; i < _parentTransform.childCount; i++)
            {
                var child = _parentTransform.GetChild(i);
                var childPage = child.GetComponent<Page>();
                siblingIndex = i;
                if (_renderingOrder >= childPage._renderingOrder) continue;
                break;
            }

            _rectTransform.SetSiblingIndex(siblingIndex);
            _canvasGroup.alpha = 0.0f;
        }

        internal void BeforeEnter()
        {
            gameObject.SetActive(true);
            _rectTransform.FillParent(_parentTransform);
            _canvasGroup.alpha = 0.0f;
        }

        internal async UniTask EnterAsync(bool push, bool playAnimation,
            CancellationToken cancellationToken = default)
        {
            _canvasGroup.alpha = 1.0f;

            if (playAnimation)
            {
                var anim = PageTransitionAnimationProvider.Get(push, true);
                await anim.PlayAsync(_rectTransform, cancellationToken);
            }

            _rectTransform.FillParent(_parentTransform);
        }

        internal void BeforeExit()
        {
            gameObject.SetActive(true);
            _rectTransform.FillParent(_parentTransform);
            _canvasGroup.alpha = 1.0f;
        }

        internal async UniTask ExitAsync(bool push, bool playAnimation,
            CancellationToken cancellationToken = default)
        {
            if (playAnimation)
            {
                var anim = PageTransitionAnimationProvider.Get(push, false);
                await anim.PlayAsync(_rectTransform, cancellationToken);
            }

            _canvasGroup.alpha = 0.0f;
        }

        internal void AfterExit() => gameObject.SetActive(false);
    }
}
