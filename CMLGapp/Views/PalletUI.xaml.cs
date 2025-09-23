// PalletUI.xaml.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Controls;
using CMLGapp.Services;

namespace CMLGapp.Views
{
    public partial class PalletUI : ContentPage
    {
        // save image and pallet id
        private readonly Dictionary<int, List<Image>> _map = new();
        private readonly OpcUaService _opc = OpcUaService.Instance;
        private bool _attached;
        private int _centralPalletCount = 24;

        public PalletUI()
        {
            InitializeComponent();

            // Default middle
            //CountPicker.SelectedIndex = 0;

            BuildImageMap();

            // Start all hole as unidentified wih grey image
            foreach (var img in _map.Values.SelectMany(x => x))
                img.Source = "no_tool_in_pallet.svg";
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await SetPalletCapacity(); //pallet

            _opc.StartAutoReconnect();
            _ = _opc.StartAppAsync();

            if (_attached) return;

            // mointor all product node
            _opc.MonitorAllProductIngredientIds((index, value) =>
            {
                string svg = SvgForValue(index, value);
                if (_map.TryGetValue(index, out var imgs))
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        foreach (var img in imgs) img.Source = svg;
                    });
                }

                Console.WriteLine($"[UI UPDATE] Product[{index}].Ingredients[0].IngredientID = {value}");
            }, samplingMs: 250);

            _attached = true;
        }


        // hide last row if it is HSK100
        private async Task SetPalletCapacity()
        {
            (string palletName, string palletUnit, int palletId, float palletValue) = await _opc.GetPalletInformationAsyc();
            int palletCapacity = int.TryParse(palletUnit, out int palletCpcty) ? palletCpcty : 0;
            _centralPalletCount = (palletCpcty == 20) ? 20 : 24;

            bool is24 = _centralPalletCount == 24;

            Img6.IsVisible = is24;
            Img12.IsVisible = is24;
            Img18.IsVisible = is24;
            Img24.IsVisible = is24;

        }

        //// 24 => 4×6; 20 => 4×5  (hide ROW 6 => IDs 6,12,18,24)
        //private void CountPicker_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    bool is24 = (CountPicker.SelectedItem as string)?.Trim() == "24";

        //    Img6.IsVisible = is24;
        //    Img12.IsVisible = is24;
        //    Img18.IsVisible = is24;
        //    Img24.IsVisible = is24;
        //}

        private static string SvgForValue(int index, int v)
        {
            if (v < 0)
            {
                return "no_tool_in_pallet.svg"; // no toll before start measuring
            }
            if (v == 0)
            {
                return "";  // when pallet present gripper start taking the tool form pallet
            }

            if (index >= 32 && index <= 37)
            {
                return "defect_tool.svg";
            }

            return "good_tool.svg";                 // tool measure or find as defect

        }


        private void add(int productIndex, Image img)
        {
            if (!_map.TryGetValue(productIndex, out var list))
                _map[productIndex] = list = new List<Image>();
            list.Add(img);
        }
        private void BuildImageMap()
        {
            // Middle pallet tray 1..24 => Product[0..23], column-major order:
            
            add(1, Img1); add(7, Img7); add(13, Img13); add(19, Img19);
            add(2, Img2); add(8, Img8); add(14, Img14); add(20, Img20);
            add(3, Img3); add(9, Img9); add(15, Img15); add(21, Img21);
            add(4, Img4); add(10, Img10); add(16, Img16); add(22, Img22);
            add(5, Img5); add(11, Img11); add(17, Img17); add(23, Img23);
            add(6, Img6); add(12, Img12); add(18, Img18); add(24, Img24);

            // left pallet tray 26..31 => Product[25..30]
            add(26, Img26); add(27, Img27); add(28, Img28);
            add(29, Img29); add(30, Img30); add(31, Img31);

            // right pallet tray 32..37 => Product[31..36]
            add(32, Img32); add(33, Img33); add(34, Img34);
            add(35, Img35); add(36, Img36); add(37, Img37);

            
        }
        
    }
}
