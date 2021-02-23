# MangaRepack
将漫画打包成EPUB。
大概效果：
+ iOS/iPadOS Apple Books：作为 EPUB3 Fixed Layout支持。自动全屏，右向左翻页（竖屏两页拼在一起滑动，横屏双页），支持目录。
+ iOS/iPadOS ComicShare：作为EPUB读取时，支持目录，右向左翻页（竖屏单页，横屏双页）。当作一般ZIP时，可以滚动，当然没目录。这App本来就全屏。
+ Kindle (使用kindlegen转换)：自动全屏，右向左翻页（竖屏单页，横屏双页）。只有Kindle横屏支持封面的rendition:center。
+ [自制PC EPUB阅读器](https://github.com/Aeroblast/AeroEpubViewer)：非标准支持。只能横向右向左全书滚动，支持目录。

其他情况至少可以当作ZIP读吧……大概。

## 使用方法
还没弄成应用，请当成脚本使用。

输入文件夹/压缩包，子文件夹将会被当做一个章节，文件夹名称当作标题。Options.cs有一堆选项，可以考虑怎么排序。

请注意代码里WEBP、HEIC之类的是私货，只用于个人奇奇怪怪的研究，无视掉吧=w=

## To-do
+ 需要个更smart的方法确认rendition对不对，或者指定每一话左/右起手。如果没双页拼一起的那种大图倒是无所谓……
