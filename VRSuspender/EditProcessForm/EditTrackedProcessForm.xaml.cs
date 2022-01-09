using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using AdonisUI;
using AdonisUI.Controls;

    
namespace VRSuspender.EditProcessForm
{
    /// <summary>
    /// Interaction logic for EditSuspendedProcessForm.xaml
    /// </summary>
    public partial class EditTrackedProcessForm : AdonisUI.Controls.AdonisWindow
    {
        private readonly EditTrackedProcessFormViewModel _espfvm;
        public EditTrackedProcessForm()
        {
            InitializeComponent();
            _espfvm = DataContext as EditTrackedProcessFormViewModel;
        }

        public EditTrackedProcessForm(TrackedProcess process) : this()
        {
            _espfvm.EditTrackedProcess(process);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public TrackedProcess GetTrackedProcess()
        {
            return _espfvm.GetTrackedProcess();
        }
    }
}
