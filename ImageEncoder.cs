using System;
using System.IO;
using System.Diagnostics;
namespace MangaRepack
{
    abstract class ImageEncoder
    {
        public abstract Source Encode(Source s);
    }
    class HeicEncoder : ImageEncoder
    {
        public override Source Encode(Source s)
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

        public override string ToString() { return "heic"; }
    }
}