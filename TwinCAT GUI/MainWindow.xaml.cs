﻿using System;
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
using System.Linq;
using TwinCAT;
using TwinCAT.Ads;
using TwinCAT.TypeSystem;
using TwinCAT.Ads.TypeSystem;
using TwinCAT.Ads.TcpRouter;

namespace TwinCAT_GUI
{

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

                //IAmsRouter routerRoutes = RouteCollection
                //adsRouter.

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
                //Debug.WriteLine(UIMode);
                //symbolLoader = adsClient.CreateSymbolInfoLoader();
                //TcAdsSymbolInfo symbol = symbolLoader.GetFirstSymbol(true);


                //TcAdsSymbolInfo symbol = loader.GetFirstSymbol(true);

                //while (symbol != null)
                //{
                //treeViewSymbols.Nodes.Add(CreateNewNode(symbol));
                //symbol = symbol.NextSymbol;
                //}


                ISymbolLoader loader = SymbolLoaderFactory.Create(adsClient, SymbolLoaderSettings.Default);
                foreach (Symbol symbol in loader.Symbols)
                {
                    treeViewSymbols.Items.Add(symbol.InstancePath.ToString());
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
        /*
        private TreeNode CreateNewNode(TcAdsSymbolInfo symbol)
        {
            //Debug.WriteLine("Creating" + symbol);
            TreeNode node = new TreeNode(symbol.Name);
            node.Tag = symbol;
            TcAdsSymbolInfoCollection subSymbols = symbol.SubSymbols;
            foreach (TcAdsSymbolInfo sub in subSymbols)
            {
                //    Debug.WriteLine(sub);
                node.Nodes.Add(CreateNewNode(sub));
            }
            return node;
        }
        */

        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            LoadSymbols();
        }
    }

}
