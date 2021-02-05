using System.Windows;

namespace DSAE_HonoursGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Begin scrapping 
            DSAEHonoursGUI.Main.main();
        }
    }
}
