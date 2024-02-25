using IWshRuntimeLibrary;

using Microsoft.Win32;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HashTools
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Path of the selected file
        /// </summary>
        string filePath = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
            startupCheck();

            // check if the application is already added to the file explorer context menu/
            // and disable the 'Add to Context Menu' setting button
            if (isAlreadyAddedToContextMenu())
            {
                menu_settings.IsEnabled = false;
                menu_settings.ToolTip = "Already added to  file explorer context menu!";
            }
        }

        private void window_DragEnter(object sender, DragEventArgs e)
        {
            Cursor = Cursors.Hand;
        }

        private void window_DragLeave(object sender, DragEventArgs e)
        {
            Cursor = Cursors.Arrow;
        }

        private void window_Drop(object sender, DragEventArgs e)
        {
            try
            {
                // clear status indication texts
                txtblk_run_algorithmType.Text = string.Empty;
                txtblk_run_fileInfo.Text = string.Empty;
                txtblk_hashResult.Text = string.Empty;
                if (e.Data.GetDataPresent(DataFormats.FileDrop))    // check if a file is present when the user drag and drops in the app 
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);    // get list of files the user dropped
                    if (files.Any())
                    {
                        filePath = files[0];    // only select a single file for the operation
                        txtblk_filePath.Text = filePath;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"There was an error: {ex.Message}");
            }
        }

        private void menu_settings_Click(object sender, RoutedEventArgs e)
        {
            string msg = "This will edit the Registry, add this tool to your Local AppData Directory and start menu." +
                "\n\nDo you wish to proceed?";
            var response = MessageBox.Show(this, msg, "Note", MessageBoxButton.OKCancel, MessageBoxImage.Information);
            if (response is MessageBoxResult.OK)
            {
                // copy application to Local AppData folder
                var appPath = copyExecutableToLocalAppData();
                if (!string.IsNullOrWhiteSpace(appPath))
                {
                    // for future implementation to enable user to set for single user or all users
                    //
                    //bool restartAfterAdding = true;
                    //if (!isRunningAsAdministrator())
                    //{
                    //    restartAfterAdding = false;
                    //    restartApplication(appPath, true, "--add-context-menu");
                    //}

                    // add application to start menu
                    if (!addAppToStartMenu(appPath))
                    {
                        msg = "Failed to add application to Start Menu! " +
                            $"\nYou can add it from the below location manually if you like.\n\n{appPath}";
                        MessageBox.Show(msg, "Note", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    // add application to registry for file explorer context menu option
                    if (!addContextMenuToRegistry(appPath))
                    {
                        msg = "Failed to add application to the Registry! Please try again.";
                        MessageBox.Show(msg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        // disable the 'Add to Context Menu' setting button
                        menu_settings.IsEnabled = false;
                        menu_settings.ToolTip = "Already added to  file explorer context menu!";
                        restartApplication(appPath);
                    }
                }
            }
        }

        private void txtbx_fileHash_TextChanged(object sender, TextChangedEventArgs e)
        {
            // clear status indication texts
            txtblk_run_algorithmType.Text = string.Empty;
            txtblk_run_fileInfo.Text = string.Empty;
            txtblk_hashResult.Text = string.Empty;
            //toggleButtonStatus(!string.IsNullOrWhiteSpace(txtbx_fileHash.Text));
        }

        private void btn_browseFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // clear status indication texts
                txtblk_filePath.Text = string.Empty;
                txtblk_run_algorithmType.Text = string.Empty;
                txtblk_run_fileInfo.Text = string.Empty;
                txtblk_hashResult.Text = string.Empty;

                OpenFileDialog openFileDialog = new OpenFileDialog()    // open a dialog to let the user chose a file
                {
                    CheckFileExists = true,
                    Multiselect = false,
                    ValidateNames = true,
                    Title = "Select File to Verify Hash"
                };
                if (openFileDialog.ShowDialog() is true)
                {
                    // store and display the selected file path
                    filePath = openFileDialog.FileName;
                    txtblk_filePath.Text = filePath;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"There was an error: {ex.Message}");
            }
        }

        private async void btn_GenerateHash_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // clear status indication texts
                txtblk_run_algorithmType.Text = string.Empty;
                txtblk_run_fileInfo.Text = string.Empty;
                txtblk_hashResult.Text = string.Empty;
                //toggleButtonStatus(false);
                toggleProgressStatus(true);     // show a progress while app generates file hash
                string filePath = txtblk_filePath.Text;
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    MessageBox.Show("Invalid file path");
                }
                else if (!System.IO.File.Exists(filePath))      // check if specified file exists
                {
                    MessageBox.Show("File doesn't exist at the specified path");
                }
                else
                {
                    // prepare the algorithm type based on user selection
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
                    // generate the file hash for user specified file 
                    var hash = await generateHashAsync(filePath, algorithm).ConfigureAwait(true);
                    if (!string.IsNullOrWhiteSpace(hash))
                    {
                        // display generated file hash details
                        txtblk_run_algorithmType.Text = algorithm;
                        txtblk_run_fileInfo.Text = $" hash of {filePath}";
                        txtblk_hashResult.Text = hash.ToUpper();
                        txtblk_hashResult.Foreground = Brushes.Green;
                        btn_copyFileHash.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        // display error message when file hash generation fails
                        txtblk_hashResult.Text = "Failed to generate file hash!";
                        txtblk_hashResult.Foreground = Brushes.Red;
                        btn_copyFileHash.Visibility = Visibility.Hidden;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"There was an error: {ex.Message}");
            }
            //toggleButtonStatus(true);
            toggleProgressStatus(false);    // hide progress bar 
        }

        private async void btn_verifyHash_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // clear status indication texts
                txtblk_run_algorithmType.Text = string.Empty;
                txtblk_run_fileInfo.Text = string.Empty;
                txtblk_hashResult.Text = string.Empty;
                //toggleButtonStatus(false);
                toggleProgressStatus(true);     // show a progress while app verifies file hash

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
                else if (!System.IO.File.Exists(filePath))      // check if specified file exists
                {
                    MessageBox.Show("File doesn't exist at the specified path");
                }
                else
                {
                    // prepare the algorithm type based on user selection
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

                    // check if the selected file's hash is valid against the specified hash
                    if (await isFileHashValidAsync(filePath, fileHash, algorithm).ConfigureAwait(true))
                    {
                        txtblk_hashResult.Text = "File hash is valid";
                        txtblk_hashResult.Foreground = Brushes.Green;
                    }
                    else
                    {
                        txtblk_hashResult.Text = "File hash is invalid";
                        txtblk_hashResult.Foreground = Brushes.Red;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"There was an error: {ex.Message}");
            }
            //toggleButtonStatus(true);
            toggleProgressStatus(false);    // hide progress bar 
        }

        private void btn_copyFileHash_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtblk_hashResult.Text))
                Clipboard.SetText(txtblk_hashResult.Text);
        }

        void toggleButtonStatus(bool isEnabled)
        {
            btn_browseFile.IsEnabled = isEnabled;
            btn_verifyHash.IsEnabled = isEnabled;
            btn_GenerateHash.IsEnabled = isEnabled;
        }

        /// <summary>
        /// Toggle the visibility of the status progress bar.
        /// </summary>
        /// <param name="isVisible">Whether to show or hide progress bar.</param>
        void toggleProgressStatus(bool isVisible)
        {
            // set the visibility of the progress bar
            prgs_status.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
        }

        /// <summary>
        /// Check if any arguments are passed when application starts. 
        /// If arguments were passed, perform the corresponding action.
        /// </summary>
        void startupCheck()
        {
            string[] args = Environment.GetCommandLineArgs();   // get the list of arguments passed to the app when it starts
            if (args.Length > 1)    // check if arguments are available
            {
                var cmd = args[1];      // get the second argument (the first argument is the executable path itself)

                // argument to open a file with this application (normally used from file explorer's context menu)
                if (string.Equals(cmd, "--open-file", StringComparison.OrdinalIgnoreCase))
                {
                    if (args.Length > 2)    // get the third argument which will be the file path in this context
                    {
                        var filePath = args[2];
                        if (System.IO.File.Exists(filePath))    // check if file exist
                        {
                            // store and display the file path
                            this.filePath = filePath;
                            txtblk_filePath.Text = filePath;
                        }
                        else
                        {
                            string msg = "Invalid file path specified!";
                            MessageBox.Show(msg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                // add file explorer context menu option
                else if (string.Equals(cmd, "--add-context-menu", StringComparison.OrdinalIgnoreCase))
                {
                    if (!isAlreadyAddedToContextMenu())     // check if application is already added to the context menu
                    {
                        // get the directory the app executable is currently in
                        var exeLocation = Assembly.GetExecutingAssembly().Location;
                        // get the local app data directory of the application
                        string localAppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HashTools");
                        // check if the application is inside its local app data folder
                        if (string.Equals(exeLocation, localAppDataPath, StringComparison.OrdinalIgnoreCase))
                        {
                            addContextMenuToRegistry(exeLocation);      // add application's option to file explorer context menu
                            addAppToStartMenu(exeLocation);     // add application to start menu
                        }
                    }
                }
                // remove application's option from file explorer context menu and start menu
                else if (string.Equals(cmd, "--remove-tool", StringComparison.OrdinalIgnoreCase))
                {
                    removeContextMenuFromRegistry();    // remove application's option from file explorer context menu
                    removeAppFromStartMenu();       // remove application from start menu
                    Application.Current.Shutdown();
                }
            }
        }

        /// <summary>
        /// Check if application's option is already added to file explorer's context menu.
        /// </summary>
        /// <returns>True if already added to menu, false otherwise.</returns>
        bool isAlreadyAddedToContextMenu()
        {
            bool isAdded = false;
            try
            {
                // check context menu option in the registry key of the current user
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\*\shell", false))
                {
                    var subkeys = key.GetSubKeyNames();     // get the list of sub-key names
                    // check if application's key exist in the list of sub-keys
                    isAdded = subkeys.Any(s => string.Equals(s, "Open in Hash Tools", StringComparison.OrdinalIgnoreCase));
                    key.Close();
                }
            }
            catch (Exception)
            {

            }
            return isAdded;
        }

        /// <summary>
        /// Check if the application is running as administrator or not.
        /// </summary>
        /// <returns>True if app is running as administrator, false otherwise.</returns>
        bool isRunningAsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <summary>
        /// Restart the current application. 
        /// </summary>
        /// <param name="filePathToStart">Optional path of an executable to start. If null, the use the current executable itself.</param>
        /// <param name="asAdmin">Whether to start the new process as an administrator or normally.</param>
        /// <param name="args">Optional arguments for the new process.</param>
        void restartApplication(string filePathToStart = null, bool asAdmin = false, params string[] args)
        {
            try
            {
                using (Process process = new Process())
                {
                    string arguments = string.Empty;
                    // prepare the arguments parameters if available
                    if (args.Length > 0)
                    {
                        foreach (var arg in args)
                        {
                            arguments = arg + " ";
                            arguments.Trim();
                        }
                    }
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = filePathToStart,
                        UseShellExecute = true,
                        Verb = asAdmin ? "runas" : "",
                        Arguments = arguments
                    };
                    if (process.Start())    // start the new process and see if successfully stared
                    {
                        Application.Current.Shutdown();     // close the current open process.
                    }
                }
            }
            catch (Exception ex)
            {
                string msg = $"Failed to restart application as administrator! Please try running the app as administrator and try again. {ex}";
                MessageBox.Show(msg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Copy the current executable to the Local App Data directory.
        /// </summary>
        /// <returns>The path of the copied executable file. If failed to copy, an empty string is returned.</returns>
        string copyExecutableToLocalAppData()
        {
            string appPath = string.Empty;
            try
            {
                // get the local app data folder path
                string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                // get the file location the current process's executable file
                var exe = Assembly.GetExecutingAssembly().Location;
                // prepare the local app data folder for the application
                string destinationFolder = Path.Combine(localAppDataPath, "HashTools");

                // check if the local app data folder for the application exists
                // and delete it before copying the executable file
                if (Directory.Exists(destinationFolder))
                {
                    Directory.Delete(destinationFolder, true);
                }
                Directory.CreateDirectory(destinationFolder);   // create local app data folder for the application
                // prepare the destination file path of the executable file
                var destinationPath = Path.Combine(destinationFolder, Path.GetFileName(exe));      
                var data = System.IO.File.ReadAllBytes(exe);    // read all bytes of the current running executable file
                // create the destination executable file from the bytes of the current running executable
                System.IO.File.WriteAllBytes(destinationPath, data);    
                appPath = destinationPath;
            }
            catch (Exception)
            {

            }
            return appPath;
        }

        /// <summary>
        /// Add application's option to the file explorer's context menu.
        /// </summary>
        /// <param name="appPath">The executable path of the application.</param>
        /// <returns>True if added successfully, false otherwise.</returns>
        bool addContextMenuToRegistry(string appPath)
        {
            bool isAdded = false;
            try
            {
                // get the required registry key for the current user
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\*\shell", true))
                using(RegistryKey newKey = key.CreateSubKey("Open in Hash Tools")) // create a new sub key for the application
                using (RegistryKey subNewkey = newKey.CreateSubKey("command"))      // create the mandatory command key
                {
                    // set the value of the command key and pass the selected file's path as an argument ("%1")
                    subNewkey.SetValue("", $"{appPath} --open-file \"%1\"");
                    // close the opened keys
                    subNewkey.Close();
                    newKey.Close();
                    key.Close();
                    isAdded = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Adding context menu failed: {ex}");
            }
            return isAdded;
        }

        /// <summary>
        /// Remove the application's option from file explorer's context menu
        /// </summary>
        /// <returns>True if removed successfully, false otherwise</returns>
        bool removeContextMenuFromRegistry()
        {
            bool isRemoved = false;
            try
            {
                // get the required registry key for the current user
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\*\shell", true))
                {
                    // delete all contents of the sub key associated with this application
                    key.DeleteSubKeyTree("Open in Hash Tools");
                    key.Close();    // close the opened key
                    isRemoved = true;
                }
            }
            catch (Exception)
            {

            }
            return isRemoved;
        }

        /// <summary>
        /// Add this application to the start menu.
        /// </summary>
        /// <param name="appPath">The path to the applications's executable file.</param>
        /// <returns>True if added successfully, false otherwise.</returns>
        bool addAppToStartMenu(string appPath)
        {
            bool isAdded = false;
            try
            {
                // get the start menu folder for the current user
                string startMenuFolder = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
                // prepare the start menu path for the application
                string startMenuPath = Path.Combine(startMenuFolder, "Programs", "Hash Tools");

                // if the directory already exist, remove it
                if (Directory.Exists(startMenuPath))
                {
                    Directory.Delete(startMenuPath, true);
                }
                Directory.CreateDirectory(startMenuPath);   // create the application's start menu directory

                // create a shortcut to launch the application
                createShortcut("Hash Tools", "Open Hash Tools", startMenuPath, appPath);
                // create a shortcut to remove the application
                createShortcut("Remove Hash Tools", "Remove Hash Tools", startMenuPath, appPath, "--remove-tool");
                isAdded = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Adding startup menu failed: {ex}");
            }
            return isAdded;

        }

        /// <summary>
        /// Remove this application from start menu.
        /// </summary>
        /// <returns>True if removed successfully, false otherwise.</returns>
        bool removeAppFromStartMenu()
        {
            bool isRemoved = false;
            try
            {
                // get the start menu folder for the current user
                string startMenuFolder = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
                // prepare the application's start menu path
                string startMenuPath = Path.Combine(startMenuFolder, "Programs", "Hash Tools");

                // remove the directory if it exists
                if (Directory.Exists(startMenuPath))
                {
                    Directory.Delete(startMenuPath, true);
                }
                isRemoved = true;
            }
            catch (Exception)
            {

            }
            return isRemoved;

        }

        /// <summary>
        /// Calculate file hash for a file.
        /// </summary>
        /// <param name="filePath">The path of the file to calculate hash for.</param>
        /// <param name="algorithm">The algorithm to use for the hash (MD4, MD5, SHA1, SHA256, SHA512).</param>
        /// <returns>Returns the calculated file hash. An empty string will be returned if calculation failed.</returns>
        Task<string> generateHashAsync(string filePath, string algorithm)
        {
            return Task.Run(() =>
            {
                // prepare the information used to start the certutil process
                ProcessStartInfo psi = new ProcessStartInfo("certutil")
                {
                    Arguments = $"-hashfile \"{filePath}\" {algorithm}",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,     // to get the error from the process
                    RedirectStandardOutput = true     // to get the output from the process
                };
                string fileHash = string.Empty;
                string processOutput = string.Empty;
                using (Process p = new Process())
                {
                    p.StartInfo = psi;
                    p.Start();      // start the certutil process
                    processOutput = p.StandardOutput.ReadToEnd();       // read the output of the certutil process
                    p.WaitForExit();        // wait for the process to exit
                }
                if (!string.IsNullOrWhiteSpace(processOutput))      // check if the output form the certutil process is not empty
                {
                    var lines = processOutput.Split(new[] { '\r', '\n' });      // split the output by lines
                    // the line that contains the file hash will not have any space in it
                    // so use that condition to get the file hash
                    fileHash = lines.FirstOrDefault(hash => !string.IsNullOrWhiteSpace(hash) && !hash.Contains(" "));
                }
                else
                {
                    MessageBox.Show("There was an error. Please try again");
                }

                return fileHash;
            });


            //// use a task completion source to run IO bound processes
            //TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
            //try
            //{
            //    // prepare the information used to start the certutil process
            //    ProcessStartInfo psi = new ProcessStartInfo("certutil")
            //    {
            //        Arguments = $"-hashfile \"{filePath}\" {algorithm}",
            //        CreateNoWindow = true,
            //        UseShellExecute = false,
            //        RedirectStandardError = true,     // to get the error from the process
            //        RedirectStandardOutput = true     // to get the output from the process
            //    };
            //    string fileHash = string.Empty;
            //    string processOutput = string.Empty;
            //    using (Process p = new Process())   
            //    {
            //        p.StartInfo = psi;  
            //        p.Start();      // start the certutil process
            //        processOutput = p.StandardOutput.ReadToEnd();       // read the output of the certutil process
            //        p.WaitForExit();        // wait for the process to exit
            //    }
            //    if (!string.IsNullOrWhiteSpace(processOutput))      // check if the output form the certutil process is not empty
            //    {
            //        var lines = processOutput.Split(new[] { '\r', '\n' });      // split the output by lines
            //        // the line that contains the file hash will not have any space in it
            //        // so use that condition to get the file hash
            //        fileHash = lines.FirstOrDefault(hash => !string.IsNullOrWhiteSpace(hash) && !hash.Contains(" "));
            //    }
            //    else
            //    {
            //        MessageBox.Show("There was an error. Please try again");
            //    }
            //    tcs.SetResult(fileHash);
            //}
            //catch (Exception ex)
            //{
            //    tcs.SetException(ex);
            //}

            //return tcs.Task;
        }

        /// <summary>
        /// Verify hash of a file.
        /// </summary>
        /// <param name="filePath">The path of the file to verify.</param>
        /// <param name="fileHash">The hash of the file to verify.</param>
        /// <param name="algorithm">The algorithm to use for verification. 
        /// The same one used to generate the file hash (MD4, MD5, SHA1, SHA256, SHA512).</param>
        /// <returns>True if file hash is valid against the provided hash, false otherwise.</returns>
        Task<bool> isFileHashValidAsync(string filePath, string fileHash, string algorithm)
        {
            return Task.Run(() =>
            {
                ProcessStartInfo psi = new ProcessStartInfo("certutil")
                {
                    Arguments = $"-hashfile \"{filePath}\" {algorithm}",
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
                    isValid = lines.Any(hash => string.Equals(hash.ToLower(), fileHash.ToLower(), StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    MessageBox.Show("There was an error. Please try again");
                }

                return isValid;
            });
        }

        /// <summary>
        /// Create a shortcut for an executable.
        /// </summary>
        /// <param name="name">The name of the shortcut.</param>
        /// <param name="description">A description for the shortcut.</param>
        /// <param name="path">The directory path to create the shortcut in.</param>
        /// <param name="targetFileLocation">The path of the executable to launch by the shortcut.</param>
        /// <param name="args">Optional argument parameters for the executable.</param>
        void createShortcut(string name, string description, string path, string targetFileLocation, params string[] args)
        {
            // prepare the file path of the shortcut
            string shortcutLocation = Path.Combine(path, name + ".lnk");

            WshShell shell = new WshShell();    // get a windows script host COM object
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutLocation);   // create a shortcut object 
            shortcut.Description = description;         // The description of the shortcut
            shortcut.TargetPath = targetFileLocation;       // The path of the file that will launch when the shortcut is run
            // add arguments if available
            if (args.Length > 0)
            {
                foreach (var arg in args)
                {
                    shortcut.Arguments += arg + " ";
                }
                shortcut.Arguments.Trim();
            }
            shortcut.Save();                                    // Save the shortcut
        }
    }
}
