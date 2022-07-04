using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dbd_tgbot.Model
{
    public class Perk
    {
        public string _id { get; set; }
        public string role { get; set; }
        public string name { get; set; }
        public string name_tag { get; set; }
        public string perk_name { get; set; }
        public string perk_tag { get; set; }
        public string description { get; set; }
        public int teach_level { get; set; }
        public bool is_ptb { get; set; }
        public string lang { get; set; }
        public string icon { get; set; }
    }
}
