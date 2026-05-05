using System.Collections.Specialized;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ListBox = System.Windows.Controls.ListBox;
using ItemsControl = System.Windows.Controls.ItemsControl;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using KeyEventHandler = System.Windows.Input.KeyEventHandler;
using ScrollChangedEventArgs = System.Windows.Controls.ScrollChangedEventArgs;
using ScrollViewer = System.Windows.Controls.ScrollViewer;
using System.Windows.Threading;

namespace QuickExplain
{
    public static class AutoScrollBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(AutoScrollBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        private static readonly DependencyProperty SubscriptionProperty =
            DependencyProperty.RegisterAttached(
                "Subscription",
                typeof(CollectionSubscription),
                typeof(AutoScrollBehavior),
                new PropertyMetadata(null));

        private static readonly DependencyProperty ItemsSourceChangedHandlerProperty =
            DependencyProperty.RegisterAttached(
                "ItemsSourceChangedHandler",
                typeof(EventHandler),
                typeof(AutoScrollBehavior),
                new PropertyMetadata(null));

        private static readonly DependencyProperty ScrollViewerProperty =
            DependencyProperty.RegisterAttached(
                "ScrollViewer",
                typeof(ScrollViewer),
                typeof(AutoScrollBehavior),
                new PropertyMetadata(null));

        private static readonly DependencyProperty IsPinnedToBottomProperty =
            DependencyProperty.RegisterAttached(
                "IsPinnedToBottom",
                typeof(bool),
                typeof(AutoScrollBehavior),
                new PropertyMetadata(true));

        private static readonly DependencyProperty IsAutoScrollingProperty =
            DependencyProperty.RegisterAttached(
                "IsAutoScrolling",
                typeof(bool),
                typeof(AutoScrollBehavior),
                new PropertyMetadata(false));

        private static readonly DependencyProperty IsScrollViewerAttachPendingProperty =
            DependencyProperty.RegisterAttached(
                "IsScrollViewerAttachPending",
                typeof(bool),
                typeof(AutoScrollBehavior),
                new PropertyMetadata(false));

        private static readonly DependencyProperty IsScrollToEndPendingProperty =
            DependencyProperty.RegisterAttached(
                "IsScrollToEndPending",
                typeof(bool),
                typeof(AutoScrollBehavior),
                new PropertyMetadata(false));

        private const double BottomTolerance = 2.0;

        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ListBox listBox || e.NewValue is not bool enabled)
                return;

