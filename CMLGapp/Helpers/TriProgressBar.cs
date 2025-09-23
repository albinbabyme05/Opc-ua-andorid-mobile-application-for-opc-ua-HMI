using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace CMLGapp.Helpers
{
    public class TriProgressBar : Grid
    {
        // Existing tri-mode bindables (kept for compatibility)
        public static readonly BindableProperty RedValueProperty =
            BindableProperty.Create(nameof(RedValue), typeof(int), typeof(TriProgressBar), 0, propertyChanged: OnValuesChanged);
        public static readonly BindableProperty GreenValueProperty =
            BindableProperty.Create(nameof(GreenValue), typeof(int), typeof(TriProgressBar), 0, propertyChanged: OnValuesChanged);
        public static readonly BindableProperty RemainingValueProperty =
            BindableProperty.Create(nameof(RemainingValue), typeof(int), typeof(TriProgressBar), 0, propertyChanged: OnValuesChanged);
        public static readonly BindableProperty TotalCapacityProperty =
            BindableProperty.Create(nameof(TotalCapacity), typeof(int), typeof(TriProgressBar), 0, propertyChanged: OnValuesChanged);

        public static readonly BindableProperty TrackColorProperty =
            BindableProperty.Create(nameof(TrackColor), typeof(Color), typeof(TriProgressBar), Color.FromArgb("#5A6571"));
        public static readonly BindableProperty RedColorProperty =
            BindableProperty.Create(nameof(RedColor), typeof(Color), typeof(TriProgressBar), Color.FromArgb("#FF8C8C"));
        public static readonly BindableProperty GreenColorProperty =
            BindableProperty.Create(nameof(GreenColor), typeof(Color), typeof(TriProgressBar), Color.FromArgb("#78C841"));

        public int RedValue { get => (int)GetValue(RedValueProperty); set => SetValue(RedValueProperty, value); }
        public int GreenValue { get => (int)GetValue(GreenValueProperty); set => SetValue(GreenValueProperty, value); }
        public int RemainingValue { get => (int)GetValue(RemainingValueProperty); set => SetValue(RemainingValueProperty, value); }
        public int TotalCapacity { get => (int)GetValue(TotalCapacityProperty); set => SetValue(TotalCapacityProperty, value); }

        public Color TrackColor { get => (Color)GetValue(TrackColorProperty); set => SetValue(TrackColorProperty, value); }
        public Color RedColor { get => (Color)GetValue(RedColorProperty); set => SetValue(RedColorProperty, value); }
        public Color GreenColor { get => (Color)GetValue(GreenColorProperty); set => SetValue(GreenColorProperty, value); }

        // --- internals ---
        readonly Border _track;
        readonly Grid _segments = new() { ColumnSpacing = 0, RowSpacing = 0 };      // tri-mode
        readonly BoxView _red, _green, _gray;

        // overlay 2-color bars (measured green behind, defects red on top)
        readonly Border _greenOverlay;
        readonly Border _redOverlay;

        bool _twoColorMode;
        int _defects, _measured, _capacity;

        public TriProgressBar()
        {
            HeightRequest = 10;

            _track = new Border
            {
                BackgroundColor = TrackColor,
                Stroke = Colors.Transparent,
                StrokeThickness = 0,
                Padding = 0,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                StrokeShape = new RoundRectangle { CornerRadius = 1 }
            };

            // tri-mode pieces
            _red = new BoxView { Color = RedColor };
            _green = new BoxView { Color = GreenColor };
            _gray = new BoxView { Color = TrackColor.WithAlpha(0.8f) };

            // overlay bars
            _greenOverlay = new Border
            {
                BackgroundColor = GreenColor,
                StrokeThickness = 0,
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Fill,
                HeightRequest = -1,
                StrokeShape = new RoundRectangle { CornerRadius = 1 },
                IsVisible = false
            };
            _redOverlay = new Border
            {
                BackgroundColor = RedColor,
                StrokeThickness = 0,
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Fill,
                HeightRequest = -1,
                StrokeShape = new RoundRectangle { CornerRadius = 1 },
                IsVisible = false
            };

            // layering: track -> overlays -> tri-grid
            Children.Add(_track);
            Children.Add(_greenOverlay);
            Children.Add(_redOverlay);
            Children.Add(_segments);

            SizeChanged += (_, __) => UpdateBars();
            UpdateBars();
        }

        static void OnValuesChanged(BindableObject b, object o, object n)
            => ((TriProgressBar)b).UpdateBars();

        public void SetValues(int red, int green, int remaining, int totalCapacity = 0)
        {
            _twoColorMode = false;         // tri-mode
            RedValue = red;
            GreenValue = green;
            RemainingValue = remaining;
            TotalCapacity = totalCapacity;
        }

        // NEW: two-color overlay API (defects + measured vs capacity)
        public void SetTwoColor(int defects, int measured, int capacity)
        {
            _twoColorMode = true;
            _defects = Math.Max(0, defects);
            _measured = Math.Max(0, measured);
            _capacity = Math.Max(0, capacity);
            UpdateBars();
        }

        void UpdateBars()
        {
            // keep colors fresh
            _track.BackgroundColor = TrackColor;
            _greenOverlay.BackgroundColor = GreenColor;
            _redOverlay.BackgroundColor = RedColor;
            _red.Color = RedColor; _green.Color = GreenColor; _gray.Color = TrackColor.WithAlpha(0.8f);

            if (_twoColorMode)
            {
                // OVERLAY MODE: bar shows measured (green) against capacity, and defects (red) on top.
                _segments.IsVisible = false;
                _greenOverlay.IsVisible = true;
                _redOverlay.IsVisible = true;

                var cap = Math.Max(1, _capacity); // avoid div-by-zero
                var greenRatio = Math.Clamp(_measured / (double)cap, 0, 1);
                var redRatio = Math.Clamp(_defects / (double)cap, 0, 1);

                var w = Width;
                if (w <= 0)
                {
                    // wait for layout pass
                    return;
                }
                _greenOverlay.WidthRequest = w * greenRatio;
                _redOverlay.WidthRequest = w * redRatio;
                return;
            }

            // TRI MODE (back-compat)
            _segments.IsVisible = true;
            _greenOverlay.IsVisible = false;
            _redOverlay.IsVisible = false;

            int red = Math.Max(0, RedValue);
            int green = Math.Max(0, GreenValue);
            int rem = Math.Max(0, RemainingValue);

            int capacity = TotalCapacity > 0 ? TotalCapacity : (red + green + rem);
            if (capacity <= 0 && (red + green + rem) <= 0) return;

            if (capacity > 0)
            {
                int sum = red + green + rem;
                if (sum > capacity)
                {
                    double k = capacity / (double)Math.Max(1, sum);
                    red = (int)Math.Round(red * k);
                    green = (int)Math.Round(green * k);
                    rem = Math.Max(0, capacity - red - green);
                }
            }

            float Star(int v) => v <= 0 ? 0.0001f : v;

            _segments.ColumnDefinitions.Clear();
            _segments.Children.Clear();
            _segments.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Star(red), GridUnitType.Star) });
            _segments.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Star(green), GridUnitType.Star) });
            _segments.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Star(rem), GridUnitType.Star) });

            Grid.SetColumn(_red, 0); _segments.Children.Add(_red);
            Grid.SetColumn(_green, 1); _segments.Children.Add(_green);
            Grid.SetColumn(_gray, 2); _segments.Children.Add(_gray);
        }
    }
}
