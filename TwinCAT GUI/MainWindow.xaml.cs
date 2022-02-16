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
using System.Buffers.Binary;

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
        private AdsClient adsRouter;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void AdsConnect()
        {

            //connect to the target system ADS server and PLC symbol runtime
            //ToDo make target remotely targetable, and PLC port selectable

            try
            {
                //connect to system service (runtime)
                adsSysSrv = new AdsClient();
                adsSysSrv.Connect((int)AmsPort.SystemService);
                StateInfo AdsSysServiceState = adsSysSrv.ReadState();
                
                Debug.WriteLine(adsSysSrv.ReadState().ToString());
                Debug.WriteLine(AdsSysServiceState.AdsState);
                Debug.WriteLine(AdsSysServiceState.DeviceState);

                //connect to PLC instance
                adsSymbolClient = new AdsClient();
                adsSymbolClient.Connect((int)AmsPort.PlcRuntime_851);
                StateInfo AdsSymbolClientState = adsSymbolClient.ReadState();

                Debug.WriteLine(adsSymbolClient.ReadState().ToString());
                Debug.WriteLine(AdsSymbolClientState.AdsState);
                Debug.WriteLine(AdsSymbolClientState.DeviceState);
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


        private TreeViewItem SymbolsToTreeView(Symbol symbol) {
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
                else {
                   TreeItem.Items.Add(subSymbol.InstancePath);
                    
                }
            }

            return TreeItem;
        }

        private void findSymbolInTreeView() {
            //ToDo - create search/filtering for the treeview
        
        }
        private void treeUpdateUI(string symbolstr) {
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
            Debug.WriteLine(e.UserData.ToString() + " : " + e.Value.ToString() + " Handle: "+ e.Handle.ToString() + " " + e.TimeStamp.ToString());

            //ToDo - find more efficient way rather than a for loop
            foreach (symbolListWatchItem watchedSymbol in ListViewSymbolsWatchlist.Items)
            {
                if (watchedSymbol.symbolHandle == e.Handle) {
                    watchedSymbol.symbolValue = e.Value.ToString();
                }
            }


            //for threading reasons
            Action action = () => ListViewSymbolsWatchlist.Items.Refresh();
            Dispatcher.Invoke(action);
        }



        private void SymbolNotification(bool Enable) {
            //resumes and pauses the update of the ads notifications
            if (Enable){
                adsSymbolClient.AdsNotificationEx += Client_AdsNotificationEx;
            } 
            else { 
                adsSymbolClient.AdsNotificationEx -= Client_AdsNotificationEx; 
            }
            
        }
        /////////////////////////////////////////////////////////////////////////////////////////
        // UI Button actions
        private void btnToolBarConnect_Click(object sender, RoutedEventArgs e)
        {
            AdsConnect();
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

        /////////////////////////////////////////////////////////////////////////////////////////
        //Garbage to be deleted later

        private void btnLog_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine(ListViewSymbolsWatchlist.SelectedItem);
            Debug.WriteLine(ListViewSymbolsWatchlist.Items[0].ToString()); //first item in listview
            //Debug.WriteLine(ListViewSymbolsWatchlist.)
            // ListViewSymbolsWatchlist.
            //foreach (symbolListWatchItem watchedSymbol in ListViewSymbolsWatchlist.Items) {
            //  Debug.WriteLine(watchedSymbol.symbolHandle);
            //  Debug.WriteLine(watchedSymbol);
            //  watchedSymbol.symbolValue = "hi";
            //}
            ListViewSymbolsWatchlist.Items.Refresh();
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

    }




}

/* Legacy items for posterity

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
                //treeViewSymbols.Items.Add(symbol.InstancePath.ToString());
                treeViewSymbols.Items.Add(SymbolsToTreeView(symbol));


                PropertyInfo[] properties = symbol.GetType().GetProperties();
                foreach (PropertyInfo property in properties)
                {
                    //property.SetValue(record, value);
                    //Debug.WriteLine("//////////////////////////////////////////////////////");
                    //Debug.WriteLine(property + " prop"); //gives data type and property name
                    //Debug.WriteLine(property.Name + " name"); //just give property name

                    ///the god line, returns value of property, variably
                    Debug.WriteLine(symbol.InstancePath + " " + property.Name + " " + GetPropValue(symbol, property.Name)); //just give property name
                }


        }

            }
                catch (TwinCAT.Ads.AdsErrorException err)
            {
                MessageBox.Show(err.Message + " Start service and try again");
            }

                catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }

        }

        public static object GetPropValue(object src, string propName)
        {
            return src.GetType().GetProperty(propName).GetValue(src, null);
        }
        
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            //ListViewItem lvi = new();


            ListViewSymbolsWatchlist.Items.Add(new symbolListWatchItem  {
                symbolPath = "pathsymbol",
                symbolDataType = "int",
                symbolValue = "6"
            });
        }
        
 private TreeViewItem SymbolsToTreeView(Symbol symbol) {
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
                    //if it has children, recursive callw
                    TreeItem.Items.Add(SymbolsToTreeView(subSymbol));
                    //PropertyInfo[] myPropertyInfo;
                    //Debug.WriteLine(" !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!Properties");
                    //Debug.WriteLine(subSymbol.GetType().GetProperties());
                    /*
                    myPropertyInfo = subSymbol.GetType().GetProperties();
                    Console.WriteLine("Properties of System.Type are:");

                    
                    for (int i = 0; i < myPropertyInfo.Length; i++)
                    {
                        Debug.WriteLine(myPropertyInfo[i].ToString() + " " + subSymbol.myPropertyInfo[i].ToString());
                    }

                    */

