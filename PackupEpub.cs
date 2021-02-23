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
        + "<manifest>\n{1}</manifest>\n<spine page-progression-direction=\"rtl\">\n{2}</spine>\n</package>";
        const string nav_template =
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<html xmlns=\"http://www.w3.org/1999/xhtml\" xmlns:epub=\"http://www.idpf.org/2007/ops\">\n"
        + "<head><title>TOC</title></head>\n<body>\n<nav epub:type=\"toc\">\n    <ol>\n{0}    </ol>\n</nav>\n</body>\n</html>";
        const string container =
        "<?xml version=\"1.0\" ?>\n<container version=\"1.0\" xmlns=\"urn:oasis:names:tc:opendocument:xmlns:container\">\n"
        + "<rootfiles><rootfile full-path=\"OEBPS/manga.opf\" media-type=\"application/oebps-package+xml\"/></rootfiles>\n"
        + "</container>";
        const string xhtml_template =
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
        + "<!DOCTYPE html>\n<html xmlns=\"http://www.w3.org/1999/xhtml\" xmlns:epub=\"http://www.idpf.org/2007/ops\">"
        + "<head>\n  <meta charset=\"UTF-8\"/>\n  <title>{3}</title>\n  <meta name=\"viewport\" content=\"width={1}, height={2}\" />\n</head>"
        + "<body style=\"margin:0;padding:0;\">\n  <div>\n"
        + "    <svg style=\"margin:0;padding:0;\" xmlns=\"http://www.w3.org/2000/svg\" version=\"1.1\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" width=\"100%\" height=\"100%\" viewBox=\"0 0 {1} {2}\">\n"
        + "      <image width=\"{1}\" height=\"{2}\" xlink:href=\"{0}\"/>\n"
        + "    </svg>\n  </div>\n</body>\n</html>";
        public static void GenEpub(List<Source> inputSource, string inputPath, PackupOptions options)
        {
            inputSource.Sort(options.sortMethed.Sort);
            string outputEpubPath;
            if (Directory.Exists(inputPath))
                outputEpubPath = Path.GetFileNameWithoutExtension(inputPath) + ".epub";
            else
                outputEpubPath = Path.GetFileName(inputPath) + ".epub";
            if (File.Exists(outputEpubPath)) File.Delete(outputEpubPath);

            using (FileStream fs = new FileStream(outputEpubPath, FileMode.Create))
            using (ZipArchive epub = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                Utils.WriteTextToZip(epub, "mimetype", "application/epub+zip", CompressionLevel.NoCompression);//还不能用StreamWriter指定编码 似乎会往里写编码标志
                StringBuilder innerMetadata = new StringBuilder();
                StringBuilder innerManifest = new StringBuilder();
                StringBuilder innerManifest2 = new StringBuilder();
                StringBuilder innerSpine = new StringBuilder();
                StringBuilder innerNavOl = new StringBuilder();
                List<string> navEntries = new List<string>();

                innerMetadata.Append($"    <dc:identifier id=\"uuid\">{System.Guid.NewGuid()}</dc:identifier>\n");
                innerMetadata.Append($"    <dc:language>{options.language}</dc:language>\n");
                innerMetadata.Append(Utils.GetMetaFromFileName(Path.GetFileNameWithoutExtension(inputPath)));
                innerMetadata.Append("    <meta property=\"dcterms:modified\">" + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ") + "</meta>\n");
                innerManifest.Append("    <item id=\"nav\" href=\"nav.xhtml\" media-type=\"application/xhtml+xml\" properties=\"nav\"/>\n");

                int chapterCount = 0;
                bool coverSet = false;
                foreach (Source s in inputSource)
                {
                    Source current = s;
                    bool newNavFlag = false;
                    string path = s.GetPath();
                    string navEntry = Path.GetDirectoryName(path).Replace("_", "");

                    if (navEntry.Length > 0)
                        if (navEntries.IndexOf(navEntry) == -1)
                        {
                            chapterCount++;
                            navEntries.Add(navEntry);
                            newNavFlag = true;
                        }

                    string imagePath = "Images/" + Utils.Number(chapterCount, 2) + "_" + Utils.MapZipPath(path).Replace("/", "_");
                    string docPath = "Text/" + Path.GetFileNameWithoutExtension(imagePath) + ".xhtml";
                    string id = "i-" + Path.GetFileNameWithoutExtension(imagePath);
                    string mediaType = Utils.GetMediaType(imagePath);
                    string properties = "";

                    if (newNavFlag)
                    {
                        var navOlLi = $"        <li><a href=\"{docPath}\">{navEntry}</a></li>\n";
                        innerNavOl.Append(navOlLi);
                    }

                    if (
                        (options.coverOption == CoverOption.TopDirectoryFirstImage && !coverSet && Path.GetDirectoryName(imagePath).Length == 0)
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
                            imagePath = "Images/" + Utils.Number(chapterCount, 2) + "_" + Utils.MapZipPath(path).Replace("/", "_");
                            mediaType = Utils.GetMediaType(imagePath);
                        }
                    }


                    var item = $"    <item id=\"{id}\" href=\"{imagePath}\" media-type=\"{mediaType}\" {properties}/>\n";
                    var item_xhtml = $"    <item id=\"x-{id}\" href=\"{docPath}\" media-type=\"application/xhtml+xml\" properties=\"svg\" />\n";
                    innerManifest.Append(item);
                    innerManifest2.Append(item_xhtml);

                    var itemref = $"    <itemref idref=\"x-{id}\" />\n";
                    if (options.hideTopDirectoryContent)
                    {
                        if (Path.GetDirectoryName(imagePath) == "")
                        {
                            itemref = itemref.Insert(itemref.LastIndexOf("/>"), "linear=\"no\" ");
                        }
                    }
                    innerSpine.Append(itemref);

                    var zipEntry = epub.CreateEntry("OEBPS/" + imagePath, CompressionLevel.NoCompression);
                    using (var zs = zipEntry.Open())
                        current.GetStream().CopyTo(zs);
                    var (w, h) = Utils.GetImageSize(s);
                    Utils.WriteTextToZip(epub, "OEBPS/" + docPath, string.Format(xhtml_template, "../" + imagePath, w, h, "CONTENT"));
                }
                innerManifest.Append(innerManifest2);
                Utils.WriteTextToZip(epub, "OEBPS/manga.opf", string.Format(opf_template, innerMetadata, innerManifest, innerSpine));
                Utils.WriteTextToZip(epub, "OEBPS/nav.xhtml", string.Format(nav_template, innerNavOl));
                Utils.WriteTextToZip(epub, "META-INF/container.xml", container);

            }


        }

    }
}