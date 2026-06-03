using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FestKasse.Models;

namespace FestKasse.ViewModels;

/// <summary>
/// Abstract base ViewModel for pages that manage a sortable list of items
/// (Articles, Categories). Provides the common <see cref="Items"/> collection,
/// editing-state flags, sort-order stepper, and move-up / move-down helpers.
/// Derived classes use <c>[RelayCommand]</c> thin wrappers over the protected
/// base methods to keep the source generator happy.
/// </summary>
public abstract class SortableListViewModelBase<T> : ObservableObject
    where T : ISortable
{
    // ── Collection ──────────────────────────────────────────────────────────

    public ObservableCollection<T> Items { get; } = new();

    // ── Editing state ───────────────────────────────────────────────────────

    private bool _isEditing;
    public bool IsEditing
    {
        get => _isEditing;
        protected set => SetProperty(ref _isEditing, value);
    }

    private bool _isNewItem;
    public bool IsNewItem
    {
        get => _isNewItem;
        protected set => SetProperty(ref _isNewItem, value);
    }

    // ── Sort order stepper ──────────────────────────────────────────────────

    private int _editSortOrder;
    public int EditSortOrder
    {
        get => _editSortOrder;
        set => SetProperty(ref _editSortOrder, value);
    }

    /// <summary>Increments the sort-order field by one. Wire up with <c>[RelayCommand]</c> in the derived class.</summary>
    protected void IncreaseSortOrderBase() => EditSortOrder++;

    /// <summary>Decrements the sort-order field by one (min 0). Wire up with <c>[RelayCommand]</c> in the derived class.</summary>
    protected void DecreaseSortOrderBase()
    {
        if (EditSortOrder > 0)
            EditSortOrder--;
    }

    // ── Cancel helper ───────────────────────────────────────────────────────

    /// <summary>Resets editing state. Call from the derived class's <c>CancelEdit</c> command.</summary>
    protected void CancelEditBase()
    {
        IsEditing = false;
        IsNewItem = false;
    }

    // ── Move helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Moves <paramref name="item"/> one position earlier in <see cref="Items"/>
    /// and re-indexes <see cref="ISortable.SortOrder"/> on all items.
    /// Returns <c>true</c> if the move was possible.
    /// </summary>
    protected bool MoveItemUp(T item)
    {
        var index = Items.IndexOf(item);
        if (index <= 0) return false;
        Items.Move(index, index - 1);
        UpdateSortOrderBase();
        return true;
    }

    /// <summary>
    /// Moves <paramref name="item"/> one position later in <see cref="Items"/>
    /// and re-indexes <see cref="ISortable.SortOrder"/> on all items.
    /// Returns <c>true</c> if the move was possible.
    /// </summary>
    protected bool MoveItemDown(T item)
    {
        var index = Items.IndexOf(item);
        if (index < 0 || index >= Items.Count - 1) return false;
        Items.Move(index, index + 1);
        UpdateSortOrderBase();
        return true;
    }

    /// <summary>Re-assigns <see cref="ISortable.SortOrder"/> based on the current collection order.</summary>
    protected void UpdateSortOrderBase()
    {
        for (int i = 0; i < Items.Count; i++)
            Items[i].SortOrder = i;
    }
}
