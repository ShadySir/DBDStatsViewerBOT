using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dbd_tgbot.Model
{
    public class Killer
    {
        public string _id { get; set; }
        public string name { get; set; }
        public string name_tag { get; set; }
        public string full_name { get; set; }
        public string alias { get; set; }
        public string gender { get; set; }
        public string nationality { get; set; }
        public string realm { get; set; }
        public string power { get; set; }
        public string weapon { get; set; }
        public string speed { get; set; }
        public string terror_radius { get; set; }
        public string height { get; set; }
        public string voice_actor { get; set; }
        public string difficulty { get; set; }
        public string overview { get; set; }
        public string lore { get; set; }
        public string dlc { get; set; }
        public string dlc_id { get; set; }
        public bool is_free { get; set; }
        public bool is_ptb { get; set; }
        public string lang { get; set; }
        public string[] perks { get; set; }
    }
}
