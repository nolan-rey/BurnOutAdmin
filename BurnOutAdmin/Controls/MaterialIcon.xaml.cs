namespace BurnOutAdmin.Controls;

public partial class MaterialIcon : ContentView
{
    public static readonly BindableProperty IconDataProperty =
        BindableProperty.Create(nameof(IconData), typeof(string), typeof(MaterialIcon), string.Empty);

    public static readonly BindableProperty IconPathProperty =
        BindableProperty.Create(nameof(IconPath), typeof(string), typeof(MaterialIcon), string.Empty,
            propertyChanged: OnIconPathChanged);

    public static readonly BindableProperty IconColorProperty =
        BindableProperty.Create(nameof(IconColor), typeof(Color), typeof(MaterialIcon), Colors.Black);

    public static readonly BindableProperty SizeProperty =
        BindableProperty.Create(nameof(Size), typeof(double), typeof(MaterialIcon), 24.0);

    public string IconData
    {
        get => (string)GetValue(IconDataProperty);
        set => SetValue(IconDataProperty, value);
    }

    public string IconPath
    {
        get => (string)GetValue(IconPathProperty);
        set => SetValue(IconPathProperty, value);
    }

    public Color IconColor
    {
        get => (Color)GetValue(IconColorProperty);
        set => SetValue(IconColorProperty, value);
    }

    public double Size
    {
        get => (double)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public MaterialIcon()
    {
        InitializeComponent();
    }

    private static void OnIconPathChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is MaterialIcon icon && newValue is string pathData)
        {
            icon.IconData = pathData;
        }
    }
}
