# UnityPageSystem

provides a simple page navigation system for Unity uGUI using [Addressables](https://docs.unity3d.com/Packages/com.unity.addressables@2.7/manual/index.html), [VContainer](https://github.com/hadashiA/VContainer), and [UniTask](https://github.com/Cysharp/UniTask), featuring push/pop stack transitions with customizable animations.

## Requirements

- Unity 2022.3 or later
- [Addressables](https://docs.unity3d.com/Packages/com.unity.addressables@latest) (1.22+ / 2.x)
- [VContainer](https://github.com/hadashiA/VContainer) (1.16+)
- [UniTask](https://github.com/Cysharp/UniTask) (2.5+)

## Installation

Add to Package Manager:
```
https://github.com/doyasu24/UnityPageSystem.git?path=Assets/Plugins/PageSystem#v0.1.1
```

## Architecture

- Pages are Prefabs registered in Addressables
- Each page inherits from the `Page` base class
- `PageContainer` manages page instantiation, destruction, and transition animations (pages are spawned as children)
- `PagePublisher` provides Push/Pop operations for navigation
- `IPageStackProvider` provides read-only access to the navigation stack

### Scene Hierarchy

```
Scene
├── PageLifetimeScope    ← User implements (registers PageSystem)
└── Canvas
    └── PageContainer    ← Attach PageContainer component (or attach to Canvas)
        ├── Page1        (instantiated at runtime)
        ├── Page2
        └── ...
```

## Implementation Patterns

PageSystem provides two implementation patterns for specifying Addressable keys:

### Type-based Pattern

Use `PagePublisher.Push<TPage>()` for type-safe navigation. Requires:
1. Page class inherits from `Page`
2. `[ResourceKey("AddressableKey")]` attribute on the class

```csharp
// Page definition
[ResourceKey("MainMenu")]
public class MainMenuPage : Page { }

// Navigation
_pagePublisher.Push<MainMenuPage>();
```

### String-based Pattern

Use `PagePublisher.Push(resourceKey)` for dynamic page loading. Only requires the Addressable key string.

```csharp
// Navigation with string key (no [ResourceKey] attribute needed)
_pagePublisher.Push("MainMenu");

// Useful for dynamic scenarios
string pageKey = GetPageKeyFromConfig();
_pagePublisher.Push(pageKey);
```

## Implementation Examples

This example demonstrates a simple two-page navigation: `InitialPage` → `NextPage`.

### Scene LifetimeScope

```csharp
using System;
using PageSystem;
using VContainer;
using VContainer.Unity;

public class PageLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // Register PageSystem
        builder.RegisterComponentInHierarchy<PageContainer>()
            .AsImplementedInterfaces();
        builder.RegisterPage();

        // Configure animations
        var duration = TimeSpan.FromSeconds(0.15f);
        PageTransitionAnimationProvider.PagePushEnter = PageTransitionAnimations.AlphaIn(duration);
        PageTransitionAnimationProvider.PagePushExit = PageTransitionAnimations.Wait(duration);
        PageTransitionAnimationProvider.PagePopEnter = PageTransitionAnimations.AlphaIn(duration);
        PageTransitionAnimationProvider.PagePopExit = PageTransitionAnimations.Wait(duration);

        // Slide from left to center animation.
        // PageTransitionAnimationProvider.PagePushEnter = PageTransitionAnimations.LinearFromLeftToCenter(duration);
        // PageTransitionAnimationProvider.PagePushExit = PageTransitionAnimations.Wait(duration);
        // PageTransitionAnimationProvider.PagePopEnter = PageTransitionAnimations.Wait(duration);
        // PageTransitionAnimationProvider.PagePopExit = PageTransitionAnimations.LinearFromCenterToLeft(duration);

        // Register the startup entry point for opening the initial page.
        builder.RegisterEntryPoint<Startup>();
    }
}

public class Startup : IStartable
{
    private readonly PagePublisher _pagePublisher;

    [Inject]
    public Startup(PagePublisher pagePublisher)
    {
        _pagePublisher = pagePublisher;
    }

    public void Start()
    {
        // Open the main page without animation.
        _pagePublisher.Push<InitialPage>(playAnimation: false); // Type-based
        // _pagePublisher.Push("InitialPage", playAnimation: false); // Or string-based
    }
}
```

### Page Prefab

In this example, each page Prefab has:
- A `Page` subclass (e.g., `InitialPage`)
- A `LifetimeScope` subclass (e.g., `InitialPageLifetimeScope`)

#### Type-based (with ResourceKey)

**InitialPage.cs**

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using PageSystem;
using UnityEngine;
using UnityEngine.UI;

// [ResourceKey] is REQUIRED for PagePublisher.Push<InitialPage>()
[ResourceKey("InitialPage")]
public class InitialPage : Page
{
    [SerializeField] private Button _nextPageButton = null!;

    public IUniTaskAsyncEnumerable<AsyncUnit> OnNextClickedAsync(CancellationToken cancellationToken) =>
        _nextPageButton.OnClickAsAsyncEnumerable(cancellationToken);
}
```

#### String-based (without ResourceKey)

For pages loaded only via string key, the `[ResourceKey]` attribute is not required:

```csharp
using PageSystem;

// No [ResourceKey] attribute needed when using PagePublisher.Push("InitialPage")
public class InitialPage : Page
{
    // Page implementation
}
```

```csharp
// Load using string key
_pagePublisher.Push("InitialPage");
```

#### Page LifetimeScope

**InitialPageLifetimeScope.cs**

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using PageSystem;
using VContainer;
using VContainer.Unity;

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
```

## Animation Configuration

Configure animations via `PageTransitionAnimationProvider`:

| Property        | When                                  |
|-----------------|---------------------------------------|
| `PagePushEnter` | New page entering during Push         |
| `PagePushExit`  | Current page leaving during Push      |
| `PagePopEnter`  | Previous page re-entering during Pop  |
| `PagePopExit`   | Current page leaving during Pop       |

**Built-in Animations:**

```csharp
// No animation (instant)
PageTransitionAnimations.Nop

// Wait without visual change
PageTransitionAnimations.Wait(TimeSpan duration)

// Fade animations
PageTransitionAnimations.AlphaIn(TimeSpan duration)   // 0 -> 1
PageTransitionAnimations.AlphaOut(TimeSpan duration)  // 1 -> 0
PageTransitionAnimations.Alpha(float from, float to, TimeSpan duration)

// Slide animations
PageTransitionAnimations.LinearFromLeftToCenter(TimeSpan duration)
PageTransitionAnimations.LinearFromCenterToLeft(TimeSpan duration)
PageTransitionAnimations.Linear(Vector2 fromAnchor, Vector2 toAnchor, TimeSpan duration)
```

**Custom Animation:**

Implement `IPageTransitionAnimation`:

```csharp
public class MyAnimation : IPageTransitionAnimation
{
    public async UniTask PlayAsync(RectTransform rectTransform, CancellationToken cancellationToken)
    {
        // Your animation logic
    }
}
```

## License

MIT License
