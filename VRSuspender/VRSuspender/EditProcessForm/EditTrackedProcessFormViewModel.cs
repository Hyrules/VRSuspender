using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using VRSuspender.Utils;

namespace VRSuspender.EditProcessForm
{
    public class EditTrackedProcessFormViewModel : ValidatableBindableBase
    {
        public EditTrackedProcessFormViewModel()
        {

        }

        public ICommand BrowseExecutableCommand => new RelayCommand(param => BrowseExecutable());

        private void BrowseExecutable()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Executable file (*.exe)|*.exe";
            ofd.ShowDialog();

        }
    }
}
