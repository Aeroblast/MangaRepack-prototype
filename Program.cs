using System;
using System.IO;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text.Encodings;
using System.Text.RegularExpressions;
using System.Text;
using System.Diagnostics;
namespace MangaRepack
{
    class Program
    {

        ///<summary>
        ///设置选择封面图片的规则。
        ///</summary>
        static CoverOption coverOption = CoverOption.FirstImage;
        ///<summary>
        ///当coverOption为CoverOption.Choose时，只有符合该文件名的图片被设为封面。
        ///</summary>
        static string chosenCoverImageName = null;
        ///<summary>
        ///是否为顶层级内容标记linear="no"。
        ///</summary>
        static bool hideTopDirectoryContent = false;
        ///<summary>
        ///输出EPUB的语言
        ///</summary>
        static string language = "ja";
        ///<summary>
        ///输入为ZIP压缩包时，压缩包使用的编码
        ///</summary>
        static Encoding inputZipEncoding = Encoding.GetEncoding("gbk");
        ///<summary>
        ///设置排序模式
        ///</summary>
        static Comparison<Source> sortMode = SortByFullPath;
        ///<summary>
        ///设置压缩方法，不需要时设为null。需要注意封面不会被压缩。
        ///</summary>
        static EncodeMethed compressMethed = CompressHeic;
        ///////////以上是设定////////////

        enum CoverOption
        {
            ///<summary>
            ///所有图片排序后第一个出现的图片将作为封面。
            ///</summary>
            FirstImage,
            ///<summary>
            ///顶级目录中排序后第一个出现的图片将作为封面。
            ///</summary>
            TopDirectoryFirstImage,
            ///<summary>
            ///设置chosenCoverImageName以选择封面。
            ///</summary>
            Choose
        }
        ///<summary>
        ///使用整个路径排序，适合分话资源合并为一卷时使用。
        ///</summary>
        static int SortByFullPath(Source x, Source y)
        { return x.GetPath().CompareTo(y.GetPath()); }
        ///<summary>
        ///使用文件名排序，适合将连续序号的资源划分目录时使用。
        ///</summary>
        static int SortByFileName(Source x, Source y)
        { return Path.GetFileName(x.GetPath()).CompareTo(Path.GetFileName(y.GetPath())); }
        delegate Source EncodeMethed(Source s);
        static Source CompressHeic(Source s)
        {
            using (var fs = File.OpenWrite("tempimg"))
                s.GetStream().CopyTo(fs);
            Process p = new Process();
            p.StartInfo.FileName = @"vips\bin\vips.exe";
            p.StartInfo.Arguments = "heifsave tempimg tempimg.heic --Q 50";
            p.Start();
            p.WaitForExit();
            var r = File.ReadAllBytes("tempimg.heic");
            File.Delete("tempimg");
            File.Delete("tempimg.heic");
            Console.WriteLine("Compress heic:" + s.GetPath());
            Source s2 = new MemorySource(r, Path.ChangeExtension(s.GetPath(), ".heic"));
            return s2;
        }
        const string opf_template =
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
        + "<package xmlns=\"http://www.idpf.org/2007/opf\" version=\"3.0\" unique-identifier=\"uuid\">\n"
        + "<metadata xmlns:dc=\"http://purl.org/dc/elements/1.1/\">\n{0}</metadata>\n"
        + "<manifest>\n{1}</manifest>\n<spine toc=\"nav\" page-progression-direction=\"rtl\">\n{2}</spine>\n</package>";
        const string nav_template =
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<html xmlns=\"http://www.w3.org/1999/xhtml\" xmlns:epub=\"http://www.idpf.org/2007/ops\">\n"
        + "<head></head>\n<body>\n<nav epub:type=\"toc\">\n    <ol>\n{0}    </ol>\n</nav>\n</body>\n</html>";
        const string container =
        "<?xml version=\"1.0\" ?>\n<container version=\"1.0\" xmlns=\"urn:oasis:names:tc:opendocument:xmlns:container\">\n"
        + "<rootfiles><rootfile full-path=\"manga.opf\" media-type=\"application/oebps-package+xml\"/></rootfiles>\n"
        + "</container>";


