using System.Numerics;
using StlViewer.Exceptions;
using StlViewer.Utilities;

namespace StlViewer.UnitTests.Utilities
{
    public class StlParserTests : IDisposable
    {
        private const string TestFilesDirectory = "TestFiles";
        private bool _disposed;

        public StlParserTests()
        {
            if (!Directory.Exists(TestFilesDirectory))
            {
                Directory.CreateDirectory(TestFilesDirectory);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // マネージドリソースの解放
                    if (Directory.Exists(TestFilesDirectory))
                    {
                        Directory.Delete(TestFilesDirectory, true);
                    }
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [Fact]
        public void Load_ValidAsciiFile_ReturnsCorrectStlFile()
        {
            // Arrange
            var filePath = Path.Combine(TestFilesDirectory, "test.stl");
            var asciiContent = @"solid test
  facet normal 1 0 0
    outer loop
      vertex 0 0 0
      vertex 0 1 0
      vertex 0 0 1
    endloop
  endfacet
endsolid test";
            File.WriteAllText(filePath, asciiContent);

            // Act
            var result = StlParser.Load(filePath);

            // Assert
            Assert.Equal("test", result.SolidName);
            Assert.Single(result.Triangles);

            var triangle = result.Triangles[0];
            Assert.Equal(1f, triangle.Normal.X);
            Assert.Equal(0f, triangle.Normal.Y);
            Assert.Equal(0f, triangle.Normal.Z);

            Assert.Equal(0f, triangle.Vertex1.X);
            Assert.Equal(0f, triangle.Vertex1.Y);
            Assert.Equal(0f, triangle.Vertex1.Z);

            Assert.Equal(0f, triangle.Vertex2.X);
            Assert.Equal(1f, triangle.Vertex2.Y);
            Assert.Equal(0f, triangle.Vertex2.Z);

            Assert.Equal(0f, triangle.Vertex3.X);
            Assert.Equal(0f, triangle.Vertex3.Y);
            Assert.Equal(1f, triangle.Vertex3.Z);
        }

        [Fact]
        public void Load_ValidBinaryFile_ReturnsCorrectStlFile()
        {
            // Arrange
            var filePath = Path.Combine(TestFilesDirectory, "test.stl");
            using (var writer = new BinaryWriter(File.Open(filePath, FileMode.Create)))
            {
                // ヘッダー
                writer.Write(new byte[80]);

                // 三角形の数
                writer.Write((uint)1);

                // 法線ベクトル
                writer.Write(1f); // X
                writer.Write(0f); // Y
                writer.Write(0f); // Z

                // 頂点1
                writer.Write(0f);
                writer.Write(0f);
                writer.Write(0f);

                // 頂点2
                writer.Write(0f);
                writer.Write(1f);
                writer.Write(0f);

                // 頂点3
                writer.Write(0f);
                writer.Write(0f);
                writer.Write(1f);

                // 属性バイト
                writer.Write((ushort)0);
            }

            // Act
            var result = StlParser.Load(filePath);

            // Assert
            Assert.Single(result.Triangles);

            var triangle = result.Triangles[0];
            Assert.Equal(1f, triangle.Normal.X);
            Assert.Equal(0f, triangle.Normal.Y);
            Assert.Equal(0f, triangle.Normal.Z);

            Assert.Equal(0f, triangle.Vertex1.X);
            Assert.Equal(0f, triangle.Vertex1.Y);
            Assert.Equal(0f, triangle.Vertex1.Z);

            Assert.Equal(0f, triangle.Vertex2.X);
            Assert.Equal(1f, triangle.Vertex2.Y);
            Assert.Equal(0f, triangle.Vertex2.Z);

            Assert.Equal(0f, triangle.Vertex3.X);
            Assert.Equal(0f, triangle.Vertex3.Y);
            Assert.Equal(1f, triangle.Vertex3.Z);
        }

        [Fact]
        public void Save_AsciiFormat_CreatesCorrectFile()
        {
            // Arrange
            var filePath = Path.Combine(TestFilesDirectory, "output.stl");
            var stlFile = new StlFile
            {
                SolidName = "test",
                Triangles =
                [
                    new Triangle
                    {
                        Normal = new Vector3(1, 0, 0),
                        Vertex1 = new Vector3(0, 0, 0),
                        Vertex2 = new Vector3(0, 1, 0),
                        Vertex3 = new Vector3(0, 0, 1)
                    }
                ]
            };

            // Act
            StlParser.Save(stlFile, filePath, STLFileType.ASCII);

            // Assert
            var content = File.ReadAllText(filePath)
                .Replace("\r\n", "\n")
                .TrimEnd();

            var expectedContent = @"solid test
  facet normal 1 0 0
    outer loop
      vertex 0 0 0
      vertex 0 1 0
      vertex 0 0 1
    endloop
  endfacet
endsolid test"
                .Replace("\r\n", "\n")
                .TrimEnd();

            Assert.Equal(expectedContent, content);
        }

        [Fact]
        public void Save_BinaryFormat_CreatesCorrectFile()
        {
            // Arrange
            var filePath = Path.Combine(TestFilesDirectory, "output.stl");
            var stlFile = new StlFile
            {
                Triangles =
                [
                    new() {
                        Normal = new Vector3(1, 0, 0),
                        Vertex1 = new Vector3(0, 0, 0),
                        Vertex2 = new Vector3(0, 1, 0),
                        Vertex3 = new Vector3(0, 0, 1)
                    }
                ]
            };

            // Act
            StlParser.Save(stlFile, filePath, STLFileType.Binary);

            // Assert
            using var reader = new BinaryReader(File.OpenRead(filePath));
            
            // ヘッダーをスキップ
            reader.ReadBytes(80);
            
            // 三角形の数を確認
            Assert.Equal(1u, reader.ReadUInt32());

            // 法線ベクトルを確認
            Assert.Equal(1f, reader.ReadSingle());
            Assert.Equal(0f, reader.ReadSingle());
            Assert.Equal(0f, reader.ReadSingle());

            // 頂点を確認
            Assert.Equal(0f, reader.ReadSingle());
            Assert.Equal(0f, reader.ReadSingle());
            Assert.Equal(0f, reader.ReadSingle());

            Assert.Equal(0f, reader.ReadSingle());
            Assert.Equal(1f, reader.ReadSingle());
            Assert.Equal(0f, reader.ReadSingle());

            Assert.Equal(0f, reader.ReadSingle());
            Assert.Equal(0f, reader.ReadSingle());
            Assert.Equal(1f, reader.ReadSingle());
        }

        [Fact]
        public void Load_FileNotFound_ThrowsFileNotFoundException()
        {
            var nonExistentFile = Path.Combine(TestFilesDirectory, "nonexistent.stl");
            Assert.Throws<FileNotFoundException>(() => StlParser.Load(nonExistentFile));
        }

        [Fact]
        public void Load_InvalidAsciiFormat_ThrowsStlParserException()
        {
            // Arrange
            var filePath = Path.Combine(TestFilesDirectory, "invalid.stl");
            File.WriteAllText(filePath, "invalid content");

            // Act & Assert
            Assert.Throws<StlParserException>(() => StlParser.Load(filePath));
        }

        [Fact]
        public void Load_InvalidBinaryFormat_ThrowsStlParserException()
        {
            // Arrange
            var filePath = Path.Combine(TestFilesDirectory, "invalid.stl");
            File.WriteAllBytes(filePath, [0, 1, 2, 3]);

            // Act & Assert
            Assert.Throws<StlParserException>(() => StlParser.Load(filePath));
        }

        [Fact]
        public async Task LoadAsync_ValidAsciiFile_ReturnsCorrectStlFile()
        {
            // Arrange
            var filePath = Path.Combine(TestFilesDirectory, "test.stl");
            var asciiContent = @"solid test
  facet normal 1 0 0
    outer loop
      vertex 0 0 0
      vertex 0 1 0
      vertex 0 0 1
    endloop
  endfacet
endsolid test";
            await File.WriteAllTextAsync(filePath, asciiContent);

            var progressUpdates = new List<(int progress, string message)>();
            void ProgressCallback(int progress, string message)
            {
                progressUpdates.Add((progress, message));
            }

            // Act
            var result = await StlParser.LoadAsync(filePath, ProgressCallback);

            // Assert
            Assert.Equal("test", result.SolidName);
            Assert.Single(result.Triangles);
            Assert.NotEmpty(progressUpdates);
            Assert.Equal(100, progressUpdates[^1].progress);
        }

        [Fact]
        public async Task SaveAsync_WithProgress_ReportsProgress()
        {
            // Arrange
            var filePath = Path.Combine(TestFilesDirectory, "output.stl");
            var stlFile = new StlFile
            {
                SolidName = "test",
                Triangles =
                [
                    new Triangle
                    {
                        Normal = new Vector3(1, 0, 0),
                        Vertex1 = new Vector3(0, 0, 0),
                        Vertex2 = new Vector3(0, 1, 0),
                        Vertex3 = new Vector3(0, 0, 1)
                    }
                ]
            };

            var progressUpdates = new List<(int progress, string message)>();
            void ProgressCallback(int progress, string message)
            {
                progressUpdates.Add((progress, message));
            }

            // Act
            await StlParser.SaveAsync(stlFile, filePath, STLFileType.ASCII, ProgressCallback);

            // Assert
            Assert.NotEmpty(progressUpdates);
            Assert.Equal(100, progressUpdates[^1].progress);
            Assert.True(File.Exists(filePath));
        }

        [Fact]
        public async Task LoadAsync_Cancellation_ThrowsOperationCanceledException()
        {
            // Arrange
            var filePath = Path.Combine(TestFilesDirectory, "test.stl");
            var asciiContent = @"solid test
  facet normal 1 0 0
    outer loop
      vertex 0 0 0
      vertex 0 1 0
      vertex 0 0 1
    endloop
  endfacet
endsolid test";
            await File.WriteAllTextAsync(filePath, asciiContent);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await StlParser.LoadAsync(filePath, cancellationToken: cts.Token));
        }

        [Fact]
        public void Load_RealWorldStlFile_LoadsSuccessfully()
        {
            // Arrange
            var filePath = Path.Combine("..\\..\\..\\TestData", "DeLorean.STL");

            // Act
            var result = StlParser.Load(filePath);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.Triangles);
            Assert.All(result.Triangles, triangle =>
            {
                Assert.NotNull(triangle.Normal);
                Assert.NotNull(triangle.Vertex1);
                Assert.NotNull(triangle.Vertex2);
                Assert.NotNull(triangle.Vertex3);
            });
        }