/*
//////////////////////
PropertyInfo[] properties = subSymbol.GetType().GetProperties();
foreach (PropertyInfo property in properties)
{
    //property.SetValue(record, value);
    //Debug.WriteLine(property + " prop"); //gives data type and property name
    //Debug.WriteLine(property.Name + " name"); //just give property name

    ///the god line, returns value of property, variably
    Debug.WriteLine(subSymbol.InstancePath + " " + property.Name + " " + GetPropValue(subSymbol,property.Name)); //just give property name



    //Debug.WriteLine(subSymbol + " hasvalue");
    //Debug.WriteLine(subSymbol.GetType().GetProperty(property.Name) + "property name");
    //Debug.WriteLine(subSymbol.GetType().GetProperty(property.PropertyType.ToString()));
/////////////////////////////


}

}
else
{
TreeItem.Items.Add(subSymbol.InstancePath);
//TreeItem.Items.Add(subSymbol);
/*
Debug.WriteLine(subSymbol.Connection + " Connection");
Debug.WriteLine(subSymbol.Flags + " Flags");

Debug.WriteLine(subSymbol.HasValue + " hasvalue");

Debug.WriteLine(subSymbol.ImageBaseAddress + " Imagabaseaddress");
Debug.WriteLine(subSymbol.InstancePath + " InstancePath");

Debug.WriteLine(subSymbol.IsBitType + " IsBitType");
Debug.WriteLine(subSymbol.IsBound + " IsBound");

Debug.WriteLine(subSymbol.IsDereferencedReference + " IsDereferencedReference");
Debug.WriteLine(subSymbol.IsPrimitiveType + " IsPrimitiveType");
Debug.WriteLine(subSymbol.IsReadOnly + " IsReadOnly");
Debug.WriteLine(subSymbol.IsStatic + " isstatic");



Debug.WriteLine(subSymbol.Namespace + " Namespace");
Debug.WriteLine(subSymbol.NotificationSettings + " NotificationSettings");
Debug.WriteLine(subSymbol.Parent + " Parent");
Debug.WriteLine(subSymbol.TypeName + " TypeName");

Debug.WriteLine(subSymbol.AccessRights + " AccessRights");



}
}

return TreeItem;
}




private void Button_Click(object sender, RoutedEventArgs e)
{
Debug.WriteLine("Test 1");

//ComboboxPorts.ItemsSource = Enum.GetValues(typeof(AmsPort)).Cast<AmsPort>().ToDictionary(t => (int)t, t => t.ToString());
//ComboboxPorts.ItemsSource = Enum.GetValues(typeof(AmsPort));
//ComboboxPorts.ItemsSource = Enum.GetValues(typeof(AmsPort)).Cast<AmsPort>().ToLookup(t => t.ToString(), t => (int)t);
try
{
//connect to system service
adsSysSrv = new AdsClient();
adsSysSrv.Connect((int)AmsPort.SystemService);
Debug.WriteLine(adsSysSrv.ReadState().ToString());
StateInfo test1 = adsSysSrv.ReadState();
Debug.WriteLine(test1.AdsState);
Debug.WriteLine(test1.DeviceState);
//connect to PLC
adsSymbolClient = new AdsClient();
adsSymbolClient.Connect((int)AmsPort.PlcRuntime_851);
Debug.WriteLine(adsSymbolClient.ReadState().ToString());
StateInfo test2 = adsSymbolClient.ReadState();
Debug.WriteLine(test2.AdsState);
Debug.WriteLine(test2.DeviceState);
//connect to other
adsRouter = new AdsClient();
adsRouter.Connect((int)AmsPort.Router);
Debug.WriteLine(adsRouter.ReadState().ToString());
StateInfo test3 = adsRouter.ReadState();
Debug.WriteLine(test3.AdsState);
Debug.WriteLine(test3.DeviceState);
//adsSysSrv.Synchronize = true;
Debug.WriteLine(adsSysSrv.Address);
Debug.WriteLine(adsSysSrv.ClientAddress);
Debug.WriteLine(adsSysSrv.IsConnected);
Debug.WriteLine(adsSysSrv.IsLocal);
Debug.WriteLine(adsSysSrv.Logger);
//Debug.WriteLine(adsSysSrv.);
Debug.WriteLine(adsRouter.Address);
Debug.WriteLine(adsRouter.ClientAddress);
Debug.WriteLine(adsRouter.IsConnected);
Debug.WriteLine(adsRouter.IsLocal);
Debug.WriteLine(adsRouter.Logger);



ISymbolLoader loader = SymbolLoaderFactory.Create(adsSymbolClient, SymbolLoaderSettings.Default);
/*
foreach (Symbol symbol in loader.Symbols)
{
//lists all GVLs and POUs
Debug.WriteLine(symbol.InstancePath);
Debug.WriteLine(symbol.ImageBaseAddress);
Debug.WriteLine(symbol.Namespace);
Debug.WriteLine(symbol.Parent);
Debug.WriteLine(symbol.SubSymbolCount);


foreach (Symbol subSymbol in symbol.SubSymbols)
{
    Debug.WriteLine(subSymbol.InstancePath + " " + subSymbol.ReadValue());
    Debug.WriteLine(subSymbol.Parent + " Parent");
    Debug.WriteLine(subSymbol.Namespace + " Namespace");
    Debug.WriteLine(subSymbol.IsReadOnly + " isreadlonly");
    Debug.WriteLine(subSymbol.IndexGroup + " indexgrp");
    Debug.WriteLine(subSymbol.HasValue + " hasvalue");
    Debug.WriteLine(subSymbol.Attributes + " attributes");
    Debug.WriteLine(subSymbol.Category + " Parent");
}
}


Console.WriteLine("Writing simbols");

}

catch (Exception err)
{
MessageBox.Show(err.Message);
}

}


*/
