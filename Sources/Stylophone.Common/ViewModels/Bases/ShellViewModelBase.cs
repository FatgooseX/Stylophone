﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;

using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using MpcNET;
using MpcNET.Commands.Database;
using MpcNET.Commands.Queue;
using MpcNET.Commands.Reflection;
using MpcNET.Commands.Status;
using MpcNET.Tags;
using MpcNET.Types;
using Stylophone.Common.Interfaces;
using Stylophone.Common.Services;
using Stylophone.Localization.Strings;

namespace Stylophone.Common.ViewModels
{
    public abstract class ShellViewModelBase : ViewModelBase
    {
        protected INavigationService _navigationService;
        protected INotificationService _notificationService;
        protected MPDConnectionService _mpdService;

        public ShellViewModelBase(INavigationService navigationService, INotificationService notificationService, IDispatcherService dispatcherService, MPDConnectionService mpdService):
            base(dispatcherService)
        {
            _navigationService = navigationService;
            _notificationService = notificationService;
            _mpdService = mpdService;

            // First View, use that to initialize our DispatcherService
            _dispatcherService.Initialize();

            ((NotificationServiceBase)_notificationService).InAppNotificationRequested += ShowInAppNotification;
            ((NavigationServiceBase)_navigationService).Navigated += OnFrameNavigated;

            TryUpdatePlaylists();
            _mpdService.PlaylistsChanged += (s, e) => TryUpdatePlaylists();
            _mpdService.StatusChanged += async (s, e) =>
            {
                await _dispatcherService.ExecuteOnUIThreadAsync(() => OnPropertyChanged(nameof(IsServerUpdating)));
            };
        }

        private bool _isBackEnabled;
        public bool IsBackEnabled
        {
            get { return _isBackEnabled; }
            set { Set(ref _isBackEnabled, value); }
        }

        public bool IsServerUpdating => _mpdService.CurrentStatus.UpdatingDb != -1;

        private string _shellHeader;
        public string HeaderText
        {
            get { return _shellHeader; }
            set { Set(ref _shellHeader, value); }
        }

        private ICommand _loadedCommand;
        public ICommand LoadedCommand => _loadedCommand ?? (_loadedCommand = new RelayCommand(OnLoaded));

        private ICommand _navigateCommand;
        public ICommand NavigateCommand => _navigateCommand ?? (_navigateCommand = new RelayCommand<object>(OnItemInvoked));

        private ICommand _shuffleTracksCommand;
        public ICommand AddRandomTracksCommand => _shuffleTracksCommand ?? (_shuffleTracksCommand = new RelayCommand(() => QueueRandomTracks(5)));

        protected abstract void ShowInAppNotification(object sender, InAppNotificationRequestedEventArgs e);
        protected abstract void OnLoaded();
        protected abstract void OnItemInvoked(object item);
        protected abstract void UpdatePlaylistNavigation();

        private void OnFrameNavigated(object sender, CoreNavigationEventArgs e)
        {
            IsBackEnabled = _navigationService.CanGoBack;

            var viewModelType = e.NavigationTarget;
            if (viewModelType == null) return;

            // Use some reflection magic to get the static Header text for this ViewModel
            var headerMethod = viewModelType.GetMethod(nameof(ViewModelBase.GetHeader), BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            HeaderText = (string)headerMethod?.Invoke(null, null) ?? "";
        }

        public async Task<IList<object>> SearchAsync(string text)
        {
            var suitableItems = new List<object>();

            if (text.Trim().Length > 0)
                suitableItems.Add(string.Format(Resources.SearchGoToDetail, text));

            if (text.Length > 2)
            {
                var response = await _mpdService.SafelySendCommandAsync(new SearchCommand(FindTags.Title, text));

                if (response != null)
                {
                    foreach (var f in response)
                        suitableItems.Add(f);
                }
            }

            return suitableItems;
        }

        private void QueueRandomTracks(int count)
        {
            _notificationService.ShowInAppNotification(Resources.RandomTracksInProgress, false);
            _ = Task.Run(async () =>
            {
                var response = await _mpdService.SafelySendCommandAsync(new StatsCommand());
                var songs = int.Parse(response["songs"]); // Total songs on the server

                var commandList = new CommandList();
                while (count > 0)
                {
                    count--;
                    // Pick a song n°, and queue it directly with searchadd
                    var r = new Random().Next(0, songs-1);
                    commandList.Add(new SearchAddCommand(MpdTags.Title, "", r, r + 1));
                }

                await _mpdService.SafelySendCommandAsync(commandList);
                _notificationService.ShowInAppNotification(Resources.NotificationAddedToQueue);
            });
        }

        public async Task HandleSearchRequestAsync(string text, object chosenSuggestion)
        {
            if (chosenSuggestion != null && chosenSuggestion is IMpdFile)
            {
                var response = await _mpdService.SafelySendCommandAsync(new AddCommand((chosenSuggestion as IMpdFile).Path));

                if (response != null)
                    _notificationService.ShowInAppNotification(Resources.NotificationAddedToQueue);
            }
            else
            {
                // Navigate to detailed search page
                await _dispatcherService.ExecuteOnUIThreadAsync(() => _navigationService.Navigate<SearchResultsViewModel>(text));
                HeaderText = string.Format(Resources.SearchResultsFor, text);
            }
        }

        private void TryUpdatePlaylists()
        {
            _dispatcherService.ExecuteOnUIThreadAsync(() => {
                try
                {
                    UpdatePlaylistNavigation();
                }
                catch (Exception e)
                {
                    //TODO localize
                    _notificationService.ShowInAppNotification($"Updating Playlist Navigation failed: {e.Message}", false);
                }
            });
        }
    }
}