        static void Main(string[] args)
        {
            Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            //args[0] = @"E:\_Download\_Manga\";
            if (args.Length > 0)
            {
                string inputPath = args[0];
                GenEpub(ReadSource(inputPath), inputPath);
            }
        }
        static List<Source> ReadSource(string inputPath)
        {
            List<Source> list = new List<Source>();
            if (File.Exists(inputPath))
            {
                using (FileStream fs = File.OpenRead(inputPath))
                using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Read, false, inputZipEncoding))
                {
                    foreach (ZipArchiveEntry e in zip.Entries)
                    {
                        string ext = Path.GetExtension(e.FullName).ToLower();
                        switch (ext)
                        {
                            case ".jpg":
                            case ".png":
                            case ".webp":
                                list.Add(new MemorySource(e));
                                break;
                        }

                    }
                }
            }
            else if (Directory.Exists(inputPath))
            {
                foreach (string file in Directory.GetFiles(inputPath, "", SearchOption.AllDirectories))
                {
                    string ext = Path.GetExtension(file).ToLower();
                    switch (ext)
                    {
                        case ".jpg":
                        case ".png":
                        case ".webp":
                            list.Add(new StaticFileSource(file, inputPath));
                            break;
                    }

                }
            }
            else { return null; }
            return list;
        }
        static void GenEpub(List<Source> inputSource, string inputPath)
        {
            inputSource.Sort(sortMode);
            string outputEpubPath = Path.GetFileNameWithoutExtension(inputPath) + ".epub";
            if (File.Exists(outputEpubPath)) File.Delete(outputEpubPath);
            ZipArchive epub = ZipFile.Open(outputEpubPath, ZipArchiveMode.Create);
            WriteTextToZip(epub, "mimetype", "application/epub+zip");
            StringBuilder innerMetadata = new StringBuilder();
            StringBuilder innerManifest = new StringBuilder();
            StringBuilder innerSpine = new StringBuilder();
            StringBuilder innerNavOl = new StringBuilder();
            List<string> navEntries = new List<string>();

            innerMetadata.Append($"    <dc:identifier id=\"uuid\">{System.Guid.NewGuid()}</dc:identifier>\n");
            innerMetadata.Append($"    <dc:language>{language}</dc:language>\n");
            innerMetadata.Append(GetMetaFromFileName(Path.GetFileNameWithoutExtension(inputPath)));
            innerManifest.Append("    <item id=\"nav\" href=\"nav.xhtml\" media-type=\"application/xhtml+xml\" properties=\"nav\"/>\n");
            int chapterCount = 0;
            bool coverSet = false;
            foreach (Source s in inputSource)
            {
                Source current = s;
                string path = s.GetPath();
                string navEntry = Path.GetDirectoryName(path).Replace("_", "");
                string zipPath = Number(chapterCount, 2) + "_" + MapZipPath(path);
                string id = Path.GetDirectoryName(zipPath) + "_" + Path.GetFileNameWithoutExtension(zipPath);
                string mediaType = GetMediaType(zipPath);
                string properties = "";
                if (
                    (coverOption == CoverOption.TopDirectoryFirstImage && !coverSet && Path.GetDirectoryName(zipPath).Length == 0)
                    || (coverOption == CoverOption.FirstImage && !coverSet)
                    || (coverOption == CoverOption.Choose && !coverSet && path == chosenCoverImageName)
                    )
                {
                    properties = "properties=\"cover-image\" ";
                    coverSet = true;
                }
                else
                {
                    if (compressMethed != null)
                    {
                        current = compressMethed(s);
                        path = current.GetPath();
                        navEntry = Path.GetDirectoryName(path).Replace("_", "");
                        zipPath = Number(chapterCount, 2) + "_" + MapZipPath(path);
                        id = Path.GetDirectoryName(zipPath) + "_" + Path.GetFileNameWithoutExtension(zipPath);
                        mediaType = GetMediaType(zipPath);
                    }
                }

                if (navEntry.Length > 0)
                    if (navEntries.IndexOf(navEntry) == -1)
                    {
                        chapterCount++;
                        zipPath = Number(chapterCount, 2) + "_" + MapZipPath(path);
                        navEntries.Add(navEntry);
                        var navOlLi = $"        <li><a href=\"{zipPath}\">{navEntry}</a></li>\n";
                        innerNavOl.Append(navOlLi);
                    }

                var item = $"    <item id=\"{id}\" href=\"{zipPath}\" media-type=\"{mediaType}\" {properties}/>\n";

                innerManifest.Append(item);

                var itemref = $"    <itemref idref=\"{id}\" />\n";
                if (hideTopDirectoryContent)
                {
                    if (Path.GetDirectoryName(zipPath) == "")
                    {
                        itemref = itemref.Insert(itemref.LastIndexOf("/>"), "linear=\"no\" ");
                    }
                }
                innerSpine.Append(itemref);

                var zipEntry = epub.CreateEntry(zipPath, CompressionLevel.NoCompression);
                using (var zs = zipEntry.Open())
                    current.GetStream().CopyTo(zs);
            }
            WriteTextToZip(epub, "manga.opf", string.Format(opf_template, innerMetadata, innerManifest, innerSpine));
            WriteTextToZip(epub, "nav.xhtml", string.Format(nav_template, innerNavOl));
            WriteTextToZip(epub, "META-INF/container.xml", container);
            epub.Dispose();
        }
        static void WriteTextToZip(ZipArchive zip, string entryName, string text)
        {
            var zipEntry = zip.CreateEntry(entryName);
            using (var zs = zipEntry.Open())
            using (var sr = new StreamWriter(zs, Encoding.UTF8))
                sr.Write(text);
        }
        static Regex filenameMeta = new Regex("^(\\[(.*?)\\]){0,1}(.+?)(\\[(.*?)\\])*$");
        static string GetMetaFromFileName(string filename)
        {
            Match m = filenameMeta.Match(filename);
            if (!m.Success) throw new Exception("Filename not valid. [作者]标题[可选tag][可选tag]");
            string creator = Trim(m.Groups[2].Value);
            string title = Trim(m.Groups[3].Value);
            var tags = m.Groups[5].Captures;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append($"    <dc:title>{Trim(title)}</dc:title>\n");
            foreach (string s in creator.Split(new char[] { ',', ';', '；', '，' }))
            {
                string trimed = Trim(s);
                if (trimed.Length > 0)
                    stringBuilder.Append($"    <dc:creator>{trimed}</dc:creator>\n");
            }
            int count = 0;
            foreach (Capture c in tags)
            {
                string s = Trim(c.Value);
                if (s.Length == 0) continue;
                count++;
                stringBuilder.Append($"    <meta name=\"Filename Metadata {count}\" content=\"{s}\"/>\n");
            }
            return stringBuilder.ToString();
        }
        static string MapZipPath(string s)
        {
            string[] rs = s.Split('/');
            for (int i = 0; i < rs.Length; i++) rs[i] = MapZipName(rs[i]);
            return string.Join('/', rs);
        }
        static Regex reg_epname = new Regex("^第{0,1}([0-9]+)[话話]");
        static Regex reg_exepname = new Regex("^番外.*?([0-9]+)");
        static string MapZipName(string s)
        {
            var m = reg_epname.Match(s);
            if (m.Success)
            {
                return "ep" + m.Groups[1].Value;
            }
            m = reg_exepname.Match(s);
            if (m.Success) return "extra_ep" + m.Groups[1].Value;
            string r = "";
            foreach (char c in s)
            {
                if (char.IsDigit(c) || char.IsLower(c) || char.IsUpper(c) || c == '_' || c == '.')
                    r += c;
                else if (c == '-') r += '_';
                else if (c == ' ')
                    r += ' ';
            }
            r = Trim(r).Replace(' ', '_');
            return r;
        }
        public static string Number(int number, int length = 4)
        {
            string r = number.ToString();
            for (int j = length - r.Length; j > 0; j--) r = "0" + r;
            return r;
        }
        static string GetMediaType(string path)
        {
            string ext = Path.GetExtension(path).ToLower();
            switch (ext)
            {
                case ".jpg": return "image/jpeg";
                case ".webp": return "image/webp";
                case ".png": return "image/png";
                case ".heic": return "image/heic";
            }
            throw new Exception("Unknown Media");
        }
        public static string Trim(string str)
        {
            int s = 0, e = str.Length - 1;
            for (; s < str.Length; s++) { if (str[s] == ' ' || str[s] == '\t' || str[s] == '\n' || str[s] == '\r') { } else break; }
            for (; e >= 0; e--) { if (str[e] == ' ' || str[e] == '\t' || str[e] == '\n' || str[e] == '\r') { } else break; }
            if (s <= e) return str.Substring(s, e - s + 1);
            else return "";
        }

    }
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
