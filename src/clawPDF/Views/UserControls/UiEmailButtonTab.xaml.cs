using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using clawSoft.clawPDF.Shared.Helper;
using clawSoft.clawPDF.ViewModels.UserControls;

namespace clawSoft.clawPDF.Views.UserControls
{
    internal partial class UiEmailButtonTab
    {
        public UiEmailButtonTab()
        {
            InitializeComponent();
        }

        public UiEmailButtonTabViewModel ViewModel => (UiEmailButtonTabViewModel)DataContext;

        private void BrowseCommandButton_OnClick(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            if (dlg.ShowDialog() == true)
                CommandPathTextBox.Text = dlg.FileName;
        }

        private void CommandPathTokenComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CommandPathTokenComboBox.SelectedIndex < 0)
                return;

            TextBoxHelper.InsertText(CommandPathTextBox,
                CommandPathTokenComboBox.Items[CommandPathTokenComboBox.SelectedIndex] as string);
            CommandPathTokenComboBox.SelectedIndex = -1;
        }

        private void CommandArgumentsTokenComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CommandArgumentsTokenComboBox.SelectedIndex < 0)
                return;

            TextBoxHelper.InsertText(CommandArgumentsTextBox,
                CommandArgumentsTokenComboBox.Items[CommandArgumentsTokenComboBox.SelectedIndex] as string);
            CommandArgumentsTokenComboBox.SelectedIndex = -1;
        }
    }
}
