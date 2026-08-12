using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Scribble
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int PenColorNumber = 7;
        double PenWidth = 2;
        int LastTabNumber = 1;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // load the properties from disk
            Properties.Settings.Default.Reload();

            // check for upgrade
            if (Properties.Settings.Default.UpgradeRequired)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpgradeRequired = false;
                Properties.Settings.Default.Save();
            }

            this.Left = Properties.Settings.Default.WindowLeft;
            this.Top = Properties.Settings.Default.WindowTop;
            this.Width = Properties.Settings.Default.WindowWidth;
            this.Height = Properties.Settings.Default.WindowHeight;
            PenWidth = Properties.Settings.Default.PenWidth;
            PenColorNumber = Properties.Settings.Default.PenColor;
            //PenColor = ColorPicker.GetBrushColor(PenColorNumber);
            DrawingPage? dp = tabControl.SelectedContent as DrawingPage;
            if (dp != null)
            {
                dp.PenColorNumber = PenColorNumber;
                dp.PenWidth = PenWidth;
            }

            double screenWidth = SystemParameters.WorkArea.Width;
            double screenHeight = SystemParameters.WorkArea.Height;

            // ensure window size doesn't exceed screen size
            if (this.Width > screenWidth) this.Width = screenWidth;
            if (this.Height > screenHeight) this.Height = screenHeight;

            // ensure window is not off the left or top
            if (this.Left < 0) this.Left = 0;
            if (this.Top < 0) this.Top = 0;

            // ensure window is not off the right or bottom
            if (this.Left + this.Width > screenWidth)
                this.Left = screenWidth - this.Width;
            if (this.Top + this.Height > screenHeight)
                this.Top = screenHeight - this.Height;

            if (Properties.Settings.Default.WindowState == "Maximized")
                this.WindowState = WindowState.Maximized;

            PopulateRecentFilesMenu();
            EnableUndo(false);
            EnableRedo(false);
            EnableSave(false);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool anyTabIsDirty = false;

            foreach (TabItem tab in tabControl.Items)
            {
                if (tab.Content is DrawingPage dp && dp.isDirty)
                {
                    anyTabIsDirty = true;
                    break; // you just need to know "any dirty tab exists" — no need to keep checking
                }
            }

            if (anyTabIsDirty)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes in one or more tabs. Closing now will discard them.",
                    "Unsaved Changes",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true; // aborts the close entirely — window stays open
                    return;
                }
            }

            dp = tabControl.SelectedContent as DrawingPage;
            if (dp != null)
            {
                PenColorNumber = dp.PenColorNumber;
                PenWidth = dp.PenWidth;
            }

            Properties.Settings.Default.WindowState = this.WindowState.ToString();
            if (this.WindowState == WindowState.Normal)
            {
                Properties.Settings.Default.WindowLeft = this.Left;
                Properties.Settings.Default.WindowTop = this.Top;
                Properties.Settings.Default.WindowWidth = this.Width;
                Properties.Settings.Default.WindowHeight = this.Height;
            }
            Properties.Settings.Default.PenColor = PenColorNumber;
            Properties.Settings.Default.PenWidth = PenWidth;
            Properties.Settings.Default.Save();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+N - New
            if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control)
            {
                New_Click(sender, e);
                e.Handled = true;
            }
            // Ctrl+O - Open
            if (e.Key == Key.O && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Open_Click(sender, e);
                e.Handled = true;
            }
            // Ctrl+S - Save
            else if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Save_Click(sender, e);
                e.Handled = true;
            }
            // Ctrl+P - Print
            else if (e.Key == Key.P && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Print_Click(sender, e);
                e.Handled = true;
            }
            // Ctrl+Z - Undo
            if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Undo_Click(sender, e);
                e.Handled = true;
            }
            // Ctrl+Y - Redo
            if (e.Key == Key.Y && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Redo_Click(sender, e);
                e.Handled = true;
            }
            // Ctrl+C - Copy
            else if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Copy_Click(sender, e);
                e.Handled = true;
            }
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            DrawingPage dp = new DrawingPage();
            dp.PenColorNumber = PenColorNumber;
            dp.PenWidth = PenWidth;
            TabItem tab = new TabItem();
            LastTabNumber++;
            tab.Header = "Tab " + LastTabNumber;
            tab.Content = dp;
            tabControl.Items.Add(tab);
            tabControl.SelectedItem = tab;
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            DrawingPage? dp = tabControl.SelectedContent as DrawingPage;
            if (dp != null)
            {
                dp.Open();
                if (dp.ScribbleFileName != "")
                {
                    string name = Path.GetFileNameWithoutExtension(dp.ScribbleFileName);
                    if (tabControl.SelectedItem is TabItem selectedTab)
                    {
                        selectedTab.Header = name;
                    }
                    UpdateRecentFiles(dp.ScribbleFileName);
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            DrawingPage? dp = tabControl.SelectedContent as DrawingPage;
            if (dp != null)
            {
                dp.Save();
                if (dp.ScribbleFileName != "")
                {
                    string name = Path.GetFileNameWithoutExtension(dp.ScribbleFileName);
                    if (tabControl.SelectedItem is TabItem selectedTab)
                    {
                        selectedTab.Header = name;
                    }
                    UpdateRecentFiles(dp.ScribbleFileName);
                }
            }
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            DrawingPage? dp = tabControl.SelectedContent as DrawingPage;
            if (dp != null)
            {
                dp.SaveAs();
                if (dp.ScribbleFileName != "")
                {
                    string name = Path.GetFileNameWithoutExtension(dp.ScribbleFileName);
                    if (tabControl.SelectedItem is TabItem selectedTab)
                    {
                        selectedTab.Header = name;
                    }
                    UpdateRecentFiles(dp.ScribbleFileName);
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DrawingPage? dp = tabControl.SelectedContent as DrawingPage;
            if (dp != null)
            {
                if (dp.isDirty)
                {
                    MessageBoxResult result = MessageBox.Show(
                        "You have unsaved changes. Do you want to save before closing?",
                        "Save changes?",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        dp.Save(); // your existing save logic
                    }
                    else if (result == MessageBoxResult.Cancel)
                    {
                        return; // abort closing entirely
                    }
                    // MessageBoxResult.No falls through — close without saving
                }

                if (tabControl.Items.Count > 1)
                {
                    tabControl.Items.Remove(tabControl.SelectedItem);
                }
                else
                {
                    dp.isDirty = false;
                    dp.ClearAll_Click();
                }
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            DrawingPage? dp = tabControl.SelectedContent as DrawingPage;
            if (dp != null)
                dp.Export_Click();
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            DrawingPage? dp = tabControl.SelectedContent as DrawingPage;
            if (dp != null)
                dp.Print_Click();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            /*
            bool anyTabIsDirty = false;

            foreach (TabItem tab in tabControl.Items)
            {
                if (tab.Content is DrawingPage dp && dp.isDirty)
                {
                    anyTabIsDirty = true;
                    break; // you just need to know "any dirty tab exists" — no need to keep checking
                }
            }

            if (anyTabIsDirty)
            {
                MessageBoxResult result = MessageBox.Show(
                    "You have unsaved changes in one or more tabs. Exiting now will discard them.",
                    "Unsaved Changes",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.OK)
                {
                    //Application.Current.Shutdown(); // or however you close the app
                    this.Close();
                }
            }
            else
            */
                this.Close();
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            DrawingPage? dp = tabControl.SelectedContent as DrawingPage;
            if (dp != null)
                dp.Undo_Click();
        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            DrawingPage? dp = tabControl.SelectedContent as DrawingPage;
            if (dp != null)
                dp.Redo_Click();
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            DrawingPage? dp = tabControl.SelectedContent as DrawingPage;
            if (dp != null)
                dp.Copy_Click();
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            DrawingPage? dp = tabControl.SelectedContent as DrawingPage;
            if (dp != null)
                dp.ClearAll_Click();
        }

        private void PenWidth_Click(object sender, RoutedEventArgs e)
        {
            DrawingPage? dp = tabControl.SelectedContent as DrawingPage;
            if (dp != null)
                PenWidth = dp.PenWidth;
            PenWidthDialog dialog = new PenWidthDialog(PenWidth);
            dialog.Owner = this;
            bool? result = dialog.ShowDialog(); // Modal dialog
            if (result == true)
            {
                PenWidth = dialog.PenWidth;
                if (dp != null)
                    dp.PenWidth = dialog.PenWidth;
            }
        }

        private void PenColor_Click(object sender, RoutedEventArgs e)
        {
            DrawingPage? dp = tabControl.SelectedContent as DrawingPage;
            if (dp != null)
                PenColorNumber = dp.PenColorNumber;
            ColorPicker dialog = new ColorPicker(PenColorNumber);
            dialog.Owner = this;
            bool? result = dialog.ShowDialog(); // Modal dialog
            if (result == true)
            {
                PenColorNumber = dialog.colorNumber;
                if (dp != null)
                    dp.PenColorNumber = dialog.colorNumber;
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.Owner = this;
            bool? result = aboutWindow.ShowDialog(); // Modal dialog
        }

        public void UpdateRecentFiles(string fileName)
        {
            if (fileName != Properties.Settings.Default.RecentFile3 &&
                fileName != Properties.Settings.Default.RecentFile2 &&
                fileName != Properties.Settings.Default.RecentFile1)
            {
                Properties.Settings.Default.RecentFile4 = Properties.Settings.Default.RecentFile3;
                Properties.Settings.Default.RecentFile3 = Properties.Settings.Default.RecentFile2;
                Properties.Settings.Default.RecentFile2 = Properties.Settings.Default.RecentFile1;
                Properties.Settings.Default.RecentFile1 = fileName;
                Properties.Settings.Default.Save();
                PopulateRecentFilesMenu();
            }
        }

        private void PopulateRecentFilesMenu()
        {
            RecentFilesMenuItem.Items.Clear(); // clear any previous entries first

            string[] recentFiles =
            {
                Properties.Settings.Default.RecentFile1,
                Properties.Settings.Default.RecentFile2,
                Properties.Settings.Default.RecentFile3,
                Properties.Settings.Default.RecentFile4
            };

            foreach (string filePath in recentFiles)
            {
                if (string.IsNullOrEmpty(filePath)) continue; // skip empty slots

                var item = new MenuItem
                {
                    Header = Path.GetFileNameWithoutExtension(filePath),
                    Tag = filePath // stash the full path for the click handler to use
                };
                item.Click += RecentFileMenuItem_Click;
                RecentFilesMenuItem.Items.Add(item);
            }
        }

        private void RecentFileMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item && item.Tag is string filePath)
            {
                DrawingPage? dp = tabControl.SelectedContent as DrawingPage;
                if (dp != null)
                {
                    dp.OpenFile(filePath);
                    if (dp.ScribbleFileName != "")
                    {
                        string name = Path.GetFileNameWithoutExtension(dp.ScribbleFileName);
                        if (tabControl.SelectedItem is TabItem selectedTab)
                        {
                            selectedTab.Header = name;
                        }
                    }
                }
            }
        }

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DrawingPage? dp = tabControl.SelectedContent as DrawingPage;
            if (dp != null)
            {
                EnableUndo(dp.EnableUndo);
                EnableRedo(dp.EnableRedo);
                EnableSave(dp.isDirty);
            }
        }

        public void EnableUndo(bool enable)
        {
            UndoMenu.IsEnabled = enable;
            UndoButton.IsEnabled = enable;
        }

        public void EnableRedo(bool enable)
        {
            RedoMenu.IsEnabled = enable;
            RedoButton.IsEnabled = enable;
        }

        public void EnableSave(bool enable)
        {
            SaveMenu.IsEnabled = enable;
            SaveButton.IsEnabled = enable;
        }
    }
}