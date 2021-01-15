using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrawelNovel.Model
{
    public class Chapter
    {
        [Key]
        public int Id { get; set; }

        public int NoteBookId { get; set; }

        [StringLength(255)]
        public string ChapterName { get; set; }
        [StringLength(255)]
        public string ChapterUrl { get; set; }

        public string ChapterContent { get; set; }

        public DateTime CreateTime { get; set; }


        public bool IsFinished { get; set; }

    }
}
