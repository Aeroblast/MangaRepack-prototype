using System.IO;
using System.IO.Compression;
namespace MangaRepack
{

    abstract class Source
    {
        public abstract Stream GetStream();
        public abstract string GetPath();
        public override string ToString() { return GetPath(); }
    }
    class StaticFileSource : Source
    {
        string fullPath;
        string rootPath;
        string path;
        public override Stream GetStream()
        {
            return File.OpenRead(fullPath);
        }
        public override string GetPath()
        {
            if (path == null) path = Path.GetRelativePath(rootPath, fullPath).Replace('\\', '/');
            return path;
        }
        public StaticFileSource(string fullPath, string rootPath)
        {
            this.fullPath = fullPath;
            this.rootPath = rootPath;
        }
    }
    class MemorySource : Source
    {
        byte[] data;
        string path;
        public override Stream GetStream()
        {
            return new MemoryStream(data);
        }
        public override string GetPath()
        {
            return path;
        }
        public MemorySource(ZipArchiveEntry e)
        {
            path = e.FullName;
            data = new byte[e.Length];
            e.Open().Read(data);
        }
        public MemorySource(byte[] data, string name)
        {
            path = name;
            this.data = data;
        }
    }
}