            if (enabled)
            {
                listBox.Loaded += OnLoaded;
                listBox.Unloaded += OnUnloaded;
                AttachItemsSourceChanged(listBox);
                AttachToCollection(listBox);
                AttachScrollViewer(listBox);
            }
            else
            {
                listBox.Loaded -= OnLoaded;
                listBox.Unloaded -= OnUnloaded;
                DetachItemsSourceChanged(listBox);
                DetachFromCollection(listBox);
                DetachScrollViewer(listBox);
            }
        }

        private static void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is ListBox listBox && GetIsEnabled(listBox))
            {
                AttachToCollection(listBox);
                AttachScrollViewer(listBox);
            }
        }

        private static void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is ListBox listBox)
            {
                DetachFromCollection(listBox);
                DetachScrollViewer(listBox);
            }
        }

        private static void AttachItemsSourceChanged(ListBox listBox)
        {
            var handler = new EventHandler((_, _) => AttachToCollection(listBox));
            SetItemsSourceChangedHandler(listBox, handler);

            var descriptor = DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(ItemsControl));
            descriptor?.AddValueChanged(listBox, handler);
        }

        private static void DetachItemsSourceChanged(ListBox listBox)
        {
            var handler = GetItemsSourceChangedHandler(listBox);
            if (handler == null)
                return;

            var descriptor = DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(ItemsControl));
            descriptor?.RemoveValueChanged(listBox, handler);
            SetItemsSourceChangedHandler(listBox, null);
        }

        private static void AttachToCollection(ListBox listBox)
        {
            DetachFromCollection(listBox);

            if (listBox.ItemsSource is INotifyCollectionChanged collection)
            {
                NotifyCollectionChangedEventHandler handler = (_, _) =>
                {
                    RefreshItemSubscriptions(listBox);
                    ScrollToEndIfPinned(listBox);
                };
                collection.CollectionChanged += handler;
                SetSubscription(listBox, new CollectionSubscription(collection, handler));
            }
            else if (listBox.Items is INotifyCollectionChanged items)
            {
                NotifyCollectionChangedEventHandler handler = (_, _) =>
                {
                    RefreshItemSubscriptions(listBox);
                    ScrollToEndIfPinned(listBox);
                };
                items.CollectionChanged += handler;
                SetSubscription(listBox, new CollectionSubscription(items, handler));
            }

            RefreshItemSubscriptions(listBox);
            SetIsPinnedToBottom(listBox, true);
            ScrollToEnd(listBox);
        }

        private static void DetachFromCollection(ListBox listBox)
        {
            var subscription = GetSubscription(listBox);
            if (subscription?.Collection != null && subscription.Handler != null)
            {
                subscription.Collection.CollectionChanged -= subscription.Handler;
            }
            ClearItemSubscriptions(listBox);
            SetSubscription(listBox, null);
        }

        private static void AttachScrollViewer(ListBox listBox)
        {
            if (GetScrollViewer(listBox) != null || GetIsScrollViewerAttachPending(listBox))
                return;

            SetIsScrollViewerAttachPending(listBox, true);
            listBox.Dispatcher.BeginInvoke(() =>
            {
                SetIsScrollViewerAttachPending(listBox, false);
                if (!GetIsEnabled(listBox))
                    return;

                if (GetScrollViewer(listBox) != null)
                    return;

                var scrollViewer = FindVisualChild<ScrollViewer>(listBox);
                if (scrollViewer == null)
                    return;

                scrollViewer.ScrollChanged += OnScrollChanged;
                listBox.AddHandler(UIElement.PreviewMouseWheelEvent, new MouseWheelEventHandler(OnPreviewMouseWheel), true);
                listBox.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), true);
                SetScrollViewer(listBox, scrollViewer);
                SetIsPinnedToBottom(listBox, IsAtBottom(scrollViewer));
            }, DispatcherPriority.Loaded);
        }

        private static void DetachScrollViewer(ListBox listBox)
        {
            var scrollViewer = GetScrollViewer(listBox);
            if (scrollViewer != null)
            {
                scrollViewer.ScrollChanged -= OnScrollChanged;
            }

            listBox.RemoveHandler(UIElement.PreviewMouseWheelEvent, new MouseWheelEventHandler(OnPreviewMouseWheel));
            listBox.RemoveHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown));
            SetScrollViewer(listBox, null);
            SetIsScrollViewerAttachPending(listBox, false);
            SetIsScrollToEndPending(listBox, false);
        }

        private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                return;

            var listBox = sender as ListBox ?? (sender is DependencyObject dependencyObject ? FindAncestor<ListBox>(dependencyObject) : null);
            if (listBox == null)
                return;

            var scrollViewer = GetScrollViewer(listBox);
            if (scrollViewer == null)
                return;

            if (e.Delta > 0)
            {
                SetIsPinnedToBottom(listBox, false);
                SetIsAutoScrolling(listBox, false);
                return;
            }

            if (!IsAtBottom(scrollViewer))
                SetIsPinnedToBottom(listBox, false);
        }

        private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            var listBox = sender as ListBox ?? (sender is DependencyObject dependencyObject ? FindAncestor<ListBox>(dependencyObject) : null);
            if (listBox == null)
                return;

            if (e.Key is Key.Up or Key.PageUp or Key.Home)
            {
                SetIsPinnedToBottom(listBox, false);
                SetIsAutoScrolling(listBox, false);
            }
        }

        private static void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (sender is not ScrollViewer scrollViewer)
                return;

            var listBox = FindAncestor<ListBox>(scrollViewer);
            if (listBox == null)
                return;

            if (GetIsAutoScrolling(listBox))
            {
                SetIsPinnedToBottom(listBox, IsAtBottom(scrollViewer));
                return;
            }

            if (e.ExtentHeightChange > 0 && GetIsPinnedToBottom(listBox))
            {
                ScrollToEnd(listBox);
                return;
            }

            SetIsPinnedToBottom(listBox, IsAtBottom(scrollViewer));
        }

        private static void ScrollToEndIfPinned(ListBox listBox)
        {
            if (GetIsPinnedToBottom(listBox))
                ScrollToEnd(listBox);
        }

        private static void ScrollToEnd(ListBox listBox)
        {
            var scrollViewer = GetScrollViewer(listBox);
            if (scrollViewer == null)
                return;

            if (GetIsScrollToEndPending(listBox))
                return;

            SetIsScrollToEndPending(listBox, true);
            listBox.Dispatcher.BeginInvoke(
                () =>
                {
                    SetIsScrollToEndPending(listBox, false);
                    if (!GetIsPinnedToBottom(listBox))
                        return;

                    SetIsAutoScrolling(listBox, true);
                    scrollViewer.ScrollToEnd();
                    listBox.Dispatcher.BeginInvoke(
                        () => SetIsAutoScrolling(listBox, false),
                        DispatcherPriority.Background);
                },
                DispatcherPriority.Background);
        }

        private static bool IsAtBottom(ScrollViewer scrollViewer)
        {
            return scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight - BottomTolerance;
        }

        private static CollectionSubscription? GetSubscription(DependencyObject obj)
        {
            return (CollectionSubscription?)obj.GetValue(SubscriptionProperty);
        }

        private static void SetSubscription(DependencyObject obj, CollectionSubscription? value)
        {
            obj.SetValue(SubscriptionProperty, value);
        }

        private static EventHandler? GetItemsSourceChangedHandler(DependencyObject obj)
        {
            return (EventHandler?)obj.GetValue(ItemsSourceChangedHandlerProperty);
        }

        private static void SetItemsSourceChangedHandler(DependencyObject obj, EventHandler? value)
        {
            obj.SetValue(ItemsSourceChangedHandlerProperty, value);
        }

        private static ScrollViewer? GetScrollViewer(DependencyObject obj)
        {
            return (ScrollViewer?)obj.GetValue(ScrollViewerProperty);
        }

        private static void SetScrollViewer(DependencyObject obj, ScrollViewer? value)
        {
            obj.SetValue(ScrollViewerProperty, value);
        }

        private static bool GetIsPinnedToBottom(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsPinnedToBottomProperty);
        }

        private static void SetIsPinnedToBottom(DependencyObject obj, bool value)
        {
            obj.SetValue(IsPinnedToBottomProperty, value);
        }

        private static bool GetIsAutoScrolling(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsAutoScrollingProperty);
        }

        private static void SetIsAutoScrolling(DependencyObject obj, bool value)
        {
            obj.SetValue(IsAutoScrollingProperty, value);
        }

        private static bool GetIsScrollViewerAttachPending(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsScrollViewerAttachPendingProperty);
        }

        private static void SetIsScrollViewerAttachPending(DependencyObject obj, bool value)
        {
            obj.SetValue(IsScrollViewerAttachPendingProperty, value);
        }

        private static bool GetIsScrollToEndPending(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsScrollToEndPendingProperty);
        }

        private static void SetIsScrollToEndPending(DependencyObject obj, bool value)
        {
            obj.SetValue(IsScrollToEndPendingProperty, value);
        }

        private sealed class CollectionSubscription
        {
            public INotifyCollectionChanged Collection { get; }
            public NotifyCollectionChangedEventHandler Handler { get; }

            public CollectionSubscription(INotifyCollectionChanged collection, NotifyCollectionChangedEventHandler handler)
            {
                Collection = collection;
                Handler = handler;
            }
        }

        private static readonly DependencyProperty ItemSubscriptionsProperty =
            DependencyProperty.RegisterAttached(
                "ItemSubscriptions",
                typeof(Dictionary<INotifyPropertyChanged, PropertyChangedEventHandler>),
                typeof(AutoScrollBehavior),
                new PropertyMetadata(null));

        private static void RefreshItemSubscriptions(ListBox listBox)
        {
            ClearItemSubscriptions(listBox);

            var subscriptions = new Dictionary<INotifyPropertyChanged, PropertyChangedEventHandler>();
            foreach (var item in listBox.Items)
            {
                if (item is INotifyPropertyChanged notifyItem)
                {
                    PropertyChangedEventHandler handler = (_, _) => ScrollToEndIfPinned(listBox);
                    notifyItem.PropertyChanged += handler;
                    subscriptions[notifyItem] = handler;
                }
            }

            listBox.SetValue(ItemSubscriptionsProperty, subscriptions);
        }

        private static void ClearItemSubscriptions(ListBox listBox)
        {
            if (listBox.GetValue(ItemSubscriptionsProperty) is Dictionary<INotifyPropertyChanged, PropertyChangedEventHandler> subscriptions)
            {
                foreach (var pair in subscriptions)
                    pair.Key.PropertyChanged -= pair.Value;
            }
            listBox.SetValue(ItemSubscriptionsProperty, null);
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T match)
                    return match;

                var descendant = FindVisualChild<T>(child);
                if (descendant != null)
                    return descendant;
            }

            return null;
        }

        private static T? FindAncestor<T>(DependencyObject child) where T : DependencyObject
        {
            var current = child;
            while (current != null)
            {
                if (current is T match)
                    return match;

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }
    }
}
