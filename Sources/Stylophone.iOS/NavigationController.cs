// This file has been autogenerated from a class added in the UI designer.

using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using ObjCRuntime;
using Stylophone.Common.Interfaces;
using Stylophone.Common.ViewModels;
using Stylophone.iOS.Services;
using Stylophone.iOS.ViewControllers;
using UIKit;

namespace Stylophone.iOS
{
    [Register("NavigationController")]
    public class NavigationController : UINavigationController, IUINavigationControllerDelegate
	{
        private PlaybackViewController _playbackViewController;
        private CompactPlaybackView _compactView;
        private NSLayoutConstraint _compactViewBottomConstraint;
        private NSLayoutConstraint _compactViewLeftConstraint;
        private NSLayoutConstraint _compactViewRightConstraint;

        public NavigationController (IntPtr handle) : base (handle)
		{
		}

        void ReleaseDesignerOutlets()
        {
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            var concreteNavService = (NavigationService)Ioc.Default.GetRequiredService<INavigationService>();
            concreteNavService.NavigationController = this;

            Delegate = this;

            var storyboard = concreteNavService.GetStoryboardForViewModel(typeof(PlaybackViewModelBase));
            _playbackViewController = storyboard.InstantiateInitialViewController() as PlaybackViewController;

            // Add the compact view of the playback VC as an overlay
            _compactView = _playbackViewController.CompactView;
            _compactView.TranslatesAutoresizingMaskIntoConstraints = false;
            View.AddSubview(_compactView);

            _compactView.VolumeButton.PrimaryActionTriggered += (s, e) => _playbackViewController.ShowVolumePopover(_compactView.VolumeButton, this);

            // Add the playbackVC itself to the Navigation Service's known VCs so it can be reused later
            concreteNavService.AddViewControllerToNavigationStack(_playbackViewController);

            // Add some layout constraints to affix it to the bottom
            var constraints = new List<NSLayoutConstraint>();

            _compactViewBottomConstraint = _compactView.BottomAnchor.ConstraintEqualTo(View.BottomAnchor, -16);
            _compactViewLeftConstraint = _compactView.LeftAnchor.ConstraintEqualTo(View.LeftAnchor, 32);
            _compactViewRightConstraint = _compactView.RightAnchor.ConstraintEqualTo(View.RightAnchor, -32);

            constraints.Add(_compactViewBottomConstraint);
            constraints.Add(_compactViewLeftConstraint);
            constraints.Add(_compactViewRightConstraint);
            constraints.Add(_compactView.HeightAnchor.ConstraintEqualTo(128));

            NSLayoutConstraint.ActivateConstraints(constraints.ToArray());

            // Navigate to the queue
            concreteNavService.Navigate<QueueViewModel>();
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();

            // Move elements depending on available screen estate
            if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Compact ||
                TraitCollection.VerticalSizeClass == UIUserInterfaceSizeClass.Compact)
            {
                _compactViewLeftConstraint.Constant = -8;
                _compactViewRightConstraint.Constant = 8;

                if (_compactViewBottomConstraint.Constant < 128)
                {
                    if (TraitCollection.VerticalSizeClass == UIUserInterfaceSizeClass.Compact)
                        _compactViewBottomConstraint.Constant = 64;
                    else
                        _compactViewBottomConstraint.Constant = 0;
                } 
            }
            else
            {
                _compactViewLeftConstraint.Constant = 32;
                _compactViewRightConstraint.Constant = -32;

                if (_compactViewBottomConstraint.Constant >= 0 && _compactViewBottomConstraint.Constant < 128)
                    _compactViewBottomConstraint.Constant = -16;
            }
        }


        [Export("navigationController:willShowViewController:animated:")] 
        public void WillShowViewController(UINavigationController navigationController, [Transient] UIViewController viewController, bool animated)
        {
            // If the navigation occurred through the back button instead of the sidebar,
            // The NavigationService doesn't intervene and can't call the Navigated event.
            // We call it manually here instead.
            var navService = Ioc.Default.GetRequiredService<INavigationService>() as NavigationService;
            navService.Navigate(navService.CurrentPageViewModelType);

            if (navService.CurrentPageViewModelType == typeof(PlaybackViewModelBase))
            {
                View.LayoutIfNeeded();
                UIView.Animate(0.2, 0, UIViewAnimationOptions.CurveEaseIn, () =>
                {
                    _compactViewBottomConstraint.Constant = 128;
                    View.LayoutIfNeeded();
                }, null);    
            }
            else
            {
                View.LayoutIfNeeded();
                UIView.Animate(0.2, 0, UIViewAnimationOptions.CurveEaseOut, () =>
                {
                    _compactViewBottomConstraint.Constant = View.Frame.Width < 425 ? 0 : -16;
                    View.LayoutIfNeeded();
                }, null);
            }
        }
    }
}
