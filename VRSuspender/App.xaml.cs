using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using System.Windows;

namespace VRSuspender
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
           
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            
            MainWindow wnd = new MainWindow();

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
            Action setAlignmentValue = () =>
            {
                if (SystemParameters.MenuDropAlignment && menuDropAlignmentField != null) menuDropAlignmentField.SetValue(null, false);
            };

            setAlignmentValue();

            SystemParameters.StaticPropertyChanged += (sender, e) =>
            {
                setAlignmentValue();
            };
        }

    }
}
