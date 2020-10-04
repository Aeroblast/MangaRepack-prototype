using System;
using System.IO;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text.Encodings;
using System.Text.RegularExpressions;
using System.Text;

using System.Reflection;
namespace MangaRepack
{

    class Program
    {
        static void Main(string[] args)
        {
            Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            //args[0] = "";
            if (args.Length > 0)
            {
                string inputPath = args[0];
                PackupEpub.GenEpub(ReadSource(inputPath), inputPath,new PackupOptions());
            }
        }
        static List<Source> ReadSource(string inputPath)
        {
            List<Source> list = new List<Source>();
            if (File.Exists(inputPath))
            {
                using (FileStream fs = File.OpenRead(inputPath))
                using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Read, false, Encoding.GetEncoding("gbk")))
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
       

    }
    
}
