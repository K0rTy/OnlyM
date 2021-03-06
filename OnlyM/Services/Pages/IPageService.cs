﻿namespace OnlyM.Services.Pages
{
    using System;
    using System.Threading.Tasks;
    using System.Windows;
    using Models;
    using Unosquare.FFME.Events;

    internal interface IPageService
    {
        event EventHandler<NavigationEventArgs> NavigationEvent;

        event EventHandler MediaMonitorChangedEvent;

        event EventHandler<MediaEventArgs> MediaChangeEvent;

        event EventHandler<PositionChangedRoutedEventArgs> MediaPositionChangedEvent;

        event EventHandler MediaWindowOpenedEvent;

        event EventHandler MediaWindowClosedEvent;

        bool AllowMediaWindowToClose { get; }

        string OperatorPageName { get; }

        string SettingsPageName { get; }

        void GotoOperatorPage();

        void GotoSettingsPage();

        FrameworkElement GetPage(string pageName);
        
        void OpenMediaWindow();

        bool ApplicationIsClosing { get; }

        bool IsMediaWindowVisible { get; }
        
        Task StartMedia(MediaItem mediaItemToStart, MediaItem currentMediaItem, bool startFromPaused);

        Task StopMediaAsync(MediaItem mediaItem);

        Task PauseMediaAsync(MediaItem mediaItem);

        void CacheImageItem(MediaItem mediaItem);

        Guid CurrentMediaId { get; }

        bool IsMediaItemActive { get; }
    }
}
