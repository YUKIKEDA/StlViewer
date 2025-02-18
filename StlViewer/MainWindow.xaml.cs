using System.Windows;
using StlViewer.ViewModels;
using System.Windows.Input;

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

        private void OpenTkControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var viewModel = (StlViewerViewModel)DataContext;
            viewModel.OnMouseDown(e.GetPosition(OpenTkControl));
        }

        private void OpenTkControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var viewModel = (StlViewerViewModel)DataContext;
            viewModel.OnMouseUp();
        }

        private void OpenTkControl_MouseMove(object sender, MouseEventArgs e)
        {
            var viewModel = (StlViewerViewModel)DataContext;
            viewModel.OnMouseMove(e.GetPosition(OpenTkControl));
        }

        private void OpenTkControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var viewModel = (StlViewerViewModel)DataContext;
            viewModel.OnMouseWheel(e.Delta);
        }
    }
}