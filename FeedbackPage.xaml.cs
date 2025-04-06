using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;

namespace PackageConsole
{
    public partial class FeedbackPage : Page
    {
        private static readonly string[] AdminUsers = { "sankara", "venkat", "archana", "Dell"};
        private ObservableCollection<FeedbackEntry> feedbackEntries = new ObservableCollection<FeedbackEntry>();
        private string screenshotPath = null;
        private readonly string feedbackDir = @"\\SankaraSubrahmanyam-PC\D$\Files\Feedback";

        private bool IsCurrentUserAdmin()
        {
            string currentUser = Environment.UserName.ToLower();
            return AdminUsers.Any(admin => admin.Equals(currentUser, StringComparison.OrdinalIgnoreCase));
        }
        public FeedbackPage()
        {
            InitializeComponent();
            LoadFeedbackEntries();
        }

        private void LoadFeedbackEntries()
        {
            feedbackEntries.Clear();
            foreach (var entry in FeedbackIOHelper.LoadLatestFeedbacks())
                feedbackEntries.Add(entry);

            feedbackGrid.ItemsSource = feedbackEntries;
            btnUpdateResponse.Visibility = IsCurrentUserAdmin() ? Visibility.Visible : Visibility.Collapsed;

        }

        private string GetValue(string[] lines, string key)
        {
            var match = lines.FirstOrDefault(l => l.StartsWith($"{key}:"));
            return match?.Substring(key.Length + 1).Trim() ?? "Unknown";
        }

        private void SubmitYes_Checked(object sender, RoutedEventArgs e)
        {
            submissionPanel.Visibility = Visibility.Visible;
        }

        private void SubmitNo_Checked(object sender, RoutedEventArgs e)
        {
           // submissionPanel.Visibility = Visibility.Collapsed;
        }

        private void BtnAttachScreenshot_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "Image Files (*.png;*.jpg)|*.png;*.jpg"
            };
            if (dlg.ShowDialog() == true)
            {
                screenshotPath = dlg.FileName;
                lblScreenshotPath.Text = Path.GetFileName(screenshotPath);
            }
        }

        private void BtnSubmitFeedback_Click(object sender, RoutedEventArgs e)
        {
            string type = (cmbFeedbackType.SelectedItem as ComboBoxItem)?.Content?.ToString();
            string msg = txtFeedbackMessage.Text.Trim();
            string user = Environment.UserName;

            if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(msg))
            {
                MessageBox.Show("Please select type and enter message.", "Validation");
                return;
            }

            try
            {
                var entry = new FeedbackEntry
                {
                    User = user,
                    Type = type,
                    Time = DateTime.Now,
                    Message = msg,
                    Response = GenerateResponse(msg),
                    Severity = "Normal"
                };

                FeedbackIOHelper.SaveFeedback(entry, screenshotPath);

                screenshotPath = null;
                txtFeedbackMessage.Clear();
                lblScreenshotPath.Text = "";
                cmbFeedbackType.SelectedIndex = -1;
                submissionPanel.Visibility = Visibility.Collapsed;
                LoadFeedbackEntries();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving feedback: {ex.Message}");
            }
        }

        private string GenerateResponse(string msg)
        {
            msg = msg.ToLower();
            if (msg.Contains("bug") || msg.Contains("crash")) return "We’ll look into this bug 🛠️";
            if (msg.Contains("feature") || msg.Contains("add")) return "Feature request noted! 📌";
            if (msg.Contains("idea") || msg.Contains("suggest")) return "Thanks for the idea 💡";
            return "Thank you for your feedback!";
        }
    }
}
