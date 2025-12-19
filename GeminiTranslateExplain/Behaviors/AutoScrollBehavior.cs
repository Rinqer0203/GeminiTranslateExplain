using System.Collections.Specialized;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using ListBox = System.Windows.Controls.ListBox;
using ItemsControl = System.Windows.Controls.ItemsControl;
using System.Windows.Threading;

namespace GeminiTranslateExplain
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
            }
            else
            {
                listBox.Loaded -= OnLoaded;
                listBox.Unloaded -= OnUnloaded;
                DetachItemsSourceChanged(listBox);
                DetachFromCollection(listBox);
            }
        }

        private static void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is ListBox listBox && GetIsEnabled(listBox))
                AttachToCollection(listBox);
        }

        private static void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is ListBox listBox)
                DetachFromCollection(listBox);
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
                    ScrollToEnd(listBox);
                };
                collection.CollectionChanged += handler;
                SetSubscription(listBox, new CollectionSubscription(collection, handler));
            }
            else if (listBox.Items is INotifyCollectionChanged items)
            {
                NotifyCollectionChangedEventHandler handler = (_, _) =>
                {
                    RefreshItemSubscriptions(listBox);
                    ScrollToEnd(listBox);
                };
                items.CollectionChanged += handler;
                SetSubscription(listBox, new CollectionSubscription(items, handler));
            }

            RefreshItemSubscriptions(listBox);
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

        private static void ScrollToEnd(ListBox listBox)
        {
            if (listBox.Items.Count == 0)
                return;

            var lastItem = listBox.Items[listBox.Items.Count - 1];
            listBox.Dispatcher.BeginInvoke(
                () => listBox.ScrollIntoView(lastItem),
                DispatcherPriority.Background);
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
                    PropertyChangedEventHandler handler = (_, _) => ScrollToEnd(listBox);
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
    }
}
