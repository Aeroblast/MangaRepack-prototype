using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
namespace MangaRepack
{
    class Utils
    {
        public static void WriteTextToZip(ZipArchive zip, string entryName, string text, CompressionLevel compressionLevel = CompressionLevel.Optimal)
        {
            var zipEntry = zip.CreateEntry(entryName, compressionLevel);
            using (var zs = zipEntry.Open())
            using (var sr = new StreamWriter(zs))
            { sr.Write(text); }
        }
        static Regex filenameMeta = new Regex("^(\\[(.*?)\\]){0,1}(.+?)(\\[(.*?)\\])*$");
        public static string GetMetaFromFileName(string filename)
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
        public static string MapZipPath(string s)
        {
            string[] rs = s.Split('/');
            for (int i = 0; i < rs.Length; i++) rs[i] = MapZipName(rs[i]);
            return string.Join('/', rs);
        }
        static Regex reg_epname = new Regex("^第{0,1}([0-9]+)[话話]");
        static Regex reg_exepname = new Regex("^番外.*?([0-9]+)");
        public static string MapZipName(string s)
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
        public static string GetMediaType(string path)
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

        public static (int, int) GetImageSize(Source s)
        {
            using (var img = Image.FromStream(s.GetStream()))
            {
                return (img.Width, img.Height);
            }
        }
    }
}