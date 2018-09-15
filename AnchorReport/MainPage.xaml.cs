using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows.Input;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace AnchorReport
{
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private const string IgnoreHttpsErrorSettingKey = "IgnoreHttpsErrors";
        private PerceptionConnection connection;
        private StringBuilder log = new StringBuilder();
        private AnchorStats stats;
        private StringMru addressMru;

        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand SetAddressCommand
        { get; private set; }

        private string addressText = "localhost:10080";
        public string AddressText
        {
            get { return this.addressText; }
            set { this.PropertyChangedHelper(value, ref addressText); }
        }

        private string userName = "user";
        public string UserName
        {
            get { return this.userName; }
            set { this.PropertyChangedHelper(value, ref userName); }
        }

        private string password = "password";
        public string Password
        {
            get { return this.password; }
            set { this.PropertyChangedHelper(value, ref password); }
        }

        private bool ignoreHttpsErrors = false;
        public bool IgnoreHttpsErrors
        {
            get { return this.ignoreHttpsErrors; }
            set
            {
                this.PropertyChangedHelper(value, ref this.ignoreHttpsErrors);
                {
                    ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                    localSettings.Values[IgnoreHttpsErrorSettingKey] = value;
                }
            }
        }

        public MainPage()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if(localSettings.Values.TryGetValue(IgnoreHttpsErrorSettingKey, out var settingObject))
            {
                // not using accessor so it doesn't write the value right to settings
                this.ignoreHttpsErrors = (bool)settingObject;
            }

            this.InitializeComponent();
            this.DataContext = this;
            this.RegisterCommands();
            this.addressMru = new StringMru("AddressMru");
            this.RefreshAddressMru();
        }

        private void RefreshAddressMru()
        {
            this.addressMenu.Items.Clear();
            string first = null;
            foreach (var s in this.addressMru.Strings)
            {
                if (first == null) { first = s; }
                var flyout = new MenuFlyoutItem() { Text = s, Command = this.SetAddressCommand, CommandParameter = s };
                this.addressMenu.Items.Add(flyout);
            }

            if (first != null)
            {
                this.AddressText = first;
            }
        }

        private void RegisterCommands()
        {
            this.SetAddressCommand = new Command(
                (parameter) =>
                {
                    this.AddressText = (string)parameter;
                });
        }

        private async void OnConnect(object sender, RoutedEventArgs args)
        {
            this.CloseConnection();
            this.stats = new AnchorStats();
            this.connection = new PerceptionConnection(this.AddressText, this.UserName, this.Password);
            this.connection.SetLogger((message) => { this.Log(message); });
            this.connection.SetAnchorHandler((anchors) => { this.OnAnchors(anchors); });
            this.addressMru.StringUsed(this.AddressText);
            try
            {
                await this.connection.ConnectAsync(this.IgnoreHttpsErrors);
                this.RefreshAddressMru();
            }
            catch(Exception)
            {
                // Eat the exception - it's already been logged in the logger.
            }
        }

        private void OnDisconnect(object sender, RoutedEventArgs args)
        {
            this.CloseConnection();
        }

        private void OnClearLog(object sender, RoutedEventArgs args)
        {
            this.log.Clear();
            this.logText.Text = string.Empty;
        }

        /// <summary>
        /// Called when we get a set of anchors from the HoloLens
        /// </summary>
        private void OnAnchors(AnchorCollection anchors)
        {
            this.stats.AddSample(anchors);

            foreach (var anchor in anchors.Anchors)
            {
                var origin = anchor.Transform.Translation;
                this.Log($"    Anchor: {anchor.Guid}  ValidTransform: {anchor.ValidTransform}    origin: {origin.X}, {origin.Y}, {origin.Z}   movement volume:{this.stats.AnchorMovementVolume(anchor.Guid)}");
            }

            var ignoredAnchors = this.stats.IgnoredAnchors;
            if (ignoredAnchors.Length > 0)
            {
                this.Log("Ignoring duplicate anchors:");
                foreach (var guid in ignoredAnchors)
                {
                    this.Log($"    {guid}");
                }
            }

            this.Log($"Mean movement volume {this.stats.MeanMovementVolume}");
        }

        private void CloseConnection()
        {
            if (this.connection != null)
            {
                this.connection.Dispose();
                this.connection = null;
            }
        }

        private void Log(string message)
        {
            Debug.WriteLine(message);

            log.AppendLine(message);
            this.logText.Text = this.log.ToString(); // Very inefficient
        }

        private void PageUnloaded(object sender, RoutedEventArgs e)
        {
            this.CloseConnection();
        }

        private bool PropertyChangedHelper<T>(T newValue, ref T storage, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            if (IComparable<T>.Equals(newValue, storage))
            {
                return false;
            }

            storage = newValue;
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

    }
}
