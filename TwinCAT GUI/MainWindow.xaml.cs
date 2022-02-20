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


    public class SymbolListWatchItem
    {
        public uint SymbolHandle { get; set; }
        public string SymbolPath { get; set; }
        public string SymbolDataType { get; set; }
        public string SymbolValue { get; set; }
        public string SymbolTimestamp { get; set; }

    }


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private AdsClient adsPLCRuntime;
        private AdsClient adsSysSrv;

        private bool TcPLCIsConnected = false;
        
        
        public MainWindow()
        {
            InitializeComponent();

        }

        
        private void AdsSystemServiceConnect()
        {
            //connect to the target system ADS server and PLC symbol runtime
            //ToDo make target remotely targetable, and PLC port selectable
            try
            {
                //connect to system service (runtime)
                adsSysSrv = new AdsClient();
                adsSysSrv.Connect((int)AmsPort.SystemService);
                //UI update reads, connect to PLC runtime if in run mode
                if (CheckServiceState(adsSysSrv.ReadState()) == 1)
                {
                    Debug.WriteLine("System Service in Run Mode... Trying to connect to PLC");
                    AdsPLCPortConnect();
                }
                else
                {
                    Debug.WriteLine("System Service NOT in Run Mode....");
                }

            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
            adsSysSrv.RouterStateChanged += AdsSysSrv_RouterStateChanged;
        }


        private void AdsSysSrv_RouterStateChanged(object sender, AmsRouterNotificationEventArgs e)
        {
            Debug.WriteLine("System Service Router State Changed");
            Debug.WriteLine(e.State.ToString() + " AMSRouter State"); //always flags "Start for some reason"
            Debug.WriteLine(sender.ToString());
            
            //update UI icons
            Action action = () => CheckServiceState(adsSysSrv.ReadState());
            Dispatcher.Invoke(action);
        }



        private void AdsPLCPortConnect()
        {
            try
            {
                //connect to PLC instance
                adsPLCRuntime = new AdsClient();
                adsPLCRuntime.Connect((int)AmsPort.PlcRuntime_851);
                StateInfo AdsSymbolClientState = adsPLCRuntime.ReadState();

                TcPLCIsConnected = true;
                //Debug.WriteLine(adsPLCRuntime.ReadState().ToString());
                //Debug.WriteLine(AdsSymbolClientState.AdsState);
                //Debug.WriteLine(AdsSymbolClientState.DeviceState);

            }
            catch (Exception err)
            {
                TcPLCIsConnected = false;
                MessageBox.Show(err.Message);
            }

            adsPLCRuntime.AdsStateChanged += AdsPLCRuntime_AdsStateChanged;
        }


        private void AdsPLCRuntime_AdsStateChanged(object sender, AdsStateChangedEventArgs e)
        {
            Debug.WriteLine("EVENT: PLC Ads State Changed to: " + e.State.AdsState);            
            Action action = () => CheckPLCState(adsSysSrv.ReadState());
            Dispatcher.Invoke(action);

        }

        private void AdsDisconnect()
        {
            //Disconnect and cleanup all ads connections

            try
            {

                adsSysSrv.Dispose();
                adsPLCRuntime.Dispose();
                adsSysSrv.Disconnect();
                adsPLCRuntime.Disconnect();

                //ToDo Update UI

            }
            catch (Exception err)
            {
                //MessageBox.Show(err.Message);
            }
        }

        private void LoadSymbols()
        {
            //Populate the symbols into the tree
            Debug.WriteLine("Attempting to load symbols");
            //empty the treeview
            TreeViewSymbols.Items.Clear();
            
            try
            {

                ISymbolLoader loader = SymbolLoaderFactory.Create(adsPLCRuntime, SymbolLoaderSettings.Default);
                foreach (Symbol symbol in loader.Symbols)
                {
                    Debug.WriteLine("///////////////" + symbol + "///////////////");
                    TreeViewSymbols.Items.Add(SymbolsToTreeView(symbol));
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

        private void TreeUpdateUI(string symbolstr)
        {
            // Use typed object to use InfoTips
            ISymbolLoader loader = SymbolLoaderFactory.Create(adsPLCRuntime, SymbolLoaderSettings.Default);
            try
            {
                
                Symbol symbol = (Symbol)loader.Symbols[symbolstr];
                // Debug.WriteLine(symbol.ReadValue());
                Debug.WriteLine("Is Symbol Primitive? " + symbol.IsPrimitiveType);
                
                if (symbol.IsPrimitiveType)
                {
                    btnAddToWatchlist.IsEnabled = true;
                    btnWriteSymbol.IsEnabled = true;
                    UpdateSymbolDataWindow(symbol);

                }
            }
            catch (Exception err)
            {
                Debug.WriteLine(err.Message);
                btnAddToWatchlist.IsEnabled = false;
            }
        }

        private void UpdateSymbolDataWindow(Symbol symbol)
        {
           

                //populate data to the righthand window
                TxtSymbolName.Text = symbol.InstanceName;
                TxtSymbolSize.Text = symbol.Size.ToString();
                TxtSymbolDatatype.Text = symbol.DataType.ToString();
                TxtSymbolPeristence.Text = symbol.IsPersistent.ToString();
                TxtSymbolComment.Text = symbol.Comment;
                TxtSymbolValue.Text = symbol.ReadValue().ToString();
                TxtSymbolValueToWrite.Text = symbol.ReadValue().ToString();

                if (symbol.IsReadOnly)
                {
                    TxtSymbolValueToWrite.IsEnabled = false;
                }
                else
                {
                    TxtSymbolValueToWrite.IsEnabled = true;
                }
           
        }

        private void ClearSymbolDataWindow()
        {
            TxtSymbolName.Text = "";
            TxtSymbolSize.Text = "";
            TxtSymbolDatatype.Text = "";
            TxtSymbolPeristence.Text = "";
            TxtSymbolComment.Text = "";
            TxtSymbolValue.Text = "";
            TxtSymbolValueToWrite.Text = "";

            btnAddToWatchlist.IsEnabled = false;
            btnWriteSymbol.IsEnabled = false;
        }

        private void WriteSymbol(Symbol symbol, string value)
        {

            try
            {
                adsPLCRuntime.TryWriteValue(symbol, value);
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);

            }
            finally
            {
                UpdateSymbolDataWindow(symbol);
            }
        }

        private void TreeViewSymbols_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            //if the item is a primitive, allow the 'add to watchlist' button to be enabled

            if (TreeViewSymbols.SelectedItem.GetType().BaseType == typeof(Object))
            {
                TreeUpdateUI(TreeViewSymbols.SelectedItem.ToString());
                
            }
            else
            {
                ClearSymbolDataWindow();
            }
            
            //
        }


        private uint RegisterNotification(Symbol mySymbol)
        {
            // Add the Notification event handler
            uint notificationHandle; //increments automatically if called more than once

            // Check for change every 200 ms
            notificationHandle = adsPLCRuntime.AddDeviceNotificationEx(mySymbol.InstancePath, new NotificationSettings(AdsTransMode.OnChange, 200, 0), mySymbol.InstancePath, mySymbol.ReadValue().GetType());
            return notificationHandle;
        }

        private void UnRegisterNotification(uint notificationHandle)
        {
            // Add the Notification event handler
            adsPLCRuntime.DeleteDeviceNotification(notificationHandle);
        }

        private void Client_AdsNotificationEx(object sender, AdsNotificationExEventArgs e)
        {
            //This event fires anytime a handle that has been registered by the RegisterNotification changes value
            Debug.WriteLine(e.UserData.ToString() + " : " + e.Value.ToString() + " Handle: " + e.Handle.ToString() + " " + e.TimeStamp.ToString());

            //ToDo - find more efficient way rather than a for loop
            foreach (SymbolListWatchItem watchedSymbol in ListViewSymbolsWatchlist.Items)
            {
                if (watchedSymbol.SymbolHandle == e.Handle)
                {
                    watchedSymbol.SymbolValue = e.Value.ToString();
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
                adsPLCRuntime.AdsNotificationEx += Client_AdsNotificationEx;
            }
            else
            {
                adsPLCRuntime.AdsNotificationEx -= Client_AdsNotificationEx;
            }

        }


        private void SetTCState(AdsClient adsClient,AdsState state)
        {
            //Change ADS client states
            Debug.WriteLine("Setting " + (AmsPort)adsClient.Address.Port + " to state: " + state.ToString() );
            try
            {
                adsClient.TryWriteControl(new StateInfo(state, adsSysSrv.ReadState().DeviceState));

            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }
        /////////////////////////////////////////////////////////////////////////////////////////
        // UI Button actions
        private void btnToolBarConnect_Click(object sender, RoutedEventArgs e)
        {
            AdsSystemServiceConnect();
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
            
            ListViewSymbolsWatchlist.Items.Remove(ListViewSymbolsWatchlist.SelectedItem); //Remove from treeview
            SymbolListWatchItem SelectedSymbol = (SymbolListWatchItem)ListViewSymbolsWatchlist.SelectedItem;
            Debug.WriteLine("removing " + SelectedSymbol.SymbolPath + " from watchlist");
            UnRegisterNotification(SelectedSymbol.SymbolHandle); //unsubscribe from events

        }


        private void btnAddToWatchlist_Click(object sender, RoutedEventArgs e)
        {
            ISymbolLoader loader = SymbolLoaderFactory.Create(adsPLCRuntime, SymbolLoaderSettings.Default);

            Symbol symbol = (Symbol)loader.Symbols[TreeViewSymbols.SelectedItem.ToString()];

            //add to notification
            uint handleNumber = RegisterNotification(symbol);

            ListViewSymbolsWatchlist.Items.Add(new SymbolListWatchItem
            {
                SymbolHandle = handleNumber,
                SymbolPath = symbol.InstancePath,
                SymbolDataType = symbol.DataType.ToString(),
                SymbolValue = symbol.ReadValue().ToString()
            });
        }


        private void btnToolbarTcStart_Click(object sender, RoutedEventArgs e)
        {
            SetTCState(adsSysSrv, AdsState.Reset);
        }

        private void btnToolbarTcConfig_Click(object sender, RoutedEventArgs e)
        {
            SetTCState(adsSysSrv, AdsState.Reconfig);
        }

        private int CheckServiceState(StateInfo stateInfo)
        {
            Debug.WriteLine("EVENT: Checking Service State:");
            try
            {
                switch (stateInfo.AdsState)
                {
                    case AdsState.Run:
                        //style UI buttons
                        btnToolbarTcConfig.Content = (Image)FindResource("TcConfig");
                        btnToolbarTcConfig.IsEnabled = true;
                        btnToolbarTcStart.Content = (Image)FindResource("TcStart");
                        btnToolbarTcStart.IsEnabled = true;
                        btnToolbarTcStart.Background = Brushes.White;
                        btnToolbarTcStart.BorderBrush = Brushes.Black;
                        btnToolbarTcConfig.Background = Brushes.Transparent;
                        btnToolbarTcConfig.BorderBrush = Brushes.Transparent;
                        Debug.WriteLine("Service in Run state");
                        return 1;

                    case AdsState.Config:
                        //style UI buttons
                        btnToolbarTcConfig.Content = (Image)FindResource("TcConfig");
                        btnToolbarTcConfig.IsEnabled = true;
                        btnToolbarTcStart.Content = (Image)FindResource("TcStart");
                        btnToolbarTcStart.IsEnabled = true;
                        btnToolbarTcStart.Background = Brushes.Transparent;
                        btnToolbarTcStart.BorderBrush = Brushes.Transparent;
                        btnToolbarTcConfig.Background = Brushes.White;
                        btnToolbarTcConfig.BorderBrush = Brushes.Black;
                        Debug.WriteLine("Service in Config state");
                        return 0;

                    default:
                        btnToolbarTcConfig.Content = (Image)FindResource("TcGrey");
                        btnToolbarTcConfig.IsEnabled = false;
                        btnToolbarTcStart.Content = (Image)FindResource("TcGrey");
                        btnToolbarTcStart.IsEnabled = false;
                        btnToolbarTcStart.Background = Brushes.Transparent;
                        btnToolbarTcStart.BorderBrush = Brushes.Transparent;
                        btnToolbarTcConfig.Background = Brushes.Transparent;
                        btnToolbarTcConfig.BorderBrush = Brushes.Transparent;
                        Debug.WriteLine("Service in unknown state");
                        return 99;

                }

                
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
                return 99;
            }
            finally
            {
                //check PLC service, or connect if not connected
                if (stateInfo.AdsState == AdsState.Run & !TcPLCIsConnected)
                {
                    AdsPLCPortConnect();
                } 
                else{
                    CheckPLCState(adsSysSrv.ReadState());
                }
            }
        }

        private void btnLoadSymbols_Click(object sender, RoutedEventArgs e)
        {
            LoadSymbols();
        }


        private void CheckPLCState(StateInfo RouterStateInfo)
        {
            Debug.WriteLine("Checking PLC State:....");
            //Check system service state
            if (RouterStateInfo.AdsState == AdsState.Run)
            {
                //AdsState PLCState = adsPLCRuntime.ReadState().AdsState;
                switch (adsPLCRuntime.ReadState().AdsState)
                {
                    case AdsState.Run:
                        //style UI buttons
                        btnToolbarPLCStart.Foreground = Brushes.Green;
                        btnToolbarPLCStart.IsEnabled = true;
                        btnToolbarPLCStart.Background = Brushes.White;
                        btnToolbarPLCStart.BorderBrush = Brushes.Black;

                        btnToolbarPLCStop.Foreground = Brushes.Red;
                        btnToolbarPLCStop.IsEnabled = true;
                        btnToolbarPLCStop.Background = Brushes.Transparent;
                        btnToolbarPLCStop.BorderBrush = Brushes.Transparent;

                        btnLoadSymbols.IsEnabled = true;

                        Debug.WriteLine("   PLC in Run state");
                        return;

                    case AdsState.Stop:
                        //style UI buttons

                        btnToolbarPLCStart.Foreground = Brushes.Green;
                        btnToolbarPLCStart.IsEnabled = true;
                        btnToolbarPLCStart.Background = Brushes.Transparent;
                        btnToolbarPLCStart.BorderBrush = Brushes.Transparent;

                        btnToolbarPLCStop.Foreground = Brushes.Red;
                        btnToolbarPLCStop.IsEnabled = true;
                        btnToolbarPLCStop.Background = Brushes.White;
                        btnToolbarPLCStop.BorderBrush = Brushes.Black;

                        btnLoadSymbols.IsEnabled = false;

                        Debug.WriteLine("   PLC in Stop state");
                        return;
                }

            }
            else
            {
                Debug.WriteLine("   System Service in Config state, Disabling PLC contol buttons");
                btnToolbarPLCStart.Foreground = Brushes.Gray;
                btnToolbarPLCStart.IsEnabled = false;
                btnToolbarPLCStart.Background = Brushes.Transparent;
                btnToolbarPLCStart.BorderBrush = Brushes.Transparent;


                btnToolbarPLCStop.Foreground = Brushes.Gray;
                btnToolbarPLCStop.IsEnabled = false;
                btnToolbarPLCStop.Background = Brushes.Transparent;
                btnToolbarPLCStop.BorderBrush = Brushes.Transparent;

                
                return;
            }



        }


        private void btnToolbarPLCStart_Click(object sender, RoutedEventArgs e)
        {
            //Start the PLC runtime
            SetTCState(adsPLCRuntime, AdsState.Run);
        }

        private void btnToolbarPLCStop_Click(object sender, RoutedEventArgs e)
        {
            //Stop the PLC runtime
            SetTCState(adsPLCRuntime, AdsState.Stop);
        }


        private void BtnPauseSymbolUpdates_Click(object sender, RoutedEventArgs e)
        {
            //pause reading of the watchlist symbols
            SymbolNotification(false);
        }

        private void btnResumeSymbolUpdates_Click(object sender, RoutedEventArgs e)
        {
            //resume reading of the watchlist symbols
            SymbolNotification(true);
        }

        private void btnWriteSymbol_Click(object sender, RoutedEventArgs e)
        {
            //SymbolListWatchItem SelectedSymbol = (SymbolListWatchItem)ListViewSymbolsWatchlist.SelectedItem;

            ISymbolLoader loader = SymbolLoaderFactory.Create(adsPLCRuntime, SymbolLoaderSettings.Default);

            Symbol symbol = (Symbol)loader.Symbols[TreeViewSymbols.SelectedItem.ToString()];
            Debug.WriteLine("Writing " + TxtSymbolValueToWrite.Text + " to " + symbol.InstancePath);
            WriteSymbol(symbol, TxtSymbolValueToWrite.Text);
        }
    }
}