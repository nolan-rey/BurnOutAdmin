using System.ComponentModel;
using BurnOutAdmin.Services;
using BurnOutAdmin.ViewModels;
using Microsoft.Maui.Controls.Shapes;

namespace BurnOutAdmin.Views.ClientProfile;

public partial class ClientProfileView : ContentView
{
    private ClientProfileViewModel? _vm;

    public ClientProfileView()
    {
        InitializeComponent();
        BindingContextChanged += OnBindingContextChanged;
    }

    private void OnBindingContextChanged(object? sender, EventArgs e)
    {
        if (_vm is not null)
            _vm.PropertyChanged -= OnVmPropertyChanged;
        _vm = BindingContext as ClientProfileViewModel;
        if (_vm is not null)
            _vm.PropertyChanged += OnVmPropertyChanged;
        RebuildRpeCategories();
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ClientProfileViewModel.ViewingFeedback))
            RebuildRpeCategories();
    }

    /// <summary>
    /// Reconstruit l'arbre visuel des 3 catégories de RPE détaillé en code
    /// à chaque changement de <see cref="ClientProfileViewModel.ViewingFeedback"/>.
    /// Évite le bug MAUI où les vues d'un <see cref="BindableLayout"/> sont
    /// gardées en cache entre 2 séances et provoquent des chevauchements.
    /// </summary>
    private void RebuildRpeCategories()
    {
        RpeCategoriesHost.Children.Clear();

        var details = _vm?.ViewingFeedback?.RpeDetails;
        if (details is null) return;

        foreach (var cat in details.Categories)
            RpeCategoriesHost.Children.Add(BuildCategoryView(cat));
    }

    private static View BuildCategoryView(RpeCategoryView cat)
    {
        var container = new VerticalStackLayout { Spacing = 10 };

        // ── En-tête : titre + sous-titre + badge moyenne ──
        var headerGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 10
        };

        var titles = new VerticalStackLayout { Spacing = 2 };
        titles.Children.Add(new Label
        {
            Text       = cat.Title,
            FontSize   = 13,
            FontAttributes = FontAttributes.Bold,
            TextColor  = Color.FromArgb("#0F172A")
        });
        titles.Children.Add(new Label
        {
            Text      = cat.Subtitle,
            FontSize  = 11,
            TextColor = Color.FromArgb("#64748B")
        });
        Grid.SetColumn(titles, 0);
        headerGrid.Children.Add(titles);

        var avgBadge = new Border
        {
            BackgroundColor = cat.AverageBg,
            StrokeThickness = 0,
            StrokeShape     = new RoundRectangle { CornerRadius = 8 },
            Padding         = new Thickness(10, 5),
            VerticalOptions = LayoutOptions.Center,
            Content = new Label
            {
                Text       = cat.AverageDisplay,
                FontSize   = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor  = cat.AverageColor
            }
        };
        Grid.SetColumn(avgBadge, 1);
        headerGrid.Children.Add(avgBadge);

        container.Children.Add(headerGrid);

        // ── Items détaillés (générés un par un, sans BindableLayout) ──
        if (cat.HasItems)
        {
            var itemsStack = new VerticalStackLayout { Spacing = 8 };
            foreach (var item in cat.Items)
                itemsStack.Children.Add(BuildItemView(item));
            container.Children.Add(itemsStack);
        }

        // ── Commentaire de catégorie ──
        if (cat.HasComment)
        {
            var commentStack = new VerticalStackLayout { Spacing = 4 };
            commentStack.Children.Add(new Label
            {
                Text             = "COMMENTAIRE",
                FontSize         = 9,
                FontAttributes   = FontAttributes.Bold,
                TextColor        = Color.FromArgb("#6366F1"),
                CharacterSpacing = 0.6
            });
            commentStack.Children.Add(new Border
            {
                BackgroundColor = Colors.White,
                Stroke          = new SolidColorBrush(Color.FromArgb("#E2E8F0")),
                StrokeThickness = 1,
                StrokeShape     = new RoundRectangle { CornerRadius = 8 },
                Padding         = new Thickness(10, 8),
                Content = new Label
                {
                    Text          = cat.Comment,
                    FontSize      = 12,
                    TextColor     = Color.FromArgb("#1E293B"),
                    LineBreakMode = LineBreakMode.WordWrap
                }
            });
            container.Children.Add(commentStack);
        }

        // Cadre extérieur de la catégorie
        return new Border
        {
            BackgroundColor = Color.FromArgb("#F8FAFC"),
            Stroke          = new SolidColorBrush(Color.FromArgb("#E2E8F0")),
            StrokeThickness = 1,
            StrokeShape     = new RoundRectangle { CornerRadius = 10 },
            Padding         = new Thickness(14, 12),
            Content         = container
        };
    }

    private static View BuildItemView(RpeItemView item)
    {
        var wrapper = new VerticalStackLayout
        {
            Spacing = 4,
            Margin  = new Thickness(0, 0, 0, 2)
        };

        // Ligne : label + score
        var line = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 10
        };
        var lblLabel = new Label
        {
            Text          = item.Label,
            FontSize      = 12,
            TextColor     = Color.FromArgb("#334155"),
            VerticalOptions = LayoutOptions.Center,
            LineBreakMode = LineBreakMode.TailTruncation
        };
        Grid.SetColumn(lblLabel, 0);
        line.Children.Add(lblLabel);

        var lblScore = new Label
        {
            Text            = $"{item.ScoreDisplay}/10",
            FontSize        = 12,
            FontAttributes  = FontAttributes.Bold,
            TextColor       = Color.FromArgb("#0F172A"),
            VerticalOptions = LayoutOptions.Center
        };
        Grid.SetColumn(lblScore, 1);
        line.Children.Add(lblScore);

        wrapper.Children.Add(line);

        // Barre de progression colorée
        wrapper.Children.Add(new ProgressBar
        {
            Progress          = item.Progress01,
            ProgressColor     = item.BarColor,
            BackgroundColor   = Color.FromArgb("#E2E8F0"),
            HeightRequest     = 6,
            HorizontalOptions = LayoutOptions.Fill
        });

        return wrapper;
    }
}
