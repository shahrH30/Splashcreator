using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using template.Server.Data;
using template.Shared.DTOS;

namespace template.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UnityController : ControllerBase
    {
        private readonly DbRepository _db;
        public UnityController(DbRepository db)
        {
            _db = db;
        }

        [HttpGet("{Code}")]
        public async Task<IActionResult> GetTasks(int Code)
        {
            // בדיקה אם הקוד קטן או שווה ל-100
            if (Code <= 100)
            {
                return BadRequest("הקוד צריך להיות גדול מ-100.");
            }

            // פרמטר לשאילתה
            var param = new { Code = Code };

            // שאילתת בדיקה אם המשחק קיים ופורסם
            string queryGameCheck = "SELECT * FROM Games WHERE Code = @Code AND IsPublish = true";
            var gameCheck = await _db.GetRecordsAsync<Game>(queryGameCheck, param);
            Game CurrentGame = gameCheck.FirstOrDefault();

            // בדיקה אם המשחק לא קיים או לא פורסם
            if (CurrentGame == null)
            {
                // שאילתת בדיקה אם המשחק קיים (ללא בדיקת פרסום)
                string queryGameCheckCode = "SELECT * FROM Games WHERE Code = @Code";
                var gameCheckCode = await _db.GetRecordsAsync<Game>(queryGameCheckCode, param);
                Game CurrentGameCheckCode = gameCheckCode.FirstOrDefault();

                // אם המשחק לא קיים כלל
                if (CurrentGameCheckCode == null)
                {
                    return BadRequest("המשחק לא קיים");
                }
                else
                {
                    // אם המשחק קיים אך לא פורסם
                    return BadRequest("המשחק לא פורסם");
                }
            }

            // פרמטר לשאילתת השאלות
            var param3 = new { GameID = CurrentGame.ID };

            // שאילתת שליפת השאלות הקשורות למשחק
            string queryQuestions = "SELECT * FROM Questions WHERE GameID = @GameID";
            var questions = await _db.GetRecordsAsync<Question>(queryQuestions, param3);

            // עבור כל שאלה, שליפת התשובות הקשורות לשאלה
            foreach (var question in questions)
            {
                var param4 = new { QuestionID = question.ID };
                string queryAnswers = "SELECT * FROM Answers WHERE QuestionID = @QuestionID";
                var answers = await _db.GetRecordsAsync<Answer>(queryAnswers, param4);
                question.Answers = answers.ToList();
            }

            // הכנסה של רשימת השאלות לתוך פרטי המשחק
            CurrentGame.Questions = questions.ToList();

            // החזרת המידע של המשחק עם השאלות והתשובות
            return Ok(CurrentGame);
        }
    }
}
