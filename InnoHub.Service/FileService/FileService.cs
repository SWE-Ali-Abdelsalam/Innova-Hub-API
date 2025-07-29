using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Service.FileService
{
    public class FileService : IFileService
    {
        public string GetAbsolutePath(string relativePath) =>
       Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath.TrimStart('/'));
        // ✅ Get Product by ID (With Full Details)
        public void DeleteFile(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return;

            var fullPath = Path.Combine("wwwroot", relativePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
        }
        public string EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }


        public async Task<List<string>> SaveFilesAsync(IEnumerable<IFormFile> files, string folderPath)
        {
            var filePaths = new List<string>();
            foreach (var file in files)
            {
                var path = await SaveFileAsync(file, folderPath);
                filePaths.Add(path);
            }
            return filePaths;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string folderPath)
        {
            if (file == null || file.Length == 0) return null;

            // ✅ التأكد من أن المجلد موجود
            EnsureDirectory(folderPath);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(folderPath, fileName);

            await using var fileStream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(fileStream);

            // ✅ إرجاع المسار الصحيح بالنسبة لـ `wwwroot`
            return $"/{folderPath.Replace("wwwroot", "").TrimStart('/')}/{fileName}".Replace("\\", "/");
        }



    }
}
