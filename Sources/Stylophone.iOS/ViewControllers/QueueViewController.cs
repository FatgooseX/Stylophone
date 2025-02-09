// This file has been autogenerated from a class added in the UI designer.

using System;
using System.Linq;
using Foundation;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Stylophone.Common.Services;
using Stylophone.Common.ViewModels;
using Stylophone.iOS.Helpers;
using Strings = Stylophone.Localization.Strings.Resources;
using UIKit;
using System.Collections.Generic;
using System.ComponentModel;

namespace Stylophone.iOS.ViewControllers
{
	public partial class QueueViewController : UITableViewController, IViewController<QueueViewModel>
	{
        private MPDConnectionService _mpdService;

        public QueueViewController (IntPtr handle) : base (handle)
		{
            _mpdService = Ioc.Default.GetRequiredService<MPDConnectionService>();
        }

        public QueueViewModel ViewModel => Ioc.Default.GetRequiredService<QueueViewModel>();
		public PropertyBinder<QueueViewModel> Binder => new(ViewModel);

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            ViewModel.PropertyChanged += UpdateListOnPlaylistVersionChange;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            NavigationItem.LargeTitleDisplayMode = UINavigationItemLargeTitleDisplayMode.Always;
            Title = QueueViewModel.GetHeader();

            var negateBoolTransformer = NSValueTransformer.GetValueTransformer(nameof(ReverseBoolValueTransformer));
            Binder.Bind<bool>(EmptyView, "hidden", nameof(ViewModel.IsSourceEmpty),
                valueTransformer: negateBoolTransformer);

            NavigationItem.RightBarButtonItem = CreateSettingsButton();

            var trackDataSource = new TrackTableViewDataSource(TableView, ViewModel.Source, GetRowContextMenu, GetRowSwipeActions);
            TableView.DataSource = trackDataSource;
            TableView.Delegate = trackDataSource;

            _mpdService.SongChanged += ScrollToPlayingSong;
        }

        private void UpdateListOnPlaylistVersionChange(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.PlaylistVersion))
            {
                ScrollToPlayingSong();
            }
        }

        private void ScrollToPlayingSong(object sender = null, SongChangedEventArgs e = null)
        {
            // Scroll to currently playing song
            var playing = ViewModel.Source.Where(t => t.IsPlaying).FirstOrDefault();
            
            if (playing != null)
                UIApplication.SharedApplication.BeginInvokeOnMainThread(() =>
                TableView.ScrollToRow(NSIndexPath.FromRowSection(ViewModel.Source.IndexOf(playing), 0),
                    UITableViewScrollPosition.Middle, true));
        }

        private UIMenu GetRowContextMenu(NSIndexPath indexPath)
        {
            // The common commands take a list of objects
            var trackList = new List<object>();

            if (TableView.IndexPathsForSelectedRows == null)
            {
                trackList.Add(ViewModel?.Source[indexPath.Row]);
            }
            else
            {
                trackList = TableView.IndexPathsForSelectedRows.Select(indexPath => ViewModel?.Source[indexPath.Row])
                .ToList<object>();
            }

            var playAction = Binder.GetCommandAction(Strings.ContextMenuPlay, "play", ViewModel.PlayTrackCommand, trackList);
            var albumAction = Binder.GetCommandAction(Strings.ContextMenuViewAlbum, "opticaldisc", ViewModel.ViewAlbumCommand, trackList);
            var playlistAction = Binder.GetCommandAction(Strings.ContextMenuAddToPlaylist, "music.note.list", ViewModel.AddToPlayListCommand, trackList);

            var removeAction = Binder.GetCommandAction(Strings.ContextMenuRemoveFromQueue, "trash", ViewModel.RemoveFromQueueCommand, trackList);
            removeAction.Attributes = UIMenuElementAttributes.Destructive;

            return UIMenu.Create(new[] { playAction, albumAction, playlistAction, removeAction });
        }

        private UISwipeActionsConfiguration GetRowSwipeActions(NSIndexPath indexPath, bool isLeadingSwipe)
        {
            // The common commands take a list of objects
            var trackList = new List<object>();
            trackList.Add(ViewModel?.Source[indexPath.Row]);
            
            var action = isLeadingSwipe ? Binder.GetContextualAction(UIContextualActionStyle.Normal,Strings.ContextMenuPlay, ViewModel.PlayTrackCommand, trackList)
                : Binder.GetContextualAction(UIContextualActionStyle.Destructive, Strings.ContextMenuRemoveFromQueue, ViewModel.RemoveFromQueueCommand, trackList);

            return UISwipeActionsConfiguration.FromActions(new[] { action });
        }

        private UIBarButtonItem CreateSettingsButton()
        {
            var addQueueAction = Binder.GetCommandAction(Strings.ContextMenuAddQueueToPlaylist, "sdcard", ViewModel.SaveQueueCommand);
            var clearQueueAction = Binder.GetCommandAction(Strings.ContextMenuClearQueue, "trash", ViewModel.ClearQueueCommand);
            clearQueueAction.Attributes = UIMenuElementAttributes.Destructive;

            var barButtonMenu = UIMenu.Create(new[] { addQueueAction, clearQueueAction });
            return new UIBarButtonItem(UIImage.GetSystemImage("ellipsis.circle"), barButtonMenu);
        }
    }
}
