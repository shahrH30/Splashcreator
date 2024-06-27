using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace template.Shared.Models.Games
{
    public class QuestionWithAnswers
    {
        public int QuestionId { get; set; }
        public string Text { get; set; }
        public string Image { get; set; }
        public int GameId { get; set; }
        public List<AnswerUpdate> Answers { get; set; }
    }
}