        [Fact]
        public async Task LoadAsync_RealWorldStlFile_LoadsSuccessfully()
        {
            // Arrange
            var filePath = Path.Combine("..\\..\\..\\TestData", "DeLorean.STL");
            Assert.True(File.Exists(filePath), $"テストファイルが見つかりません: {filePath}");

            var progressUpdates = new List<(int progress, string message)>();
            void ProgressCallback(int progress, string message)
            {
                progressUpdates.Add((progress, message));
            }

            try
            {
                // Act
                var result = await StlParser.LoadAsync(filePath, ProgressCallback);

                // Assert
                Assert.NotNull(result);
                Assert.NotEmpty(result.Triangles);
                Assert.NotEmpty(progressUpdates);
                Assert.Contains(progressUpdates, update => update.progress == 100);
                Assert.All(result.Triangles, triangle =>
                {
                    Assert.NotNull(triangle.Normal);
                    Assert.NotNull(triangle.Vertex1);
                    Assert.NotNull(triangle.Vertex2);
                    Assert.NotNull(triangle.Vertex3);
                });
            }
            catch (StlParserException ex)
            {
                Assert.Fail($"STLファイルの解析に失敗しました: {ex.Message}\nファイルパス: {Path.GetFullPath(filePath)}");
            }
        }

