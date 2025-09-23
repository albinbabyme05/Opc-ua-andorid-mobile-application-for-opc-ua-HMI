
using CMLGapp.Services;

namespace CMLGapp.Views;
public partial class CMLGSummaryContentPage : BaseContentPage
{
    private OpcUaService _opcuaService;
    private MainPage _mainpage;

    public CMLGSummaryContentPage()
    {
        InitializeComponent();
        _opcuaService = OpcUaService.Instance;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        bool connected = await _opcuaService.StartAppAsync();
        if (!connected)
        {
            await Shell.Current.GoToAsync("//offline");
            return;
        }

        //pallet info
        _opcuaService.MonitorStatusNodes("Parameter", async (_) =>
        {
            await LoadAndDisplayPalletInfo();
        });
        //_opcuaService.MonitorPlcDateTime(UpdatePlcTime);

        //consumed
        _opcuaService.MonitorNodes("ProdConsumedCount", async (_) =>
        {
            await LoadAndDisplayProdConsumed();
        });
        //defect
        _opcuaService.MonitorNodes("ProdDefectiveCount", async (_) =>
        {
            await LoadAndDisplayProdDefect();
        });
        // processed
        _opcuaService.MonitorNodes("ProdProcessedCount", async (_) =>
        {
            await LoadAndDisplayProdProcessing();

        });
        // progressbart
        //_opcuaService.MonitorNodes("ProdProcessedCount", async (_) =>
        //{
        //    await LoadAndDisplayProgressbar();

        //});
        // State + Mode
        _opcuaService.MonitorSingleNodeValue("StateCurrent", (state) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                DisplayMachineState(state);
            });
        });

        _opcuaService.MonitorSingleNodeValue("UnitModeCurrent", (mode) =>
        {
            MainThread.BeginInvokeOnMainThread(() => DisplayMachineMode(mode));
        }); ;

        
    }

    private async void OnViewMoreClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(DetailsPage));
    }

    //update plc label
    //private void UpdatePlcTime(string time)
    //{
    //    MainThread.BeginInvokeOnMainThread(() =>
    //    {
    //        lblPlcTime.Text = $" ⌚ PLC Time : {time}";
    //    });
    //}

    //display prod. measured
    private async Task LoadAndDisplayProdConsumed()
    {
        var prodConsumed = await _opcuaService.LoadProdConsumedAsync();
        //(string palletName, string palletUnit, int palletId, float palletValue) = await GetPalletInformationAsyc();
        if (prodConsumed != null)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                foreach (var item in prodConsumed)
                {
                    if(item.AccCount == 0)
                    {
                        lblTotalMeasured.Text = $"{item.AccCount}";
                    }
                    else
                    {
                        lblTotalMeasured.Text = $"{item.AccCount}";
                    }
                        
                }
            });
        }
    }

    //machinemode
    private void DisplayMachineMode(int mode)
    {
        (string unitMode, Color clr) = _opcuaService.MachineUnitMode(mode);
        lblMachineMode.Text = $"{unitMode}";
    }

    //machine state
    private void DisplayMachineState(int status)
    {
        (string txtStatus, string svgImage, Color clr) = _opcuaService.MachineStateCurrent(status);
        lblMachineState.Text = $"{txtStatus}";
    }

    // fetchh prod. processing data
    private async Task<(int, int)> GetProdProcessing()
    {
        try
        {
            var prodProcessed = await _opcuaService.LoadProdProcessedAsync();
            if (prodProcessed !=null)
            {
                var item = prodProcessed.First();
                return (item.Count, item.AccCount);
            }
        }
        catch(Exception e)
        {
            Console.WriteLine("Error Loading ProdProcessing method "+e);
        }
        return (0, 0);
    }

    // display accumilated prod. processing
    private async Task LoadAndDisplayProdProcessing()
    {
        (int prodPorcessedCount, int prodPorcessedAccCount) = await GetProdProcessing();
        //(string palletName, string palletUnit, int palletId, float palletValue) = await GetPalletInformationAsyc();
        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (prodPorcessedAccCount == 0)
                {
                    lblProdProcessingCount.Text = $"0";
                }
                else
                {
                    lblProdProcessingCount.Text = $"{prodPorcessedAccCount}";
                }
                
            });
            
        }
        catch (Exception e)
        {
            Console.WriteLine("error in loaddisplayProddefect method " + e);
        }
    }

    //display  accumilated defect
    private async Task LoadAndDisplayProdDefect()
    {
        var prodDefect = await _opcuaService.LoadProdDefectAsync();
        //(string palletName, string palletUnit, int palletId, float palletValue) = await GetPalletInformationAsyc();
        try
        {
            if (prodDefect != null )
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    foreach (var item in prodDefect)
                    {
                        if (item.AccCount == 2)
                        {
                            lblDefectCount.Text = $"0";
                        }
                        else
                        {
                            lblDefectCount.Text = $"{(item.AccCount) - 2}";
                        }     
                    }
                });
            }
        }
        catch (Exception e) { 
            Console.WriteLine("error in loaddisplayProddefect method "+e);
        }
    }


    //private async Task LoadAndDisplayProgressbar()
    //{
    //    int maxCount = 24;
    //    var prodProcessed = await _opcuaService.LoadProdProcessedAsync();
    //    if (prodProcessed != null)
    //    {
    //        foreach (var item in prodProcessed)
    //        {
    //            int currentCount = Math.Min(item.Count, maxCount);
    //            double progress = (double)currentCount / maxCount;
    //            int progressPercent = (int)(progress * 100);
    //            MainThread.BeginInvokeOnMainThread(() =>
    //            {
    //                lblCompleted.Text = $"⌛ Completed : {progressPercent} %";
    //                progressBar.Progress = progress;
    //            });
    //            await progressBar.ProgressTo(progress, 250, Easing.Linear);
    //        }

    //    }
    //}

    //fetch pallet data
    private async Task<(string,string, int, float)> GetPalletInformationAsyc()
    {
        var palletDetails = await _opcuaService.LoadPalletInfoAsync();
        if (palletDetails != null)
        {
            var item = palletDetails.LastOrDefault();
            
            if (item != null && item?.Value>=1 && item?.Value<=7)
            {
                return (item.Name, item.Unit, item.ID, item.Value);
            }  
            
            return ("No pallet available", "", 0, 0);
        }
        return ("No pallet available", "", 0, 0);
    }

    //PALLET INFO display
    private async Task LoadAndDisplayPalletInfo()
    {
        (string palletName, string palletUnit,int palletId, float palletValue) = await GetPalletInformationAsyc();
        
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (palletValue == 7)
                {
                    lblPalletName.Text = "HSK63";
                    lblPalletCapacity.Text = $"{palletUnit}";
                }
                else if (palletValue == 11)
                {
                    lblPalletName.Text = "HSK100";
                    lblPalletCapacity.Text = $"{palletUnit}";
                }
                else
                {
                    lblPalletName.Text = "No Pallet is available";
                }
            });
    }
    


    //end class
}