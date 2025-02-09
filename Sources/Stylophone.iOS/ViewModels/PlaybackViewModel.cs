﻿using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using SkiaSharp.Views.iOS;
using Stylophone.Common.Helpers;
using Stylophone.Common.Interfaces;
using Stylophone.Common.Services;
using Stylophone.Common.ViewModels;
using Stylophone.iOS.Services;
using UIKit;

namespace Stylophone.iOS.ViewModels
{
    public class PlaybackViewModel : PlaybackViewModelBase
    {
        public PlaybackViewModel(INavigationService navigationService, INotificationService notificationService, IDispatcherService dispatcherService, IInteropService interop, MPDConnectionService mpdService, TrackViewModelFactory trackVmFactory, LocalPlaybackViewModel localPlayback) :
            base(navigationService, notificationService, dispatcherService, interop, mpdService, trackVmFactory, localPlayback)
        {
            //Application.Current.LeavingBackground += CurrentOnLeavingBackground;

            ((NavigationService)_navigationService).Navigated += (s, e) =>
                _dispatcherService.ExecuteOnUIThreadAsync(() => {
                    //ShowTrackName = _navigationService.CurrentPageViewModelType != typeof(PlaybackViewModelBase);
                });
        }

        public override Task SwitchToCompactViewAsync(EventArgs obj)
        {
            throw new NotImplementedException();
        }
    }
}