        [Fact]
        public async Task SaveAndLoadRoundTrip_RealWorldStlFile_PreservesData()
        {
            // Arrange
            var originalFilePath = Path.Combine("..\\..\\..\\TestData", "DeLorean.STL");
            var asciiOutputPath = Path.Combine(TestFilesDirectory, "DeLorean_ascii.stl");
            var binaryOutputPath = Path.Combine(TestFilesDirectory, "DeLorean_binary.stl");

            // Act - Load original file
            var original = StlParser.Load(originalFilePath);

            // Save as ASCII and Binary
            await StlParser.SaveAsync(original, asciiOutputPath, STLFileType.ASCII);
            await StlParser.SaveAsync(original, binaryOutputPath, STLFileType.Binary);

            // Load back the saved files
            var loadedAscii = StlParser.Load(asciiOutputPath);
            var loadedBinary = StlParser.Load(binaryOutputPath);

            // Assert
            Assert.Equal(original.Triangles.Count, loadedAscii.Triangles.Count);
            Assert.Equal(original.Triangles.Count, loadedBinary.Triangles.Count);

            // 三角形データの検証（浮動小数点の誤差を考慮）
            for (var i = 0; i < original.Triangles.Count; i++)
            {
                var originalTriangle = original.Triangles[i];
                var asciiTriangle = loadedAscii.Triangles[i];
                var binaryTriangle = loadedBinary.Triangles[i];

                AssertVector3Equal(originalTriangle.Normal, asciiTriangle.Normal);
                AssertVector3Equal(originalTriangle.Vertex1, asciiTriangle.Vertex1);
                AssertVector3Equal(originalTriangle.Vertex2, asciiTriangle.Vertex2);
                AssertVector3Equal(originalTriangle.Vertex3, asciiTriangle.Vertex3);

                AssertVector3Equal(originalTriangle.Normal, binaryTriangle.Normal);
                AssertVector3Equal(originalTriangle.Vertex1, binaryTriangle.Vertex1);
                AssertVector3Equal(originalTriangle.Vertex2, binaryTriangle.Vertex2);
                AssertVector3Equal(originalTriangle.Vertex3, binaryTriangle.Vertex3);
            }
        }

