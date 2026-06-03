using FestKasse.ViewModels;

namespace FestKasse.Views;

public partial class QrDisplayPage : ContentPage
{
    public QrDisplayPage(QrDisplayViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
