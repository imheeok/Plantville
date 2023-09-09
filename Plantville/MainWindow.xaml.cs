using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using Newtonsoft.Json;

namespace Plantville
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static int LAND_PlOTS_MAX = 15;
        private static int TRIP_TO_MARKET_FEE = 10;
        private static string FILE_PATH = "data.txt";

        private List<Seed> seed_list;
        private List<Plant> garden_list;
        private List<Plant> inventory_list;
        private int money = 100;
        private int landPlots = 15;
        private GameState gameState;

        private DispatcherTimer timer;


        public MainWindow()
        {
            InitializeComponent();
            InitialSeedList();
            UpdateStatusBar();
            garden_list = new List<Plant>();
            inventory_list = new List<Plant>();

            gameState = new GameState();
            LoadGameState();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        
        private void InitialSeedList()
        {
            seed_list = new List<Seed>() {
                new Seed("Strawberries", 2, 8, TimeSpan.FromSeconds(10)),
                new Seed("Spinach", 5, 8, TimeSpan.FromMinutes(1)),
                new Seed("Pears", 2, 20, TimeSpan.FromMinutes(3))
            };
            EmporiumListbox.ItemsSource = seed_list;

        }

        private void EmporiumListbox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

            Seed selectedSeed = (Seed)EmporiumListbox.SelectedItem;
            MessageBox.Show($"You purchased {selectedSeed.Name}.");
            PurchaseSeed(selectedSeed);
        }

        private void PurchaseSeed(Seed selectedSeed)
        {
            if (money < selectedSeed.SeedPrice)
            {
                MessageBox.Show($"You cannot purchases {selectedSeed.Name}. Check your money.");
                return;
            }
            if(landPlots < 1)
            {
                MessageBox.Show($"You don't have enough land to plant another crop.");
                return;
            }
            money -= selectedSeed.SeedPrice;
            garden_list.Add(new Plant(selectedSeed));

            UpdateGardenListbox();
            UpdateStatusBar();
            SaveGameState();

        }
        private void GardenListbox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

            Plant selectedPlant = (Plant)GardenListbox.SelectedItem;
            if (!selectedPlant.IsHarvestable || selectedPlant.IsSpoiled)
            {
                return;
            }
            HarvestPlant(selectedPlant);
        }


        private void HarvestAllButton_Click(object sender, RoutedEventArgs e)
        {
            int count = 0;
            foreach (var plant in garden_list.ToList())
            {
                if (plant.IsHarvestable && !plant.IsSpoiled)
                {
                    count++;
                    HarvestPlant(plant);
                }
            }
            String msg = count > 0 ? $"Harvested {count} plants." : "Nothing to harvest.";
            MessageBox.Show(msg);

        }
        private void HarvestPlant(Plant selectedPlant)
        {
            garden_list.Remove(selectedPlant);
            inventory_list.Add(selectedPlant);

            UpdateGardenListbox();
            UpdateInventoryListbox();
            UpdateStatusBar();
            SaveGameState();

        }
        private void SellButton_Click(object sender, RoutedEventArgs e)
        {
            if (inventory_list.Count() == 0)
            {
                MessageBoxResult result = MessageBox.Show("Are you sure you want to go to the farmer's market without any inventory?", "Harvest Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }
            SellPlant();
        }

        private void SellPlant()
        {
            int madeMoney = 0;
            madeMoney -= TRIP_TO_MARKET_FEE;
            foreach (var plant in inventory_list.ToList())
            {
                madeMoney += plant.Seed.HarvestPrice;
            }
            inventory_list.Clear();
            UpdateInventoryListbox();

            MessageBox.Show($"Cleared inventory. Made ${madeMoney}");
            money += madeMoney;
            UpdateStatusBar();
            SaveGameState();

        }
        private void UpdateStatusBar()
        {
            if (inventory_list != null)
            {
                landPlots = LAND_PlOTS_MAX - garden_list.Count();
            }
            MoneyStatusLabel.Text = $"Money: ${money}";
            LandStatusLabel.Text = $"Land: {landPlots}";

        }
        private void UpdateGardenListbox()
        {
            GardenListbox.ItemsSource = garden_list;
            GardenListbox.Items.Refresh();
            
        }
        private void UpdateInventoryListbox()
        {
            InventoryListbox.ItemsSource = inventory_list;
            InventoryListbox.Items.Refresh();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            foreach (var plant in garden_list)
            {
                if (!plant.IsHarvestable || !plant.IsSpoiled)
                {
                    plant.UpdateTimeLeftToHarvest();

                }
            }
        }

        private void SaveGameState()
        {

            gameState.Garden = garden_list;
            gameState.Inventory = inventory_list;
            gameState.Money = money;

            string jsonData = JsonConvert.SerializeObject(gameState);
            File.WriteAllText(FILE_PATH, jsonData);

        }
        private void LoadGameState()
        {
            if (File.Exists(FILE_PATH))
            {
                string jsonData = File.ReadAllText(FILE_PATH);
                GameState gameState = JsonConvert.DeserializeObject<GameState>(jsonData);
                if (!gameState.Equals(null))
                {
                    garden_list = gameState.Garden;
                    inventory_list = gameState.Inventory;
                    money = gameState.Money;
                    UpdateGardenListbox();
                    UpdateInventoryListbox();
                    UpdateStatusBar();
                }
            }

        }
    }

    public class Seed
    {
        public string Name { get; set; }
        public int SeedPrice { get; set; }
        public int HarvestPrice { get; set; }
        public TimeSpan HarvestDuration { get; set; }


        public Seed(string name, int price, int hPrice, TimeSpan hDuration)
        {
            this.Name = name;
            this.SeedPrice = price;
            this.HarvestPrice = hPrice;
            this.HarvestDuration = hDuration;
        }

    }
    public class Plant : INotifyPropertyChanged
    {
        public Seed Seed { get; set; }

        public bool IsHarvestable { get; set; }
        public DateTime HarvestTime { get; set; }

        private TimeSpan SpoilDuration = TimeSpan.FromMinutes(1);
        private bool _isSpoiled;
        public bool IsSpoiled
        {
            get { return _isSpoiled; }
            set
            {
                if (_isSpoiled != value)
                {
                    _isSpoiled = value;
                    OnPropertyChanged(nameof(IsSpoiled));
                }
            }

        }
        private TimeSpan _timeLeftToHarvest;
        public TimeSpan TimeLeftToHarvest
        {
            get { return _timeLeftToHarvest; }
            set
            {
                if (_timeLeftToHarvest != value)
                {
                    _timeLeftToHarvest = value;
                    OnPropertyChanged(nameof(TimeLeftToHarvest));
                }
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Plant(Seed seed)
        {
            this.Seed = seed;
            this.IsHarvestable = false;
            this.HarvestTime = DateTime.Now + seed.HarvestDuration;
            UpdateTimeLeftToHarvest();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void UpdateTimeLeftToHarvest()
        {
            TimeLeftToHarvest = HarvestTime - DateTime.Now;
            if (TimeLeftToHarvest.TotalSeconds <= 0)
            {
                this.IsHarvestable = true;
                if (TimeLeftToHarvest < -SpoilDuration)
                {
                    this.IsSpoiled = true;
                }
            }
        }

    }

    public class GameState
    {
        public List<Plant> Garden { get; set; }
        public List<Plant> Inventory { get; set; }
        public int Money { get; set; }

        public GameState()
        {
            Garden = new List<Plant>();
            Inventory = new List<Plant>();
        }
    }

    public class PlantInfoConverter : IMultiValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            bool isSpoiled = (bool)value;
            return isSpoiled ? "(Spoiled)" : string.Empty;
            
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {

            var name = (String)values[0];
            var timeLeftToHarvest = (TimeSpan)values[1];
            var isSpoiled = (bool)values[2];
            string PlantInfo = name;
            
            if (isSpoiled)
            {
                PlantInfo += "(Spoiled)";
            }else if (timeLeftToHarvest.TotalSeconds <= 0)
            {
                PlantInfo += "(Harvest)";
            }
            else
            {
                PlantInfo += $"({timeLeftToHarvest:%m}m {timeLeftToHarvest:%s}s)";
            }
            return PlantInfo;
        }

 
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }




}
