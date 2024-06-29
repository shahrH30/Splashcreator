using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace template.Shared.Models.Games
{
    public class QuestionsUpdate
    {
        public int ID { get; set; }
        public string QuestionsText { get; set; }
        public string QuestionsImage { get; set; } = "DefaultName";
        //public int GameID { get; set; }
    }
}