        private static void AssertVector3Equal(Vector3 expected, Vector3 actual, float tolerance = 0.000001f)
        {
            Assert.True(Math.Abs(expected.X - actual.X) < tolerance, 
                $"X coordinates differ: expected {expected.X}, actual {actual.X}");
            Assert.True(Math.Abs(expected.Y - actual.Y) < tolerance,
                $"Y coordinates differ: expected {expected.Y}, actual {actual.Y}");
            Assert.True(Math.Abs(expected.Z - actual.Z) < tolerance,
                $"Z coordinates differ: expected {expected.Z}, actual {actual.Z}");
        }

        [Theory]
        [InlineData("DeLorean.STL", "バイナリ")]
        [InlineData("DeLorean_ascii.stl", "ASCII")]
        public void Load_DifferentFormatStlFiles_LoadsSuccessfully(string fileName, string format)
        {
            // Arrange
            var filePath = Path.Combine("..\\..\\..\\TestData", fileName);

            // Act
            var result = StlParser.Load(filePath);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.Triangles);
            Assert.True(result.Triangles.Count > 1000, $"{format}形式のSTLファイルから期待される数の三角形データを読み込めませんでした。");
            Assert.All(result.Triangles, triangle =>
            {
                Assert.NotNull(triangle.Normal);
                Assert.NotNull(triangle.Vertex1);
                Assert.NotNull(triangle.Vertex2);
                Assert.NotNull(triangle.Vertex3);
            });
        }

        [Theory]
        [InlineData("DeLorean.STL", "バイナリ")]
        [InlineData("DeLorean_ascii.stl", "ASCII")]
        public async Task LoadAsync_DifferentFormatStlFiles_LoadsSuccessfully(string fileName, string format)
        {
            // Arrange
            var filePath = Path.Combine("..\\..\\..\\TestData", fileName);
            var progressUpdates = new List<(int progress, string message)>();
            void ProgressCallback(int progress, string message)
            {
                progressUpdates.Add((progress, message));
            }

            // Act
            var result = await StlParser.LoadAsync(filePath, ProgressCallback);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.Triangles);
            Assert.True(result.Triangles.Count > 1000, $"{format}形式のSTLファイルから期待される数の三角形データを読み込めませんでした。");
            Assert.NotEmpty(progressUpdates);
            Assert.Contains(progressUpdates, update => update.progress == 100);
            Assert.All(result.Triangles, triangle =>
            {
                Assert.NotNull(triangle.Normal);
                Assert.NotNull(triangle.Vertex1);
                Assert.NotNull(triangle.Vertex2);
                Assert.NotNull(triangle.Vertex3);
            });
        }

        [Theory]
        [InlineData("DeLorean.STL", STLFileType.Binary)]
        [InlineData("DeLorean_ascii.stl", STLFileType.ASCII)]
        public async Task SaveAndLoadRoundTrip_PreservesOriginalFormat(string fileName, STLFileType format)
        {
            // Arrange
            var originalFilePath = Path.Combine("..\\..\\..\\TestData", fileName);
            var outputPath = Path.Combine(TestFilesDirectory, $"output_{fileName}");

            // Act - Load original file
            var original = await StlParser.LoadAsync(originalFilePath);

            // Save in the same format
            await StlParser.SaveAsync(original, outputPath, format);

            // Load back the saved file
            var loaded = await StlParser.LoadAsync(outputPath);

            // Assert
            Assert.Equal(original.Triangles.Count, loaded.Triangles.Count);

            // 三角形データの検証（浮動小数点の誤差を考慮）
            for (var i = 0; i < original.Triangles.Count; i++)
            {
                var originalTriangle = original.Triangles[i];
                var loadedTriangle = loaded.Triangles[i];

                AssertVector3Equal(originalTriangle.Normal, loadedTriangle.Normal);
                AssertVector3Equal(originalTriangle.Vertex1, loadedTriangle.Vertex1);
                AssertVector3Equal(originalTriangle.Vertex2, loadedTriangle.Vertex2);
                AssertVector3Equal(originalTriangle.Vertex3, loadedTriangle.Vertex3);
            }
        }
    }
}
