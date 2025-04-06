using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
//using IWshRuntimeLibrary;

namespace PackageConsole
{
    public class TileItem
    {
        public string Title { get; set; }
        public Action ClickAction { get; set; } // Trigger when clicked
    }

    public partial class HomePage : Page
    {
        private List<TileItem> tiles;

        public HomePage()
        {
            InitializeComponent();
            LoadTiles();
            TilePanel.ItemsSource = tiles;
        }

        private void LoadTiles()
        {
            tiles = new List<TileItem>
        {
            new TileItem { Title = "PowerBI Report", ClickAction = () => OpenLink("https://powerbi.microsoft.com") },
            new TileItem { Title = "Service Now", ClickAction = () => OpenLink("https://servicenow.com") },
            new TileItem { Title = "UPI Tool", ClickAction = () => MessageBox.Show("Launching UPI Tool...") },
            new TileItem { Title = "Peer Review Portal", ClickAction = () => MessageBox.Show("Opening Peer Review Portal...") },
            new TileItem { Title = "Notepad++", ClickAction = () => LaunchApp(@"Tools\Git-2.47.1-64-bit.exe") },
            new TileItem { Title = "CMTrace", ClickAction = () => LaunchApp(@"C:\Tools\CMTrace.exe") },
            new TileItem { Title = "Local EXE", ClickAction = () => LaunchApp(@"Tools\Run.exe") },
            new TileItem { Title = "Absolute Path", ClickAction = () => LaunchApp(@"C:\Program Files\Test\test.exe") },
            new TileItem { Title = "Desktop Shortcut", ClickAction = () =>
            LaunchApp(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MyAppShortcut.lnk")) },

            // 🔥 Add more tiles here easily
            new TileItem { Title = "Settings", ClickAction = () => MessageBox.Show("Settings") }
        };
        }

        private void Tile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is TileItem tile && tile.ClickAction != null)
                tile.ClickAction.Invoke();
        }

        private void OpenLink(string url)
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true }); }
            catch { MessageBox.Show("Unable to open link."); }
        }

        private void LaunchApp(string path)
        {
            try
            {
                string fullPath = path;

                // Case 1: Relative path inside app
                if (!System.IO.Path.IsPathRooted(path))
                {
                    fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
                }

                // Case 3: If it's a shortcut (.lnk), resolve the target
                if (System.IO.Path.GetExtension(fullPath).Equals(".lnk", StringComparison.OrdinalIgnoreCase))
                {
                    //fullPath = ResolveShortcut(fullPath);
                }

                if (File.Exists(fullPath))
                {
                    var startInfo = new System.Diagnostics.ProcessStartInfo(fullPath)
                    {
                        UseShellExecute = true
                    };
                    System.Diagnostics.Process.Start(startInfo);
                }
                else
                {
                    MessageBox.Show($"Executable not found:\n{fullPath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Launch failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
       // private string ResolveShortcut(string shortcutPath)
       //{
       //  try            {
      //       var shell = new IWshRuntimeLibrary.WshShell()
      //       var link = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(shortcutPath);
      //        return link.TargetPath;            }
      //    catch            {
      //        MessageBox.Show("Failed to resolve shortcut target.");
      //         return shortcutPath;            }
      //}


    }

}
