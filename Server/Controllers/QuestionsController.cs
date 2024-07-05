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

        private async Task<bool> CheckIfQuestionExists(int id)
        {
            var param = new { ID = id };
            string checkQuery = "SELECT ID FROM Questions WHERE ID = @ID";
            var gameExists = await _db.GetRecordsAsync<int>(checkQuery, param);
            return gameExists.Any();
        } 
        
        private async Task<bool> CheckIfAnswerExists(int id)
        {
            var param = new { ID = id };
            string checkQuery = "SELECT ID FROM Answers WHERE ID = @ID";
            var gameExists = await _db.GetRecordsAsync<int>(checkQuery, param);
            return gameExists.Any();
        }

        private async Task<List<Answer>> GetAnswersByQuestionId(int qID)
        {
            var answerQuery = "SELECT * FROM Answers WHERE QuestionID = @Id";
            var answersRecord = await _db.GetRecordsAsync<Answer>(answerQuery, new { Id = qID });
            return answersRecord.ToList();
        }

        private async void UpdateAnswers(int questionId, List<Answer> answers)
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

        private async void InsertAnswers(int questionId, List<Answer> answers)
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


        [HttpGet("byGame/{gameId}")]//OK
        public async Task<IActionResult> GetQuestions(int gameId)
        {
            var parameters = new { GameID = gameId };
            string query = "SELECT ID, QuestionsText, QuestionsImage FROM Questions WHERE GameID = @GameID";
            var questions = await _db.GetRecordsAsync<QuestionDetailed>(query, parameters);

            if (questions == null)
            {
                _logger.LogError("Error occurred while fetching questions for GameID: {GameID}", gameId);
                return StatusCode(500, "Internal server error.");
            }

            foreach (var question in questions)
            {
                question.Answers = await GetAnswersByQuestionId(question.ID);
            }

            return Ok(questions);
        }

        [HttpGet("{id}")] // OK
        public async Task<IActionResult> GetQuestionWithAnswers(int id)
        {
            var questionExists = await _db.GetRecordsAsync<bool>("SELECT COUNT(*) FROM Questions WHERE ID = @QuestionID", new { QuestionID = id });
            if (!questionExists.FirstOrDefault())
            {
                return BadRequest("Question does not exist in DB");
            }

            var questionQuery = "SELECT QuestionsText, QuestionsImage FROM Questions WHERE ID = @Id";
            var questionRecord = await _db.GetRecordsAsync<QuestionDetailed>(questionQuery, new { Id = id });
            var question = questionRecord.FirstOrDefault();

            if (question == null)
            {
                return StatusCode(500, "Internal server error.");
            }

            question.Answers = await GetAnswersByQuestionId(id);

            return Ok(question);
        }

        [HttpPost("{gameId}")] // OK
        public async Task<IActionResult> CreateQuestion(int gameId, [FromBody] QuestionDetailed newQuestion)
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

            // Insert answers
            if (newQuestion.Answers != null && newQuestion.Answers.Any())
            {
                InsertAnswers(questionId, newQuestion.Answers);
            }

            return Ok(questionId);
        }

        [HttpPost("update")] // OK
        public async Task<IActionResult> UpdateQuestion([FromBody] QuestionDetailed newQuestion)
        {
            if (newQuestion == null || string.IsNullOrEmpty(newQuestion.QuestionsText))
            {
                return BadRequest("Invalid question data.");
            }

            var id = newQuestion.ID;
            var QuestionExists = await CheckIfQuestionExists(id);

            if (!QuestionExists)
            {
                return NotFound("Question not found");
            }

            // Update question
            var questionParameters = new { QuestionsText = newQuestion.QuestionsText, QuestionsImage = newQuestion.QuestionsImage ?? "DefaultName", ID = id };
            string questionQuery = "UPDATE Questions SET QuestionsText = @QuestionsText, QuestionsImage = @QuestionsImage WHERE ID = @ID";
            int rowsAffected = await _db.SaveDataAsync(questionQuery, questionParameters);

            if (rowsAffected == 0)
            {
                _logger.LogWarning("Question not found or not updated. ID: {Id}", id);
                return NotFound($"Question with ID {id} not found or not updated.");
            }

            // Updats answers
            if (newQuestion.Answers != null && newQuestion.Answers.Any())
            {
                UpdateAnswers(id, newQuestion.Answers);
            }

            return Ok("Question updated successfully.");
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

        [HttpPost("deleteAnswers")] // OK
        public async Task<IActionResult> DeleteAnswer([FromBody] List<int> answersIds)
        {
            foreach (var id in answersIds)
            {
                string query = "DELETE FROM Answers WHERE ID = @ID";
                var parameters = new { ID = id };
                int rowsAffected = await _db.SaveDataAsync(query, parameters);

                if (rowsAffected > 0)
                {
                    continue;
                }
                else
                {
                    _logger.LogWarning("Failed to delete answer. No rows affected. ID: {Id}", id);
                    return NotFound($"Answer with ID {id} not found or already deleted.");
                }
            }

            return Ok("Answer deleted successfully");
        }

        [HttpPost ("updateImage/{id}")]
        public async Task<IActionResult> UpdateQuestionImage(int id, [FromBody] string imageName)
        {
            var QuestionExists = await CheckIfQuestionExists(id);

            if (!QuestionExists)
            {
                return NotFound("Question not found");
            }

            var param = new { 

                ID = id,
                QuestionsImage = imageName
            };
            string questionQuery = "UPDATE Questions SET QuestionsImage = @QuestionsImage WHERE ID = @ID";
            int rowsAffected = await _db.SaveDataAsync(questionQuery, param);

            if (rowsAffected == 0)
            {
                _logger.LogWarning("Question not found or not updated. ID: {Id}", id);
                return NotFound($"Question with ID {id} not found or not updated.");
            }

            return Ok("Question image updated successfully.");

        }


        [HttpPost("answer/updateImages")]
        public async Task<IActionResult> updateAnswerImage([FromBody] List<AnswerImage> answerImages)
        {
            foreach (var answerImage in answerImages) {
            
                var AnswerExists = await CheckIfAnswerExists(answerImage.ID);

                if (!AnswerExists)
                {
                    return NotFound("Answer not found");
                }

                var param = new
                {

                    ID = answerImage.ID,
                    Content = answerImage.Content
                };

                string Query = "UPDATE Answers SET Content = @Content WHERE ID = @ID";
                int rowsAffected = await _db.SaveDataAsync(Query, param);

                if (rowsAffected == 0)
                {
                    _logger.LogWarning("Answer not found or not updated. ID: {Id}", param.ID);
                    return NotFound($"Answer with ID {param.ID} not found or not updated.");
                }             
            }

            return Ok("Answer image updated successfully.");
        }
    }
}