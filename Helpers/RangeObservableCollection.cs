using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace FestKasse.Helpers;

/// <summary>
/// An <see cref="ObservableCollection{T}"/> that supports bulk replacement via
/// <see cref="ReplaceRange"/>, firing a single <see cref="NotifyCollectionChangedAction.Reset"/>
/// instead of one notification per item.  This prevents the UI from performing N
/// incremental layout passes when switching categories.
/// </summary>
public sealed class RangeObservableCollection<T> : ObservableCollection<T>
{
    /// <summary>
    /// Replaces all current items with <paramref name="newItems"/> and raises
    /// a single <see cref="NotifyCollectionChangedAction.Reset"/> event.
    /// </summary>
    public void ReplaceRange(IEnumerable<T> newItems)
    {
        Items.Clear();
        foreach (var item in newItems)
            Items.Add(item);

        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(Count)));
    }
}
