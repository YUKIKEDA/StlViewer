using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using StlViewer.Commands;
using StlViewer.Utilities;
using Microsoft.Win32;
using System.Windows;
using System.IO;
using System.Threading.Tasks;

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
        }
    }

    public class Camera
    {
        // カメラ位置
        public Vector3 Position { get; set; }

        // 注視点
        public Vector3 Target { get; set; }

        // カメラの上方向
        public Vector3 Up { get; set; }

        // 視野角（Field of View）- ラジアン単位
        public double Fov { get; set; }

        // アスペクト比（width/height）
        public double AspectRatio { get; set; }

        // ニアクリップ面（最小描画距離）
        public double Near { get; set; }

        // ファークリップ面（最大描画距離）
        public double Far { get; set; }

        // 正投影用の左端座標
        public double Left { get; set; }

        // 正投影用の右端座標
        public double Right { get; set; }

        // 正投影用の下端座標
        public double Bottom { get; set; }

        // 正投影用の上端座標
        public double Top { get; set; }
    }

    public class Renderer
    {
    }

    public class Material
    {
    }

    public class Geometry
    {
    }

    public class Texture
    {
    }

    public class FrameBuffer
    {
    }
}
