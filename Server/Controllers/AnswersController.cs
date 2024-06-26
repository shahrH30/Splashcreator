using Microsoft.AspNetCore.Mvc;
using template.Server.Data;
using template.Shared.DTOS;
using template.Shared.Models.Games;

namespace template.Server.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class AnswersController : ControllerBase
    {
        private readonly DbRepository _db;
        private readonly ILogger<AnswersController> _logger;

        public AnswersController(DbRepository db, ILogger<AnswersController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet("{questionId}")]
        public async Task<IActionResult> GetAnswers(int questionId)
        {
            try
            {
                string query = "SELECT * FROM Answers WHERE QuestionID = @QuestionID";
                var parameters = new { QuestionID = questionId };
                var answers = await _db.GetRecordsAsync<AnswerUpdate>(query, parameters);
                return Ok(answers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching answers for QuestionID: {QuestionID}", questionId);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateAnswer([FromBody] AnswerUpdate answer)
        {
            if (answer == null || string.IsNullOrEmpty(answer.Content))
            {
                return BadRequest("Invalid answer data.");
            }

            try
            {
                string query = @"INSERT INTO Answers (Content, IsPicture, IsCorrect, QuestionID) 
                             VALUES (@Content, @IsPicture, @IsCorrect, @QuestionID)";
                var parameters = new
                {
                    answer.Content,
                    answer.IsPicture,
                    answer.IsCorrect,
                    answer.QuestionID
                };
                int rowsAffected = await _db.SaveDataAsync(query, parameters);

                if (rowsAffected > 0)
                {
                    return Ok();
                }
                else
                {
                    return StatusCode(500, "Failed to create answer.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating answer for QuestionID: {QuestionID}", answer.QuestionID);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAnswer(int id, [FromBody] AnswerUpdate answer)
        {
            if (id != answer.ID || answer == null)
            {
                return BadRequest("Invalid answer data.");
            }

            try
            {
                string query = @"UPDATE Answers 
                             SET Content = @Content, IsPicture = @IsPicture, IsCorrect = @IsCorrect 
                             WHERE ID = @ID";
                var parameters = new
                {
                    answer.Content,
                    answer.IsPicture,
                    answer.IsCorrect,
                    answer.ID
                };
                int rowsAffected = await _db.SaveDataAsync(query, parameters);

                if (rowsAffected > 0)
                {
                    return NoContent();
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating answer ID: {ID}", id);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAnswer(int id)
        {
            try
            {
                string query = "DELETE FROM Answers WHERE ID = @ID";
                var parameters = new { ID = id };
                int rowsAffected = await _db.SaveDataAsync(query, parameters);

                if (rowsAffected > 0)
                {
                    return NoContent();
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting answer ID: {ID}", id);
                return StatusCode(500, "Internal server error.");
            }
        }
    }
}