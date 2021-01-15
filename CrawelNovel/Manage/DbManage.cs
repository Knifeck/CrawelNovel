using CrawelNovel.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrawelNovel.Manage
{
    public class DbManage
    {
        public Catalog GetCataLogByName(string NovelName)
        {
            using(CrawelNovelDbContext context = new CrawelNovelDbContext())
            {
                return context.Catalog.FirstOrDefault(c => c.NoteName.Equals(NovelName));
            }
        }

        public List<Chapter> GetChapterList(int NovelId)
        {
            using (CrawelNovelDbContext context = new CrawelNovelDbContext())
            {
                return context.Chapter.Where(c => c.NoteBookId.Equals(NovelId)).ToList();
            }
        }
    }
}
