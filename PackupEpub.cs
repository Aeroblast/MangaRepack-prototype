using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Collections.Generic;

namespace MangaRepack
{
    public enum CoverOption
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
    class PackupEpub
    {
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
        public static void GenEpub(List<Source> inputSource, string inputPath, PackupOptions options)
        {
            inputSource.Sort(options.sortMethed.Sort);
            string outputEpubPath;
            if (Directory.Exists(inputPath))
                outputEpubPath = Path.GetFileNameWithoutExtension(inputPath) + ".epub";
            else
                outputEpubPath = Path.GetFileName(inputPath) + ".epub";
            if (File.Exists(outputEpubPath)) File.Delete(outputEpubPath);
            ZipArchive epub = ZipFile.Open(outputEpubPath, ZipArchiveMode.Create);
            Utils.WriteTextToZip(epub, "mimetype", "application/epub+zip");
            StringBuilder innerMetadata = new StringBuilder();
            StringBuilder innerManifest = new StringBuilder();
            StringBuilder innerSpine = new StringBuilder();
            StringBuilder innerNavOl = new StringBuilder();
            List<string> navEntries = new List<string>();

            innerMetadata.Append($"    <dc:identifier id=\"uuid\">{System.Guid.NewGuid()}</dc:identifier>\n");
            innerMetadata.Append($"    <dc:language>{options.language}</dc:language>\n");
            innerMetadata.Append(Utils.GetMetaFromFileName(Path.GetFileNameWithoutExtension(inputPath)));
            innerManifest.Append("    <item id=\"nav\" href=\"nav.xhtml\" media-type=\"application/xhtml+xml\" properties=\"nav\"/>\n");
            int chapterCount = 0;
            bool coverSet = false;
            foreach (Source s in inputSource)
            {
                Source current = s;
                string path = s.GetPath();
                string navEntry = Path.GetDirectoryName(path).Replace("_", "");
                string zipPath = Utils.Number(chapterCount, 2) + "_" + Utils.MapZipPath(path);
                string id = Path.GetDirectoryName(zipPath) + "_" + Path.GetFileNameWithoutExtension(zipPath);
                string mediaType = Utils.GetMediaType(zipPath);
                string properties = "";
                if (
                    (options.coverOption == CoverOption.TopDirectoryFirstImage && !coverSet && Path.GetDirectoryName(zipPath).Length == 0)
                    || (options.coverOption == CoverOption.FirstImage && !coverSet)
                    || (options.coverOption == CoverOption.Choose && !coverSet && path == options.chosenCoverImageName)
                    )
                {
                    properties = "properties=\"cover-image\" ";
                    coverSet = true;
                }
                else
                {
                    if (options.imageEncoder != null)
                    {
                        current = options.imageEncoder.Encode(s);
                        path = current.GetPath();
                        navEntry = Path.GetDirectoryName(path).Replace("_", "");
                        zipPath = Utils.Number(chapterCount, 2) + "_" + Utils.MapZipPath(path);
                        id = Path.GetDirectoryName(zipPath) + "_" + Path.GetFileNameWithoutExtension(zipPath);
                        mediaType = Utils.GetMediaType(zipPath);
                    }
                }

                if (navEntry.Length > 0)
                    if (navEntries.IndexOf(navEntry) == -1)
                    {
                        chapterCount++;
                        zipPath = Utils.Number(chapterCount, 2) + "_" + Utils.MapZipPath(path);
                        navEntries.Add(navEntry);
                        var navOlLi = $"        <li><a href=\"{zipPath}\">{navEntry}</a></li>\n";
                        innerNavOl.Append(navOlLi);
                    }

                var item = $"    <item id=\"{id}\" href=\"{zipPath}\" media-type=\"{mediaType}\" {properties}/>\n";

                innerManifest.Append(item);

                var itemref = $"    <itemref idref=\"{id}\" />\n";
                if (options.hideTopDirectoryContent)
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
            Utils.WriteTextToZip(epub, "manga.opf", string.Format(opf_template, innerMetadata, innerManifest, innerSpine));
            Utils.WriteTextToZip(epub, "nav.xhtml", string.Format(nav_template, innerNavOl));
            Utils.WriteTextToZip(epub, "META-INF/container.xml", container);
            epub.Dispose();
        }

    }
}