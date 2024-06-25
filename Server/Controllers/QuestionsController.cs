using Microsoft.AspNetCore.Mvc;
using template.Server.Data;
using template.Shared.Models.Games;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;

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
            try
            {
                string query = "SELECT * FROM Questions WHERE GameID = @GameID";
                var parameters = new { GameID = gameId };
                var questions = await _db.GetRecordsAsync<QuestionsUpdate>(query, parameters);
                return Ok(questions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching questions for GameID: {GameID}", gameId);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpPost("{gameId}")]
        public async Task<IActionResult> CreateQuestion(int gameId, [FromBody] QuestionsUpdate question)
        {
            if (question == null || string.IsNullOrEmpty(question.QuestionsText) || question.QuestionsText.Length < 2)
            {
                _logger.LogError("Valid question text is required.");
                return BadRequest("Valid question text is required.");
            }

            // אם המשתמש לא הזין כתובת תמונה, נשתמש ב-"DefaultName"
            if (string.IsNullOrEmpty(question.QuestionsImage))
            {
                question.QuestionsImage = "DefaultName";
            }

            try
            {
                string query = "INSERT INTO Questions (QuestionsText, QuestionsImage, GameID) VALUES (@QuestionsText, @QuestionsImage, @GameID)";
                var parameters = new { QuestionsText = question.QuestionsText, QuestionsImage = question.QuestionsImage, GameID = gameId };
                _logger.LogInformation("Executing query: {Query} with parameters: {Parameters}", query, parameters);
                int rowsAffected = await _db.SaveDataAsync(query, parameters);

                if (rowsAffected > 0)
                {
                    return Ok();
                }
                else
                {
                    _logger.LogError("Failed to create question. No rows affected.");
                    return StatusCode(500, "Failed to create question. No rows affected.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while creating question. Game ID: {GameId}, Question Text: {QuestionText}, Question Image: {QuestionImage}", gameId, question.QuestionsText, question.QuestionsImage);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateQuestion(int id, [FromBody] QuestionsUpdate question)
        {
            if (id != question.ID || question == null || string.IsNullOrEmpty(question.QuestionsText) || question.QuestionsText.Length < 2)
            {
                _logger.LogError("Invalid question data.");
                return BadRequest("Invalid question data.");
            }

            // אם המשתמש לא הזין כתובת תמונה, נשתמש ב-"DefaultName"
            if (string.IsNullOrEmpty(question.QuestionsImage))
            {
                question.QuestionsImage = "DefaultName";
            }

            try
            {
                string query = "UPDATE Questions SET QuestionsText = @QuestionsText, QuestionsImage = @QuestionsImage WHERE ID = @ID";
                var parameters = new { QuestionsText = question.QuestionsText, QuestionsImage = question.QuestionsImage, ID = id };
                _logger.LogInformation("Executing query: {Query} with parameters: {Parameters}", query, parameters);
                int rowsAffected = await _db.SaveDataAsync(query, parameters);

                if (rowsAffected > 0)
                {
                    return NoContent();
                }
                else
                {
                    _logger.LogError("Failed to update question. No rows affected.");
                    return StatusCode(500, "Failed to update question. No rows affected.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while updating question. ID: {Id}, Question Text: {QuestionText}, Question Image: {QuestionImage}", id, question.QuestionsText, question.QuestionsImage);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            try
            {
                string query = "DELETE FROM Questions WHERE ID = @ID";
                var parameters = new { ID = id };
                int rowsAffected = await _db.SaveDataAsync(query, parameters);

                if (rowsAffected > 0)
                {
                    return Ok();
                }
                else
                {
                    _logger.LogError("Failed to delete question. No rows affected.");
                    return StatusCode(500, "Failed to delete question. No rows affected.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while deleting question. ID: {Id}", id);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpPost("duplicate/{id}")]
        public async Task<IActionResult> DuplicateQuestion(int id)
        {
            try
            {
                string query = "INSERT INTO Questions (QuestionsText, QuestionsImage, GameID) SELECT QuestionsText, QuestionsImage, GameID FROM Questions WHERE ID = @ID";
                var parameters = new { ID = id };
                int rowsAffected = await _db.SaveDataAsync(query, parameters);

                if (rowsAffected > 0)
                {
                    return Ok();
                }
                else
                {
                    _logger.LogError("Failed to duplicate question. No rows affected.");
                    return StatusCode(500, "Failed to duplicate question. No rows affected.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while duplicating question. ID: {Id}", id);
                return StatusCode(500, "Internal server error.");
            }
        }
    }
}
