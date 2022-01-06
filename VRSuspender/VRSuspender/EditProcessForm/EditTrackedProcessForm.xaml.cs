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

namespace VRSuspender.EditProcessForm
{
    /// <summary>
    /// Interaction logic for EditSuspendedProcessForm.xaml
    /// </summary>
    public partial class EditTrackedProcessForm : Window
    {
        private EditTrackedProcessFormViewModel _espfvm;
        public EditTrackedProcessForm()
        {
            InitializeComponent();
            _espfvm = DataContext as EditTrackedProcessFormViewModel;
        }

        public EditTrackedProcessForm(TrackedProcess process) : this()
        {
            _espfvm.EditTrackedProcess(process);
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void btnCance_Click(object sender, RoutedEventArgs e)
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
