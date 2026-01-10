using System;
using PageSystem.Examples.Pages.Initial;
using VContainer;
using VContainer.Unity;

namespace PageSystem.Examples
{
    public class PageLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // Register PageSystem
            builder.RegisterComponentInHierarchy<PageContainer>()
                .AsSelf().AsImplementedInterfaces();
            builder.RegisterPage();

            // Configure animations
            var duration = TimeSpan.FromSeconds(0.15f);
            PageTransitionAnimationProvider.PagePushEnter = PageTransitionAnimations.AlphaIn(duration);
            PageTransitionAnimationProvider.PagePushExit = PageTransitionAnimations.Wait(duration);
            PageTransitionAnimationProvider.PagePopEnter = PageTransitionAnimations.Wait(duration);
            PageTransitionAnimationProvider.PagePopExit = PageTransitionAnimations.AlphaOut(duration);

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
}
