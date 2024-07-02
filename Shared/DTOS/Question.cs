using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace template.Shared.DTOS
{
    public class Question
    {
        public int ID { get; set; } // todo : check if it can be removed 
        public string QuestionsText { get; set; }
        public string QuestionsImage { get; set; }

        //public int GameID { get; set; }
        public List<Answer> Answers { get; set; }
    }
}
