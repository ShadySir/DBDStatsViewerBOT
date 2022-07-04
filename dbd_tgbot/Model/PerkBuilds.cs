using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dbd_tgbot.Model
{
    public class PerkBuilds { 
        public List<PerkBuild> perkBuilds { get; set; }
        public long ChatID { get; set; }

    }
    public class PerkBuild
    {
        public string BuildName { get; set; }
        public List<Perk> Perks { get; set; }
        public string CharacterName { get; set; }
        public override string ToString()
        {
            string ans = $"{BuildName}, build for {CharacterName}\n";

            if (Perks.Count == 0)
            {
                ans += "\nNothing yet";
                return ans;
            }
            for (int i = 0; i < Perks.Count; i++)
            {
                ans += $"\n{i + 1}.{Perks[i].perk_name}";
            }

            return ans;
        }
    }
}
