using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using NLog;

namespace PackageConsole
{
    public partial class IniConsolePage : Page, IIniContext
    {
        private string iniFilePath;
        private readonly string productFolder;
        private readonly string supportFilesFolder;
        // public Dictionary<string, Dictionary<string, string>> iniSections { get; private set; }
        public Dictionary<string, Dictionary<string, string>> iniSections { get; private set; } = new();
        public Dictionary<string, Dictionary<string, string>> IniSections => iniSections;
        private List<string> rawContent;
        //public List<string> GetSectionNames()
        //{            return iniSections?.Keys.ToList() ?? new List<string>();        }
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // Feature: Backup for Undo
        private Dictionary<string, Dictionary<string, string>> lastBackup;
        private bool deleteModeEnabled = false;

        public bool HasSection(string sectionName)
        {
            return iniSections.Keys.Any(sec => sec.Trim().Equals(sectionName.Trim(), StringComparison.OrdinalIgnoreCase));
        }
        public Dictionary<string, string> GetKeyValues(string section)
        {
            if (iniSections != null && iniSections.ContainsKey(section))
            {
                return iniSections[section];
            }
            return new Dictionary<string, string>();
        }
        public IniConsolePage(string productFolder, string supportFilesFolder)
        {
            InitializeComponent();
            this.productFolder = productFolder;
            this.supportFilesFolder = supportFilesFolder;
            Logger.Info($"IniConsolePage initialized at location: {supportFilesFolder}");
            richIniContent.Document.PageWidth = 1000; // Adjust width if needed

            iniFilePath = System.IO.Path.Combine(supportFilesFolder, "Package.ini");
            LoadIniFile();
            RefreshIniContent();  //  Update UI
        }
        public void UpdateIniSection(string section, Dictionary<string, string> updatedValues)
        {
            if (!iniSections.ContainsKey(section))
            {
                MessageBox.Show($"Error: Section '{section}' not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            iniSections[section] = updatedValues;

            SaveIniFile();  //  Save changes to INI file
            RefreshIniContent();  //  Update UI
        }
        public void UpdateIniSections(Dictionary<string, Dictionary<string, string>> updatedSections)
        {
            iniSections = updatedSections;
            SaveIniFile();  //  Save changes
            RefreshIniContent();  //  Update UI
        }
        private void LoadIniFile()
        {
            if (!File.Exists(iniFilePath))
            {
                MessageBox.Show($"INI file not found at: {iniFilePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                iniSections = IniFileHelper.ParseIniFile(iniFilePath, out rawContent);
                BackupCurrentState(); // Save initial state
                cmbSections.ItemsSource = iniSections.Keys.ToList();
                // Display in RichTextBox
                richIniContent.Document.Blocks.Clear();
                richIniContent.Document.Blocks.Add(new Paragraph(new Run(
                    string.Join(Environment.NewLine, rawContent)
                )));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error loading INI file");
                MessageBox.Show($"Error loading INI: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BackupCurrentState()
        {
            lastBackup = iniSections.ToDictionary(
                section => section.Key,
                section => section.Value.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
        }
        private void cmbSections_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            stackKeyValueEditor.Children.Clear();

            if (cmbSections.SelectedItem is string selectedSection &&
                iniSections.TryGetValue(selectedSection, out Dictionary<string, string> keyValues))
            {
                foreach (var kvp in keyValues)
                {
                    var grid = new Grid { Margin = new Thickness(5) };
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition());

                    if (deleteModeEnabled)  // Add third column if delete mode is ON
                        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });

                    var lbl = new TextBlock
                    {
                        Text = kvp.Key,
                        Foreground = Brushes.White,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    var txt = new TextBox
                    {
                        Text = kvp.Value,
                        Tag = kvp.Key,
                        Margin = new Thickness(5, 0, 0, 0)
                    };

                    Grid.SetColumn(lbl, 0);
                    Grid.SetColumn(txt, 1);

                    grid.Children.Add(lbl);
                    grid.Children.Add(txt);

                    if (deleteModeEnabled)
                    {
                        var btnDelete = new Button
                        {
                            Content = "❌",
                            Width = 25,
                            Height = 25,
                            Margin = new Thickness(5, 0, 0, 0),
                            Tag = kvp.Key
                        };
                        btnDelete.Click += BtnDeleteKey_Click;

                        Grid.SetColumn(btnDelete, 2);
                        grid.Children.Add(btnDelete);
                    }

                    stackKeyValueEditor.Children.Add(grid);
                }
            }
        }

        private void DeleteToggle_Checked(object sender, RoutedEventArgs e)
        {
            deleteModeEnabled = radioDeleteYes.IsChecked == true;
            cmbSections_SelectionChanged(null, null); // Refresh editor
        }

        private void btnOpenPostconfig_Click(object sender, RoutedEventArgs e)
        {
            // Postconfig postconfigWindow = new(this);
            // postconfigWindow.Show();
            var postWindow = new Postconfig(this);
            postWindow.Show();

        }
        private void UndoLastChange()
        {
            if (lastBackup != null)
            {
                iniSections = lastBackup;
                SaveIniFile();
                RefreshIniContent();
                MessageBox.Show("Last change undone!", "Undo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new()
            {
                Filter = "INI Files (*.ini)|*.ini|All Files (*.*)|*.*",
                FileName = "ExportedPackage.ini"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                // Save to file
                TextRange range = new TextRange(
                    richIniContent.Document.ContentStart,
                    richIniContent.Document.ContentEnd
                );
                File.WriteAllText(saveFileDialog.FileName, range.Text);
                MessageBox.Show("INI file exported successfully!", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void btnUndo_Click(object sender, RoutedEventArgs e)
        {
            UndoLastChange();
        }  
        private void btnAddKey_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Add Key button clicked.");
        }
        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (cmbSections.SelectedItem is not string selectedSection) return;
            if (!iniSections.ContainsKey(selectedSection)) return;

            var updatedValues = new Dictionary<string, string>();

            foreach (Grid grid in stackKeyValueEditor.Children.OfType<Grid>())
            {
                var txt = grid.Children.OfType<TextBox>().FirstOrDefault();
                if (txt != null && txt.Tag is string key)
                {
                    updatedValues[key] = txt.Text.Trim();
                }
            }

            // Update iniSections
            iniSections[selectedSection] = updatedValues;

            // Save and refresh
            SaveIniFile();
            RefreshIniContent();

            MessageBox.Show("Section updated!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void txtIniContent_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                iniFilePath = files[0]; // Load first dropped file
                LoadIniFile();
            }
        }
        private void btnInsert_Click(object sender, RoutedEventArgs e)
        {
            string tmpIniPath = System.IO.Path.Combine(supportFilesFolder, "tmpPackage.ini");

            if (!File.Exists(tmpIniPath))
            {
                MessageBox.Show("Temporary INI file not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Parse tmpPackage.ini
                Dictionary<string, Dictionary<string, string>> tmpSections = IniFileHelper.ParseIniFile(tmpIniPath, out _);

                // Merge tmpPackage.ini into iniSections
                foreach (var section in tmpSections)
                {
                    if (!iniSections.ContainsKey(section.Key))
                    {
                        // If the section doesn't exist, add it
                        iniSections[section.Key] = new Dictionary<string, string>(section.Value);
                    }
                    else
                    {
                        // Merge keys into existing sections
                        foreach (var kvp in section.Value)
                        {
                            iniSections[section.Key][kvp.Key] = kvp.Value; // Overwrites existing keys
                        }
                    }
                }

                // Save and refresh INI content
                SaveIniFile();
                RefreshIniContent();

                MessageBox.Show("tmpPackage.ini data merged successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error merging tmpPackage.ini");
                MessageBox.Show($"Error merging tmpPackage.ini: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void btnAddSection_Click(object sender, RoutedEventArgs e)
        {
            var postWindow = new Postconfig(this);
            postWindow.Show();

        }
        private void btnRemoveSection_Click(object sender, RoutedEventArgs e)
        {
            if (cmbSections.SelectedItem == null)
            {
                MessageBox.Show("Select a section first!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string section = cmbSections.SelectedItem.ToString();

            // Confirmation prompt before deletion
            var result = MessageBox.Show(
                $"Are you sure you want to remove the '{section}' section?",
                "Confirm Deletion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                iniSections.Remove(section);  // Remove from dictionary
                cmbSections.ItemsSource = iniSections.Keys.ToList();  // Refresh ComboBox

                SaveIniFile();      // Persist changes
                RefreshIniContent(); // Update the INI view

                MessageBox.Show($"Section '{section}' removed successfully!", "Deleted", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void btnSaveAll_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to save changes?", "Confirm Save",
                                         MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                SaveIniFile();
            }
        }
        public void LoadDataFromPostconfig(string postconfigData, string selectedSection)
        {
            if (string.IsNullOrWhiteSpace(postconfigData) || string.IsNullOrWhiteSpace(selectedSection))
            {
                MessageBox.Show("Invalid data or section!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (iniSections.ContainsKey(selectedSection))
            {
                var newEntries = postconfigData.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                foreach (var entry in newEntries)
                {
                    var parts = entry.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        string key = parts[0].Trim();
                        string value = parts[1].Trim();

                        // Insert into the correct section
                        iniSections[selectedSection][key] = value;
                    }
                }

                // Save changes and update UI
                SaveIniFile();
                RefreshIniContent();
                MessageBox.Show($"Data added to {selectedSection} successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Section not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public List<string> GetSectionNames()
        {
            return iniSections?.Keys.ToList() ?? new List<string>();
        }
        public void RefreshIniContent()
        {
            var content = string.Join(Environment.NewLine + Environment.NewLine,
            iniSections.Select(section =>
            $"[{section.Key}]{Environment.NewLine}" +
            string.Join(Environment.NewLine, section.Value.Select(kvp => $"{kvp.Key}={kvp.Value}"))
                    )
                );

            HighlightIniContent(content); //  Use the rich text formatter here
        }
        public void SaveIniFile()
        {
            List<string> lines = new List<string>();

            foreach (var section in iniSections)
            {
                lines.Add($"[{section.Key}]");  //  Keep section headers in order
                foreach (var kvp in section.Value)
                {
                    lines.Add($"{kvp.Key}={kvp.Value}");
                }
                lines.Add(""); // Add a blank line between sections
            }

            File.WriteAllLines(iniFilePath, lines);
        }
        private void HighlightIniContent(string content)
        {
            richIniContent.Document.Blocks.Clear();

            var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            foreach (string line in lines)
            {
                var paragraph = new Paragraph { Margin = new Thickness(0) };

                if (line.TrimStart().StartsWith("[") && line.TrimEnd().EndsWith("]"))
                {
                    paragraph.Inlines.Add(new Run(line)
                    {
                        Foreground = Brushes.Orange,
                        FontWeight = FontWeights.Bold
                    });
                }
                else
                {
                    paragraph.Inlines.Add(new Run(line));
                }

                richIniContent.Document.Blocks.Add(paragraph);
            }
        }

        private void BtnDeleteKey_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string keyToRemove && cmbSections.SelectedItem is string section)
            {
                var result = MessageBox.Show($"Remove key '{keyToRemove}' from section '{section}'?", "Confirm Delete",
                                             MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    if (iniSections.ContainsKey(section) && iniSections[section].ContainsKey(keyToRemove))
                    {
                        iniSections[section].Remove(keyToRemove);
                        SaveIniFile();
                        RefreshIniContent();
                        cmbSections_SelectionChanged(null, null); // Refresh editor view
                    }
                }
            }
        }


    }
}
