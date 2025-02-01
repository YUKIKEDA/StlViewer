using System.Windows.Media.Imaging;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using StlViewer.Utilities;

namespace StlViewer.ViewModels
{
    public class StlViewAreaViewModel
    {
        private const string VertexShaderSource = @"#version 330 core

// 頂点属性
in vec3 a_position;

// ユニフォーム変数
uniform mat4 u_model;
uniform mat4 u_view;
uniform mat4 u_projection;

void main()
{
    // 最終的な頂点位置の計算
    gl_Position = u_projection * u_view * u_model * vec4(a_position, 1.0);
}";

        private const string FragmentShaderSource = @"#version 330 core

// 出力変数
out vec4 o_color;

void main()
{
    // 単色で描画
    o_color = vec4(0.7, 0.7, 0.7, 1.0);
}";
        private Renderer? _renderer;
        private Material? _material;

        private struct StlData
        {
            public float[] Vertices { get; set; }
            public int[] Indices { get; set; }
        }

        private StlData? _stlData;

        public void Initialize()
        {
            _renderer = CreateRenderer();
            _material = CreateMaterial();
        }

        public void Render(TimeSpan delta)
        {
            // カラーバッファーとZバッファーのクリア
            Renderer.ClearBuffer();

            if (_stlData == null || _material == null) return;


            // ジオメトリの生成
            var geometry = CreateGeometry(_stlData.Value.Vertices, _stlData.Value.Indices, PrimitiveType.Triangles);

            // ジオメトリの描画
            Renderer.RenderGeometry(geometry, _material);
        }

        public void SetStlFile(StlFile? stlFile)
        {
        }

        private static Renderer CreateRenderer()
        {
            return new Renderer();
        }

        private static Material CreateMaterial()
        {
            return new Material(VertexShaderSource, FragmentShaderSource);
        }

        private static Geometry CreateGeometry(float[] vertices, int[] indices, PrimitiveType primitiveType)
        {
            return new Geometry(vertices, indices, primitiveType);
        }

