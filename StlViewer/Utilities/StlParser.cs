using System.IO;
using System.Globalization;
using StlViewer.Exceptions;
using System.Numerics;

namespace StlViewer.Utilities
{
    public class StlParser
    {
        private const int HEADER_SIZE_BYTES = 80;
        private const int BUFFER_SIZE = 8192;
        private const int TRIANGLE_SIZE_BYTES = 50; // 12 (normal) + 36 (vertices) + 2 (attribute)

        /// <summary>
        /// 進捗状況を報告するためのデリゲート
        /// </summary>
        /// <param name="progress">0から100までの進捗率</param>
        /// <param name="message">進捗状況の説明メッセージ</param>
        public delegate void ProgressCallback(int progress, string message);

        /// <summary>
        /// STLファイルを読み込み、StlFileオブジェクトとして返します。
        /// </summary>
        /// <param name="filePath">読み込むSTLファイルのパス</param>
        /// <returns>読み込まれたSTLファイルのデータ</returns>
        /// <exception cref="StlParserException">ファイルの読み込みに失敗した場合</exception>
        public static StlFile Load(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("指定されたSTLファイルが見つかりません。", filePath);

            try
            {
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                using var reader = new BinaryReader(fileStream);

                var isAscii = IsAsciiStl(filePath);
                return isAscii ? ParseAscii(filePath) : ParseBinary(reader);
            }
            catch (Exception ex) when (ex is not StlParserException)
            {
                throw new StlParserException($"STLファイルの読み込み中にエラーが発生しました: {ex.Message}", ex);
            }
        }

