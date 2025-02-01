using System.Windows;
using StlViewer.ViewModels;

namespace StlViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly StlViewerViewModel _viewModel = new();

        public MainWindow()
        {
            InitializeComponent();

            DataContext = _viewModel;

            // XAMLやコードベースの他の場所で設定されたSettings プロパティを使用して開始できます。
            OpenTkControl.Start();
        }

        private void OpenTkControl_OnRender(TimeSpan delta)
        {
            _viewModel.Render(delta);
        }

        private void OpenTkControl_Ready()
        {
            _viewModel.Initialize();
        }
    }
}