using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CrawelNovel
{
    public partial class ProgressForm : Form
    {
        private BackgroundWorker backgroundWorker1; //ProgressForm窗体事件(进度条窗体)

        public ProgressForm(BackgroundWorker bgWork)
        {
            InitializeComponent();
            // add my code
            this.backgroundWorker1 = bgWork;
            //绑定进度条改变事件
            this.backgroundWorker1.ProgressChanged += new ProgressChangedEventHandler(backgroundWorker1_ProgressChanged);
            //绑定后台操作完成，取消，异常时的事件
            this.backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted);
        }
        void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage > 100)
            {
                this.progressBar1.Value = 100;
                return;
            }
            this.progressBar1.Value = e.ProgressPercentage;  //获取异步任务的进度百分比
        }

        void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.Close();  //执行完之后，直接关闭页面
        }

    }
}
