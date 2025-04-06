using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PackageConsole
{
    public partial class Postconfig : Window
    {
        private Dictionary<string, List<string>> savedData = new();
        private Dictionary<string, List<string>> savedTagData = new();
        //private IniConsolePage iniConsolePage;
        //private PreviousApps PreviousApps;
        private IIniContext iniContext;


        public Postconfig(Page pageContext)
        {
            InitializeComponent();

            if (pageContext is IIniContext context)
            {
                iniContext = context;
                LoadIniSections();
            }
            else
            {
                MessageBox.Show("Unsupported page type passed to Postconfig window.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }


        private void LoadIniSections()
        {
            var sectionNames = iniContext?.GetSectionNames();
            if (sectionNames != null)
            {
                cmbSection.ItemsSource = sectionNames;
                dynamicFieldsPanel.Visibility = Visibility.Collapsed;
            }
        }
        private void AddTextBox(string labelText)
        {
            Grid grid = new() { Margin = new Thickness(0, 5, 0, 0) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });

            TextBlock label = new()
            {
                Text = labelText + ":",
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center
            };

            TextBox textBox = new() { Width = 200, FontSize = 14, Name = "txt" + labelText.Replace(" ", "") };

            Grid.SetColumn(label, 0);
            Grid.SetColumn(textBox, 1);

            grid.Children.Add(label);
            grid.Children.Add(textBox);
            stackDynamicInputs.Children.Add(grid);
        }
        private void cmbSection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbSection.SelectedItem == null) return;

            string selectedSection = cmbSection.SelectedItem.ToString();
            stackDynamicInputs.Children.Clear(); //  Clear previous dynamic inputs

            //  MACHINESPECIFIC & USERSPECIFIC (Existing logic remains unchanged)
            if (selectedSection.Equals("MACHINESPECIFIC", StringComparison.OrdinalIgnoreCase) ||
                selectedSection.Equals("USERSPECIFIC", StringComparison.OrdinalIgnoreCase))
            {
                dynamicFieldsPanel.Visibility = Visibility.Visible;
                addSectionPanel.Visibility = Visibility.Collapsed;
                txtNewSection.Visibility = Visibility.Collapsed;
                lblNewSection.Visibility = Visibility.Collapsed;
                tagSectionPanel.Visibility = Visibility.Collapsed;
                stackDynamicInputs.Visibility = Visibility.Visible;
            }
            //  INSTALL1, UNINSTALL1, ARP1, UPGRADE1 (Existing logic remains unchanged)
            else if (selectedSection.StartsWith("INSTALL") || selectedSection.StartsWith("UNINSTALL") ||
                     selectedSection.StartsWith("ARP") || selectedSection.StartsWith("UPGRADE"))
            {
                dynamicFieldsPanel.Visibility = Visibility.Collapsed;
                addSectionPanel.Visibility = Visibility.Visible;
                txtNewSection.Visibility = Visibility.Collapsed;
                lblNewSection.Visibility = Visibility.Collapsed;
                tagSectionPanel.Visibility = Visibility.Collapsed;
                PopulateSectionData(selectedSection);
            }
            //  New TAG Logic (Only modifies behavior for TAG)
            else if (selectedSection.Equals("TAG", StringComparison.OrdinalIgnoreCase))
            {
                
                dynamicFieldsPanel.Visibility = Visibility.Collapsed;
                addSectionPanel.Visibility = Visibility.Collapsed;
                stackDynamicInputs.Visibility = Visibility.Collapsed;

                // Show TAG section fields
                tagSectionPanel.Visibility = Visibility.Visible;

                // Auto-assign next Tag key
                txtTagKey.Text = GetNextTagKey();

            }
            else
            {
                //  Reset to default when other sections are selected
                dynamicFieldsPanel.Visibility = Visibility.Collapsed;
                addSectionPanel.Visibility = Visibility.Collapsed;
                txtNewSection.Visibility = Visibility.Collapsed;
                lblNewSection.Visibility = Visibility.Collapsed;
                tagSectionPanel.Visibility = Visibility.Collapsed;
                stackDynamicInputs.Children.Clear();
            }
        }
        private void PopulateSectionData(string selectedSection)
        {
            stackDynamicInputs.Children.Clear();

            if (!iniContext.HasSection(selectedSection))
            {
                MessageBox.Show($"No data found for {selectedSection}", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Dictionary<string, string> keyValues = iniContext.GetKeyValues(selectedSection);

            foreach (var kvp in keyValues)
            {
                AddLabelTextBox(kvp.Key, kvp.Value);
            }

            // Determine next available section name
            txtNewSection.Text = GetNextSectionName(selectedSection);
        }
        private string GetNextSectionName(string baseSection)
        {
            string sectionPrefix = new string(baseSection.TakeWhile(char.IsLetter).ToArray()); // Extract "UPGRADE"
            int sectionNumber = 1;

            // Find the highest existing number
            while (iniContext.HasSection($"{sectionPrefix}{sectionNumber}"))
            {
                sectionNumber++;
            }

            return $"{sectionPrefix}{sectionNumber}";  //  Correctly returns "Upgrade2", "Upgrade3"...
        }
        private void btnAddSection_Click(object sender, RoutedEventArgs e)
        {
            if (cmbSection.SelectedItem == null) return;

            string originalSection = cmbSection.SelectedItem.ToString();

            //  Ensure this logic only applies to INSTALL1, UNINSTALL1, etc.
            if (!(originalSection.StartsWith("INSTALL") || originalSection.StartsWith("UNINSTALL") ||
                  originalSection.StartsWith("ARP") || originalSection.StartsWith("UPGRADE")))
            {
                return;
            }

            string newSection = GetNextSectionName(originalSection);  //  Generate section name only when clicked
            string existingSections = string.Join(", ", iniContext.GetSectionNames());
          //  MessageBox.Show($"Existing Sections: {existingSections}", "Debug");
            if (iniContext.HasSection(newSection))
            {
                MessageBox.Show($"Section '{newSection}' already exists! Existing sections: {existingSections}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            txtNewSection.Text = newSection;  //  Display new section name in UI
            lblNewSection.Visibility = Visibility.Visible;  //  Show "New Section" label
            txtNewSection.Visibility = Visibility.Visible;  // Show "New Section" textbox

            CreateNewSection(originalSection, newSection);

            //  Refresh UI
            cmbSection.ItemsSource = iniContext.GetSectionNames();
            cmbSection.SelectedItem = newSection;
        }
        public void CreateNewSection(string originalSection, string newSection)
        {
            if (!iniContext.IniSections .ContainsKey(originalSection))
            {
                MessageBox.Show($"Error: Section '{originalSection}' not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Debugging: Show existing sections before checking
            string existingSections = string.Join(", ", iniContext.IniSections .Keys);
            MessageBox.Show($"Checking for duplicate section: '{newSection}'\nExisting Sections: {existingSections}", "Debug");

            if (iniContext.IniSections .ContainsKey(newSection))
            {
                MessageBox.Show($"Section already exists '{newSection}'!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //  Copy key-value pairs from original section
            Dictionary<string, string> copiedValues = new Dictionary<string, string>(iniContext.IniSections [originalSection]);

            //  Insert new section immediately after the original section
            Dictionary<string, Dictionary<string, string>> newIniSections  = new();
            bool sectionInserted = false;

            foreach (var entry in iniContext.IniSections )
            {
                newIniSections[entry.Key] = entry.Value;

                if (entry.Key == originalSection && !sectionInserted)
                {
                    newIniSections[newSection] = copiedValues;  // Insert right after the original
                    sectionInserted = true;
                }
            }

            iniContext.UpdateIniSections(newIniSections);  //  Use the update method
            iniContext.SaveIniFile();  // Save the updated order to the INI file
            iniContext.RefreshIniContent();  //  Update UI
        }
        private void AddLabelTextBox(string label, string value)
        {
            Grid grid = new() { Margin = new Thickness(0, 5, 0, 0) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });

            TextBlock lbl = new()
            {
                Text = label + ":",
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center
            };

            TextBox txtBox = new()
            {
                Width = 200,
                FontSize = 14,
                Name = "txt" + label.Replace(" ", ""),
                Text = value
            };

            Grid.SetColumn(lbl, 0);
            Grid.SetColumn(txtBox, 1);

            grid.Children.Add(lbl);
            grid.Children.Add(txtBox);
            stackDynamicInputs.Children.Add(grid);
        }
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (cmbSection.SelectedItem == null) return;

            string selectedSection = cmbSection.SelectedItem.ToString();
            txtPostconfigValues.Text = ""; //  Clear previous section data before saving new one

            // Keep existing logic for MACHINESPECIFIC & USERSPECIFIC
            if (selectedSection.Equals("MACHINESPECIFIC", StringComparison.OrdinalIgnoreCase) ||
                selectedSection.Equals("USERSPECIFIC", StringComparison.OrdinalIgnoreCase))
            {
                if (cmbPathType.SelectedItem is ComboBoxItem pathTypeItem &&
                    cmbValueType.SelectedItem is ComboBoxItem valueTypeItem)
                {
                    string pathType = pathTypeItem.Content?.ToString();
                    if (string.IsNullOrEmpty(pathType)) return;

                    string key = txtKey.Text;

                    List<string> values = stackDynamicInputs.Children
                        .OfType<Grid>()
                        .Select(g => g.Children.Count > 1 && g.Children[1] is TextBox textBox ? textBox.Text.Trim() : "")
                        .Where(v => !string.IsNullOrWhiteSpace(v))
                        .ToList();

                    if (!values.Any())
                    {
                        MessageBox.Show("Please fill all fields!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    string formattedValue = $"{key} = {string.Join(",", values)}";

                    if (!savedData.ContainsKey(pathType))
                        savedData[pathType] = new List<string>();

                    savedData[pathType].Add(formattedValue);
                    txtPostconfigValues.Text = string.Join("\n", savedData.SelectMany(kvp => kvp.Value));

                    txtKey.Text = $"{pathType}{savedData[pathType].Count + 1}";
                    stackDynamicInputs.Children.Clear();
                    cmbValueType.SelectedIndex = -1;
                }
            }
            else if (selectedSection.StartsWith("INSTALL") || selectedSection.StartsWith("UNINSTALL") ||
                     selectedSection.StartsWith("ARP") || selectedSection.StartsWith("UPGRADE"))
            {
                Dictionary<string, string> newValues = new();

                foreach (Grid grid in stackDynamicInputs.Children.OfType<Grid>())
                {
                    if (grid.Children[1] is TextBox txtBox)
                    {
                        string key = ((TextBlock)grid.Children[0]).Text.Replace(":", "").Trim();
                        newValues[key] = txtBox.Text.Trim();
                    }
                }

                txtPostconfigValues.Text = string.Join("\n", newValues.Select(kvp => $"{kvp.Key}={kvp.Value}")); //  Update display with correct section's values

                iniContext.UpdateIniSection(selectedSection, newValues); //  Save changes in INI

            }
            //  New Logic: Save TAG values in incremental format
            else if (selectedSection.Equals("TAG", StringComparison.OrdinalIgnoreCase))
            {
                string tagKey = txtTagKey.Text.Trim();
                string appName = txtAppName.Text.Trim();
                string appGuid = txtAppGuid.Text.Trim();
                string enabled = (cmbTagEnabled.SelectedItem as ComboBoxItem)?.Content.ToString();

                if (string.IsNullOrWhiteSpace(appName) || string.IsNullOrWhiteSpace(appGuid))
                {
                    MessageBox.Show("Please fill APPNAME and APPGUID.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string formattedValue = $"{tagKey} = {appName},{appGuid},{enabled}";

                // Store to savedTagData
                if (!savedTagData.ContainsKey("TAG"))
                    savedTagData["TAG"] = new List<string>();

                savedTagData["TAG"].Add(formattedValue);

                //  Update txtPostconfigValues
                txtPostconfigValues.Text = string.Join("\n", savedTagData["TAG"]);

                //  Update TagKey to TAG{count+1}
                txtTagKey.Text = $"TAG{savedTagData["TAG"].Count + 1}";

                //  Clear inputs
                txtAppName.Clear();
                txtAppGuid.Clear();
                cmbTagEnabled.SelectedIndex = 0;
            }
        }
        private string GetNextTagKey()
        {
            int tagCounter = 1;
            var lines = txtPostconfigValues.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            while (lines.Any(line => line.StartsWith($"TAG{tagCounter}=", StringComparison.OrdinalIgnoreCase)))
            {
                tagCounter++;
            }

            return $"TAG{tagCounter}";
        }

        public void AppendKeyValues(string section, Dictionary<string, string> newEntries)
        {
            if (!iniContext.IniSections.ContainsKey(section))
                iniContext.IniSections[section] = new Dictionary<string, string>();

            foreach (var entry in newEntries)
            {
                iniContext.IniSections[section][entry.Key] = entry.Value;
            }

            iniContext.SaveIniFile();
            iniContext.RefreshIniContent();

        }
        private void AutoPopulateKeyValues(string selectedSection)
        {
            stackDynamicInputs.Children.Clear(); //  Clear previous fields

            if (!iniContext.HasSection(selectedSection)) return;

            Dictionary<string, string> keyValues = iniContext.GetKeyValues(selectedSection);

            foreach (var kvp in keyValues)
            {
                AddLabelTextBox(kvp.Key, kvp.Value); //  Add labels & text boxes
            }

            btnSave.Visibility = Visibility.Visible; //  Show Save button
        }
        private void cmbPathType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbPathType.SelectedItem is ComboBoxItem selectedItem)
            {
                string selectedType = selectedItem.Content?.ToString();
                if (string.IsNullOrEmpty(selectedType)) return;

                if (!savedData.ContainsKey(selectedType))
                    savedData[selectedType] = new List<string>();

                int newKeyIndex = savedData[selectedType].Count + 1;
                txtKey.Text = $"{selectedType}{newKeyIndex}";
            }
        }
        private void cmbValueType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            stackDynamicInputs.Children.Clear();

            if (cmbValueType.SelectedItem is ComboBoxItem selectedItem)
            {
                string selectedValueType = selectedItem.Content?.ToString();
                if (string.IsNullOrEmpty(selectedValueType)) return;

                AddDynamicFields(selectedValueType);
            }
        }
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            txtPostconfigValues.Clear();  //  Clear TextBox
            cmbSection.SelectedIndex = -1;  //  Clear Section ComboBox
            cmbPathType.SelectedIndex = -1;  // Clear Path Type
            cmbValueType.SelectedIndex = -1;  //  Clear Value Type
            txtKey.Text = "";  //  Clear Key field
            stackDynamicInputs.Children.Clear(); //  Clear dynamically added fields
        }
        private void btnLoadToINI_Click(object sender, RoutedEventArgs e)
        {
            if (cmbSection.SelectedItem == null) return;

            string selectedSection = cmbSection.SelectedItem.ToString();
            Dictionary<string, string> updatedValues = new();

            //  Parse key-values from `txtPostconfigValues`
            foreach (string line in txtPostconfigValues.Text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = line.Split('=');
                if (parts.Length == 2)
                {
                    updatedValues[parts[0].Trim()] = parts[1].Trim();
                }
            }

            //  Update INI data
            iniContext.UpdateIniSection(selectedSection, updatedValues);
        }
        private void LoadExistingData()
        {
            cmbSection.Items.Clear(); //  Clear items before setting ItemsSource
            cmbSection.ItemsSource = iniContext.GetSectionNames();
        }
        private void AddDynamicFields(string valueType)
        {
            stackDynamicInputs.Children.Clear();

            switch (valueType)
            {
                case "FILE COPY":
                    AddTextBox("Source");
                    AddTextBox("Destination");
                    break;
                case "Delete File":
                    AddTextBox("Destination");
                    break;
                case "Directory Copy":
                    AddTextBox("Source");
                    AddTextBox("Destination");
                    break;
                case "RegWrite Value":
                    AddTextBox("Key");
                    AddTextBox("SubKey");
                    AddTextBox("Value");
                    AddTextBox("Type");
                    break;
                case "RegDelete Value":
                    AddTextBox("Key");
                    AddTextBox("SubKey");
                    break;
                case "RegDelete Key":
                    AddTextBox("Key");
                    break;
                default:
                    MessageBox.Show("Unknown Value Type!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
            }
        }
        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (txtPostconfigValues.SelectedText.Length == 0)
            {
                MessageBox.Show("Select a line to edit!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string selectedEntry = txtPostconfigValues.SelectedText;
            txtKey.Text = selectedEntry.Split('=')[0].Trim();
            stackDynamicInputs.Children.Clear();

            foreach (var value in selectedEntry.Split('=')[1].Split(','))
            {
                AddTextBox(value.Trim());
            }
        }
        private void btnSaveInFile_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
