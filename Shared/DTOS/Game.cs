using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace template.Shared.DTOS
{
    public class Game
    {
        public int ID { get; set; }
        public string GameName { get; set; }

        public int TimeLimitPerQues { get; set; }

        public List<Question> Questions { get; set; }
    }
}
