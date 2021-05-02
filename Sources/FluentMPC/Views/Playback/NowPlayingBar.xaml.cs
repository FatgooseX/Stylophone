﻿using FluentMPC.ViewModels;
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Stylophone.Common.Helpers;
using Stylophone.Common.Services;
using Stylophone.Common.Interfaces;
using Microsoft.Toolkit.Mvvm.DependencyInjection;

namespace FluentMPC.Views
{
    public sealed partial class NowPlayingBar
    {
        public PlaybackViewModel PlaybackViewModel => (PlaybackViewModel)DataContext;

        public NowPlayingBar()
        {
            InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<PlaybackViewModel>();
        }

        private void Volume_PointerWheelChanged(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var delta = e.GetCurrentPoint(this).Properties.MouseWheelDelta;
            PlaybackViewModel.MediaVolume += 5 * delta / 120;
        }

    }
}
