using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrawelNovel.Model
{
    public class Catalog
    {
        [Key]
        public int Id { get; set; }

        [StringLength(255)]
        public string NoteName { get; set; }

        [StringLength(255)]
        public string Url { get; set; }

        public Byte[] Img { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime? UpdateTime { get; set; }
    }
}
