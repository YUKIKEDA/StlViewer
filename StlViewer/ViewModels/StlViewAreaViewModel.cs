using System.Diagnostics;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using StlViewer.Utilities;

namespace StlViewer.ViewModels
{
    public static class Matrix4Extensions
    {
        public static float[] ToArray(this Matrix4 matrix)
        {
            return
            [
                matrix.M11, matrix.M12, matrix.M13, matrix.M14,
                matrix.M21, matrix.M22, matrix.M23, matrix.M24,
                matrix.M31, matrix.M32, matrix.M33, matrix.M34,
                matrix.M41, matrix.M42, matrix.M43, matrix.M44
            ];
        }
    }

    public class StlViewAreaViewModel
    {
        private const string VertexShaderSource = @"#version 330

in vec3 a_position;
uniform mat4 u_mvp;

void main()
{
    gl_Position = u_mvp * vec4(a_position, 1.0);
}";

        private const string FragmentShaderSource = @"#version 330

uniform vec4 u_color;
out vec4 o_color;

void main()
{
    o_color = u_color;
}";
        private Renderer? _renderer;
        private Material? _material;
        private Camera? _camera;
        private Vector3 _modelCenter;
        private float _modelSize;

        private struct StlData
        {
            public float[] Vertices { get; set; }
            public int[] Indices { get; set; }
        }

        private StlData? _stlData;

        private bool _isDragging;
        private System.Windows.Point _lastMousePosition;
        private const float ROTATION_SPEED = 0.5f;
        private const float ZOOM_SPEED = 0.1f;
        private const float PAN_SPEED = 0.01f;

        public void Initialize()
        {
            _renderer = CreateRenderer();
            _material = CreateMaterial();
            _camera = new Camera();
        }

        public void Render(TimeSpan delta)
        {
            if (_stlData == null || _material == null || _camera == null)
            {
                Debug.WriteLine("Render: データが不足しています");
                return;
            }

            // カメラ行列
            var viewMatrix = _camera.GetViewMatrix();
            var projectionMatrix = _camera.GetProjectionMatrix(true);

            // カラーバッファーとZバッファーのクリア
            Renderer.ClearBuffer();

            // ジオメトリの生成
            var geometry = CreateGeometry(_stlData.Value.Vertices, _stlData.Value.Indices, PrimitiveType.Triangles);
            Debug.WriteLine($"Render: 頂点数 = {_stlData.Value.Vertices.Length / 3}, インデックス数 = {_stlData.Value.Indices.Length}");

            // モデル・ビュー・プロジェクション行列の計算
            var modelMatrix = Matrix4.Identity;
            var mvpMatrix = modelMatrix * viewMatrix * projectionMatrix;

            // シェーダーにユニフォーム変数を設定
            _material.SetUniformFloat("u_mvp", mvpMatrix.ToArray());
            _material.SetUniformFloat("u_color", [0.7f, 0.7f, 0.7f, 1.0f]);

            // OpenGLのエラーをチェック
            var error = GL.GetError();
            if (error != ErrorCode.NoError)
            {
                Debug.WriteLine($"OpenGLエラー (シェーダー設定前): {error}");
            }

            // ジオメトリの描画
            Renderer.RenderGeometry(geometry, _material);

            error = GL.GetError();
            if (error != ErrorCode.NoError)
            {
                Debug.WriteLine($"OpenGLエラー (描画後): {error}");
            }

            // ジオメトリの解放
            geometry.Dispose();
        }

