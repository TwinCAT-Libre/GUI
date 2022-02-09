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
using System.Linq; //
using System.Reflection; //
using TwinCAT;
using TwinCAT.Ads;
using TwinCAT.TypeSystem;
using TwinCAT.Ads.TypeSystem;
using TwinCAT.Ads.TcpRouter;

namespace TwinCAT_GUI
{


    public class symbolListWatchItem
    {
        public string symbolPath { get; set; }
        public string symbolDataType { get; set; }

        public string symbolValue { get; set; }
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private AdsClient adsClient;
        private AdsClient adsSysSrv;
        private AdsClient adsRouter;
        // private TcAdsSymbolInfoLoader symbolLoader;

        public MainWindow()
        {
            InitializeComponent();
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
                adsClient = new AdsClient();
                adsClient.Connect((int)AmsPort.PlcRuntime_851);
                Debug.WriteLine(adsClient.ReadState().ToString());
                StateInfo test2 = adsClient.ReadState();
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
                


                ISymbolLoader loader = SymbolLoaderFactory.Create(adsClient, SymbolLoaderSettings.Default);

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

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }


        private void LoadSymbols()
        {
            treeViewSymbols.Items.Clear();
            //Debug.WriteLine("Attempting to load symbols");
            try
            {
                ISymbolLoader loader = SymbolLoaderFactory.Create(adsClient, SymbolLoaderSettings.Default);
                foreach (Symbol symbol in loader.Symbols)
                {
                    Debug.WriteLine("//////////////////////////////////////////////////////" + symbol);
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

        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            LoadSymbols();
        }

        private TreeViewItem SymbolsToTreeView(Symbol symbol) {
            TreeViewItem TreeItem = new TreeViewItem();
            TreeItem.Header = symbol.InstancePath;
            
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

                    //////////////////////
                    PropertyInfo[] properties = subSymbol.GetType().GetProperties();
                    foreach (PropertyInfo property in properties)
                    {
                        //property.SetValue(record, value);
                        //Debug.WriteLine(property + " prop"); //gives data type and property name
                        //Debug.WriteLine(property.Name + " name"); //just give property name

                        ///the god line, returns value of property, variably
                        //////////////////Debug.WriteLine(subSymbol.InstancePath + " " + property.Name + " " + GetPropValue(subSymbol,property.Name)); //just give property name



                        //Debug.WriteLine(subSymbol + " hasvalue");
                        //Debug.WriteLine(subSymbol.GetType().GetProperty(property.Name) + "property name");
                        //Debug.WriteLine(subSymbol.GetType().GetProperty(property.PropertyType.ToString()));
                    /////////////////////////////


                    }

                }
                else {
                    TreeItem.Items.Add(subSymbol.InstanceName);
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
                    */


                }
            }

            return TreeItem;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine(treeViewSymbols.SelectedItem.ToString());
            Debug.WriteLine(treeViewSymbols.SelectedItem.GetType());
            
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
    }

}
