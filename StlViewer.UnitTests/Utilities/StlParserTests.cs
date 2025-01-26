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
    }
}
