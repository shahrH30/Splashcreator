using Microsoft.AspNetCore.Mvc;
using template.Server.Data;
using template.Server.Helpers;
using template.Shared.Models.Games;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace template.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ServiceFilter(typeof(AuthCheck))]
    public class GamesController : Controller
    {
        private readonly DbRepository _db;
        private readonly ILogger<GamesController> _logger;

        public GamesController(DbRepository db, ILogger<GamesController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetUserGames(int authUserId)
        {
            if (authUserId > 0)
            {
                object param = new { UserId = authUserId };
                string gameQuery = "SELECT ID, GameName, Code AS GameCode, IsPublish, CanPublish FROM Games WHERE UserId = @UserId";
                var gamesRecords = await _db.GetRecordsAsync<GameToTable>(gameQuery, param);
                List<GameToTable> GamesList = gamesRecords.ToList();

                foreach (var game in GamesList)
                {
                    object questionParam = new { GameId = game.ID };
                    string questionQuery = "SELECT COUNT(*) AS NumQuestion FROM Questions WHERE GameId = @GameId";
                    var questionRecords = await _db.GetRecordsAsync<int>(questionQuery, questionParam);
                    game.NumQuestion = questionRecords.FirstOrDefault();
                }

                if (GamesList.Count > 0)
                {
                    return Ok(GamesList);
                }
                else
                {
                    return BadRequest("No games for this user");
                }
            }
            else
            {
                return Unauthorized("user is not authenticated");
            }
        }

        [HttpPost("addGame")]
        public async Task<IActionResult> AddGames(int authUserId, GameToAdd gameToAdd)
        {
            if (authUserId <= 0)
            {
                return Unauthorized("user is not authenticated");
            }

            if (string.IsNullOrWhiteSpace(gameToAdd.GameName) || gameToAdd.GameName.StartsWith(" "))
            {
                return BadRequest("Game name cannot be empty or contain only whitespace");
            }

            if (gameToAdd.TimeLimitPerQues < 0)
            {
                return BadRequest("Time limit per question cannot be negative");
            }

            int newGameId = await CreateGameInDb(authUserId, gameToAdd);

            if (newGameId == 0)
            {
                return BadRequest("Game not created");
            }

            bool isCodeUpdated = await UpdateGameCode(newGameId);

            if (!isCodeUpdated)
            {
                return BadRequest("Game code not created");
            }

            GameToTable newGame = await GetGameById(newGameId);

            if (newGame != null)
            {
                _logger.LogInformation("Game created successfully with ID: {GameId}", newGameId); // הודעת לוג להצלחה
                return Ok(newGame);
            }
            else
            {
                _logger.LogError("Failed to create the new game with ID: {GameId}", newGameId); // הודעת לוג לשגיאה
                return StatusCode(500, "Failed to create the new game");
            }
        }

        private async Task<int> CreateGameInDb(int authUserId, GameToAdd gameToAdd)
        {
            object newGameParam = new
            {
                GameName = gameToAdd.GameName.Trim(),
                Code = 0,
                IsPublish = false,
                TimeLimitPerQues = gameToAdd.TimeLimitPerQues,
                UserId = authUserId,
                CanPublish = false
            };

            string insertGameQuery = "INSERT INTO Games (GameName, Code, IsPublish, TimeLimitPerQues, UserId, CanPublish) " +
                                     "VALUES (@GameName, @Code, @IsPublish, @TimeLimitPerQues, @UserId, @CanPublish)";
            return await _db.InsertReturnIdAsync(insertGameQuery, newGameParam);
        }

        private async Task<bool> UpdateGameCode(int newGameId)
        {
            int code = newGameId + 100;
            object updateParam = new
            {
                ID = newGameId,
                Code = code
            };

            string updateCodeQuery = "UPDATE Games SET Code = @Code WHERE ID=@ID";
            int rowsAffected = await _db.SaveDataAsync(updateCodeQuery, updateParam);

            if (rowsAffected > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private async Task<GameToTable> GetGameById(int gameId)
        {
            object param = new { ID = gameId };
            string gameQuery = "SELECT ID, GameName, Code, IsPublish, CanPublish, TimeLimitPerQues FROM Games WHERE ID = @ID";
            var gameRecord = await _db.GetRecordsAsync<GameToTable>(gameQuery, param);
            return gameRecord.FirstOrDefault();
        }

        [HttpPost("publishGame")]
        public async Task<IActionResult> PublishGame(int authUserId, PublishGame game)
        {
            var param = new { UserId = authUserId, gameID = game.ID };
            string checkQuery = "SELECT GameName FROM Games WHERE UserId = @UserId and ID=@gameID";
            var checkRecords = await _db.GetRecordsAsync<string>(checkQuery, param);
            string gameName = checkRecords.FirstOrDefault();

            if (gameName != null)
            {
                if (game.IsPublish)
                {
                    bool canPublish = await CanPublishFunc(game.ID);

                    if (!canPublish)
                    {
                        return BadRequest("This game cannot be published");
                    }
                }

                string updateQuery = "UPDATE Games SET IsPublish=@IsPublish WHERE ID=@ID";
                int isUpdate = await _db.SaveDataAsync(updateQuery, new { game.IsPublish, game.ID });

                if (isUpdate == 1)
                {
                    return Ok();
                }
                return BadRequest("Update Failed");
            }
            return BadRequest("It's Not Your Game");
        }

        private async Task<bool> CanPublishFunc(int gameId)
        {
            int minQuestions = 10;
            bool canPublish = false;

            var param = new { ID = gameId };
            var queryQuestionCount = "SELECT Count(ID) FROM Questions WHERE GameID = @ID";
            var recordQuestionCount = await _db.GetRecordsAsync<int>(queryQuestionCount, param);
            int numberOfQuestions = recordQuestionCount.FirstOrDefault();

            string updateQuery;

            // בדיקה אם מספר השאלות הוא לפחות 10 ואם הוא מספר זוגי
            if (numberOfQuestions >= minQuestions && numberOfQuestions % 2 == 0)
            {
                canPublish = true;
                updateQuery = "UPDATE Games SET CanPublish = true WHERE ID = @ID";
            }
            else
            {
                updateQuery = "UPDATE Games SET IsPublish = false, CanPublish = false WHERE ID = @ID";
            }

            await _db.SaveDataAsync(updateQuery, param);
            return canPublish;
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGame(int id)
        {
            var game = await _db.GetRecordsAsync<GameToTable>("SELECT * FROM Games WHERE ID = @ID", new { ID = id });
            if (game == null)
            {
                return NotFound();
            }

            var deleted = await _db.SaveDataAsync("DELETE FROM Games WHERE ID = @ID", new { ID = id });
            return deleted > 0 ? Ok() : StatusCode(500, "Failed to delete game.");
        }

        [HttpGet("details/{id}")]
        public async Task<IActionResult> GetGameDetails(int id)
        {
            var param = new { ID = id };
            string gameQuery = "SELECT ID, GameName, TimeLimitPerQues FROM Games WHERE ID = @ID";
            var gameRecord = await _db.GetRecordsAsync<GameToAdd>(gameQuery, param);
            var game = gameRecord.FirstOrDefault();

            if (game != null)
            {
                return Ok(game);
            }
            else
            {
                return NotFound("Game not found");
            }
        }

        [HttpPut("updateGame/{id}")]
        public async Task<IActionResult> UpdateGame(int id, [FromBody] GameToAdd updatedGame)
        {
            var param = new { ID = id };
            string checkQuery = "SELECT ID FROM Games WHERE ID = @ID";
            var gameExists = await _db.GetRecordsAsync<int>(checkQuery, param);

            if (gameExists.Any())
            {
                string updateQuery = "UPDATE Games SET GameName = @GameName, TimeLimitPerQues = @TimeLimitPerQues WHERE ID = @ID";
                int rowsAffected = await _db.SaveDataAsync(updateQuery, new
                {
                    updatedGame.GameName,
                    updatedGame.TimeLimitPerQues,
                    ID = id
                });

                if (rowsAffected > 0)
                {
                    var updatedGameRecord = await GetGameById(id); // קבלת המשחק המעודכן
                    return Ok(updatedGameRecord);
                }
                return BadRequest("Update Failed");
            }
            return NotFound("Game not found");
        }
    }
}
