namespace FestKasse.Controls;

/// <summary>
/// Base <see cref="ContentPage"/> that automatically calls
/// <see cref="IInitializable.InitializeAsync"/> on <see cref="Page.OnAppearing"/>.
/// Derive pages whose ViewModels implement <see cref="IInitializable"/> to
/// eliminate the repeated boilerplate in every page code-behind.
/// </summary>
public abstract class BaseContentPage<TViewModel> : ContentPage
    where TViewModel : class, IInitializable
{
    protected readonly TViewModel ViewModel;

    protected BaseContentPage(TViewModel viewModel)
    {
        ViewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await ViewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }
}

/// <summary>
/// Marks a ViewModel as having an async initialization step that should be
/// invoked when its page appears.
/// </summary>
public interface IInitializable
{
    Task InitializeAsync();
}
