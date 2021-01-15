using MySql.Data.Entity;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrawelNovel.Model
{
    [DbConfigurationType(typeof(MySqlEFConfiguration))]
    public class CrawelNovelDbContext : DbContext
    {
        public CrawelNovelDbContext()
            : base("name=connStr")  //对应连接数据库字符串的名字
        {

        }
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            //此代码的功能是移除复数的约定  就是指生成的表名后面不加S

        }
        //对应的表
        public DbSet<Chapter> Chapter { get; set; }

        public DbSet<Catalog> Catalog { get; set; }
    }
}
