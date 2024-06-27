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
        private readonly DbRepository _repository;

        public AnswersController(DbRepository repository)
        {
            _repository = repository;
        }

        [HttpPost("update")]
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
                await _repository.SaveDataAsync(query, parameters);
            }

            return Ok("Answers updated successfully");
        }

        [HttpGet("question/{id}")]
        public async Task<IActionResult> GetQuestionWithAnswers(int id)
        {
            var questionQuery = "SELECT ID, QuestionsText AS Text, QuestionsImage AS Image FROM Questions WHERE ID = @Id";
            var answerQuery = "SELECT ID, Content, IsPicture, IsCorrect FROM Answers WHERE QuestionID = @Id";

            var question = (await _repository.GetRecordsAsync<QuestionsUpdate>(questionQuery, new { Id = id })).FirstOrDefault();
            if (question == null)
                return NotFound("Question not found");

            var answers = await _repository.GetRecordsAsync<AnswerUpdate>(answerQuery, new { Id = id });

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
    }
}