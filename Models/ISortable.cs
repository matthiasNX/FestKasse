namespace FestKasse.Models;

/// <summary>
/// Marks a model as having a <see cref="SortOrder"/> property that controls
/// its display position in a list. Used by
/// <see cref="FestKasse.ViewModels.SortableListViewModelBase{T}"/>
/// to provide generic move-up / move-down / re-index logic.
/// </summary>
public interface ISortable
{
    int SortOrder { get; set; }
}
