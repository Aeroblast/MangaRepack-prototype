using System;
using System.IO;
namespace MangaRepack
{
    abstract class SortMethed
    {
        public abstract int Sort(Source x, Source y);
    }
    ///<summary>
    ///使用整个路径排序，适合分话资源合并为一卷时使用。
    ///</summary>
    class SortByFullPath : SortMethed
    {
        public override int Sort(Source x, Source y)
        { return x.GetPath().CompareTo(y.GetPath()); }

        public override string ToString() { return "SortByFullPath"; }
    }
    ///<summary>
    ///使用文件名排序，适合将连续序号的资源划分目录时使用。
    ///</summary>
    class SortByFileName : SortMethed
    {
        public override int Sort(Source x, Source y)
        { return Path.GetFileName(x.GetPath()).CompareTo(Path.GetFileName(y.GetPath())); }

        public override string ToString() { return "SortByFileName"; }
    }
}