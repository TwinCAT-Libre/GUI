using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using TwinCAT;
using TwinCAT.Ads;
using TwinCAT.TypeSystem;
using TwinCAT.Ads.TypeSystem;


namespace TwinCAT_GUI
{


    public class symbolListWatchItem
    {
        public uint symbolHandle { get; set; }
        public string symbolPath { get; set; }
        public string symbolDataType { get; set; }
        public string symbolValue { get; set; }
        public string symbolTimestamp { get; set; }

    }


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private AdsClient adsSymbolClient;
        private AdsClient adsSysSrv;
        
        public MainWindow()
        {
            InitializeComponent();

        }

        
        private void AdsServiceConnect()
        {

            //connect to the target system ADS server and PLC symbol runtime
            //ToDo make target remotely targetable, and PLC port selectable
            try
            {
                //connect to system service (runtime)
                adsSysSrv = new AdsClient();
                adsSysSrv.Connect((int)AmsPort.SystemService);
                StateInfo AdsSysServiceState = adsSysSrv.ReadState();
                
                if (adsSysSrv.IsConnected)
                {
                    //If running, connect to PLC instance
                    AdsPortConnect();
                };
                
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }


            adsSysSrv.RouterStateChanged += AdsSysSrv_RouterStateChanged;

            //UI update reads
            CheckServiceState();

        }


        private void AdsSysSrv_RouterStateChanged(object sender, AmsRouterNotificationEventArgs e)
        {
            Debug.WriteLine("Router State Changed");
            //update UI icons
            Action action = () => CheckServiceState();
            Dispatcher.Invoke(action);
        }



        private void AdsPortConnect()
        {
            try
            {
                //connect to PLC instance
                adsSymbolClient = new AdsClient();
                adsSymbolClient.Connect((int)AmsPort.PlcRuntime_851);
                StateInfo AdsSymbolClientState = adsSymbolClient.ReadState();

                //Debug.WriteLine(adsSymbolClient.ReadState().ToString());
                //Debug.WriteLine(AdsSymbolClientState.AdsState);
                //Debug.WriteLine(AdsSymbolClientState.DeviceState);

            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }

        private void AdsDisconnect()
        {
            //Disconnect and cleanup all ads connections

            try
            {

                adsSysSrv.Dispose();
                adsSymbolClient.Dispose();
                adsSysSrv.Disconnect();
                adsSymbolClient.Disconnect();

                //ToDo Update UI

            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }


        }

        private void LoadSymbols()
        {
            //Populate the symbols into the tree
            Debug.WriteLine("Attempting to load symbols");
            //empty the treeview
            treeViewSymbols.Items.Clear();

            try
            {

                ISymbolLoader loader = SymbolLoaderFactory.Create(adsSymbolClient, SymbolLoaderSettings.Default);
                foreach (Symbol symbol in loader.Symbols)
                {
                    Debug.WriteLine("///////////////" + symbol + "///////////////");
                    treeViewSymbols.Items.Add(SymbolsToTreeView(symbol));
                }

            }
            catch (AdsErrorException err)
            {
                MessageBox.Show(err.Message + " Start service and try again");
            }

            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }

        }


        private TreeViewItem SymbolsToTreeView(Symbol symbol)
        {
            //TreeViewItem are the nested elements of a treeview
            TreeViewItem TreeItem = new TreeViewItem();
            TreeItem.Header = symbol.InstancePath;
            TreeItem.Tag = symbol;

            Debug.WriteLine(TreeItem.Tag);

            foreach (Symbol subSymbol in symbol.SubSymbols)
            {
                //multilevel nesting of items
                if (subSymbol.SubSymbolCount > 0)
                {
                    //if it has children, recursively call
                    TreeItem.Items.Add(SymbolsToTreeView(subSymbol));
                }
                else
                {
                    TreeItem.Items.Add(subSymbol.InstancePath);

                }
            }

            return TreeItem;
        }

        private void treeUpdateUI(string symbolstr)
        {
            // Use typed object to use InfoTips
            ISymbolLoader loader = SymbolLoaderFactory.Create(adsSymbolClient, SymbolLoaderSettings.Default);
            try
            {
                Symbol symbol = (Symbol)loader.Symbols[symbolstr];
                // Debug.WriteLine(symbol.ReadValue());
                if (symbol.IsPrimitiveType)
                {
                    btnAddToWatchlist.IsEnabled = true;
                }
                else
                {
                    btnAddToWatchlist.IsEnabled = false;
                }
            }
            catch (Exception err)
            {
                //MessageBox.Show(err.Message);
                Debug.WriteLine(err.Message);
                btnAddToWatchlist.IsEnabled = false;
            }



        }



