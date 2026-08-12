using System.Windows;

namespace Scribble
{
    /// <summary>
    /// Interaction logic for PenWidthDialog.xaml
    /// </summary>
    public partial class PenWidthDialog : Window
    {
        public double PenWidth = 2;

        public PenWidthDialog(double prevWidth)
        {
            InitializeComponent();
            WidthSlider.Value = prevWidth;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            PenWidth = WidthSlider.Value;
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
