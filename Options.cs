using System.Text;
namespace MangaRepack
{
    class PackupOptions
    {
        ///<summary>
        ///设置选择封面图片的规则。
        ///</summary>
        public CoverOption coverOption = CoverOption.FirstImage;
        ///<summary>
        ///当coverOption为CoverOption.Choose时，只有符合该文件名的图片被设为封面。
        ///</summary>
        public string chosenCoverImageName = null;
        ///<summary>
        ///是否为顶层级内容标记linear="no"。
        ///</summary>
        public bool hideTopDirectoryContent = false;
        ///<summary>
        ///输出EPUB的语言
        ///</summary>
        public string language = "zh-CN";//ja zh-CN zh-TW
        ///<summary>
        ///输入为ZIP压缩包时，压缩包使用的编码
        ///</summary>
        public Encoding inputZipEncoding = Encoding.GetEncoding("gbk");
        ///<summary>
        ///设置排序模式
        ///</summary>
        public SortMethed sortMethed = new SortByFileName();
        ///<summary>
        ///设置压缩方法，不需要时设为null。需要注意封面不会被压缩。
        ///</summary>
        public ImageEncoder imageEncoder = null;

    }
}