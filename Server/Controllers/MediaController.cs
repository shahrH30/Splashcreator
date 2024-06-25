using Microsoft.AspNetCore.Mvc;
using TriangleFileStorage;

namespace BlazorApp3.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class MediaController : Controller
    {
        private readonly FilesManage _filesManage;

        public MediaController(FilesManage filesManage)
        {
            _filesManage = filesManage;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromBody] string imageBase64) // פונקציה המקבלת מחרוזת של תאור התונה כדי לעדכן אותה
        {
            string fileName = await _filesManage.SaveFile(imageBase64, "png", "uploadedFiles"); // שומר את התמונה בספרייה ומחזיר את שם הקובץ
            return Ok(fileName); // מחזיר תגובה מוצלחת עם שם הקובץ שנשמר
        }


        [HttpPost("deleteImages")]
        public IActionResult DeleteImages([FromBody] List<string> images)
        {
            var countFalseTry = 0;
            var failedImages = new List<string>();
            foreach (string img in images)
            {
                string fullPath = Path.Combine("uploadedFiles", Path.GetFileName(img));
                if (!_filesManage.DeleteFile(fullPath, ""))
                {
                    countFalseTry++;
                    failedImages.Add(img);
                }
            }
            if (countFalseTry > 0)
            {
                return BadRequest($"problem with {countFalseTry} images: {string.Join(", ", failedImages)}");
            }
            return Ok("deleted");
        }

    }

}