        private void treeViewSymbols_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            //if the item is a primitive, allow the 'add to watchlist' button to be enabled
            treeUpdateUI(treeViewSymbols.SelectedItem.ToString());
        }


        private uint RegisterNotification(Symbol mySymbol)
        {
            // Add the Notification event handler

            uint notificationHandle; //increments automatically if called more than once

            // Check for change every 200 ms
            notificationHandle = adsSymbolClient.AddDeviceNotificationEx(mySymbol.InstancePath, new NotificationSettings(AdsTransMode.OnChange, 200, 0), mySymbol.InstancePath, mySymbol.ReadValue().GetType());
            return notificationHandle;
        }

        private void UnRegisterNotification(uint notificationHandle)
        {
            // Add the Notification event handler
            adsSymbolClient.DeleteDeviceNotification(notificationHandle);
        }

        private void Client_AdsNotificationEx(object sender, AdsNotificationExEventArgs e)
        {
            //This event fires anytime a handle that has been registered by the RegisterNotification changes value
            Debug.WriteLine(e.UserData.ToString() + " : " + e.Value.ToString() + " Handle: " + e.Handle.ToString() + " " + e.TimeStamp.ToString());

            //ToDo - find more efficient way rather than a for loop
            foreach (symbolListWatchItem watchedSymbol in ListViewSymbolsWatchlist.Items)
            {
                if (watchedSymbol.symbolHandle == e.Handle)
                {
                    watchedSymbol.symbolValue = e.Value.ToString();
                }
            }


            //for threading reasons
            Action action = () => ListViewSymbolsWatchlist.Items.Refresh();
            Dispatcher.Invoke(action);
        }



        private void SymbolNotification(bool Enable)
        {
            //resumes and pauses the update of the ads notifications
            if (Enable)
            {
                adsSymbolClient.AdsNotificationEx += Client_AdsNotificationEx;
            }
            else
            {
                adsSymbolClient.AdsNotificationEx -= Client_AdsNotificationEx;
            }

        }
        /////////////////////////////////////////////////////////////////////////////////////////
        // UI Button actions
        private void btnToolBarConnect_Click(object sender, RoutedEventArgs e)
        {
            AdsServiceConnect();
        }

        private void btnToolbarDisconnect_Click(object sender, RoutedEventArgs e)
        {
            AdsDisconnect();
        }

        private void btnClearWatchlist_Click(object sender, RoutedEventArgs e)
        {
            ListViewSymbolsWatchlist.Items.Clear();

        }

        private void btnRemoveWatchlistItem_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("removing " + ListViewSymbolsWatchlist.SelectedItem + " from watchlist");
            symbolListWatchItem removeItem = (symbolListWatchItem)ListViewSymbolsWatchlist.SelectedItem;
        }


        private void btnAddToWatchlist_Click(object sender, RoutedEventArgs e)
        {
            ISymbolLoader loader = SymbolLoaderFactory.Create(adsSymbolClient, SymbolLoaderSettings.Default);

            Symbol symbol = (Symbol)loader.Symbols[treeViewSymbols.SelectedItem.ToString()];

            //add to notification
            uint handleNumber = RegisterNotification(symbol);

            ListViewSymbolsWatchlist.Items.Add(new symbolListWatchItem
            {
                symbolHandle = handleNumber,
                symbolPath = symbol.InstancePath,
                symbolDataType = symbol.DataType.ToString(),
                symbolValue = symbol.ReadValue().ToString()
            });
        }


        private void btnToolbarTcStart_Click(object sender, RoutedEventArgs e)
        {
            SetTCState(AdsState.Reset);
        }

        private void btnToolbarTcConfig_Click(object sender, RoutedEventArgs e)
        {
            SetTCState(AdsState.Reconfig);
        }

        private void SetTCState(AdsState state)
        {
            Debug.WriteLine("Setting state " + state);
            try
            {
                adsSysSrv.TryWriteControl(new StateInfo(state, adsSysSrv.ReadState().DeviceState));

            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }


        /////////////////////////////////////////////////////////////////////////////////////////
        //Garbage to be deleted later

        private void btnLog_Click(object sender, RoutedEventArgs e)
        {
            //Debug.WriteLine(ListViewSymbolsWatchlist.SelectedItem);
            //Debug.WriteLine(ListViewSymbolsWatchlist.Items[0].ToString()); //first item in listview
            //Debug.WriteLine(ListViewSymbolsWatchlist.)
            // ListViewSymbolsWatchlist.
            //foreach (symbolListWatchItem watchedSymbol in ListViewSymbolsWatchlist.Items) {
            //  Debug.WriteLine(watchedSymbol.symbolHandle);
            //  Debug.WriteLine(watchedSymbol);
            //  watchedSymbol.symbolValue = "hi";
            //}
            //ListViewSymbolsWatchlist.Items.Refresh();
            //TriggerTest(true);
            //btnToolBarConnect.Content = "True"; 
            
        }

        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            LoadSymbols();
        }
        private void btnNotifySubtract_Click(object sender, RoutedEventArgs e)
        {
            SymbolNotification(false);
        }

        private void btnAddNotify_Click(object sender, RoutedEventArgs e)
        {
            SymbolNotification(true);
        }




 


        private bool CheckServiceState()
        {
            try
            {
                Debug.WriteLine(adsSysSrv.ReadState().AdsState + " Ads State");
                Debug.WriteLine(adsSysSrv.ReadState().AdsState.ToString() + " Ads State ToString");
                switch (adsSysSrv.ReadState().AdsState.ToString())
                {
                    case "Run":
                        //style UI buttons
                        btnToolbarTcConfig.Content = (Image)FindResource("TcConfig");
                        btnToolbarTcConfig.IsEnabled = true;
                        btnToolbarTcStart.Content = (Image)FindResource("TcStart");
                        btnToolbarTcStart.IsEnabled = true;
                        btnToolbarTcStart.Background = Brushes.White;
                        btnToolbarTcStart.BorderBrush = Brushes.Black;
                        btnToolbarTcConfig.Background = Brushes.Transparent;
                        btnToolbarTcConfig.BorderBrush = Brushes.Transparent;
                        Debug.WriteLine("Run state");

                        return true;

                    case "Config":
                        //style UI buttons
                        btnToolbarTcConfig.Content = (Image)FindResource("TcConfig");
                        btnToolbarTcConfig.IsEnabled = true;
                        btnToolbarTcStart.Content = (Image)FindResource("TcStart");
                        btnToolbarTcStart.IsEnabled = true;
                        btnToolbarTcStart.Background = Brushes.Transparent;
                        btnToolbarTcStart.BorderBrush = Brushes.Transparent;
                        btnToolbarTcConfig.Background = Brushes.White;
                        btnToolbarTcConfig.BorderBrush = Brushes.Black;
                        Debug.WriteLine("Config state");
                        return true;

                    default:
                        btnToolbarTcConfig.Content = (Image)FindResource("TcGrey");
                        btnToolbarTcConfig.IsEnabled = false;
                        btnToolbarTcStart.Content = (Image)FindResource("TcGrey");
                        btnToolbarTcStart.IsEnabled = false;
                        btnToolbarTcStart.Background = Brushes.Transparent;
                        btnToolbarTcStart.BorderBrush = Brushes.Transparent;
                        btnToolbarTcConfig.Background = Brushes.Transparent;
                        btnToolbarTcConfig.BorderBrush = Brushes.Transparent;
                        Debug.WriteLine("Defualt state");
                        return true;

                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
                return false;
            }
        }


        private void btnConnectPLC_Click(object sender, RoutedEventArgs e)
        {
            AdsPortConnect();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine(adsSymbolClient.IsLocal +  " IsLocal");
            Debug.WriteLine(adsSymbolClient.IsConnected + " IsConnected");
            Debug.WriteLine(adsSymbolClient.ReadState() + " State");
            Debug.WriteLine(adsSymbolClient.ReadState().ToString() + " State");
            StateInfo AdsSymbolClientState = adsSymbolClient.ReadState();

            Debug.WriteLine(adsSymbolClient.ReadState().ToString() + " adsSymbolClient ReadState");
            Debug.WriteLine(AdsSymbolClientState.AdsState + " AdsState"); // returns run/stop of PLC runtime
            Debug.WriteLine(AdsSymbolClientState.DeviceState + " DeviceState"); //does nothing

        }
    }
}