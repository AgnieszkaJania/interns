using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.Parsing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text.Json;

namespace interns
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var ageGt = new Option<int>("--age-gt", "counts interns where age is greater")
                {
                    IsRequired = false
                };
                var ageLt = new Option<int>("--age-lt", "counts interns where age is less")
                {
                    IsRequired = false
                };
                var countCommand = new Command("count", "counts number of interns satisifing condition");
                var maxAgeCommand = new Command("max-age", "writes a maximum age of an intern");
                var url = new Argument<string>("url", "url to download a file from");

                countCommand.AddArgument(url);
                countCommand.AddOption(ageGt);
                countCommand.AddOption(ageLt);
                maxAgeCommand.AddArgument(url);

                var cmd = new RootCommand();
                cmd.Add(countCommand);
                cmd.Add(maxAgeCommand);

                if (!ValidateCommands(args))
                {
                    throw new Exception("Error: Invalid command.");
                }
                countCommand.Handler = CommandHandler.Create((string url, int? ageGt, int? ageLt, IConsole console) =>
                {
                    FileType fileType = DetermineFileType(url);
                    string count = GetCount(url, ageGt, ageLt, fileType);
                    console.Out.Write(count);
                });
                maxAgeCommand.Handler = CommandHandler.Create((string url, IConsole console) =>
                {
                    FileType fileType = DetermineFileType(url);
                    string maxAge = GetMaxAge(url, fileType);
                    console.Out.Write(maxAge);
                });
                var parser = new CommandLineBuilder(cmd).UseExceptionHandler((Exception ex, InvocationContext ic) =>
                {

                    if (ex.InnerException is WebException || ex.InnerException is UriFormatException)
                    {
                        throw new Exception("Error: Cannot get file.");
                    }
                    if (ex.InnerException is IOException)
                    {
                        throw new Exception("Error: Cannot process the file.");
                    }
                    throw ex.InnerException;

                }).UseParseErrorReporting().Build();
                ParserExtensions.Invoke(parser, args);
            }
            catch (Exception e)
            {
                Console.Write(e.Message);
            }

        }
        private static bool ValidateCommands(string[] commands)
        {
            if (commands.Length < 1)
            {
                return false;
            }
            string command = commands[0];
            string[] availableCommands = { "max-age", "count" };
            if (!availableCommands.Contains(command))
            {
                return false;
            }
            return true;

        }
        private static FileType DetermineFileType(string url)
        {
            string ext = Path.GetExtension(url);
            switch (ext)
            {
                case ".json":
                    return FileType.json;
                case ".csv":
                    return FileType.csv;
                case ".zip":
                    return FileType.zip;
                default:
                    throw new Exception("Error: Can not process the file.");
            }
        }
        private static List<Intern> GetInterns(FileType fileType, string url)
        {
            switch (fileType)
            {
                case FileType.json:
                    return GetDataFromJsonFile(url);
                case FileType.csv:
                    return GetDataFromCSVFile(url);
                case FileType.zip:
                    return GetDataFromZipFile(url);
                default:
                    throw new Exception("Error: Invalid format.");

            }
        }
        private static string GetMaxAge(string url, FileType fileType)
        {
            List<Intern> interns = GetInterns(fileType, url);
            if (interns == null)
            {
                throw new Exception("Error: Cannot process the file.");
            }
            return interns.Max(x => x.age).ToString();
        }
        private static string GetCount(string url, int? ageGt, int? ageLt, FileType fileType)
        {
            List<Intern> interns = GetInterns(fileType, url);
            if (interns == null)
            {
                throw new Exception("Error: Cannot process the file.");
            }
            if(ageGt != null && ageLt != null)
            {
                return interns.Where(x => x.age > ageGt && x.age < ageLt).Count().ToString();
            }
            if (ageGt != null)
            {
                return interns.Where(x => x.age > ageGt).Count().ToString();
            }
            if (ageLt != null)
            {
                return interns.Where(x => x.age < ageLt).Count().ToString();
            }
            return interns.Count().ToString();
        }
        private static Stream GetFileStream(string url)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            Stream stream = resp.GetResponseStream();
            return stream;
        }
        private static List<Intern> GetDataFromJsonFile(string url)
        {
            Stream stream = GetFileStream(url);
            StreamReader streamReader = new StreamReader(stream);
            string data = streamReader.ReadToEnd();
            var interns = JsonSerializer.Deserialize<Interns>(data);
            return interns.interns;
        }
        private static List<Intern> GetDataFromCSVFile(string url)
        {
            Stream stream = GetFileStream(url);
            StreamReader streamReader = new StreamReader(stream);
            return ProcessCSVFile(streamReader);
        }
        private static List<Intern> ProcessCSVFile(StreamReader streamReader)
        {

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = (args) =>
                {
                    int index = args.Header.IndexOf("/");
                    var prepared = args.Header.Substring(index + 1, args.Header.Length - (index + 1));
                    return prepared;
                }
            };
            using (var csv = new CsvReader(streamReader, config))
            {
                var records = csv.GetRecords<Intern>().ToList();
                if (records.Count() == 0)
                {
                    throw new Exception("Error: Can not process the file.");
                }
                return records;

            }
        }
        private static List<Intern> GetDataFromZipFile(string url)
        {
            Stream zipStream = GetFileStream(url);
            ZipArchive zip = new ZipArchive(zipStream);
            ZipArchiveEntry entry = zip.Entries.Single();
            Stream fileStream = entry.Open();
            StreamReader fileStreamReader = new StreamReader(fileStream);
            return ProcessCSVFile(fileStreamReader);
        }
    }
}
