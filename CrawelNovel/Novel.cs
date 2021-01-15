using CrawelNovel.Model;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;
using EPubDocument = Epub.Document;
using NavPoint = Epub.NavPoint;
using CrawelNovel.Manage;
using System.Drawing.Imaging;

namespace CrawelNovel
{
    public partial class Novel : Form
    {
        public int ProgressCount = 0;

        public int CataId = 0;

        public HtmlNodeCollection col = null;

        public string CataUrl = string.Empty;
        public Novel()
        {
            InitializeComponent();
            this.backgroundWorker1.WorkerReportsProgress = true;  //设置能报告进度更新
            this.backgroundWorker1.WorkerSupportsCancellation = true;  //设置支持异步取消

        }

        private void btnCrawel_Click(object sender, EventArgs e)
        {
            col = null;
            if (!txtWebSite.Text.IsNotNullOrEmpty())
            {
                MessageBox.Show("必须输入抓取的网页");
                return;
            }

            string htmlContent = GetContent(txtWebSite.Text);
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);
            Catalog log = new Catalog();
            HtmlNode navFM = doc.GetElementbyId("fmimg");
            string ImgUrl = navFM.ChildNodes[1].Attributes["src"].Value;
            

            log.NoteName = navFM.ChildNodes[1].Attributes["alt"].Value;
            string search = log.NoteName.UrlEncode(Encoding.UTF8);


            string ImgHtml = GetContent("https://www.qidian.com/search?kw=" + search);
            log.Img = GetImageByteFromQiDian(ImgHtml);
            log.CreateTime = DateTime.Now;
            log.Url = txtWebSite.Text;
            CataId = SaveCatalog(log);
            txtNovel.Text = log.NoteName;
            col = doc.DocumentNode.SelectNodes("//dd");
            //Parser parser = new Parser(urls);
            
            ProgressCount = col.Count;

            this.backgroundWorker1.DoWork += backgroundWorker1_DoWork;
            this.backgroundWorker1.RunWorkerCompleted += backgroundWorker1_RunWorkerCompleted;
            this.backgroundWorker1.RunWorkerAsync();  //运行backgroundWorker组件
            ProgressForm form = new ProgressForm(this.backgroundWorker1);  //显示进度条窗体
            form.ShowDialog(this);



        }

        

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            DbManage manage = new DbManage();
            var catalog = manage.GetCataLogByName(txtNovel.Text);

            var epub = new EPubDocument();

            epub.AddTitle(txtNovel.Text);

            epub.AddLanguage("zh-cn");

            String css = CrawelNovel.Properties.Resources.style;

            css += "\nbody { font-family: LiberationSerif; }\n";
            css += "@font-face { font-family : LiberationSerif; font-weight : normal; font-style: normal; src : url(LiberationSerif-Regular.ttf); }\n";
            css += "@font-face { font-family : LiberationSerif; font-weight : normal; font-style: italic; src : url(LiberationSerif-Italic.ttf); }\n";
            css += "@font-face { font-family : LiberationSerif; font-weight : bold; font-style: normal; src : url(LiberationSerif-Bold.ttf); }\n";
            css += "@font-face { font-family : LiberationSerif; font-weight : bold; font-style: italic; src : url(LiberationSerif-BoldItalic.ttf); }\n";

            epub.AddData("LiberationSerif-Regular.ttf", CrawelNovel.Properties.Resources.LiberationSerif_Regular, "application/octet-stream");
            epub.AddData("LiberationSerif-Bold.ttf", CrawelNovel.Properties.Resources.LiberationSerif_Bold, "application/octet-stream");
            epub.AddData("LiberationSerif-Italic.ttf", CrawelNovel.Properties.Resources.LiberationSerif_Italic, "application/octet-stream");
            epub.AddData("LiberationSerif-BoldItalic.ttf", CrawelNovel.Properties.Resources.LiberationSerif_BoldItalic, "application/octet-stream");

            epub.AddStylesheetData("style.css", css);
            
            String coverId = epub.AddImageData("cover.png", catalog.Img);
            epub.AddMetaItem("cover", coverId);
            

            String page_template = Encoding.UTF8.GetString(CrawelNovel.Properties.Resources.page);

            int navCounter = 1;
            int pageCounter = 1;

