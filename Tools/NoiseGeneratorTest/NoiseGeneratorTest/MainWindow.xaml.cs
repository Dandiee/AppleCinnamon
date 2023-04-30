namespace NoiseGeneratorTest
{
    public partial class MainWindow
    {
        public MainWindowViewModel ViewModel { get; }

        public MainWindow()
        {
            InitializeComponent();

            ViewModel = new MainWindowViewModel(this);
            DataContext = ViewModel;
        }
    }
}
