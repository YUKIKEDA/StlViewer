using System.ComponentModel;
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
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        public StlViewerViewModel()
        {
            LoadStlFileCommand = new RelayCommand(LoadStlFile);
            new StlViewAreaViewModel();
        }
    }
}