        public void SetStlFile(StlFile? stlFile)
        {
            if (stlFile == null)
            {
                _stlData = null;
                return;
            }

            // 頂点データの生成
            var vertices = new List<float>();
            var indices = new List<int>();
            var currentIndex = 0;

            // 各三角形について処理
            foreach (var triangle in stlFile.Triangles)
            {
                // 頂点座標を追加
                vertices.AddRange(new[]
                {
                    triangle.Vertex1.X, triangle.Vertex1.Y, triangle.Vertex1.Z,
                    triangle.Vertex2.X, triangle.Vertex2.Y, triangle.Vertex2.Z,
                    triangle.Vertex3.X, triangle.Vertex3.Y, triangle.Vertex3.Z
                });

                // インデックスを追加
                indices.AddRange(new[]
                {
                    currentIndex,
                    currentIndex + 1,
                    currentIndex + 2
                });

                currentIndex += 3;
            }

            // STLデータを保存
            _stlData = new StlData
            {
                Vertices = [.. vertices],
                Indices = [.. indices]
            };

            // モデルの中心と大きさを計算
            if (_stlData.Value.Vertices.Length > 0)
            {
                var minX = float.MaxValue;
                var minY = float.MaxValue;
                var minZ = float.MaxValue;
                var maxX = float.MinValue;
                var maxY = float.MinValue;
                var maxZ = float.MinValue;

                for (int i = 0; i < _stlData.Value.Vertices.Length; i += 3)
                {
                    var x = _stlData.Value.Vertices[i];
                    var y = _stlData.Value.Vertices[i + 1];
                    var z = _stlData.Value.Vertices[i + 2];

                    minX = Math.Min(minX, x);
                    minY = Math.Min(minY, y);
                    minZ = Math.Min(minZ, z);
                    maxX = Math.Max(maxX, x);
                    maxY = Math.Max(maxY, y);
                    maxZ = Math.Max(maxZ, z);
                }

                _modelCenter = new Vector3(
                    (minX + maxX) / 2,
                    (minY + maxY) / 2,
                    (minZ + maxZ) / 2
                );

                _modelSize = Math.Max(Math.Max(maxX - minX, maxY - minY), maxZ - minZ);

                // カメラの位置を調整
                if (_camera != null)
                {
                    // モデルの大きさに応じてカメラの位置を調整
                    float distance = _modelSize * 2.0f;
                    _camera.Position = new Vector3(
                        _modelCenter.X,
                        _modelCenter.Y,
                        _modelCenter.Z + distance
                    );
                    _camera.Target = _modelCenter;
                    _camera.Up = new Vector3(0, 1, 0);
                    _camera.Near = distance * 0.01f;
                    _camera.Far = distance * 10.0f;
                }
            }
        }

        private static Renderer CreateRenderer()
        {
            return new Renderer();
        }

        private static Material CreateMaterial()
        {
            var material = new Material(VertexShaderSource, FragmentShaderSource);
            Debug.WriteLine($"シェーダープログラム作成: Program = {material.Program}");
            var attribLocation = GL.GetAttribLocation(material.Program, "a_position");
            Debug.WriteLine($"a_position location = {attribLocation}");
            var uniformLocation = GL.GetUniformLocation(material.Program, "u_mvp");
            Debug.WriteLine($"u_mvp location = {uniformLocation}");
            return material;
        }

        private static Geometry CreateGeometry(float[] vertices, int[] indices, PrimitiveType primitiveType)
        {
            return new Geometry(vertices, indices, primitiveType);
        }

        public void OnMouseDown(System.Windows.Point position)
        {
            _isDragging = true;
            _lastMousePosition = position;
        }

        public void OnMouseUp()
        {
            _isDragging = false;
        }

        public void OnMouseMove(System.Windows.Point position)
        {
            if (!_isDragging || _camera == null) return;

            var deltaX = (float)(position.X - _lastMousePosition.X);
            var deltaY = (float)(position.Y - _lastMousePosition.Y);

            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                // 回転操作
                RotateCamera(deltaX, deltaY);
            }
            else if (Mouse.RightButton == MouseButtonState.Pressed)
            {
                // パン操作
                PanCamera(deltaX, deltaY);
            }

            _lastMousePosition = position;
        }

        public void OnMouseWheel(int delta)
        {
            if (_camera == null) return;

            // ズーム操作
            var zoomFactor = delta > 0 ? -ZOOM_SPEED : ZOOM_SPEED;
            var direction = _camera.Position - _camera.Target;
            direction *= (1.0f + zoomFactor);

            // 最小距離と最大距離を制限
            float minDistance = _modelSize * 0.1f;
            float maxDistance = _modelSize * 10.0f;
            if (direction.Length >= minDistance && direction.Length <= maxDistance)
            {
                _camera.Position = _camera.Target + direction;
            }
        }

        private void RotateCamera(float deltaX, float deltaY)
        {
            if (_camera == null) return;

            var direction = _camera.Position - _camera.Target;
            
            // Y軸周りの回転（deltaXに負の符号を付けて回転方向を反転）
            var rotationY = Matrix4.CreateRotationY(-deltaX * ROTATION_SPEED * (float)Math.PI / 180.0f);
            direction = Vector3.TransformVector(direction, rotationY);

            // X軸周りの回転
            var right = Vector3.Cross(direction, _camera.Up);
            var rotationX = Matrix4.CreateFromAxisAngle(right, deltaY * ROTATION_SPEED * (float)Math.PI / 180.0f);
            direction = Vector3.TransformVector(direction, rotationX);

            // カメラの位置を更新
            _camera.Position = _camera.Target + direction;
            _camera.Up = Vector3.Cross(right, direction).Normalized();
        }

