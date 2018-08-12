﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.IO
{
    public enum SearchControl
    {
        /// <summary>
        /// If this is a directory, we won't search its subdirectories
        /// </summary>
        None,

        /// <summary>
        /// If this is a directory, we will visit all its subdirectories
        /// </summary>
        Visit,

        /// <summary>
        /// Stop the search.
        /// </summary>
        Abort,
    }

    public delegate SearchControl SearchCallback(string path);
    
    public interface IFileSystem
    {
        /// <summary>
        /// Count files in given list and in directories in given list.
        /// Directories will be searched recursively if <paramref name="isRecursive"/> is true.
        /// </summary>
        /// <param name="path">List of file/directory paths</param>
        /// <param name="isRecursive">Should we recursively count files in subdirectories?</param>
        /// <returns>Number of files in the directory</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> contains invalid characters</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid</exception>
        /// <exception cref="IOException"><paramref name="path"/> is a file name</exception>
        /// <exception cref="PathTooLongException"><paramref name="path"/> is too long</exception>
        /// <exception cref="SecurityException">Caller does not have required permission</exception>
        /// <exception cref="UnauthorizedAccessException">Caller does not have required permission</exception>
        long CountFiles(IEnumerable<string> path, bool isRecursive);

        /// <summary>
        /// Copy file from <paramref name="sourcePath"/> to <paramref name="destPath"/>.
        /// </summary>
        /// <inheritdoc cref="File.Copy(string, string)"/>
        void CopyFile(string sourcePath, string destPath);

        /// <summary>
        /// Move file from <paramref name="sourcePath"/> to <paramref name="destPath"/>.
        /// </summary>
        /// <inheritdoc cref="File.Move(string, string)"/>
        void MoveFile(string sourcePath, string destPath);

        /// <summary>
        /// Delete file <paramref name="path"/>
        /// </summary>
        /// <param name="path"></param>
        /// <inheritdoc cref="File.Delete(string)"/>
        void DeleteFile(string path);

        /// <summary>
        /// Replace file at <paramref name="destPath"/> with <paramref name="sourcePath"/> using <paramref name="backupFile"/> as a backup file
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destPath"></param>
        /// <param name="backupFile"></param>
        /// <inheritdoc cref="File.Replace(string,string,string)"/>
        void ReplaceFile(string sourcePath, string destPath, string backupFile);

        /// <summary>
        /// Move a directory to <paramref name="destPath"/>
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destPath"></param>
        /// <inheritdoc cref="Directory.Move(string, string)"/>
        void MoveDirectory(string sourcePath, string destPath);

        /// <summary>
        /// Create a new directory
        /// </summary>
        /// <inheritdoc cref="Directory.CreateDirectory(string)"/>
        void CreateDirectory(string path);

        /// <summary>
        /// Delete a directory
        /// </summary>
        /// <param name="path">Path to a directory</param>
        /// <param name="isRecursive"></param>
        /// <inheritdoc cref="Directory.Delete(string, bool)"/>
        void DeleteDirectory(string path, bool isRecursive);

        /// <summary>
        /// Get file/folder attributes
        /// </summary>
        /// <param name="path">Path to a file or a folder</param>
        /// <returns>Attributes of given file</returns>
        /// <inheritdoc cref="File.GetAttributes(string)"/>
        FileAttributes GetAttributes(string path);

        /// <summary>
        /// Get files in given directory
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <inheritdoc cref="Directory.EnumerateFiles(string)"/>
        IEnumerable<string> EnumerateFiles(string path);

        /// <summary>
        /// Get files in given directory
        /// </summary>
        /// <param name="path"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        /// <inheritdoc cref="Directory.EnumerateFiles(string, string)"/>
        IEnumerable<string> EnumerateFiles(string path, string pattern);

        /// <summary>
        /// Get files in given directory
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <inheritdoc cref="Directory.EnumerateDirectories(string)"/>
        IEnumerable<string> EnumerateDirectories(string path);

        /// <summary>
        /// Get files in given directory
        /// </summary>
        /// <param name="path"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        /// <inheritdoc cref="Directory.EnumerateDirectories(string, string)"/>
        IEnumerable<string> EnumerateDirectories(string path, string pattern);

        /// <summary>
        /// Check whether there is a directory at <paramref name="path"/>
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <inheritdoc cref="Directory.Exists(string)"/>
        bool DirectoryExists(string path);

        /// <summary>
        /// Read the whole file into memory as a byte array
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <inheritdoc cref="File.ReadAllBytes"/>
        byte[] ReadAllBytes(string path);

        /// <summary>
        /// Read the whole file into memory as a string
        /// </summary>
        /// <param name="path">Path to a file</param>
        /// <returns></returns>
        /// <inheritdoc cref="File.ReadAllText(string)"/>
        string ReadAllText(string path);

        /// <summary>
        /// Read the whole file into main memory
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        Task<byte[]> ReadAllBytesAsync(string path);

        /// <summary>
        /// Read the whole file asynchronously
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        Task<string> ReadToEndAsync(string path);

        /// <summary>
        /// Search given path for files and subdirectories.
        /// <paramref name="directoryCallback"/> will be called for each directory,
        /// <paramref name="fileCallback"/> will be called for each file.
        /// If <paramref name="directoryCallback"/> returns SearchControl.Visit, the algorithm will search all its subdirectories.
        /// If <paramref name="directoryCallback"/> does not return SearchControl.Visit, its subdirectories and files won't be searched.
        /// If <paramref name="path"/> is a file, <paramref name="fileCallback"/> will be called for it.
        /// </summary>
        /// <param name="path">Path to a file or directory</param>
        /// <param name="directoryCallback">Function called for each directory</param>
        /// <param name="fileCallback">Function called for each file</param>
        void Search(string path, SearchCallback directoryCallback, SearchCallback fileCallback);
    }

    [Export(typeof(IFileSystem))]
    public class FileSystem : IFileSystem
    {
        public long CountFiles(IEnumerable<string> files, bool isRecursive)
        {
            long counter = 0;
            foreach (var file in files)
            {
                Search(file,
                    dirPath => isRecursive ? SearchControl.Visit : SearchControl.None,
                    filePath =>
                    {
                        ++counter;
                        return SearchControl.None;
                    });
            }
            return counter;
        }

        public void CopyFile(string sourcePath, string destPath)
        {
            File.Copy(sourcePath, destPath);
        }

        public void MoveFile(string sourcePath, string destPath)
        {
            File.Move(sourcePath, destPath);
        }

        public void DeleteFile(string path)
        {
            File.Delete(path);
        }

        public void ReplaceFile(string sourcePath, string destPath, string backupFile)
        {
            File.Replace(sourcePath, destPath, backupFile);
        }

        public void MoveDirectory(string sourcePath, string destPath)
        {
            Directory.Move(sourcePath, destPath);
        }

        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        public void DeleteDirectory(string path, bool isRecursive)
        {
            Directory.Delete(path, isRecursive);
        }

        public FileAttributes GetAttributes(string path)
        {
            return File.GetAttributes(path);
        }

        public IEnumerable<string> EnumerateFiles(string path)
        {
            return Directory.EnumerateFiles(path);
        }

        public IEnumerable<string> EnumerateFiles(string path, string pattern)
        {
            return Directory.EnumerateFiles(path, pattern);
        }

        public IEnumerable<string> EnumerateDirectories(string path)
        {
            return Directory.EnumerateDirectories(path);
        }

        public IEnumerable<string> EnumerateDirectories(string path, string pattern)
        {
            return Directory.EnumerateDirectories(path, pattern);
        }

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public byte[] ReadAllBytes(string path)
        {
            return File.ReadAllBytes(path);
        }

        public string ReadAllText(string path)
        {
            return File.ReadAllText(path);
        }

        public async Task<byte[]> ReadAllBytesAsync(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
            {
                // read the whole file into main memory
                var buffer = new byte[stream.Length];
                int length, offset = 0;
                do
                {
                    // read length can be less than the buffer size 
                    length = await stream.ReadAsync(buffer, offset, buffer.Length - offset).ConfigureAwait(false);
                    offset += length;
                } while (length > 0);

                return buffer;
            }
        }

        public async Task<string> ReadToEndAsync(string path)
        {
            using (var reader = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true)))
            {
                return await reader.ReadToEndAsync();
            }
        }

        public void Search(string path, SearchCallback directoryCallback, SearchCallback fileCallback)
        {
            var attrs = File.GetAttributes(path);
            if (!attrs.HasFlag(FileAttributes.Directory))
            {
                fileCallback(path);
                return;
            }

            var stack = new Stack<string>();
            stack.Push(path);

            while (stack.Count > 0)
            {
                var dir = stack.Pop();
                var result = directoryCallback(dir);
                if (result == SearchControl.Abort)
                {
                    return;
                }

                if (result == SearchControl.Visit)
                {
                    // find files
                    foreach (var file in Directory.EnumerateFiles(dir))
                    {
                        var fileResult = fileCallback(file);
                        if (fileResult == SearchControl.Abort)
                        {
                            return;
                        }
                    }

                    // find directories
                    foreach (var subdir in Directory.EnumerateDirectories(dir))
                    {
                        stack.Push(subdir);
                    }
                }
            }
        }
    }
}
