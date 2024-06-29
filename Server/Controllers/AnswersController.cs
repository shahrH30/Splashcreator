using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using template.Server.Data;
using template.Shared.Models.Games;

namespace template.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnswersController : ControllerBase
    {
        private readonly DbRepository _db;

        public AnswersController(DbRepository repository)
        {
            _db = repository;
        }

        [HttpPost("update/{questionId}")] // OK
        public async Task<IActionResult> UpdateAnswers(int questionId, [FromBody] List<AnswerUpdate> answers)
        {
            var questionExists = await _db.GetRecordsAsync<bool>("SELECT COUNT(*) FROM Questions WHERE ID = @QuestionID", new { QuestionID = questionId });
            if (!questionExists.FirstOrDefault())
            {
                return BadRequest("Question does not exists in DB");
            } 

            if (answers != null && answers.Any())
            {
                string updateAnswerQuery = "UPDATE Answers SET Content = @Content, IsPicture = @IsPicture, IsCorrect = @IsCorrect WHERE ID = @ID AND QuestionID = @QuestionID";
                string insertAnswerQuery = "INSERT INTO Answers (Content, IsPicture, IsCorrect, QuestionID) VALUES (@Content, @IsPicture, @IsCorrect, @QuestionID)";

                foreach (var answer in answers)
                {
                    var answerParameters = new
                    {
                        Content = answer.Content,
                        IsPicture = answer.IsPicture,
                        IsCorrect = answer.IsCorrect,
                        ID = answer.ID,
                        QuestionID = questionId
                    };

                    if (answer.ID > 0)
                    {
                        // Update existing answer
                        await _db.SaveDataAsync(updateAnswerQuery, answerParameters);
                    }
                    else
                    {
                        // Insert new answer
                        await _db.SaveDataAsync(insertAnswerQuery, answerParameters);
                    }
                }
            }

            return Ok("Answers updated/added successfully.");
        }

        [HttpGet("byQuestion/{questionId}")] // OK 
        public async Task<IActionResult> GetQuestionWithAnswers(int questionId)
        {
            var questionExists = await _db.GetRecordsAsync<bool>("SELECT COUNT(*) FROM Questions WHERE ID = @QuestionID", new { QuestionID = questionId });
            if (!questionExists.FirstOrDefault())
            {
                return BadRequest("Question does not exists in DB");
            }
            
            var answerQuery = "SELECT * FROM Answers WHERE QuestionID = @Id";
            var answersRecoord = await _db.GetRecordsAsync<AnswerUpdate>(answerQuery, new { Id = questionId });
            List<AnswerUpdate> answers = answersRecoord.ToList();

            if (answers == null)
            {
                return StatusCode(500, "Internal server error.");
            }

            return Ok(answers);
        }
    }
}