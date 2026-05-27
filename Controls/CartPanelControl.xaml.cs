using System.Collections;
using System.Windows.Input;

namespace FestKasse.Controls;

public partial class CartPanelControl : ContentView
{
    public static readonly BindableProperty CartItemsProperty =
        BindableProperty.Create(nameof(CartItems), typeof(IEnumerable), typeof(CartPanelControl));

    public static readonly BindableProperty HasCartItemsProperty =
        BindableProperty.Create(nameof(HasCartItems), typeof(bool), typeof(CartPanelControl), false,
            propertyChanged: (b, _, _) => ((CartPanelControl)b).OnPropertyChanged(nameof(HasNoCartItems)));

    public static readonly BindableProperty ShowCompleteButtonProperty =
        BindableProperty.Create(nameof(ShowCompleteButton), typeof(bool), typeof(CartPanelControl));

    public static readonly BindableProperty TotalProperty =
        BindableProperty.Create(nameof(Total), typeof(decimal), typeof(CartPanelControl));

    public static readonly BindableProperty GivenAmountProperty =
        BindableProperty.Create(nameof(GivenAmount), typeof(decimal), typeof(CartPanelControl), 0m, BindingMode.TwoWay);

    public static readonly BindableProperty ChangeProperty =
        BindableProperty.Create(nameof(Change), typeof(decimal), typeof(CartPanelControl));

    public static readonly BindableProperty IsPaymentPanelVisibleProperty =
        BindableProperty.Create(nameof(IsPaymentPanelVisible), typeof(bool), typeof(CartPanelControl));

    public static readonly BindableProperty NoteTilesProperty =
        BindableProperty.Create(nameof(NoteTiles), typeof(IEnumerable), typeof(CartPanelControl));

    public static readonly BindableProperty CoinTilesProperty =
        BindableProperty.Create(nameof(CoinTiles), typeof(IEnumerable), typeof(CartPanelControl));

    public static readonly BindableProperty CartListMaxHeightProperty =
        BindableProperty.Create(nameof(CartListMaxHeight), typeof(double), typeof(CartPanelControl), 150.0);

    // Commands
    public static readonly BindableProperty CompleteOrderCommandProperty =
        BindableProperty.Create(nameof(CompleteOrderCommand), typeof(ICommand), typeof(CartPanelControl));

    public static readonly BindableProperty ClearCartCommandProperty =
        BindableProperty.Create(nameof(ClearCartCommand), typeof(ICommand), typeof(CartPanelControl));

    public static readonly BindableProperty TogglePaymentPanelCommandProperty =
        BindableProperty.Create(nameof(TogglePaymentPanelCommand), typeof(ICommand), typeof(CartPanelControl));

    public static readonly BindableProperty AddDenominationCommandProperty =
        BindableProperty.Create(nameof(AddDenominationCommand), typeof(ICommand), typeof(CartPanelControl));

    public static readonly BindableProperty RemoveDenominationCommandProperty =
        BindableProperty.Create(nameof(RemoveDenominationCommand), typeof(ICommand), typeof(CartPanelControl));

    public static readonly BindableProperty ResetPaymentCommandProperty =
        BindableProperty.Create(nameof(ResetPaymentCommand), typeof(ICommand), typeof(CartPanelControl));

    // CLR
    public IEnumerable? CartItems { get => (IEnumerable?)GetValue(CartItemsProperty); set => SetValue(CartItemsProperty, value); }
    public bool HasCartItems { get => (bool)GetValue(HasCartItemsProperty); set => SetValue(HasCartItemsProperty, value); }
    public bool HasNoCartItems => !HasCartItems;
    public bool ShowCompleteButton { get => (bool)GetValue(ShowCompleteButtonProperty); set => SetValue(ShowCompleteButtonProperty, value); }
    public decimal Total { get => (decimal)GetValue(TotalProperty); set => SetValue(TotalProperty, value); }
    public decimal GivenAmount { get => (decimal)GetValue(GivenAmountProperty); set => SetValue(GivenAmountProperty, value); }
    public decimal Change { get => (decimal)GetValue(ChangeProperty); set => SetValue(ChangeProperty, value); }
    public bool IsPaymentPanelVisible { get => (bool)GetValue(IsPaymentPanelVisibleProperty); set => SetValue(IsPaymentPanelVisibleProperty, value); }
    public IEnumerable? NoteTiles { get => (IEnumerable?)GetValue(NoteTilesProperty); set => SetValue(NoteTilesProperty, value); }
    public IEnumerable? CoinTiles { get => (IEnumerable?)GetValue(CoinTilesProperty); set => SetValue(CoinTilesProperty, value); }
    public double CartListMaxHeight { get => (double)GetValue(CartListMaxHeightProperty); set => SetValue(CartListMaxHeightProperty, value); }
    public ICommand? CompleteOrderCommand { get => (ICommand?)GetValue(CompleteOrderCommandProperty); set => SetValue(CompleteOrderCommandProperty, value); }
    public ICommand? ClearCartCommand { get => (ICommand?)GetValue(ClearCartCommandProperty); set => SetValue(ClearCartCommandProperty, value); }
    public ICommand? TogglePaymentPanelCommand { get => (ICommand?)GetValue(TogglePaymentPanelCommandProperty); set => SetValue(TogglePaymentPanelCommandProperty, value); }
    public ICommand? AddDenominationCommand { get => (ICommand?)GetValue(AddDenominationCommandProperty); set => SetValue(AddDenominationCommandProperty, value); }
    public ICommand? RemoveDenominationCommand { get => (ICommand?)GetValue(RemoveDenominationCommandProperty); set => SetValue(RemoveDenominationCommandProperty, value); }
    public ICommand? ResetPaymentCommand { get => (ICommand?)GetValue(ResetPaymentCommandProperty); set => SetValue(ResetPaymentCommandProperty, value); }

    public CartPanelControl()
    {
        InitializeComponent();
    }
}
