using Microsoft.Win32;

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

namespace HashTools
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string filePath = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btn_browseFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                lbl_hashResult.Content = string.Empty;
                lbl_filePath.Content = string.Empty;
                OpenFileDialog openFileDialog = new OpenFileDialog()
                {
                    CheckFileExists = true,
                    Multiselect = false,
                    ValidateNames = true,
                    Title = "Select File to Verify Hash"
                };
                if (openFileDialog.ShowDialog() is true)
                {
                    filePath = openFileDialog.FileName;
                    lbl_filePath.Content = filePath;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"There was an error: {ex.Message}");
            }
        }

        private void btn_GenerateHash_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_verifyHash_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
