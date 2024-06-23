using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace template.Shared.Models.Games
{
    public class GameToTable
    {
        public int ID { get; set; }
        public string GameName { get; set; }
        public int GameCode { get; set; }
        public bool IsPublish { get; set; }
        public bool CanPublish { get; set; }
        public int NumQuestion { get; set; }

    }
}
