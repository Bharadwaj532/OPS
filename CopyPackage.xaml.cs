using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;

namespace PackageConsole
{
    public partial class CopyPackage : Page
    {
        // Default Locations
        private const string DefaultSourceRoot = @"\\nasdslapps001\drm_pkging\RawMediaProd";
        private const string DefaultArchiveRoot = @"\\nasv0718.uhc.com\packagingarchive";
        private const string DefaultCompletedRoot = @"\\nas00036pn\Cert-Staging\2_Completed Packages";

        public CopyPackage()
        {
            InitializeComponent();
        }

        // Browse Local Path and read Package.INI file
        private void BrowseSourcePath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Source Application Path",
                Filter = "INI Files (*.ini)|*.ini|All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                SourcePathTextBox.Text = dialog.FileName;
                // Populate the folder paths dynamically
                PopulateDefaultPaths();
                // Read PRODUCT INFO Section
                var productInfo = ReadIniFile(dialog.FileName, "PRODUCT INFO");
                if (productInfo != null)
                {
                    string appVendor = productInfo["APPVENDOR"];
                    string appName = productInfo["APPNAME"];
                    string appVersion = productInfo["APPVER"];
                    string drmBuild = productInfo["DRMBUILD"];

                    string displayInfo = $"APPVENDOR: {appVendor}, " +
                                         $"APPNAME: {appName}, " +
                                         $"APPVER: {appVersion}, " +
                                         $"DRMBUILD: {drmBuild}";

                    ProductInfoTextBlock.Text = displayInfo;

                    // Set up and validate paths
                    SetupFolderStructure(appVendor, appName, appVersion, drmBuild, Path.GetDirectoryName(dialog.FileName));
                }
                else
                {
                    MessageBox.Show("PRODUCT INFO section not found in Package.INI file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void PopulateDefaultPaths()
        {
            string sourceFilePath = SourcePathTextBox.Text;

            // Validate file path
            if (!File.Exists(sourceFilePath))
            {
                MessageBox.Show("Invalid Source Application Path. Please select a valid Package.INI file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Read PRODUCT INFO from INI file
            var productInfo = ReadIniFile(sourceFilePath, "PRODUCT INFO");
            if (productInfo == null)
            {
                MessageBox.Show("PRODUCT INFO section is missing in Package.INI file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Construct default paths
            string baseArchivePath = @"\\nasv0718.uhc.com\packagingarchive";
            string baseCompletedPath = @"\\nas00036pn\Cert-Staging\2_Completed Packages";

            string expectedFolderStructure = Path.Combine(
                productInfo["APPVENDOR"] ?? "Vendor_Unknown",
                productInfo["APPNAME"] ?? "App_Unknown",
                productInfo["APPVER"] ?? "Version_Unknown",
                "Altiris",
                productInfo["DRMBUILD"] ?? "DRM_Unknown"
            );

            string archiveFullPath = Path.Combine(baseArchivePath, expectedFolderStructure);
            string completedFullPath = Path.Combine(baseCompletedPath, expectedFolderStructure);

            
            // Update the text boxes and labels dynamically
            ArchiveFolderLocationLabel.Text = "Archive Folder Location: " + baseArchivePath;
            CompletedPackageLocationLabel.Text = "Completed Package Location: " + baseCompletedPath;


            ArchiveFolderTextBox.Text = archiveFullPath;
            CompletedPackageTextBox.Text = completedFullPath;
        }

        // Validate Source Location
        private void ValidateSource_Click(object sender, RoutedEventArgs e)
        {
            string sourcePath = SourceLocationTextBox.Text;

            if (Directory.Exists(sourcePath))
            {
                MessageBox.Show("Source Location is valid.", "Validation", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Source Location does not exist. Please enter a valid path.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Create and Copy to Archive and Completed Packages
        private void SetupFolderStructure(string appVendor, string appName, string appVersion, string drmBuild, string sourcePath)
        {
            // Define Dynamic Paths
            string sourceFinalPath = Path.Combine(DefaultSourceRoot, appVendor, appName, appVersion, "Source");
            string archiveFinalPath = Path.Combine(DefaultArchiveRoot, appVendor, appName, appVersion, "Altiris", drmBuild);
            string completedFinalPath = Path.Combine(DefaultCompletedRoot, appVendor, appName, appVersion, "Altiris", drmBuild);

            // Create and Copy for Source
            EnsureDirectoryExists(sourceFinalPath);
            CopyDirectory(sourcePath, sourceFinalPath);

            // Create and Copy for Archive
            EnsureDirectoryExists(archiveFinalPath);
            CopyDirectory(sourcePath, archiveFinalPath);

            // Create and Copy for Completed Packages
            EnsureDirectoryExists(completedFinalPath);
            CopyDirectory(sourcePath, completedFinalPath);

            MessageBox.Show("Folders created and content copied successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Ensure directory exists; create if it does not
        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        // Copy Directory Contents
        private void CopyDirectory(string sourceDir, string destinationDir)
        {
            foreach (string dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourceDir, destinationDir));
            }

            foreach (string filePath in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
            {
                string targetFilePath = filePath.Replace(sourceDir, destinationDir);
                File.Copy(filePath, targetFilePath, true); // Overwrite existing files
            }
        }


        // Helper: Read INI file and extract section
        private Dictionary<string, string> ReadIniFile(string filePath, string section)
        {
            var result = new Dictionary<string, string>();
            string[] lines = File.ReadAllLines(filePath);
            bool isSection = false;

            foreach (string line in lines)
            {
                if (line.Trim().Equals($"[{section}]"))
                {
                    isSection = true;
                    continue;
                }

                if (isSection)
                {
                    if (line.StartsWith("[")) break; // Exit the section

                    var keyValue = line.Split(new[] { '=' }, 2);
                    if (keyValue.Length == 2)
                    {
                        result[keyValue[0].Trim()] = keyValue[1].Trim();
                    }
                }
            }

            return result.Count > 0 ? result : null;
        }

        private void CreateArchive_Click(object sender, RoutedEventArgs e)
        {
            string sourceFilePath = SourcePathTextBox.Text;

            // Read PRODUCT INFO values
            var productInfo = ReadIniFile(sourceFilePath, "PRODUCT INFO");
            if (productInfo == null)
            {
                MessageBox.Show("PRODUCT INFO section is missing in Package.INI file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Default base location for Package Archive
            string baseArchivePath = @"\\nasv0718.uhc.com\packagingarchive";
            string targetArchivePath = Path.Combine(
                baseArchivePath,
                productInfo["APPVENDOR"],
                productInfo["APPNAME"],
                productInfo["APPVER"],
                "Altiris",
                productInfo["DRMBUILD"]
            );

            // Create directories and copy content
            CreateAndCopyContent(sourceFilePath, targetArchivePath);
            MessageBox.Show($"Package Archive created at: {targetArchivePath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void FinalizePackage_Click(object sender, RoutedEventArgs e)
        {
            string sourceFilePath = SourcePathTextBox.Text;

            // Read PRODUCT INFO values
            var productInfo = ReadIniFile(sourceFilePath, "PRODUCT INFO");
            if (productInfo == null)
            {
                MessageBox.Show("PRODUCT INFO section is missing in Package.INI file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Default base location for Completed Packages
            string baseCompletedPath = @"\\nas00036pn\Cert-Staging\2_Completed Packages";
            string targetCompletedPath = Path.Combine(
                baseCompletedPath,
                productInfo["APPVENDOR"],
                productInfo["APPNAME"],
                productInfo["APPVER"],
                "Altiris",
                productInfo["DRMBUILD"]
            );

            // Create directories and copy content
            CreateAndCopyContent(sourceFilePath, targetCompletedPath);
            MessageBox.Show($"Package Finalized at: {targetCompletedPath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void CreateAndCopyContent(string sourceFilePath, string targetPath)
        {
            string sourceDirectory = Path.GetDirectoryName(sourceFilePath); // Current directory of the INI file
            string sourceParentDirectory = Directory.GetParent(sourceDirectory)?.Parent?.FullName;

            if (sourceParentDirectory == null || !Directory.Exists(sourceParentDirectory))
            {
                MessageBox.Show("Invalid source directory structure. Cannot determine parent folders.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Ensure target path exists
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }

            // Copy content recursively
            CopyDirectory(sourceParentDirectory, targetPath);
        }

    }
}