        private void PanCamera(float deltaX, float deltaY)
        {
            if (_camera == null) return;

            var direction = _camera.Position - _camera.Target;
            var distance = direction.Length;

            var right = Vector3.Cross(direction, _camera.Up).Normalized();
            var up = Vector3.Cross(right, direction).Normalized();

            var translation = right * (-deltaX * PAN_SPEED * distance) + up * (deltaY * PAN_SPEED * distance);

            _camera.Position += translation;
            _camera.Target += translation;
        }

        // 6面視のカメラ位置を設定するメソッド
        public void SetFrontView()
        {
            if (_camera == null) return;
            float distance = _modelSize * 2.0f;
            _camera.Position = new Vector3(_modelCenter.X, _modelCenter.Y, _modelCenter.Z + distance);
            _camera.Target = _modelCenter;
            _camera.Up = new Vector3(0, 1, 0);
        }

        public void SetBackView()
        {
            if (_camera == null) return;
            float distance = _modelSize * 2.0f;
            _camera.Position = new Vector3(_modelCenter.X, _modelCenter.Y, _modelCenter.Z - distance);
            _camera.Target = _modelCenter;
            _camera.Up = new Vector3(0, 1, 0);
        }

        public void SetTopView()
        {
            if (_camera == null) return;
            float distance = _modelSize * 2.0f;
            _camera.Position = new Vector3(_modelCenter.X, _modelCenter.Y + distance, _modelCenter.Z);
            _camera.Target = _modelCenter;
            _camera.Up = new Vector3(0, 0, -1);
        }

        public void SetBottomView()
        {
            if (_camera == null) return;
            float distance = _modelSize * 2.0f;
            _camera.Position = new Vector3(_modelCenter.X, _modelCenter.Y - distance, _modelCenter.Z);
            _camera.Target = _modelCenter;
            _camera.Up = new Vector3(0, 0, 1);
        }

        public void SetLeftView()
        {
            if (_camera == null) return;
            float distance = _modelSize * 2.0f;
            _camera.Position = new Vector3(_modelCenter.X - distance, _modelCenter.Y, _modelCenter.Z);
            _camera.Target = _modelCenter;
            _camera.Up = new Vector3(0, 1, 0);
        }

