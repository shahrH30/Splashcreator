using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace template.Shared.Models.Games
{
    public class GameToAdd
    {

        [Required(ErrorMessage = "שדה חובה")]
        [MinLength(2, ErrorMessage = "יש להזין לפחות שני תווים")]
        [MaxLength(8, ErrorMessage = "לא ניתן להזין יותר מ8 תוים")]
        public string GameName { get; set; }

        [Required(ErrorMessage = "שדה חובה")]
        //[Range(0, int.MaxValue, ErrorMessage = "הזמן לשאלה חייב להיות מספר חיובי")]
        public int TimeLimitPerQues { get; set; }
    }
}
