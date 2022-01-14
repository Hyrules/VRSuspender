using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using System.Windows;

namespace VRSuspender
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Mutex _mutex = null;

        public App()
        {
           
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {

            const string appName = "VRSuspender";
            bool createdNew;

            _mutex = new Mutex(true, appName, out createdNew);

            if (!createdNew)
            {
                //app is already running! Exiting the application  
                AdonisUI.Controls.MessageBox.Show("Only one instance of VRSuspender is possible at a time.","Error", AdonisUI.Controls.MessageBoxButton.OK, AdonisUI.Controls.MessageBoxImage.Error);
                return;
            }

            MainWindow wnd = new();

            switch (VRSuspender.Properties.Settings.Default.StartState)
            {
                case 0:
                    wnd.Show();
                    break;
                case 1:
                    wnd.Show();
                    wnd.WindowState = WindowState.Minimized;
                    break;
                case 2:                    
                    wnd.Show();
                    wnd.WindowState = WindowState.Maximized;
                    break;
                case 3:
                    wnd.Hide();
                    break;
                default:
                    wnd.Show();
                    break;
            }

            SetDropDownMenuToBeRightAligned();
 

        }
        private static void SetDropDownMenuToBeRightAligned()
        {
            var menuDropAlignmentField = typeof(SystemParameters).GetField("_menuDropAlignment", BindingFlags.NonPublic | BindingFlags.Static);
            
            void setAlignmentValue()
            {
                if (SystemParameters.MenuDropAlignment && menuDropAlignmentField != null) menuDropAlignmentField.SetValue(null, false);
            }

            setAlignmentValue();

            SystemParameters.StaticPropertyChanged += (sender, e) =>
            {
                setAlignmentValue();
            };
        }

    }
}
