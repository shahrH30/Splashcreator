﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace template.Shared.Models.Games
{
    public class AnswerUpdate
    {
        public int ID { get; set; } = 0;
        public string Content { get; set; }
        public bool IsPicture { get; set; }
        public bool IsCorrect { get; set; }
    }
}