        private static bool IsAsciiStl(string filePath)
        {
            try
            {
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                
                // ファイルが小さすぎる場合はバイナリ形式
                if (fileStream.Length < 84) // ヘッダー(80バイト) + 三角形数(4バイト)の最小サイズ
                {
                    return false;
                }

                using var reader = new StreamReader(fileStream);
                
                // 最初の行を読んで"solid"で始まるか確認
                var firstLine = reader.ReadLine()?.Trim() ?? "";
                if (string.IsNullOrEmpty(firstLine) || !firstLine.StartsWith("solid", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // 次の有効な行を読んで、ASCII STLの構造に従っているか確認
                string? nextLine;
                while ((nextLine = reader.ReadLine()) != null)
                {
                    nextLine = nextLine.Trim();
                    if (string.IsNullOrEmpty(nextLine)) continue;
                    
                    // 次の有効な行が"facet normal"で始まるかチェック
                    return nextLine.StartsWith("facet normal", StringComparison.OrdinalIgnoreCase);
                }

                return false;
            }
            catch (Exception ex)
            {
                throw new StlParserException("ファイル形式の判定に失敗しました。", ex);
            }
        }

        private static StlFile ParseAscii(string filePath)
        {
            var stlFile = new StlFile();
            var currentTriangle = new Triangle();
            var vertexCount = 0;

            try
            {
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                using var reader = new StreamReader(fileStream, bufferSize: BUFFER_SIZE);
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    var trimmedLine = line.Trim();
                    if (trimmedLine.StartsWith("solid"))
                    {
                        stlFile.SolidName = trimmedLine.Length > 6 ? trimmedLine[6..].Trim() : "DefaultSolid";
                    }
                    else if (trimmedLine.StartsWith("facet normal"))
                    {
                        currentTriangle = new Triangle
                        {
                            Normal = ParseVector3FromLine(trimmedLine, "facet normal")
                        };
                    }
                    else if (trimmedLine.StartsWith("vertex"))
                    {
                        var vertex = ParseVector3FromLine(trimmedLine, "vertex");
                        switch (vertexCount)
                        {
                            case 0:
                                currentTriangle.Vertex1 = vertex;
                                break;
                            case 1:
                                currentTriangle.Vertex2 = vertex;
                                break;
                            case 2:
                                currentTriangle.Vertex3 = vertex;
                                stlFile.Triangles.Add(currentTriangle);
                                break;
                        }
                        vertexCount = (vertexCount + 1) % 3;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new StlParserException("ASCII形式のSTLファイルの解析中にエラーが発生しました。", ex);
            }

            if (stlFile.Triangles.Count == 0)
                throw new StlParserException("有効な三角形データが見つかりませんでした。");

            return stlFile;
        }

        private static Vector3 ParseVector3FromLine(string line, string prefix)
        {
            try
            {
                var values = line.Replace(prefix, "")
                    .Trim()
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => float.Parse(v, CultureInfo.InvariantCulture))
                    .ToArray();

                if (values.Length < 3)
                    throw new StlParserException($"ベクトルデータの形式が不正です: {line}");

                return new Vector3(values[0], values[1], values[2]);
            }
            catch (Exception ex) when (ex is not StlParserException)
            {
                throw new StlParserException($"ベクトルデータの解析に失敗しました: {line}", ex);
            }
        }

        private static StlFile ParseBinary(BinaryReader reader)
        {
            var stlFile = new StlFile();
            
            try
            {
                // ヘッダーをスキップ
                reader.ReadBytes(HEADER_SIZE_BYTES);

                var triangleCount = reader.ReadUInt32();
                if (triangleCount == 0)
                    throw new StlParserException("三角形データが含まれていません。");

                // ファイルサイズの検証
                var expectedSize = HEADER_SIZE_BYTES + 4 + (triangleCount * (50)); // 50 = 4*3*4 + 2
                if (reader.BaseStream.Length != expectedSize)
                    throw new StlParserException("ファイルサイズが不正です。");

                for (var i = 0; i < triangleCount; i++)
                {
                    var triangle = new Triangle
                    {
                        Normal = ReadVector3(reader),
                        Vertex1 = ReadVector3(reader),
                        Vertex2 = ReadVector3(reader),
                        Vertex3 = ReadVector3(reader)
                    };

                    reader.ReadUInt16(); // 属性バイトをスキップ
                    stlFile.Triangles.Add(triangle);
                }
            }
            catch (Exception ex) when (ex is not StlParserException)
            {
                throw new StlParserException("バイナリ形式のSTLファイルの解析中にエラーが発生しました。", ex);
            }

            return stlFile;
        }

        private static Vector3 ReadVector3(BinaryReader reader)
        {
            try
            {
                var x = reader.ReadSingle();
                var y = reader.ReadSingle();
                var z = reader.ReadSingle();
                return new Vector3(x, y, z);
            }
            catch (Exception ex)
            {
                throw new StlParserException("ベクトルデータの読み込みに失敗しました。", ex);
            }
        }

        /// <summary>
        /// StlFileオブジェクトをファイルに保存します。
        /// </summary>
        /// <param name="stlFile">保存するSTLファイルのデータ</param>
        /// <param name="filePath">保存先のファイルパス</param>
        /// <param name="fileType">保存するファイル形式</param>
        /// <exception cref="StlParserException">ファイルの保存に失敗した場合</exception>
        public static void Save(StlFile stlFile, string filePath, STLFileType fileType)
        {
            ArgumentNullException.ThrowIfNull(stlFile);

            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            if (stlFile.Triangles.Count == 0)
                throw new StlParserException("保存するデータに三角形が含まれていません。");

            try
            {
                if (fileType == STLFileType.ASCII)
                {
                    SaveAsAscii(stlFile, filePath);
                }
                else
                {
                    SaveAsBinary(stlFile, filePath);
                }
            }
            catch (Exception ex) when (ex is not StlParserException)
            {
                throw new StlParserException($"STLファイルの保存中にエラーが発生しました: {ex.Message}", ex);
            }
        }

        private static void SaveAsAscii(StlFile stlFile, string filePath)
        {
            using var writer = new StreamWriter(filePath);
            writer.WriteLine($"solid {stlFile.SolidName}");

            foreach (var triangle in stlFile.Triangles)
            {
                writer.WriteLine($"  facet normal {FormatVector3(triangle.Normal)}");
                writer.WriteLine("    outer loop");
                writer.WriteLine($"      vertex {FormatVector3(triangle.Vertex1)}");
                writer.WriteLine($"      vertex {FormatVector3(triangle.Vertex2)}");
                writer.WriteLine($"      vertex {FormatVector3(triangle.Vertex3)}");
                writer.WriteLine("    endloop");
                writer.WriteLine("  endfacet");
            }

            writer.WriteLine($"endsolid {stlFile.SolidName}");
        }

        private static string FormatVector3(Vector3 vector)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:G9} {1:G9} {2:G9}", vector.X, vector.Y, vector.Z);
        }

