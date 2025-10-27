using System;
using System.IO;
using System.Text;

namespace multithreaded_web_server.Utils
{
    static class FileHandler
    {
        private static readonly String root = "../../Files";

        public static String ReadContentFromFile(string filename)
        {
            String filePath = "../" + root + filename;
            if (!File.Exists(filePath)) return null;
            return File.ReadAllText(filePath);
        }

        private static void ConvertBinaryToText(string sourceBinPath, string targetTxtPath)
        {
            byte[] textBytes = File.ReadAllBytes(sourceBinPath);
            string text = Encoding.UTF8.GetString(textBytes);
            File.WriteAllText(targetTxtPath, text);

            // File.Delete(sourceBinPath);
        }

        private static void ConvertTextToBinary(string sourceTxtPath, string targetBinPath)
        {
            string text = File.ReadAllText(sourceTxtPath);
            byte[] textBytes = Encoding.UTF8.GetBytes(text);
            File.WriteAllBytes(targetBinPath, textBytes);

            // File.Delete(sourceTxtPath);
        }

        public static bool ConvertFileBinaryTxt(string filename)
        {
            string sourcePath = Path.Combine(root, filename);

            // Console.WriteLine(sourcePath);
            // Console.WriteLine("Absolute: " + Path.GetFullPath(sourcePath));

            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException();
            }

            int dotIndex = filename.IndexOf('.');

            string fileExtension = filename.Substring(dotIndex + 1);
            string filenameWithoutExt = filename.Substring(0, dotIndex);

            if (fileExtension == "bin")
            {
                string targetPath = Path.Combine(root, filenameWithoutExt + ".txt");

                if (File.Exists(targetPath))
                {
                    return false;
                }

                ConvertBinaryToText(sourcePath, targetPath);
                return true;
            } 
            else if (fileExtension == "txt")
            {
                string targetPath = Path.Combine(root, filenameWithoutExt + ".bin");

                if (File.Exists(targetPath))
                {
                    return false;
                }

                ConvertTextToBinary(sourcePath, targetPath);
                return true;
            } 
            else
            {
                throw new InvalidOperationException("Requested file extension not supported.");
            }
        }
    }
}
