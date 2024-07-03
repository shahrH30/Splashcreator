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

        [HttpGet("byGame/{gameId}")]//OK
        public async Task<IActionResult> GetQuestions(int gameId)
        {
            var parameters = new { GameID = gameId };
            string query = "SELECT ID,QuestionsText,QuestionsImage FROM Questions WHERE GameID = @GameID";
            var questions = await _db.GetRecordsAsync<QuestionsUpdate>(query, parameters);

            if (questions == null)
            {
                _logger.LogError("Error occurred while fetching questions for GameID: {GameID}", gameId);
                return StatusCode(500, "Internal server error.");
            }

            return Ok(questions);
        }

        [HttpGet("{id}")] // OK
        public async Task<IActionResult> GetQuestionWithAnswers(int id)
        {
            var query = "SELECT QuestionsText, QuestionsImage FROM Questions WHERE ID = @Id";
            var question = (await _db.GetRecordsAsync<Question>(query, new { Id = id })).FirstOrDefault();
            if (question == null)
            {
                _logger.LogWarning("Question not found for ID: {Id}", id);
                return NotFound($"Question not found with ID: {id}");
            }

            return Ok(question);
        }

        [HttpPost("{gameId}")] // OK
        public async Task<IActionResult> CreateQuestion(int gameId, [FromBody] QuestionsUpdate newQuestion)
        {
            if (newQuestion == null || string.IsNullOrEmpty(newQuestion.QuestionsText))
            {
                _logger.LogWarning("Invalid question data received");
                return BadRequest("Valid question text is required.");
            }

            _logger.LogInformation($"Attempting to create question for game {gameId}");

            // Insert question
            var questionParameters = new
            {
                QuestionsText = newQuestion.QuestionsText,
                QuestionsImage = newQuestion.QuestionsImage ?? "DefaultName",
                GameID = gameId
            };
            string questionQuery = "INSERT INTO Questions (QuestionsText, QuestionsImage, GameID) VALUES (@QuestionsText, @QuestionsImage, @GameID)";
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

            return Ok(questionId);
        }

        [HttpPost("update")] // OK
        public async Task<IActionResult> UpdateQuestion([FromBody] QuestionsUpdate newQuestion)
        {
            if (newQuestion == null || string.IsNullOrEmpty(newQuestion.QuestionsText))
            {
                return BadRequest("Invalid question data.");
            }

            var id = newQuestion.ID;
            // check if question exist in DB
            var param = new { ID = id };
            string checkQuery = "SELECT ID FROM Questions WHERE ID = @ID";
            var gameExists = await _db.GetRecordsAsync<int>(checkQuery, param);

            if (gameExists.Any())
            {
                // Update question
                string questionQuery = "UPDATE Questions SET QuestionsText = @QuestionsText, QuestionsImage = @QuestionsImage WHERE ID = @ID";
                var questionParameters = new { QuestionsText = newQuestion.QuestionsText, QuestionsImage = newQuestion.QuestionsImage ?? "DefaultName", ID = id };
                int rowsAffected = await _db.SaveDataAsync(questionQuery, questionParameters);

                if (rowsAffected == 0)
                {
                    _logger.LogWarning("Question not found or not updated. ID: {Id}", id);
                    return NotFound($"Question with ID {id} not found or not updated.");
                }

                return Ok("Question updated successfully.");
            }
            return NotFound("Game not found");
        }

        [HttpDelete("{id}")] // OK
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

        //[HttpPost("duplicate/{id}")]
        //public async Task<IActionResult> DuplicateQuestion(int id)
        //{
        //    string query = "INSERT INTO Questions (QuestionsText, QuestionsImage, GameID) SELECT QuestionsText, QuestionsImage, GameID FROM Questions WHERE ID = @ID";
        //    var parameters = new { ID = id };
        //    int rowsAffected = await _db.SaveDataAsync(query, parameters);

        //    if (rowsAffected > 0)
        //    {
        //        return Ok("Question duplicated successfully");
        //    }
        //    else
        //    {
        //        _logger.LogWarning("Failed to duplicate question. No rows affected. ID: {Id}", id);
        //        return NotFound($"Question with ID {id} not found.");
        //    }
        //}

        //[HttpPost("answers/update")]
        //public async Task<IActionResult> UpdateAnswers([FromBody] List<AnswerUpdate> answers)
        //{
        //    if (answers == null || !answers.Any())
        //        return BadRequest("No answers provided");

        //    foreach (var answerUpdate in answers)
        //    {
        //        var query = "UPDATE Answers SET Content = @Content, IsPicture = @IsPicture, IsCorrect = @IsCorrect WHERE ID = @ID";
        //        var parameters = new
        //        {
        //            Content = answerUpdate.Content,
        //            IsPicture = answerUpdate.IsPicture,
        //            IsCorrect = answerUpdate.IsCorrect,
        //            ID = answerUpdate.ID
        //        };
        //        await _db.SaveDataAsync(query, parameters);
        //    }

        //    return Ok("Answers updated successfully");
        //}
    }
}