        private void InitializeExtensions()
        {
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

            // アトリビュートの設定
            List<int> attributeLocations = [];
            foreach (var vbo in geometry.Vbos)
            {
                // バーテックスバッファオブジェクトのバインド
                GL.BindBuffer(BufferTarget.ArrayBuffer, vbo.Vbo);

                // アトリビュートの取得
                var attributeLocation = GL.GetAttribLocation(material.Program, vbo.Name);

                if (attributeLocation == -1)
                {
                    Console.WriteLine($"警告: {vbo.Name}が見つかりません");
                }
                else
                {
                    // アトリビュートの有効化
                    GL.EnableVertexAttribArray(attributeLocation);
                    // アトリビュートのポインターの設定
                    GL.VertexAttribPointer(attributeLocation, vbo.Size, VertexAttribPointerType.Float, false, 0, 0);
                    // アトリビュートの位置をリストに追加
                    attributeLocations.Add(attributeLocation);
                }
            }

            // バーテックスバッファオブジェクトのバインド解除
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            // ユニフォーム変数の設定
            foreach (var uniform in material.UniformsFloat)
            {
                // ユニフォーム変数の取得
                var uniformLocation = GL.GetUniformLocation(material.Program, uniform.Name);

                if (uniformLocation == -1)
                {
                    Console.WriteLine($"警告: {uniform.Name}が見つかりません");
                }
                else
                {
                    // 配列の長さに応じて適切なuniform*fvメソッドを呼び出す
                    switch (uniform.Values.Length)
                    {
                        case 16:
                            // 4x4行列
                            var matrix = new Matrix4(
                                uniform.Values[0], uniform.Values[1], uniform.Values[2], uniform.Values[3],
                                uniform.Values[4], uniform.Values[5], uniform.Values[6], uniform.Values[7],
                                uniform.Values[8], uniform.Values[9], uniform.Values[10], uniform.Values[11],
                                uniform.Values[12], uniform.Values[13], uniform.Values[14], uniform.Values[15]
                            );
                            GL.UniformMatrix4(uniformLocation, false, ref matrix);
                            break;
                        case 4:
                            // vec4
                            GL.Uniform4(uniformLocation, uniform.Values[0], uniform.Values[1], uniform.Values[2], uniform.Values[3]);
                            break;
                        case 3:
                            // vec3
                            GL.Uniform3(uniformLocation, uniform.Values[0], uniform.Values[1], uniform.Values[2]);
                            break;
                        case 2:
                            // vec2
                            GL.Uniform2(uniformLocation, uniform.Values[0], uniform.Values[1]);
                            break;
                        case 1:
                            // float
                            GL.Uniform1(uniformLocation, uniform.Values[0]);
                            break;
                        default:
                            Console.WriteLine($"警告: サポートされていない配列の長さです: {uniform.Values.Length}");
                            break;
                    }
                }
            }

            foreach (var uniform in material.UniformsInt)
            {
                // ユニフォーム変数の取得
                var uniformLocation = GL.GetUniformLocation(material.Program, uniform.Name);

                if (uniformLocation == -1)
                {
                    Console.WriteLine($"警告: {uniform.Name}が見つかりません");
                }
                else
                {
                    // 配列の長さに応じて適切なuniform*ivメソッドを呼び出す
                    switch (uniform.Values.Length)
                    {
                        case 4:
                            // ivec4
                            GL.Uniform4(uniformLocation, uniform.Values[0], uniform.Values[1], uniform.Values[2], uniform.Values[3]);
                            break;
                        case 3:
                            // ivec3
                            GL.Uniform3(uniformLocation, uniform.Values[0], uniform.Values[1], uniform.Values[2]);
                            break;
                        case 2:
                            // ivec2
                            GL.Uniform2(uniformLocation, uniform.Values[0], uniform.Values[1]);
                            break;
                        case 1:
                            // int
                            GL.Uniform1(uniformLocation, uniform.Values[0]);
                            break;
                        default:
                            Console.WriteLine($"警告: サポートされていない配列の長さです: {uniform.Values.Length}");
                            break;
                    }
                }
            }

            // テクスチャの設定
            for (int i = 0; i < material.Textures.Length; i++)
            {
                if (i >= 4)
                {
                    Console.WriteLine("警告: テクスチャユニット数が上限を超えています");
                    break;
                }

                GL.ActiveTexture(TextureUnit.Texture0 + i);
                GL.BindTexture(TextureTarget.Texture2D, material.Textures[i]);
            }

            // ジオメトリの描画
            if (geometry.Ibos != null)
            {
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, geometry.Ibos.Ibo);
                GL.DrawElements(geometry.Mode, geometry.IndexCount, DrawElementsType.UnsignedShort, 0);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
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

            foreach (var attributeLocation in attributeLocations)
            {
                GL.DisableVertexAttribArray(attributeLocation);
            }
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
        public void SetUniformFloat(string name, float value)
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
        public void SetUniformInt(string name, int value)
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

        private int CreateShader(string source, ShaderType type)
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
                Console.WriteLine($"シェーダーコンパイルに失敗しました。: {infoLog}");
            }

            return shader;
        }

        private int CreateProgram(int vertexShader, int fragmentShader)
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
                Console.WriteLine($"プログラムのリンクに失敗しました。: {infoLog}");
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
        // インデックス数
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
            Vbos = [new VertexBufferObject(CreateVbo(vertices), "a_position", 3)];
            Mode = mode;
            VertexCount = vertices.Length / 3;
            Ibos = new IndexBufferObject(CreateIbo(indices));
            IndexCount = indices.Length;
        }

        #region Public methods

        /// <summary>
        /// リソースの解放
        /// </summary>
        public void Dispose()
        {
            // インデックスバッファオブジェクトの削除
            if (Ibos.Ibo != 0)
                GL.DeleteBuffer(Ibos.Ibo);

            // バーテックスバッファオブジェクトの削除
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
        }

        /// <summary>
        /// 法線の追加
        /// </summary>
        /// <param name="normal"> 法線 </param>
        public void AddNormal(float[] normal)
        {
            Vbos = [.. Vbos, new VertexBufferObject(CreateVbo(normal), "a_normal", 3)];
        }

        #endregion

        #region Private methods

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
                Console.WriteLine($"警告: テクスチャの読み込みに失敗しました: {imageSource}");
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

