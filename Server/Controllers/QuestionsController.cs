using Microsoft.AspNetCore.Mvc;
using template.Server.Data;
using template.Shared.Models.Games;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace template.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuestionsController : ControllerBase
    {
        private readonly DbRepository _db;
        private readonly ILogger<QuestionsController> _logger;

        public QuestionsController(DbRepository db, ILogger<QuestionsController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet("{gameId}")]
        public async Task<IActionResult> GetQuestions(int gameId)
        {
            string query = "SELECT * FROM Questions WHERE GameID = @GameID";
            var parameters = new { GameID = gameId };
            var questions = await _db.GetRecordsAsync<QuestionsUpdate>(query, parameters);

            if (questions == null)
            {
                _logger.LogError("Error occurred while fetching questions for GameID: {GameID}", gameId);
                return StatusCode(500, "Internal server error.");
            }

            return Ok(questions);
        }

        [HttpGet("question/{id}")]
        public async Task<IActionResult> GetQuestionWithAnswers(int id)
        {
            var questionQuery = "SELECT * FROM Questions WHERE ID = @Id";
            var answerQuery = "SELECT * FROM Answers WHERE QuestionID = @Id";

            var question = (await _db.GetRecordsAsync<QuestionsUpdate>(questionQuery, new { Id = id })).FirstOrDefault();
            if (question == null)
            {
                _logger.LogWarning("Question not found for ID: {Id}", id);
                return NotFound($"Question not found with ID: {id}");
            }

            var answers = await _db.GetRecordsAsync<AnswerUpdate>(answerQuery, new { Id = id });

            var questionWithAnswers = new QuestionWithAnswers
            {
                QuestionId = question.ID,
                Text = question.QuestionsText,
                Image = question.QuestionsImage,
                GameId = question.GameID,
                Answers = answers.ToList()
            };

            return Ok(questionWithAnswers);
        }

        [HttpPost("{gameId}")]
        public async Task<IActionResult> CreateQuestion(int gameId, [FromBody] QuestionWithAnswers questionWithAnswers)
        {
            if (questionWithAnswers == null || string.IsNullOrEmpty(questionWithAnswers.Text) || questionWithAnswers.Text.Length < 2)
            {
                _logger.LogWarning("Invalid question data received");
                return BadRequest("Valid question text is required.");
            }

            _logger.LogInformation($"Attempting to create question for game {gameId}");

            // Insert question
            string questionQuery = "INSERT INTO Questions (QuestionsText, QuestionsImage, GameID) VALUES (@QuestionsText, @QuestionsImage, @GameID)";
            var questionParameters = new { QuestionsText = questionWithAnswers.Text, QuestionsImage = questionWithAnswers.Image ?? "DefaultName", GameID = gameId };
            int rowsAffected = await _db.SaveDataAsync(questionQuery, questionParameters);

            if (rowsAffected == 0)
            {
                _logger.LogError("Failed to insert question");
                return StatusCode(500, "Failed to insert question");
            }

            // Get the last inserted question ID
            string getLastIdQuery = "SELECT last_insert_rowid()";
            var lastInsertedId = await _db.GetRecordsAsync<int>(getLastIdQuery);
            int questionId = lastInsertedId.FirstOrDefault();

            _logger.LogInformation($"Question inserted with ID: {questionId}");

            // Insert answers
            if (questionWithAnswers.Answers != null && questionWithAnswers.Answers.Any())
            {
                _logger.LogInformation($"Inserting {questionWithAnswers.Answers.Count} answers");
                string answerQuery = "INSERT INTO Answers (Content, IsPicture, IsCorrect, QuestionID) VALUES (@Content, @IsPicture, @IsCorrect, @QuestionID)";
                foreach (var answer in questionWithAnswers.Answers)
                {
                    var answerParameters = new
                    {
                        Content = answer.Content,
                        IsPicture = answer.IsPicture,
                        IsCorrect = answer.IsCorrect,
                        QuestionID = questionId
                    };
                    await _db.SaveDataAsync(answerQuery, answerParameters);
                }
                _logger.LogInformation("Answers inserted successfully");
            }

            return Ok(questionId);
        }

        [HttpPost("update/{id}")]
        public async Task<IActionResult> UpdateQuestion(int id, [FromBody] QuestionWithAnswers questionWithAnswers)
        {
            if (id != questionWithAnswers.QuestionId || questionWithAnswers == null || string.IsNullOrEmpty(questionWithAnswers.Text) || questionWithAnswers.Text.Length < 2)
            {
                return BadRequest("Invalid question data.");
            }

            // Update question
            string questionQuery = "UPDATE Questions SET QuestionsText = @QuestionsText, QuestionsImage = @QuestionsImage WHERE ID = @ID";
            var questionParameters = new { QuestionsText = questionWithAnswers.Text, QuestionsImage = questionWithAnswers.Image ?? "DefaultName", ID = id };
            int rowsAffected = await _db.SaveDataAsync(questionQuery, questionParameters);

            if (rowsAffected == 0)
            {
                _logger.LogWarning("Question not found or not updated. ID: {Id}", id);
                return NotFound($"Question with ID {id} not found or not updated.");
            }

            // Update or insert answers
            if (questionWithAnswers.Answers != null && questionWithAnswers.Answers.Any())
            {
                string updateAnswerQuery = "UPDATE Answers SET Content = @Content, IsPicture = @IsPicture, IsCorrect = @IsCorrect WHERE ID = @ID AND QuestionID = @QuestionID";
                string insertAnswerQuery = "INSERT INTO Answers (Content, IsPicture, IsCorrect, QuestionID) VALUES (@Content, @IsPicture, @IsCorrect, @QuestionID)";

                foreach (var answer in questionWithAnswers.Answers)
                {
                    var answerParameters = new
                    {
                        Content = answer.Content,
                        IsPicture = answer.IsPicture,
                        IsCorrect = answer.IsCorrect,
                        ID = answer.ID,
                        QuestionID = id
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

            return Ok("Question and answers updated successfully.");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            string query = "DELETE FROM Questions WHERE ID = @ID";
            var parameters = new { ID = id };
            int rowsAffected = await _db.SaveDataAsync(query, parameters);

            if (rowsAffected > 0)
            {
                return Ok("Question deleted successfully");
            }
            else
            {
                _logger.LogWarning("Failed to delete question. No rows affected. ID: {Id}", id);
                return NotFound($"Question with ID {id} not found or already deleted.");
            }
        }

        [HttpPost("duplicate/{id}")]
        public async Task<IActionResult> DuplicateQuestion(int id)
        {
            string query = "INSERT INTO Questions (QuestionsText, QuestionsImage, GameID) SELECT QuestionsText, QuestionsImage, GameID FROM Questions WHERE ID = @ID";
            var parameters = new { ID = id };
            int rowsAffected = await _db.SaveDataAsync(query, parameters);

            if (rowsAffected > 0)
            {
                return Ok("Question duplicated successfully");
            }
            else
            {
                _logger.LogWarning("Failed to duplicate question. No rows affected. ID: {Id}", id);
                return NotFound($"Question with ID {id} not found.");
            }
        }

        [HttpPost("answers/update")]
        public async Task<IActionResult> UpdateAnswers([FromBody] List<AnswerUpdate> answers)
        {
            if (answers == null || !answers.Any())
                return BadRequest("No answers provided");

            foreach (var answerUpdate in answers)
            {
                var query = "UPDATE Answers SET Content = @Content, IsPicture = @IsPicture, IsCorrect = @IsCorrect WHERE ID = @ID";
                var parameters = new
                {
                    Content = answerUpdate.Content,
                    IsPicture = answerUpdate.IsPicture,
                    IsCorrect = answerUpdate.IsCorrect,
                    ID = answerUpdate.ID
                };
                await _db.SaveDataAsync(query, parameters);
            }

            return Ok("Answers updated successfully");
        }
    }
}