using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace template.Shared.Models.Games
{
    public class QuestionWithAnswers
    {
        public QuestionsUpdate Question { get; set; } = new QuestionsUpdate();
        public List<AnswerUpdate> Answers { get; set; } = new List<AnswerUpdate>();
    }
}