            var chaps = manage.GetChapterList(catalog.Id);
            foreach(var chap in chaps)
            {
                String page = page_template.Replace("%%CONTENT%%", chap.ChapterName+ "<br />" +chap.ChapterContent);
                String pageName = String.Format("page{0}.xhtml", pageCounter);
                epub.AddXhtmlData(pageName, page);
                epub.AddNavPoint(chap.ChapterName, pageName, navCounter++);
                pageCounter++;
            }
            SaveFileDialog saveFileDialog = new SaveFileDialog();


            saveFileDialog.Filter = "epub files (*.epub)|*.epub|All files (*.*)|*.*";
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.RestoreDirectory = true;

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                epub.Generate(saveFileDialog.FileName);
            }
        }

        public void SaveChapData(List<Chapter> chaps)
        {
            using (CrawelNovelDbContext context = new CrawelNovelDbContext())
            {
                context.Chapter.AddRange(chaps);
                context.SaveChanges();
            }
        }

        public int SaveCatalog(Catalog cata)
        {
            int returnId = 0;
            using (CrawelNovelDbContext context = new CrawelNovelDbContext())
            {
                context.Catalog.Add(cata);
                context.SaveChanges();
                returnId = cata.Id;

            }
            return returnId;
        }

        public void UpdateCatalog(Catalog cata)
        {
            using (CrawelNovelDbContext context = new CrawelNovelDbContext())
            {
                context.Catalog.Attach(cata);
                context.Entry(cata).Property("UpdateTime").IsModified = true;
                context.SaveChanges();
            }

        }

        /// <summary>
        /// 获取指定网页内容
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public string GetContent(string url)
        {

            string content;
            HttpWebRequest httpRequest = (HttpWebRequest)HttpWebRequest.Create(url);
            //httpRequest.Referer = url;
            httpRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36";
            httpRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            httpRequest.Method = "GET";
            httpRequest.Headers.Set("Cache-Control", "max-age=0");
            httpRequest.Headers.Set("Accept-Language", "zh-CN;q=0.9");
            httpRequest.Headers.Set("Upgrade-Insecure-Requests", "1");
            httpRequest.Headers.Set("Accept-Encoding", "gzip, deflate");
            httpRequest.KeepAlive = true;
            httpRequest.AllowAutoRedirect = true;

            try
            {
                HttpWebResponse httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                if (httpResponse.ContentEncoding.ToLower() == "gzip")  // 如果使用了GZip则先解压
                {
                    using (Stream Stream_Receive = httpResponse.GetResponseStream())
                    {
                        using (var Zip_Stream = new GZipStream(Stream_Receive, CompressionMode.Decompress))
                        {
                            using (StreamReader Stream_Reader = new StreamReader(Zip_Stream, Encoding.UTF8))
                            {
                                content = Stream_Reader.ReadToEnd();
                            }
                        }
                    }
                }
                else
                {
                    using (Stream Stream_Receive = httpResponse.GetResponseStream())
                    {
                        using (StreamReader Stream_Reader = new StreamReader(Stream_Receive, Encoding.Default))
                        {
                            content = Stream_Reader.ReadToEnd();
                        }
                    }
                }

                return content;
            }
            catch
            {
                return null;
            }


        }


        /// <summary>
        /// 获取指定图片转换成Byte字节信息
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public Byte[] GetImage(string url)
        {
            HttpWebRequest httpRequest = (HttpWebRequest)HttpWebRequest.Create(url);
            //httpRequest.Referer = url;
            httpRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36";
            httpRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            httpRequest.Method = "GET";
            httpRequest.Headers.Set("Cache-Control", "max-age=0");
            httpRequest.Headers.Set("Accept-Language", "zh-CN;q=0.9");
            httpRequest.Headers.Set("Upgrade-Insecure-Requests", "1");
            httpRequest.Headers.Set("Accept-Encoding", "gzip, deflate");
            httpRequest.KeepAlive = true;
            httpRequest.AllowAutoRedirect = true;

            try
            {
                HttpWebResponse httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                if (httpResponse.ContentEncoding.ToLower() == "gzip")  // 如果使用了GZip则先解压
                {
                    using (Stream Stream_Receive = httpResponse.GetResponseStream())
                    {
                        using (var Zip_Stream = new GZipStream(Stream_Receive, CompressionMode.Decompress))
                        {
                            MemoryStream ms = new MemoryStream();
                            Zip_Stream.CopyTo(ms);
                            return ms.ToArray();
                        }
                    }
                }
                else
                {
                    using (Stream Stream_Receive = httpResponse.GetResponseStream())
                    {
                        MemoryStream ms = new MemoryStream();
                        Stream_Receive.CopyTo(ms);


                        return ms.ToArray();
                    }
                }
            }
            catch(Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// 获取小说内容并设置等待时间，网站做了刷新处理，好在没有做IP，否则需要动态IP
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public string GetContentWait(string url,int Min,int Max)
        {
            Random ran = new Random();
            Thread.Sleep(ran.Next(Min*1000, Max*1000));
            string content;
            HttpWebRequest httpRequest = (HttpWebRequest)HttpWebRequest.Create(url);
            //httpRequest.Referer = url;
            httpRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36";
            httpRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            httpRequest.Method = "GET";
            httpRequest.Headers.Set("Cache-Control", "max-age=0");
            httpRequest.Headers.Set("Accept-Language", "zh-CN;q=0.9");
            httpRequest.Headers.Set("Upgrade-Insecure-Requests", "1");
            httpRequest.Headers.Set("Accept-Encoding", "gzip, deflate");
            httpRequest.KeepAlive = true;
            httpRequest.AllowAutoRedirect = true;

            try
            {
                HttpWebResponse httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                if (httpResponse.ContentEncoding.ToLower() == "gzip")  // 如果使用了GZip则先解压
                {
                    using (Stream Stream_Receive = httpResponse.GetResponseStream())
                    {
                        using (var Zip_Stream = new GZipStream(Stream_Receive, CompressionMode.Decompress))
                        {
                            using (StreamReader Stream_Reader = new StreamReader(Zip_Stream, Encoding.UTF8))
                            {
                                content = Stream_Reader.ReadToEnd();
                            }
                        }
                    }
                }
                else
                {
                    using (Stream Stream_Receive = httpResponse.GetResponseStream())
                    {
                        using (StreamReader Stream_Reader = new StreamReader(Stream_Receive, Encoding.Default))
                        {
                            content = Stream_Reader.ReadToEnd();
                        }
                    }
                }

                return content;
            }
            catch
            {
                return null;
            }


        }



        public Byte[] GetImageByteFromQiDian(string Content)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(Content);
            HtmlNode navNode = doc.GetElementbyId("result-list");
            HtmlNode navNode1 = navNode.SelectSingleNode(navNode.XPath+"/div[1]/ul[1]/li[1]");
            var navNode2 = navNode1.SelectSingleNode(navNode1.XPath + "/div[1]/a[1]/img[1]");

            var imgUrl ="https:"+ navNode2.Attributes["src"].Value;
            return GetImage(imgUrl);
        }

      

        //在另一个线程上开始运行(处理进度条)
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            List<Chapter> chaps = new List<Chapter>();
            int Count = 1;
            foreach (var node in col)
            {
                Count++;
                worker.ReportProgress(Count*100/col.Count);
                if (worker.CancellationPending) //获取程序是否已请求取消后台操作
                {
                    e.Cancel = true;
                    break;
                }
                Chapter chap = new Chapter();
                chap.ChapterName = node.InnerText;
                var atag = node.ChildNodes;
                chap.ChapterUrl = atag[0].Attributes["href"].Value;
                chap.IsFinished = true;
                chap.NoteBookId = CataId;
                chap.CreateTime = DateTime.Now;
                string chapCont = GetContentWait(txtWebSite.Text + chap.ChapterUrl, Int32.Parse(txtMin.Text), Int32.Parse(txtMax.Text));
                if (chapCont.IsNotNullOrEmpty())
                {
                    HtmlDocument docNew = new HtmlDocument();
                    docNew.LoadHtml(chapCont);
                    HtmlNode navNode = docNew.GetElementbyId("content");
                    chap.ChapterContent = navNode.InnerText.Replace("&nbsp;", "").Replace("<br/>", "").Replace("\r", "").Replace("\n", "");
                    chaps.Add(chap);
                }
                else
                {
                    chap.IsFinished = false;
                    chaps.Add(chap);
                }

            }

            SaveChapData(chaps);
            
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message);
            }
            else if (e.Cancelled)
            {
                MessageBox.Show("取消");
            }
            else
            {
                MessageBox.Show("完成");
            }
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {

            this.backgroundWorker1.DoWork += backgroundWorker1_UpdateDoWork;
            this.backgroundWorker1.RunWorkerCompleted += this.backgroundWorker1_UpdateRunWorkerCompleted;
            

            List<Catalog> catas = null;
            using(CrawelNovelDbContext context = new CrawelNovelDbContext())
            {
                catas = context.Catalog.ToList();
            }
            HtmlDocument doc = new HtmlDocument();
            foreach (var cata in catas)
            {

                string htmlContent = GetContent(cata.Url);
                doc.LoadHtml(htmlContent);
                HtmlNode navInfo = doc.GetElementbyId("info");
                HtmlNode navInfo1=doc.DocumentNode.SelectSingleNode(navInfo.XPath + "/p[3]");
                string LastTime = navInfo1.InnerText.Replace("&nbsp;", "").Replace("最后更新", "").Substring(1);
                DateTime LastTimeDt = DateTime.Parse(LastTime);
                if (cata.UpdateTime.HasValue)
                {
                    if (LastTimeDt < cata.UpdateTime.Value)
                    {
                        continue;
                    }
                }
                else
                {
                    if (LastTimeDt < cata.CreateTime)
                    {
                        continue;
                    }
                }

                UpdateNovel(cata, doc);
                cata.UpdateTime = DateTime.Now;
                UpdateCatalog(cata);

            }

        }


        private void UpdateNovel(Catalog log, HtmlDocument doc)
        {
            col = null;
            using (CrawelNovelDbContext context = new CrawelNovelDbContext())
            {
                string MaxUrl = context.Chapter.Where(c=>c.NoteBookId.Equals(log.Id)).Max(c => c.ChapterUrl);
                var chap = context.Chapter.FirstOrDefault(f => f.ChapterUrl.Equals(MaxUrl));
                col = doc.DocumentNode.SelectNodes("//dd");
                ProgressCount = col.Count;
                CataUrl = log.Url;
                CataId = log.Id;
                this.backgroundWorker1.RunWorkerAsync(chap);  //运行backgroundWorker组件
            }
            


            

            
            ProgressForm form = new ProgressForm(this.backgroundWorker1);  //显示进度条窗体
            form.ShowDialog(this);

        }

        private void backgroundWorker1_UpdateDoWork(object sender, DoWorkEventArgs e)
        {
            Chapter chapMax = e.Argument as Chapter;
            BackgroundWorker worker = sender as BackgroundWorker;
            List<Chapter> chaps = new List<Chapter>();
            int Count = 1;

            foreach (var node in col)
            {
                Count++;
                worker.ReportProgress(Count * 100 / col.Count);
                if (worker.CancellationPending) //获取程序是否已请求取消后台操作
                {
                    e.Cancel = true;
                    break;
                }
                Chapter chap = new Chapter();
                chap.ChapterName = node.InnerText;
                var atag = node.ChildNodes;
                chap.ChapterUrl = atag[0].Attributes["href"].Value;
                if (string.Compare(chapMax.ChapterUrl,chap.ChapterUrl,true)>0)
                {
                    continue;
                }

                if (chapMax.ChapterUrl.Equals(chap.ChapterUrl))
                {
                    continue;
                }

                chap.IsFinished = true;
                chap.NoteBookId = CataId;
                chap.CreateTime = DateTime.Now;
                
                string chapCont = GetContentWait(CataUrl+chap.ChapterUrl, Int32.Parse(txtMin.Text), Int32.Parse(txtMax.Text));
                if (chapCont.IsNotNullOrEmpty())
                {
                    HtmlDocument docNew = new HtmlDocument();
                    docNew.LoadHtml(chapCont);
                    HtmlNode navNode = docNew.GetElementbyId("content");
                    chap.ChapterContent = navNode.InnerText.Replace("&nbsp;", "").Replace("<br/>", "").Replace("\r", "").Replace("\n", "");
                    chaps.Add(chap);
                }
                else
                {
                    chap.IsFinished = false;
                    chaps.Add(chap);
                }

            }

            SaveChapData(chaps);

        }

        private void backgroundWorker1_UpdateRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message);
            }
            else if (e.Cancelled)
            {
                MessageBox.Show("取消");
            }
            else
            {
                MessageBox.Show("完成");
            }
        }
    }
}
