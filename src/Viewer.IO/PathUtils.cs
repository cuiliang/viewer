﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.IO
{
    /// <summary>
    /// Utility functions for file system paths.
    /// </summary>
    public static class PathUtils
    {
        /// <summary>
        /// List of characters that could be used as path separators
        /// </summary>
        public static char[] PathSeparators =
        {
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar
        };

        /// <summary>
        /// Split filesystem path to parts (file and directory names) using <see cref="PathSeparators"/> as delimiters 
        /// </summary>
        /// <param name="fullPath">Path to a directory/file</param>
        /// <returns>Parts of the path</returns>
        /// <exception cref="ArgumentNullException"><paramref name="fullPath"/> is null</exception>
        public static IEnumerable<string> Split(string fullPath)
        {
            if (fullPath == null)
            {
                throw new ArgumentNullException(nameof(fullPath));
            }
            return fullPath.Split(PathSeparators, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Get last part of given path (i.e. file/folder name).
        /// Note: if the last character is a directory separator, it will be removed
        /// </summary>
        /// <param name="path">Path to a file or folder</param>
        /// <returns>Last part of the path</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        public static string GetLastPart(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            var lastPartLength = 0; 
            var separatorIndex = path.Length - 1;
            while (separatorIndex >= 0)
            {
                // check if there is a path separator at this position
                var isSeparator = false;
                foreach (var sep in PathSeparators)
                {
                    if (sep == path[separatorIndex])
                    {
                        isSeparator = true;
                        break;
                    }
                }

                if (isSeparator && separatorIndex + 1 != path.Length)
                {
                    break;
                }
                else if (!isSeparator)
                {
                    ++lastPartLength;
                }

                --separatorIndex;
            }
            
            return path.Substring(separatorIndex + 1, lastPartLength);
        }

        /// <summary>
        /// Get path to the parent directory of the file/directory at <paramref name="path"/>.
        /// </summary>
        /// <param name="path">Path to a file or directory</param>
        /// <returns>
        ///     Path to the parent directory of <paramref name="path"/>.
        ///     If <paramref name="path"/> is an empty string, it will return an empty string.
        ///     It will return null, iff <paramref name="path"/> is null.
        /// </returns>
        public static string GetDirectoryPath(string path)
        {
            if (path == null)
                return null;
            if (path.Length == 0)
                return "";

            // if the last character is a path separator, ignore it
            var index = path.Length - 1;
            if (PathSeparators.Contains(path[index]))
            {
                --index;
            }

            // find the next path separator
            for (; index >= 0; --index)
            {
                if (PathSeparators.Contains(path[index]))
                {
                    break;
                }
            }

            if (index < 0)
            {
                return "";
            }

            return path.Substring(0, index);
        }

        /// <summary>
        /// Check whether given string could be a valid file/folder name
        /// </summary>
        /// <param name="name">Name of a file</param>
        /// <returns>true iff given value could be a valid file/folder name. It returns false, if <paramref name="name"/> is null.</returns>
        public static bool IsValidFileName(string name)
        {
            return !string.IsNullOrEmpty(name) &&
                   name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
        }

        /// <summary>
        /// Get list of printable invalid file name characters in a string.
        /// </summary>
        /// <returns>String containing invalid file name characters separated by comma</returns>
        public static string GetInvalidFileCharacters()
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder();
            foreach (var c in invalid)
            {
                if (char.IsControl(c) && !char.IsWhiteSpace(c))
                    continue;

                if (c == '\n')
                    sb.Append("\\n");
                else if (c == '\t')
                    sb.Append("\\t");
                else if (c == '\r')
                    sb.Append("\\r");
                else
                    sb.Append(c);
                sb.Append(", ");
            }

            if (sb.Length > 0)
            {
                sb.Remove(sb.Length - 3, 3); // remove the last separator
            }

            return sb.ToString();
        }

        /// <summary>
        /// Unify a file system path to a common format so that it can be compared to other unified paths.
        /// </summary>
        /// <param name="path">Path to a file/folder.</param>
        /// <returns>Unified path to the same file/folder. If <paramref name="path"/> is null, it will return null.</returns>
        public static string UnifyPath(string path)
        {
            if (path == null)
            {
                return null;
            }

            var unifiedPath = new StringBuilder();
            for (var i = 0; i < path.Length; ++i)
            {
                if (path[i] == '/' || path[i] == '\\')
                {
                    unifiedPath.Append('/');

                    // remove double separator
                    if (i + 1 < path.Length && (path[i + 1] == '\\' || path[i + 1] == '/'))
                    {
                        ++i;
                    }
                }
                else
                {
                    unifiedPath.Append(path[i]);
                }
            }

            return unifiedPath.ToString();
        }
    }
}