        private static void SaveAsBinary(StlFile stlFile, string filePath)
        {
            using var writer = new BinaryWriter(File.Open(filePath, FileMode.Create));
            
            // ヘッダー（80バイト）
            var header = new byte[HEADER_SIZE_BYTES];
            writer.Write(header);

            writer.Write((uint)stlFile.Triangles.Count);

            foreach (var triangle in stlFile.Triangles)
            {
                WriteVector3(writer, triangle.Normal);
                WriteVector3(writer, triangle.Vertex1);
                WriteVector3(writer, triangle.Vertex2);
                WriteVector3(writer, triangle.Vertex3);
                writer.Write((ushort)0); // 属性バイト
            }
        }

        private static void WriteVector3(BinaryWriter writer, Vector3 vector)
        {
            writer.Write(vector.X);
            writer.Write(vector.Y);
            writer.Write(vector.Z);
        }

        /// <summary>
        /// STLファイルを非同期で読み込み、StlFileオブジェクトとして返します。
        /// </summary>
        /// <param name="filePath">読み込むSTLファイルのパス</param>
        /// <param name="progress">進捗状況を報告するコールバック（オプション）</param>
        /// <param name="cancellationToken">キャンセルトークン（オプション）</param>
        /// <returns>読み込まれたSTLファイルのデータ</returns>
        public static async Task<StlFile> LoadAsync(
            string filePath,
            ProgressCallback? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("指定されたSTLファイルが見つかりません。", filePath);

            try
            {
                progress?.Invoke(0, "ファイル形式を判定中...");
                var isAscii = await IsAsciiStlAsync(filePath, cancellationToken);
                
                await using var fileStream = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    BUFFER_SIZE,
                    FileOptions.Asynchronous);

                return isAscii
                    ? await ParseAsciiAsync(fileStream, progress, cancellationToken)
                    : await ParseBinaryAsync(fileStream, progress, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (ex is not StlParserException)
            {
                throw new StlParserException($"STLファイルの読み込み中にエラーが発生しました: {ex.Message}", ex);
            }
        }

        private static async Task<bool> IsAsciiStlAsync(string filePath, CancellationToken cancellationToken)
        {
            try
            {
                await using var fileStream = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    BUFFER_SIZE,
                    FileOptions.Asynchronous);

                // ファイルが小さすぎる場合はバイナリ形式
                if (fileStream.Length < 84) // ヘッダー(80バイト) + 三角形数(4バイト)の最小サイズ
                {
                    return false;
                }

                using var reader = new StreamReader(fileStream);
                
                // 最初の行を読んで"solid"で始まるか確認
                var firstLine = await reader.ReadLineAsync(cancellationToken);
                if (string.IsNullOrEmpty(firstLine) || !firstLine.StartsWith("solid", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // 次の有効な行を読んで、ASCII STLの構造に従っているか確認
                string? nextLine;
                while ((nextLine = await reader.ReadLineAsync(cancellationToken)) != null)
                {
                    nextLine = nextLine.Trim();
                    if (string.IsNullOrEmpty(nextLine)) continue;
                    
                    // 次の有効な行が"facet normal"で始まるかチェック
                    return nextLine.StartsWith("facet normal", StringComparison.OrdinalIgnoreCase);
                }

                return false;
            }
            catch (TaskCanceledException)
            {
                throw new OperationCanceledException();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new StlParserException("ファイル形式の判定に失敗しました。", ex);
            }
        }

        private static async Task<StlFile> ParseAsciiAsync(
            Stream stream,
            ProgressCallback? progress,
            CancellationToken cancellationToken)
        {
            var stlFile = new StlFile();
            var currentTriangle = new Triangle();
            var vertexCount = 0;
            var lineCount = 0;
            try
            {
                // ファイルの行数を概算（進捗表示用）
                int totalLines = await CountLinesAsync(stream);
                stream.Position = 0;

                using var reader = new StreamReader(stream);
                string? line;
                while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
                {
                    var trimmedLine = line.Trim();
                    if (trimmedLine.StartsWith("solid"))
                    {
                        stlFile.SolidName = trimmedLine.Length > 6 ? trimmedLine[6..].Trim() : "DefaultSolid";
                    }
                    else if (trimmedLine.StartsWith("facet normal"))
                    {
                        currentTriangle = new Triangle
                        {
                            Normal = ParseVector3FromLine(trimmedLine, "facet normal")
                        };
                    }
                    else if (trimmedLine.StartsWith("vertex"))
                    {
                        var vertex = ParseVector3FromLine(trimmedLine, "vertex");
                        switch (vertexCount)
                        {
                            case 0:
                                currentTriangle.Vertex1 = vertex;
                                break;
                            case 1:
                                currentTriangle.Vertex2 = vertex;
                                break;
                            case 2:
                                currentTriangle.Vertex3 = vertex;
                                stlFile.Triangles.Add(currentTriangle);
                                break;
                        }
                        vertexCount = (vertexCount + 1) % 3;
                    }

                    lineCount++;
                    if (progress != null && totalLines > 0 && lineCount % 100 == 0)
                    {
                        var progressPercent = (int)((float)lineCount / totalLines * 100);
                        progress(progressPercent, $"ASCII形式のSTLファイルを解析中... {stlFile.Triangles.Count}個の三角形を読み込み済み");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (ex is not StlParserException)
            {
                throw new StlParserException("ASCII形式のSTLファイルの解析中にエラーが発生しました。", ex);
            }

            if (stlFile.Triangles.Count == 0)
                throw new StlParserException("有効な三角形データが見つかりませんでした。");

            progress?.Invoke(100, $"解析完了。{stlFile.Triangles.Count}個の三角形を読み込みました。");
            return stlFile;
        }

        private static async Task<StlFile> ParseBinaryAsync(
            Stream stream,
            ProgressCallback? progress,
            CancellationToken cancellationToken)
        {
            var stlFile = new StlFile();
            
            try
            {
                using var reader = new BinaryReader(stream);
                
                // ヘッダーをスキップ
                await stream.ReadAsync(new byte[HEADER_SIZE_BYTES], cancellationToken);

                // 三角形の数を読み取る
                var triangleCountBytes = new byte[4];
                await stream.ReadAsync(triangleCountBytes, cancellationToken);
                var triangleCount = BitConverter.ToUInt32(triangleCountBytes);

                if (triangleCount == 0)
                    throw new StlParserException("三角形データが含まれていません。");

                // ファイルサイズの検証
                var expectedSize = HEADER_SIZE_BYTES + 4 + (triangleCount * TRIANGLE_SIZE_BYTES);
                if (stream.Length != expectedSize)
                    throw new StlParserException("ファイルサイズが不正です。");

                progress?.Invoke(0, $"バイナリ形式のSTLファイルを解析中... 合計{triangleCount}個の三角形");

                // メモリ効率を考慮してバッファサイズを制限
                const int batchSize = 1000;
                var buffer = new byte[TRIANGLE_SIZE_BYTES];

                for (var i = 0; i < triangleCount; i++)
                {
                    await stream.ReadAsync(buffer, cancellationToken);
                    using var memStream = new MemoryStream(buffer);
                    using var bufferReader = new BinaryReader(memStream);

                    var triangle = new Triangle
                    {
                        Normal = ReadVector3(bufferReader),
                        Vertex1 = ReadVector3(bufferReader),
                        Vertex2 = ReadVector3(bufferReader),
                        Vertex3 = ReadVector3(bufferReader)
                    };

                    bufferReader.ReadUInt16(); // 属性バイトをスキップ
                    stlFile.Triangles.Add(triangle);

                    if (progress != null && i % batchSize == 0)
                    {
                        var progressPercent = (int)((float)i / triangleCount * 100);
                        progress(progressPercent, $"バイナリ形式のSTLファイルを解析中... {i}/{triangleCount}個の三角形を読み込み済み");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (ex is not StlParserException)
            {
                throw new StlParserException("バイナリ形式のSTLファイルの解析中にエラーが発生しました。", ex);
            }

            progress?.Invoke(100, $"解析完了。{stlFile.Triangles.Count}個の三角形を読み込みました。");
            return stlFile;
        }

        private static async Task<int> CountLinesAsync(Stream stream)
        {
            var lineCount = 0;
            var buffer = new byte[BUFFER_SIZE];
            var position = stream.Position;

            try
            {
                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
                {
                    for (var i = 0; i < bytesRead; i++)
                    {
                        if (buffer[i] == '\n')
                            lineCount++;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new StlParserException("ファイルの行数カウント中にエラーが発生しました。", ex);
            }

            stream.Position = position;
            return lineCount;
        }

        /// <summary>
        /// StlFileオブジェクトを非同期でファイルに保存します。
        /// </summary>
        /// <param name="stlFile">保存するSTLファイルのデータ</param>
        /// <param name="filePath">保存先のファイルパス</param>
        /// <param name="fileType">保存するファイル形式</param>
        /// <param name="progress">進捗状況を報告するコールバック（オプション）</param>
        /// <param name="cancellationToken">キャンセルトークン（オプション）</param>
        public static async Task SaveAsync(
            StlFile stlFile,
            string filePath,
            STLFileType fileType,
            ProgressCallback? progress = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(stlFile);

            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            if (stlFile.Triangles.Count == 0)
                throw new StlParserException("保存するデータに三角形が含まれていません。");

            try
            {
                if (fileType == STLFileType.ASCII)
                {
                    await SaveAsAsciiAsync(stlFile, filePath, progress, cancellationToken);
                }
                else
                {
                    await SaveAsBinaryAsync(stlFile, filePath, progress, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (ex is not StlParserException)
            {
                throw new StlParserException($"STLファイルの保存中にエラーが発生しました: {ex.Message}", ex);
            }
        }

        private static async Task SaveAsAsciiAsync(
            StlFile stlFile,
            string filePath,
            ProgressCallback? progress,
            CancellationToken cancellationToken)
        {
            try
            {
                await using var fileStream = new FileStream(
                    filePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    BUFFER_SIZE,
                    FileOptions.Asynchronous);
                await using var writer = new StreamWriter(fileStream);

                await writer.WriteLineAsync($"solid {stlFile.SolidName}".AsMemory(), cancellationToken);

                var totalTriangles = stlFile.Triangles.Count;
                for (var i = 0; i < totalTriangles; i++)
                {
                    var triangle = stlFile.Triangles[i];
                    await writer.WriteLineAsync($"  facet normal {FormatVector3(triangle.Normal)}".AsMemory(), cancellationToken);
                    await writer.WriteLineAsync("    outer loop".AsMemory(), cancellationToken);
                    await writer.WriteLineAsync($"      vertex {FormatVector3(triangle.Vertex1)}".AsMemory(), cancellationToken);
                    await writer.WriteLineAsync($"      vertex {FormatVector3(triangle.Vertex2)}".AsMemory(), cancellationToken);
                    await writer.WriteLineAsync($"      vertex {FormatVector3(triangle.Vertex3)}".AsMemory(), cancellationToken);
                    await writer.WriteLineAsync("    endloop".AsMemory(), cancellationToken);
                    await writer.WriteLineAsync("  endfacet".AsMemory(), cancellationToken);

                    if (progress != null && i % 100 == 0)
                    {
                        var progressPercent = (int)((float)i / totalTriangles * 100);
                        progress(progressPercent, $"ASCII形式でSTLファイルを保存中... {i}/{totalTriangles}個の三角形を保存済み");
                    }
                }

                await writer.WriteLineAsync($"endsolid {stlFile.SolidName}".AsMemory(), cancellationToken);
                progress?.Invoke(100, $"保存完了。{totalTriangles}個の三角形を保存しました。");
            }
            catch (TaskCanceledException)
            {
                throw new OperationCanceledException();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (ex is not StlParserException)
            {
                throw new StlParserException("ASCII形式でのSTLファイルの保存中にエラーが発生しました。", ex);
            }
        }

        private static async Task SaveAsBinaryAsync(
            StlFile stlFile,
            string filePath,
            ProgressCallback? progress,
            CancellationToken cancellationToken)
        {
            try
            {
                await using var stream = new FileStream(
                    filePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    BUFFER_SIZE,
                    FileOptions.Asynchronous);
                
                using var writer = new BinaryWriter(stream);
                
                // ヘッダー（80バイト）
                var header = new byte[HEADER_SIZE_BYTES];
                await stream.WriteAsync(header, cancellationToken);

                var triangleCount = (uint)stlFile.Triangles.Count;
                await stream.WriteAsync(BitConverter.GetBytes(triangleCount), cancellationToken);

                progress?.Invoke(0, $"バイナリ形式でSTLファイルを保存中... 合計{triangleCount}個の三角形");

                for (var i = 0; i < triangleCount; i++)
                {
                    var triangle = stlFile.Triangles[i];
                    await WriteVector3Async(stream, triangle.Normal, cancellationToken);
                    await WriteVector3Async(stream, triangle.Vertex1, cancellationToken);
                    await WriteVector3Async(stream, triangle.Vertex2, cancellationToken);
                    await WriteVector3Async(stream, triangle.Vertex3, cancellationToken);
                    await stream.WriteAsync(BitConverter.GetBytes((ushort)0), cancellationToken);

                    if (progress != null && i % 1000 == 0)
                    {
                        var progressPercent = (int)((float)i / triangleCount * 100);
                        progress(progressPercent, $"バイナリ形式でSTLファイルを保存中... {i}/{triangleCount}個の三角形を保存済み");
                    }
                }

                progress?.Invoke(100, $"保存完了。{triangleCount}個の三角形を保存しました。");
            }
            catch (TaskCanceledException)
            {
                throw new OperationCanceledException();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (ex is not StlParserException)
            {
                throw new StlParserException("バイナリ形式でのSTLファイルの保存中にエラーが発生しました。", ex);
            }
        }

        private static async Task WriteVector3Async(Stream stream, Vector3 vector, CancellationToken cancellationToken)
        {
            try
            {
                var buffer = new byte[12]; // 3 * sizeof(float)
                Buffer.BlockCopy(BitConverter.GetBytes(vector.X), 0, buffer, 0, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(vector.Y), 0, buffer, 4, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(vector.Z), 0, buffer, 8, 4);
                await stream.WriteAsync(buffer, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new StlParserException("ベクトルデータの書き込みに失敗しました。", ex);
            }
        }
    }
}
