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
    public class QuestionAnswersController : ControllerBase
    {
        private readonly DbRepository _db;

        public QuestionAnswersController(DbRepository repository)
        {
            _db = repository;
        }

        [HttpPost("update/{questionId}")]
        public async Task<IActionResult> UpdateAnswers(int questionId, [FromBody] List<AnswerUpdate> answers)
        {
            var questionExists = await _db.GetRecordsAsync<bool>("SELECT COUNT(*) FROM Questions WHERE ID = @QuestionID", new { QuestionID = questionId });
            if (!questionExists.FirstOrDefault())
            {
                return BadRequest("Question does not exist in DB");
            }

            if (answers != null && answers.Any())
            {
                string updateAnswerQuery = "UPDATE Answers SET Content = @Content, IsPicture = @IsPicture, IsCorrect = @IsCorrect WHERE ID = @ID AND QuestionID = @QuestionID";
                string insertAnswerQuery = "INSERT INTO Answers (Content, IsPicture, IsCorrect, QuestionID) VALUES (@Content, @IsPicture, @IsCorrect, @QuestionID)";
                string deleteAnswerQuery = "DELETE FROM Answers WHERE QuestionID = @QuestionID AND ID NOT IN @AnswerIDs";

                var deleteAnswerParameters = new
                {
                    QuestionID = questionId,
                    AnswerIDs = answers.Select(a => a.ID).ToList()
                };
                await _db.SaveDataAsync(deleteAnswerQuery, deleteAnswerParameters);

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
                        await _db.SaveDataAsync(updateAnswerQuery, answerParameters);
                    }
                    else
                    {
                        await _db.SaveDataAsync(insertAnswerQuery, answerParameters);
                    }
                }
            }

            return Ok("Answers updated/added successfully.");
        }

        [HttpGet("byQuestion/{questionId}")]
        public async Task<IActionResult> GetQuestionWithAnswers(int questionId)
        {
            var questionExists = await _db.GetRecordsAsync<bool>("SELECT COUNT(*) FROM Questions WHERE ID = @QuestionID", new { QuestionID = questionId });
            if (!questionExists.FirstOrDefault())
            {
                return BadRequest("Question does not exist in DB");
            }

            var questionQuery = "SELECT * FROM Questions WHERE ID = @Id";
            var questionRecord = await _db.GetRecordsAsync<QuestionsUpdate>(questionQuery, new { Id = questionId });
            var question = questionRecord.FirstOrDefault();

            if (question == null)
            {
                return StatusCode(500, "Internal server error.");
            }

            var answerQuery = "SELECT * FROM Answers WHERE QuestionID = @Id";
            var answersRecord = await _db.GetRecordsAsync<AnswerUpdate>(answerQuery, new { Id = questionId });
            question.Answers = answersRecord.ToList();

            return Ok(question);
        }

        [HttpPost("insert/{questionId}")]
        public async Task<IActionResult> InsertAnswers(int questionId, [FromBody] List<AnswerUpdate> answers)
        {
            var questionExists = await _db.GetRecordsAsync<bool>("SELECT COUNT(*) FROM Questions WHERE ID = @QuestionID", new { QuestionID = questionId });
            if (!questionExists.FirstOrDefault())
            {
                return BadRequest("Question does not exist in DB");
            }

            if (answers != null && answers.Any())
            {
                string insertAnswerQuery = "INSERT INTO Answers (Content, IsPicture, IsCorrect, QuestionID) VALUES (@Content, @IsPicture, @IsCorrect, @QuestionID)";
                foreach (var answer in answers)
                {
                    var answerParameters = new
                    {
                        Content = answer.Content,
                        IsPicture = answer.IsPicture,
                        IsCorrect = answer.IsCorrect,
                        QuestionID = questionId
                    };
                    await _db.SaveDataAsync(insertAnswerQuery, answerParameters);
                }
            }

            return Ok("Answers added successfully.");
        }

        [HttpPost("{gameId}")]
        public async Task<IActionResult> CreateQuestion(int gameId, [FromBody] QuestionsUpdate question)
        {
            var gameExists = await _db.GetRecordsAsync<bool>("SELECT COUNT(*) FROM Games WHERE ID = @GameID", new { GameID = gameId });
            if (!gameExists.FirstOrDefault())
            {
                return BadRequest("Game does not exist in DB");
            }

            string insertQuestionQuery = "INSERT INTO Questions (GameID, QuestionsText, QuestionsImage) VALUES (@GameID, @QuestionsText, @QuestionsImage)";
            var questionParameters = new
            {
                GameID = gameId,
                QuestionsText = question.QuestionsText,
                QuestionsImage = question.QuestionsImage
            };

            var questionId = await _db.InsertReturnIdAsync(insertQuestionQuery, questionParameters);

            if (questionId > 0)
            {
                foreach (var answer in question.Answers)
                {
                    var answerParameters = new
                    {
                        Content = answer.Content,
                        IsPicture = answer.IsPicture,
                        IsCorrect = answer.IsCorrect,
                        QuestionID = questionId
                    };
                    string insertAnswerQuery = "INSERT INTO Answers (Content, IsPicture, IsCorrect, QuestionID) VALUES (@Content, @IsPicture, @IsCorrect, @QuestionID)";
                    await _db.SaveDataAsync(insertAnswerQuery, answerParameters);
                }

                return Ok(questionId);
            }

            return StatusCode(500, "Error creating question.");
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateQuestion([FromBody] QuestionsUpdate question)
        {
            var questionExists = await _db.GetRecordsAsync<bool>("SELECT COUNT(*) FROM Questions WHERE ID = @QuestionID", new { QuestionID = question.ID });
            if (!questionExists.FirstOrDefault())
            {
                return BadRequest("Question does not exist in DB");
            }

            string updateQuestionQuery = "UPDATE Questions SET QuestionsText = @QuestionsText, QuestionsImage = @QuestionsImage WHERE ID = @ID";
            var questionParameters = new
            {
                QuestionsText = question.QuestionsText,
                QuestionsImage = question.QuestionsImage,
                ID = question.ID
            };

            await _db.SaveDataAsync(updateQuestionQuery, questionParameters);

            return Ok("Question updated successfully.");
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetQuestion(int id)
        {
            var questionQuery = "SELECT * FROM Questions WHERE ID = @Id";
            var questionRecord = await _db.GetRecordsAsync<QuestionsUpdate>(questionQuery, new { Id = id });
            var question = questionRecord.FirstOrDefault();

            if (question == null)
            {
                return StatusCode(500, "Internal server error.");
            }

            return Ok(question);
        }

        [HttpGet("byGame/{gameId}")]
        public async Task<IActionResult> GetQuestionsByGame(int gameId)
        {
            var questionQuery = "SELECT * FROM Questions WHERE GameID = @GameID";
            var questionRecords = await _db.GetRecordsAsync<QuestionsUpdate>(questionQuery, new { GameID = gameId });

            if (questionRecords == null)
            {
                return StatusCode(500, "Internal server error.");
            }

            return Ok(questionRecords);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var questionExists = await _db.GetRecordsAsync<bool>("SELECT COUNT(*) FROM Questions WHERE ID = @ID", new { ID = id });
            if (!questionExists.FirstOrDefault())
            {
                return BadRequest("Question does not exist in DB");
            }

            string deleteAnswersQuery = "DELETE FROM Answers WHERE QuestionID = @QuestionID";
            string deleteQuestionQuery = "DELETE FROM Questions WHERE ID = @ID";

            await _db.SaveDataAsync(deleteAnswersQuery, new { QuestionID = id });
            await _db.SaveDataAsync(deleteQuestionQuery, new { ID = id });

            return Ok("Question and its answers deleted successfully.");
        }
    }
}
