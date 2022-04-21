using Microsoft.Win32;

using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private void txtbx_fileHash_TextChanged(object sender, TextChangedEventArgs e)
        {
            lbl_hashResult.Content = string.Empty;
            toggleButtonStatus(!string.IsNullOrWhiteSpace(txtbx_fileHash.Text));
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

        private async void btn_verifyHash_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                lbl_hashResult.Content = string.Empty;
                toggleButtonStatus(false);
                toggleProgressStatus(true);
                // TODO implement file hash format verification before processing
                string fileHash = txtbx_fileHash.Text;
                if (string.IsNullOrWhiteSpace(fileHash))
                {
                    MessageBox.Show("Invalid file hash");
                }
                else if (string.IsNullOrWhiteSpace(filePath))
                {
                    MessageBox.Show("Invalid file path");
                }
                else if (!System.IO.File.Exists(filePath))
                {
                    MessageBox.Show("File doesn't exist at the specified path");
                }
                else
                {
                    string algorithm = string.Empty;
                    if (rdibx_md4.IsChecked is true)
                    {
                        algorithm = "MD4";
                    }
                    else if (rdibx_md5.IsChecked is true)
                    {
                        algorithm = "MD5";
                    }
                    else if (rdibx_sha1.IsChecked is true)
                    {
                        algorithm = "SHA1";
                    }
                    else if (rdibx_sha256.IsChecked is true)
                    {
                        algorithm = "SHA256";
                    }
                    else if (rdibx_sha512.IsChecked is true)
                    {
                        algorithm = "SHA512";
                    }

                    if (await isFileHashValidAsync(filePath, fileHash, algorithm).ConfigureAwait(true))
                    {
                        lbl_hashResult.Content = "File hash is valid";
                        lbl_hashResult.Foreground = Brushes.Green;
                    }
                    else
                    {
                        lbl_hashResult.Content = "File hash is invalid";
                        lbl_hashResult.Foreground = Brushes.Red;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"There was an error: {ex.Message}");
            }
            toggleButtonStatus(true);
            toggleProgressStatus(false);
        }

        Task<bool> isFileHashValidAsync(string filePath, string fileHash, string algorithm)
        {
            return Task.Run(() =>
            {
                ProcessStartInfo psi = new ProcessStartInfo("certutil")
                {
                    Arguments = $"-hashfile {filePath} {algorithm}",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };
                bool isValid = false;
                string processOutput = string.Empty;
                using (Process p = new Process())
                {
                    p.StartInfo = psi;
                    p.Start();
                    processOutput = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                }
                if (!string.IsNullOrWhiteSpace(processOutput))
                {
                    var lines = processOutput.Split(new[] { '\r', '\n' });
                    isValid = lines.Any(hash => string.Equals(hash, fileHash, StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    MessageBox.Show("There was an error. Please try again");
                }

                return isValid;
            });
        }

        void toggleButtonStatus(bool isEnabled)
        {
            btn_browseFile.IsEnabled = isEnabled;
            btn_verifyHash.IsEnabled = isEnabled;
            btn_GenerateHash.IsEnabled = isEnabled;
        }

        void toggleProgressStatus(bool isVisible)
        {
            prgs_status.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
        }
    }
}
