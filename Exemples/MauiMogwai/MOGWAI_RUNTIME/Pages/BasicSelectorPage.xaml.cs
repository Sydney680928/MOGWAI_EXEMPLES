using MOGWAI_RUNTIME.Classes;
using System.Collections.ObjectModel;

namespace MOGWAI_RUNTIME.Pages;

public partial class BasicSelectorPage : ContentPage
{
    private int _InitialIndex = -1;

    public string? SelectedItem
    {
        get
        {
            return ItemsCollectionView.SelectedItem as string;
        }
    }

    public int SelectedIndex
    {
        get
        {
            if (ItemsCollectionView.SelectedItem == null) return -1;

            var s = ItemsCollectionView.SelectedItem as string;

            if (s != null)
            {
                var index = Items.IndexOf(s);
                return index;
            }
            else
            {
                return -1;  
            }
        }
    }

    public bool Done { get; private set; }

    public ObservableCollection<string> Items { get; } = new();
    
    public BasicSelectorPage(string title, List<string> items, int selectedIndex = -1)
    {
        InitializeComponent();

        TitleLabel.Text = title;

        foreach (var item in items) Items.Add(item);

        _InitialIndex = selectedIndex;

        BindingContext = this;
    }

    protected override bool OnBackButtonPressed()
    {
        Navigation.PopModalAsync();
        Done = true;
        return true;
    }

    private void ValidatePathTapGesture_Tapped(object sender, TappedEventArgs e)
    {
        if (ItemsCollectionView.SelectedItem != null)
        {
            Done = true;
            Navigation.PopModalAsync();
        }
    }

    private void CancelPathTapGesture_Tapped(object sender, TappedEventArgs e)
    {
        ItemsCollectionView.SelectedItem = null;
        Done = true;
        Navigation.PopModalAsync();
    }

    private void ContentPage_Loaded(object sender, EventArgs e)
    {
        if (_InitialIndex > -1 && Items.Count > _InitialIndex)
        {
            ItemsCollectionView.SelectedItem = Items[_InitialIndex];
            ItemsCollectionView.ScrollTo(ItemsCollectionView.SelectedItem, null, ScrollToPosition.Center, false);
        }
    }

    private void ContentPage_Unloaded(object sender, EventArgs e)
    {

    }
}