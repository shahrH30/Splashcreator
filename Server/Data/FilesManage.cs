using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace TriangleFileStorage
{
    public class FilesManage
    {
        private readonly IWebHostEnvironment _env;

        public FilesManage(IWebHostEnvironment env)
        {
            _env = env;
        }

        public bool DeleteFile(string fileName, string containerName)
        {
            try
            {
                string folderPath = Path.Combine(_env.WebRootPath, containerName);
                string savingPath = Path.Combine(folderPath, fileName);

                if (File.Exists(savingPath))
                {
                    File.Delete(savingPath);
                    return true;
                }
                else
                {
                    Console.WriteLine($"File not found: {savingPath}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting file: {ex.Message}");
                return false;
            }
        }

        public async Task<string> SaveFile(string imageBase64, string extension, string containerName)
        {
            byte[] picture = Convert.FromBase64String(imageBase64);
            using (Image image = Image.Load(picture))
            {

                image.Mutate(x => x
                     .Resize(new ResizeOptions
                     {
                         Mode = ResizeMode.Max,
                         Size = new Size(600, 600)
                     }));

                var fileName = $"{Guid.NewGuid()}.{extension}";
                string folderPath = Path.Combine(_env.WebRootPath, containerName);


                string savingPath = Path.Combine(folderPath, fileName);

                await image.SaveAsync(savingPath); // Automatic encoder selected based on extension.

                return fileName;
            }
        }

        public bool FileExists(string fileName)
        {
            string filePath = Path.Combine(_env.WebRootPath, fileName);
            return File.Exists(filePath);
        }

        public async Task<string> ReadFileAsBase64(string fileName)
        {
            string filePath = Path.Combine(_env.WebRootPath, fileName);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("File not found", fileName);
            }

            byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
            return Convert.ToBase64String(fileBytes);
        }
    }
}

