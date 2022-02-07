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

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private AdsClient adsClient; //
        private AdsClient adsSysSrv;
        // private TcAdsSymbolInfoLoader symbolLoader;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Test 1");

            try
            {
                //connect to system service
                adsSysSrv = new AdsClient();
                adsSysSrv.Connect((int)AmsPort.PlcRuntime_851);
                //adsSysSrv.Synchronize = true;
                Debug.WriteLine(adsSysSrv.Address);
                Debug.WriteLine(adsSysSrv.ClientAddress);
                Debug.WriteLine(adsSysSrv.IsConnected);
                Debug.WriteLine(adsSysSrv.IsLocal);
                Debug.WriteLine(adsSysSrv.Logger);
                //Button_Click_1();


                ISymbolLoader loader = SymbolLoaderFactory.Create(adsSysSrv, SymbolLoaderSettings.Default);

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
                        Debug.WriteLine(subSymbol.Category + " category");
                    }
                }


                Console.WriteLine("Writing simbols");

            }

            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }

        }

    }

}
