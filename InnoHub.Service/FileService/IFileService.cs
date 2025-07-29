using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Service.FileService
{
    public interface IFileService
    {
        string GetAbsolutePath(string relativePath);
        void DeleteFile(string relativePath);
        string EnsureDirectory(string path);
        Task<List<string>> SaveFilesAsync(IEnumerable<IFormFile> files, string folderPath);
        Task<string> SaveFileAsync(IFormFile file, string folderPath);
    }
}
