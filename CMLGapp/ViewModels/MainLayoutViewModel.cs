using CMLGapp.Models;
using CMLGapp.Services;
using CMLGapp.Views;
using Microsoft.Maui.Storage;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CMLGapp.ViewModels
{
    public class MainLayoutViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private readonly OpcUaService _opcUaService;

        public ObservableCollection<LayoutModel> currentLayoutCards { get; set; } = new();
        public ObservableCollection<LayoutModel> AllAvailableCards { get; set; } = new();

        public MainLayoutViewModel()
        {
            LoadLayout();
        }

        //add card to current Main layout
        public ICommand AddCommand => new Command<LayoutModel>(card =>
        {
            currentLayoutCards.Add(card);
            AllAvailableCards.Remove(card);
        });

        // remove the card from durrent Main layout
        public ICommand RemoveCommand => new Command<LayoutModel>(card =>
        {
            currentLayoutCards.Remove(card);
            AllAvailableCards.Add(card);
        });

        public ICommand SaveCommand => new Command(async () => SaveLayout());

        //Navigate to editing page 
        public ICommand GoToManageLayoutCommand => new Command(async () =>
        {
            await Shell.Current.GoToAsync(nameof(ManageLayoutPage));
        });

        public ICommand NavigateCommand => new Command<LayoutModel>(async (card) =>
        {
            if (!string.IsNullOrEmpty(card.Route))
                await Shell.Current.GoToAsync(card.Route);
        });

        //save added card layout
        private async Task SaveLayout()
        {
            var ids = currentLayoutCards.Select(c => c.Id).ToList();
            Preferences.Set("user_selected_cards", JsonSerializer.Serialize(ids));
            await Shell.Current.GoToAsync("..");
        }

        //refresh the layout for immidate effect
        public void ReloadLayout()
        {
            LoadLayout();
        }


        //Contorl card and routing
        private void LoadLayout()
        {
            currentLayoutCards.Clear();
            AllAvailableCards.Clear();
             //var alarmdes = _opcUaService.LoadAlarmDesc();

            var allCards = new List<LayoutModel>
            {
                new LayoutModel { Id = "Alarm", CardName = "Alarms",Image = "zoller_alarm.png", Route = nameof(AlarmContentPage)},
                new LayoutModel { Id = "AlarmHistory", CardName = "Alarm History",Image="alarmhistorydd.png", Route = nameof(AlarmHistoryContentPage) },
                new LayoutModel { Id = "MeasuringStatus", CardName = "Measuring Status", Image="measuringstatus.svg", Route = nameof(DeviceToolMeasuringStatus)},
                //new LayoutModel { Id = "Charts", CardName = "Charts",Image="chart.png", Route = nameof(PalletUI)},
                new LayoutModel { Id = "Summary", CardName = "Summary", Image="booksummary.svg", Route = nameof(CMLGSummaryContentPage)},
                //new LayoutModel { Id = "ViewMore", CardName = "View More",Image="link.svg", Route = nameof(DetailsPage)},
                new LayoutModel { Id = "StopReason", CardName = "Stop Reason",Image="stopreason.svg", Route = nameof(StopReasonContentPage)},
                new LayoutModel { Id = "status", CardName = "Analytics",Image="analytics.svg", Route = nameof(ProdStatsPage)},
            };

            var saved = Preferences.Get("user_selected_cards", string.Empty);
            var selectedIds = string.IsNullOrEmpty(saved)? new List<string>() : JsonSerializer.Deserialize<List<string>>(saved);

            foreach (var card in allCards)
            {
                if (selectedIds.Contains(card.Id))
                {
                    currentLayoutCards.Add(card);
                }
                else
                {
                    AllAvailableCards.Add(card);
                }
                    
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