        public void SetRightView()
        {
            if (_camera == null) return;
            float distance = _modelSize * 2.0f;
            _camera.Position = new Vector3(_modelCenter.X + distance, _modelCenter.Y, _modelCenter.Z);
            _camera.Target = _modelCenter;
            _camera.Up = new Vector3(0, 1, 0);
        }
    }

    /// <summary>
    /// レンダラー
    /// </summary>
    public class Renderer
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Renderer()
        {
            // 背面カリングの有効化
            GL.Enable(EnableCap.CullFace);
            // 裏面をカリングする
            GL.CullFace(TriangleFace.Back);
            // 反時計回りの三角形を前面とする
            GL.FrontFace(FrontFaceDirection.Ccw);

            // 深度書き込み及び深度テストの有効化
            GL.Enable(EnableCap.DepthTest);
            // 深度テストの比較関数をLessEqualに設定
            GL.DepthFunc(DepthFunction.Lequal); 
            // 深度テストの結果を書き込む
            GL.DepthMask(true);

            // αブレンドの無効化
            GL.Disable(EnableCap.Blend);
        }

        /// <summary>
        /// リソースの解放
        /// </summary>
        public static void Dispose()
        {
            // 必要に応じてリソースを解放
        }

        /// <summary>
        /// ビューポートの設定
        /// </summary>
        /// <param name="x"> ビューポートの左上隅のx座標 </param>
        /// <param name="y"> ビューポートの左上隅のy座標 </param>
        /// <param name="width"> ビューポートの幅 </param>
        /// <param name="height"> ビューポートの高さ </param>
        public static void SetViewport(int x, int y, int width, int height)
        {
            GL.Viewport(x, y, width, height);
        }

        /// <summary>
        /// バッファーのクリア
        /// </summary>
        public static void ClearBuffer()
        {
            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            GL.ClearDepth(1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }

        /// <summary>
        /// ジオメトリの描画
        /// </summary>
        /// <param name="geometry"> ジオメトリ </param>
        /// <param name="material"> マテリアル </param>
        public static void RenderGeometry(Geometry geometry, Material material)
        {
            // プログラムオブジェクトの有効化
            GL.UseProgram(material.Program);

            // VAOのバインド
            GL.BindVertexArray(geometry.Vao);

            // ユニフォーム変数の設定
            foreach (var uniform in material.UniformsFloat)
            {
                var uniformLocation = GL.GetUniformLocation(material.Program, uniform.Name);
                if (uniformLocation == -1)
                {
                    Debug.WriteLine($"警告: {uniform.Name}が見つかりません");
                }
                else
                {
                    switch (uniform.Values.Length)
                    {
                        case 16:
                            var matrix = new Matrix4(
                                uniform.Values[0], uniform.Values[1], uniform.Values[2], uniform.Values[3],
                                uniform.Values[4], uniform.Values[5], uniform.Values[6], uniform.Values[7],
                                uniform.Values[8], uniform.Values[9], uniform.Values[10], uniform.Values[11],
                                uniform.Values[12], uniform.Values[13], uniform.Values[14], uniform.Values[15]
                            );
                            GL.UniformMatrix4(uniformLocation, false, ref matrix);
                            break;
                        case 4:
                            GL.Uniform4(uniformLocation, uniform.Values[0], uniform.Values[1], uniform.Values[2], uniform.Values[3]);
                            break;
                        case 3:
                            GL.Uniform3(uniformLocation, uniform.Values[0], uniform.Values[1], uniform.Values[2]);
                            break;
                        case 2:
                            GL.Uniform2(uniformLocation, uniform.Values[0], uniform.Values[1]);
                            break;
                        case 1:
                            GL.Uniform1(uniformLocation, uniform.Values[0]);
                            break;
                        default:
                            Debug.WriteLine($"警告: サポートされていない配列の長さです: {uniform.Values.Length}");
                            break;
                    }
                }
            }

            foreach (var uniform in material.UniformsInt)
            {
                var uniformLocation = GL.GetUniformLocation(material.Program, uniform.Name);
                if (uniformLocation == -1)
                {
                    Debug.WriteLine($"警告: {uniform.Name}が見つかりません");
                }
                else
                {
                    switch (uniform.Values.Length)
                    {
                        case 4:
                            GL.Uniform4(uniformLocation, uniform.Values[0], uniform.Values[1], uniform.Values[2], uniform.Values[3]);
                            break;
                        case 3:
                            GL.Uniform3(uniformLocation, uniform.Values[0], uniform.Values[1], uniform.Values[2]);
                            break;
                        case 2:
                            GL.Uniform2(uniformLocation, uniform.Values[0], uniform.Values[1]);
                            break;
                        case 1:
                            GL.Uniform1(uniformLocation, uniform.Values[0]);
                            break;
                        default:
                            Debug.WriteLine($"警告: サポートされていない配列の長さです: {uniform.Values.Length}");
                            break;
                    }
                }
            }

            // テクスチャの設定
            for (int i = 0; i < material.Textures.Length; i++)
            {
                if (i >= 4)
                {
                    Debug.WriteLine("警告: テクスチャユニット数が上限を超えています");
                    break;
                }

                GL.ActiveTexture(TextureUnit.Texture0 + i);
                GL.BindTexture(TextureTarget.Texture2D, material.Textures[i]);
            }

            // ジオメトリの描画
            if (geometry.Ibos != null)
            {
                GL.DrawElements(geometry.Mode, geometry.IndexCount, DrawElementsType.UnsignedInt, 0);
            }
            else
            {
                GL.DrawArrays(geometry.Mode, 0, geometry.VertexCount);
            }

            // 後処理
            for (int i = 0; i < material.Textures.Length; i++)
            {
                if (i >= 4) break;
                GL.ActiveTexture(TextureUnit.Texture0 + i);
                GL.BindTexture(TextureTarget.Texture2D, 0);
            }

            // VAOのバインド解除
            GL.BindVertexArray(0);
        }

        /// <summary>
        /// バッファリングされたOpenGLコマンドの即時実行
        /// </summary>
        public static void Flush()
        {
            GL.Flush();
        }
    }

    /// <summary>
    /// ユニフォーム変数
    /// </summary>
    public readonly struct UniformFloat
    {
        public string Name { get; }
        public float[] Values { get; }

        public UniformFloat(string name, float value)
        {
            Name = name;
            Values = [value];
        }

        public UniformFloat(string name, float[] values)
        {
            Name = name;
            Values = values;
        }
    }

    /// <summary>
    /// ユニフォーム変数
    /// </summary>
    public readonly struct UniformInt
    {
        public string Name { get; }
        public int[] Values { get; }

        public UniformInt(string name, int value)
        {
            Name = name;
            Values = [value];
        }

        public UniformInt(string name, int[] values)
        {
            Name = name;
            Values = values;
        }
    }

    /// <summary>
    /// マテリアル
    /// </summary>
    public class Material
    {
        #region Public properties

        public int Program { get; private set; }
        public int VertexShader { get; private set; }
        public int FragmentShader { get; private set; }

        // float型のユニフォーム変数配列
        public UniformFloat[] UniformsFloat { get; private set; }

        // int型のユニフォーム変数配列
        public UniformInt[] UniformsInt { get; private set; }

        // テクスチャ配列
        public int[] Textures { get; private set; }

        #endregion

        public Material(string vertexShader, string fragmentShader)
        {
            VertexShader = CreateShader(vertexShader, ShaderType.VertexShader);
            FragmentShader = CreateShader(fragmentShader, ShaderType.FragmentShader);
            Program = CreateProgram(VertexShader, FragmentShader);
            UniformsFloat = [];
            UniformsInt = [];
            Textures = [];

            // シェーダーのコンパイル結果を確認
            GL.GetShader(VertexShader, ShaderParameter.CompileStatus, out int vertexCompileStatus);
            if (vertexCompileStatus == 0)
            {
                string log = GL.GetShaderInfoLog(VertexShader);
                Debug.WriteLine($"頂点シェーダーのコンパイルエラー: {log}");
            }
            else
            {
                Debug.WriteLine("頂点シェーダーのコンパイル成功");
            }

            GL.GetShader(FragmentShader, ShaderParameter.CompileStatus, out int fragmentCompileStatus);
            if (fragmentCompileStatus == 0)
            {
                string log = GL.GetShaderInfoLog(FragmentShader);
                Debug.WriteLine($"フラグメントシェーダーのコンパイルエラー: {log}");
            }
            else
            {
                Debug.WriteLine("フラグメントシェーダーのコンパイル成功");
            }

            // プログラムのリンク結果を確認
            GL.GetProgram(Program, GetProgramParameterName.LinkStatus, out int linkStatus);
            if (linkStatus == 0)
            {
                string log = GL.GetProgramInfoLog(Program);
                Debug.WriteLine($"プログラムのリンクエラー: {log}");
            }
            else
            {
                Debug.WriteLine("プログラムのリンク成功");
            }
        }

        #region Public methods

        /// <summary>
        /// リソースの解放
        /// </summary>
        public void Dispose()
        {
            if (Program != 0)
            {
                // プログラムからシェーダーをデタッチ
                if (VertexShader != 0)
                    GL.DetachShader(Program, VertexShader);
                if (FragmentShader != 0)
                    GL.DetachShader(Program, FragmentShader);
                
                // プログラムを削除
                GL.DeleteProgram(Program);
            }

            // シェーダーを削除
            if (VertexShader != 0)
                GL.DeleteShader(VertexShader);
            if (FragmentShader != 0)
                GL.DeleteShader(FragmentShader);
        }

        /// <summary>
        /// float型のユニフォーム変数の設定
        /// </summary>
        /// <param name="name"> ユニフォーム変数名 </param>
        /// <param name="value"> ユニフォーム変数値 </param>
        public void SetUniformFloat(string name, float[] value)
        {
            // 既存の値の更新
            for (int i = 0; i < UniformsFloat.Length; i++)
            {
                if (UniformsFloat[i].Name == name)
                {
                    UniformsFloat[i] = new UniformFloat(name, value);
                    return;
                }
            }
            // 新規追加
            UniformsFloat = [.. UniformsFloat, new UniformFloat(name, value)];
        }

        /// <summary>
        /// int型のユニフォーム変数の設定
        /// </summary>
        /// <param name="name"> ユニフォーム変数名 </param>
        /// <param name="value"> ユニフォーム変数値 </param>
        public void SetUniformInt(string name, int[] value)
        {
            // 既存の値の更新
            for (int i = 0; i < UniformsInt.Length; i++)
            {
                if (UniformsInt[i].Name == name)
                {
                    UniformsInt[i] = new UniformInt(name, value);
                    return;
                }
            }
            // 新規追加
            UniformsInt = [.. UniformsInt, new UniformInt(name, value)];
        }

        /// <summary>
        /// テクスチャ配列の設定
        /// </summary>
        /// <param name="textures"> テクスチャ配列 </param>
        public void SetTextures(int[] textures)
        {
            Textures = textures;
        }

        #endregion

        #region Private methods

        private static int CreateShader(string source, ShaderType type)
        {
            // シェーダーオブジェクトの作成
            int shader = GL.CreateShader(type);

            // シェーダーソースを設定
            GL.ShaderSource(shader, source);

            // シェーダーソースのコンパイル
            GL.CompileShader(shader);

            // シェーダーコンパイルの結果をチェック
            GL.GetShader(shader, ShaderParameter.CompileStatus, out int compileStatus);
            if (compileStatus == 0)
            {
                string infoLog = GL.GetShaderInfoLog(shader);
                throw new Exception($"シェーダーコンパイルに失敗しました。: {infoLog}");
            }

            return shader;
        }

        private static int CreateProgram(int vertexShader, int fragmentShader)
        {
            // プログラムオブジェクトの作成
            int program = GL.CreateProgram();

            // シェーダーの割り当て
            GL.AttachShader(program, vertexShader);
            GL.AttachShader(program, fragmentShader);

            // プログラムのリンク
            GL.LinkProgram(program);

            // プログラムのリンクの結果をチェック
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int linkStatus);
            if (linkStatus == 0)
            {
                string infoLog = GL.GetProgramInfoLog(program);
                throw new Exception($"プログラムのリンクに失敗しました。: {infoLog}");
            }

            return program;
        }

        #endregion
    }

    /// <summary>
    /// バーテックスバッファオブジェクト
    /// </summary>
    public class VertexBufferObject
    {
        public int Vbo { get; private set; }
        public string Name { get; private set; }
        public int Size { get; private set; }

        public VertexBufferObject(int vbo, string name, int size)
        {
            Vbo = vbo;
            Name = name;
            Size = size;
        }
    }

    /// <summary>
    /// インデックスバッファオブジェクト
    /// </summary>
    public class IndexBufferObject
    {
        public int Ibo { get; private set; }

        public IndexBufferObject(int ibo)
        {
            Ibo = ibo;
        }
    }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="vertices"> 頂点 </param>
    /// <param name="indices"> インデックス </param>
    /// <param name="mode"> プリミティブタイプ </param>
    public class Geometry
    {
        #region Public properties

        /// <summary>
        /// バーテックスアレイオブジェクト
        /// </summary>
        public int Vao { get; private set; }

        /// <summary>
        /// バーテックスバッファオブジェクト
        /// </summary>
        public VertexBufferObject[] Vbos { get; private set; }

        /// <summary>
        /// インデックスバッファオブジェクト
        /// </summary>
        public IndexBufferObject Ibos { get; private set; }

        /// <summary>
        /// プリミティブタイプ
        /// </summary>
        public PrimitiveType Mode { get; private set; }

        /// <summary>
        /// 頂点数
        /// </summary>
        public int VertexCount { get; private set; }

        /// <summary>
        /// インデックス数
        /// </summary>
        public int IndexCount { get; private set; }

        #endregion

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="vertices"> 頂点 </param>
        /// <param name="indices"> インデックス </param>
        /// <param name="mode"> プリミティブタイプ </param>
        public Geometry(float[] vertices, int[] indices, PrimitiveType mode)
        {
            // VAOの作成
            Vao = GL.GenVertexArray();
            
            // VBOの作成
            Vbos = [new VertexBufferObject(CreateVbo(vertices), "a_position", 3)];
            Mode = mode;
            VertexCount = vertices.Length / 3;

            // IBOの作成
            Ibos = new IndexBufferObject(CreateIbo(indices));
            IndexCount = indices.Length;

            // VAOの設定を更新
            UpdateVao();
        }

        /// <summary>
        /// リソースの解放
        /// </summary>
        public void Dispose()
        {
            if (Vao != 0)
            {
                GL.DeleteVertexArray(Vao);
                Vao = 0;
            }

            if (Ibos.Ibo != 0)
            {
                GL.DeleteBuffer(Ibos.Ibo);
            }

            foreach (var vbo in Vbos)
            {
                GL.DeleteBuffer(vbo.Vbo);
            }
        }

        /// <summary>
        /// UV0の追加
        /// </summary>
        /// <param name="uv0"> UV0 </param>
        public void AddUv0(float[] uv0)
        {
            Vbos = [.. Vbos, new VertexBufferObject(CreateVbo(uv0), "a_uv0", 2)];
            UpdateVao();
        }

        /// <summary>
        /// 法線の追加
        /// </summary>
        /// <param name="normal"> 法線 </param>
        public void AddNormal(float[] normal)
        {
            Vbos = [.. Vbos, new VertexBufferObject(CreateVbo(normal), "a_normal", 3)];
            UpdateVao();
        }

        #region Private methods

        /// <summary>
        /// VAOの設定を更新
        /// </summary>
        private void UpdateVao()
        {
            GL.BindVertexArray(Vao);

            // VBOの設定
            for (int i = 0; i < Vbos.Length; i++)
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, Vbos[i].Vbo);
                GL.EnableVertexAttribArray(i);
                GL.VertexAttribPointer(i, Vbos[i].Size, VertexAttribPointerType.Float, false, 0, 0);
            }

            // IBOの設定
            if (Ibos != null)
            {
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, Ibos.Ibo);
            }

            // バインド解除
            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            if (Ibos != null)
            {
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            }
        }

        private static int CreateVbo(float[] vertices)
        {
            // バーテックスバッファオブジェクトの作成
            int vbo = GL.GenBuffer();

            // バーテックスバッファオブジェクトのバインド
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);

            // バーテックスバッファオブジェクトのデータの設定
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                vertices.Length * sizeof(float),
                vertices,
                BufferUsageHint.StaticDraw);

            // バーテックスバッファオブジェクトのバインド解除
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            return vbo;
        }

        private static int CreateIbo(int[] indices)
        {
            // インデックスバッファオブジェクトの作成
            int ibo = GL.GenBuffer();

            // インデックスバッファオブジェクトのバインド
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo);

            // インデックスバッファオブジェクトのデータの設定
            GL.BufferData(
                BufferTarget.ElementArrayBuffer,
                indices.Length * sizeof(int),
                indices,
                BufferUsageHint.StaticDraw);

            // インデックスバッファオブジェクトのバインド解除
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            return ibo;
        }

        #endregion
    }

    /// <summary>
    /// テクスチャ
    /// </summary>
    public class Texture
    {
        private readonly BitmapImage _img;
        private readonly int _textureId;
        private bool _isLoaded;

        public BitmapImage Img => _img;
        public int TextureId => _textureId;
        public bool IsLoaded => _isLoaded;

        public Texture(string imageSource)
        {
            _img = new BitmapImage();
            _textureId = GL.GenTexture();
            _isLoaded = false;

            _img.BeginInit();
            _img.UriSource = new Uri(imageSource, UriKind.RelativeOrAbsolute);
            _img.EndInit();

            // イメージ読み込み完了時の処理
            _img.DownloadCompleted += (sender, e) =>
            {
                // テクスチャをバインド
                GL.BindTexture(TextureTarget.Texture2D, _textureId);

                // テクスチャにイメージを適用
                byte[] pixels = new byte[_img.PixelWidth * _img.PixelHeight * 4];
                _img.CopyPixels(pixels, _img.PixelWidth * 4, 0);

                GL.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    PixelInternalFormat.Rgba,
                    _img.PixelWidth,
                    _img.PixelHeight,
                    0,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    pixels
                );

                // 2のべき乗サイズかチェック（npotだとミップマップとリピートが使えない）
                bool npot = !IsPowerOfTwo(_img.PixelWidth) || !IsPowerOfTwo(_img.PixelHeight);

                // ミップマップを生成（2のべき乗サイズの場合のみ）
                if (!npot)
                {
                    GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
                }

                // テクスチャパラメータの設定
                if (!npot)
                {
                    // 2のべき乗サイズの場合
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
                }
                else
                {
                    // 非2のべき乗サイズの場合
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                }

                // バインドを解除
                GL.BindTexture(TextureTarget.Texture2D, 0);

                _isLoaded = true;
            };

            // イメージ読み込みエラー時の処理
            _img.DecodeFailed += (sender, e) =>
            {
                Debug.WriteLine($"警告: テクスチャの読み込みに失敗しました: {imageSource}");
                _isLoaded = true;
            };
        }

        /// <summary>
        /// リソースの解放
        /// </summary>
        public void Dispose()
        {
            if (_textureId != 0)
            {
                GL.DeleteTexture(_textureId);
            }
        }

        /// <summary>
        /// テクスチャパラメータの設定
        /// </summary>
        /// <param name="magFilter"> マグニフィケーションフィルタ </param>
        /// <param name="minFilter"> ミニフィケーションフィルタ </param>
        /// <param name="wrapS"> S方向のラップモード </param>
        /// <param name="wrapT"> T方向のラップモード </param>
        public void SetTextureParameter(int magFilter, int minFilter, int wrapS, int wrapT)
        {
            // テクスチャのバインド
            GL.BindTexture(TextureTarget.Texture2D, _textureId);

            // テクスチャパラメータの設定
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, magFilter);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, minFilter);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, wrapS);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, wrapT);

            // テクスチャのバインド解除
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        /// <summary>
        /// 2のべき乗かどうか
        /// </summary>
        /// <param name="value">チェックする値</param>
        /// <returns>2のべき乗ならtrue</returns>
        private static bool IsPowerOfTwo(int value)
        {
            return (value & (value - 1)) == 0;
        }
    }

    /// <summary>
    /// フレームバッファ
    /// </summary>
    public class FrameBuffer
    {
        public int FrameBufferId { get; private set; }
        public int RenderTextureId { get; private set; }
        public int DepthBufferId { get; private set; }

        public FrameBuffer(int width, int height, bool useFloatTexture = false)
        {
            // 浮動小数点テクスチャのサポートチェック
            if (useFloatTexture)
            {
                GL.GetInteger(GetPName.MaxTextureImageUnits, out int maxTextureImageUnits);
                if (maxTextureImageUnits < 1)
                {
                    throw new Exception("浮動小数点テクスチャのサポートがありません。");
                }
            }

            // フレームバッファオブジェクトの作成
            FrameBufferId = GL.GenFramebuffer();

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FrameBufferId);

            // レンダリングテクスチャの作成
            RenderTextureId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, RenderTextureId);

            // テクスチャの設定
            GL.TexImage2D(
                TextureTarget.Texture2D, 
                0, 
                PixelInternalFormat.Rgba, 
                width, 
                height, 
                0, 
                PixelFormat.Rgba, 
                useFloatTexture ? PixelType.Float : PixelType.UnsignedByte, 
                IntPtr.Zero);

            // テクスチャパラメータの設定
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            // フレームバッファにテクスチャをアタッチ
            GL.FramebufferTexture2D(
                FramebufferTarget.Framebuffer, 
                FramebufferAttachment.ColorAttachment0, 
                TextureTarget.Texture2D, 
                RenderTextureId, 
                0);

            // デプスバッファの作成
            DepthBufferId = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, DepthBufferId);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent, width, height);

            // フレームバッファにデプスバッファをアタッチ
            GL.FramebufferRenderbuffer(
                FramebufferTarget.Framebuffer, 
                FramebufferAttachment.DepthAttachment, 
                RenderbufferTarget.Renderbuffer, 
                DepthBufferId);

            // フレームバッファの状態チェック
            var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != FramebufferErrorCode.FramebufferComplete)
            {
                throw new Exception($"フレームバッファの作成に失敗しました: {status}");
            }

            // バインドを解除
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        //リソースの解放
        public void Dispose()
        {
            if (FrameBufferId != 0)
            {
                GL.DeleteFramebuffer(FrameBufferId);
            }
            if (RenderTextureId != 0)
            {
                GL.DeleteTexture(RenderTextureId);
            }
            if (DepthBufferId != 0)
            {
                GL.DeleteRenderbuffer(DepthBufferId);
            }
        }

        //テクスチャパラメータの設定
        public void SetTextureParameter(int magFilter, int minFilter, int wrapS, int wrapT)
        {
            GL.BindTexture(TextureTarget.Texture2D, RenderTextureId);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, magFilter);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, minFilter);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, wrapS);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, wrapT);
        }

        // フレームバッファの有効化
        public void Enable()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FrameBufferId);
        }

        // フレームバッファの無効化
        public static void Disable()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }
    }

    /// <summary>
    /// カメラ
    /// </summary>
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

        public Camera()
        {
            Position = new Vector3(0, 0, 5);
            Target = new Vector3(0, 0, 0);
            Up = new Vector3(0, 1, 0);
            Fov = 90.0 * Math.PI / 180.0;
            AspectRatio = 1;
            Near = 0.5;
            Far = 10.0;
            Left = -1.0;
            Right = 1.0;
            Bottom = -1.0;
            Top = 1.0;
        }

        /// <summary>
        /// ビュー行列の取得
        /// </summary>
        /// <returns>ビュー行列</returns>
        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(Position, Target, Up);
        }

        /// <summary>
        /// 射影行列の取得
        /// </summary>
        /// <param name="isPerspective">透視投影かどうか</param>
        /// <returns>射影行列</returns>
        public Matrix4 GetProjectionMatrix(bool isPerspective)
        {
            if (isPerspective)
            {
                // 透視投影行列の生成
                return Matrix4.CreatePerspectiveFieldOfView((float)Fov, (float)AspectRatio, (float)Near, (float)Far);
            }
            else
            {
                // 正射影行列の生成
                return Matrix4.CreateOrthographic((float)(Right - Left), (float)(Top - Bottom), (float)Near, (float)Far);
            }
        }
    }
}

