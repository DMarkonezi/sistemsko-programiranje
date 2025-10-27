using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace tasks_web_server.Utils
{
    static class FileHandler
    {
        private static readonly String root = "../../Files";

        /*public static String ReadContentFromFile(string filename)
        {
            String filePath = "../" + root + filename;
            if (!File.Exists(filePath)) return null;
            return File.ReadAllText(filePath);
        }*/

        private static async Task ConvertBinaryToTextAsync(string sourceBinPath, string targetTxtPath)
        {
            byte[] textBytes = await Task.Run(() => File.ReadAllBytes(sourceBinPath));
            string text = Encoding.UTF8.GetString(textBytes);
            await Task.Run(() => File.WriteAllText(targetTxtPath, text));
        }

        private static async Task ConvertTextToBinaryAsync(string sourceTxtPath, string targetBinPath)
        {
            string text = await Task.Run(() => File.ReadAllText(sourceTxtPath));
            byte[] textBytes = Encoding.UTF8.GetBytes(text);
            await Task.Run(() => File.WriteAllBytes(targetBinPath, textBytes));
        }

        public static async Task<bool >ConvertFileBinaryTxtAsync(string filename)
        {
            string sourcePath = Path.Combine(root, filename);

            // Console.WriteLine(sourcePath);
            // Console.WriteLine("Absolute: " + Path.GetFullPath(sourcePath));

            if (!await Task.Run(() => File.Exists(sourcePath)))
            {
                throw new FileNotFoundException();
            }

            int dotIndex = filename.IndexOf('.');

            if (dotIndex <= 0)
            {
                throw new InvalidOperationException("File must have a supported extension (.bin or .txt).");
            }

            string fileExtension = filename.Substring(dotIndex + 1);
            string filenameWithoutExt = filename.Substring(0, dotIndex);

            if (fileExtension == "bin")
            {
                string targetPath = Path.Combine(root, filenameWithoutExt + ".txt");

                if (File.Exists(targetPath))
                {
                    return false;
                }

                await ConvertBinaryToTextAsync(sourcePath, targetPath);
                return true;
            }
            else if (fileExtension == "txt")
            {
                string targetPath = Path.Combine(root, filenameWithoutExt + ".bin");

                if (File.Exists(targetPath))
                {
                    return false;
                }

                await ConvertTextToBinaryAsync(sourcePath, targetPath);
                return true;
            }
            else
            {
                throw new InvalidOperationException("Requested file extension not supported.");
            }
        }
    }
}
