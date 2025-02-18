using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using StlViewer.Commands;
using StlViewer.Utilities;

namespace StlViewer.ViewModels
{
    public class StlViewerViewModel : INotifyPropertyChanged
    {
        private StlFile? _currentStlFile;
        private string _statusMessage = string.Empty;
        private bool _isLoading;
        private readonly StlViewAreaViewModel _stlViewAreaViewModel;


        public StlFile? CurrentStlFile
        {
            get => _currentStlFile;
            set
            {
                _currentStlFile = value;
                OnPropertyChanged(nameof(CurrentStlFile));
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ICommand LoadStlFileCommand { get; private set; }
        public ICommand SetFrontViewCommand { get; private set; }
        public ICommand SetBackViewCommand { get; private set; }
        public ICommand SetTopViewCommand { get; private set; }
        public ICommand SetBottomViewCommand { get; private set; }
        public ICommand SetLeftViewCommand { get; private set; }
        public ICommand SetRightViewCommand { get; private set; }

        private async void LoadStlFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "STLファイル|*.stl",
                Title = "STLファイルを選択"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    IsLoading = true;
                    StatusMessage = "ファイルを読み込んでいます...";

                    await Task.Run(() =>
                    {
                        CurrentStlFile = StlParser.Load(openFileDialog.FileName);
                    });

                    if (CurrentStlFile != null)
                    {
                        Debug.WriteLine($"STLファイル読み込み完了 - 三角形数: {CurrentStlFile.Triangles.Count}");
                    }

                    _stlViewAreaViewModel.SetStlFile(CurrentStlFile);
                    StatusMessage = $"ファイルを読み込みました: {Path.GetFileName(openFileDialog.FileName)}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"ファイルの読み込み中にエラーが発生しました:\n{ex.Message}",
                        "エラー",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    StatusMessage = "ファイルの読み込みに失敗しました";
                    Debug.WriteLine($"STLファイル読み込みエラー: {ex}");
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        public StlViewerViewModel()
        {
            _stlViewAreaViewModel = new StlViewAreaViewModel();
            LoadStlFileCommand = new RelayCommand(LoadStlFile);
            SetFrontViewCommand = new RelayCommand(() => _stlViewAreaViewModel.SetFrontView());
            SetBackViewCommand = new RelayCommand(() => _stlViewAreaViewModel.SetBackView());
            SetTopViewCommand = new RelayCommand(() => _stlViewAreaViewModel.SetTopView());
            SetBottomViewCommand = new RelayCommand(() => _stlViewAreaViewModel.SetBottomView());
            SetLeftViewCommand = new RelayCommand(() => _stlViewAreaViewModel.SetLeftView());
            SetRightViewCommand = new RelayCommand(() => _stlViewAreaViewModel.SetRightView());
        }

        public void Initialize()
        {
            _stlViewAreaViewModel.Initialize();
        }

        public void Render(TimeSpan delta)
        {
            _stlViewAreaViewModel.Render(delta);
        }

        public void OnMouseDown(System.Windows.Point position)
        {
            _stlViewAreaViewModel.OnMouseDown(position);
        }

        public void OnMouseUp()
        {
            _stlViewAreaViewModel.OnMouseUp();
        }

        public void OnMouseMove(System.Windows.Point position)
        {
            _stlViewAreaViewModel.OnMouseMove(position);
        }

        public void OnMouseWheel(int delta)
        {
            _stlViewAreaViewModel.OnMouseWheel(delta);
        }
    }
}
