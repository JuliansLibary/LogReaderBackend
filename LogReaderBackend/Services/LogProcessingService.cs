using LogReaderBackend.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LogReaderBackend.Services
{
    public class LogProcessingService
    {
        public List<PostAccessContent> ReadAccessLog(IFormFile file, bool isChecked)
        {
            var contentCounts = new ConcurrentDictionary<AccessContent, int>();
            var lines = new BlockingCollection<string>();
            int processedLines = 0;
            bool isIdGroupChecked = isChecked;
            Task readTask = Task.Run(() =>
            {
                using (var reader = new StreamReader(file.OpenReadStream()))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        lines.Add(line);
                    }
                }
                lines.CompleteAdding();
            });

            try
            {
                Parallel.ForEach(lines.GetConsumingEnumerable(), new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, line =>
                {

                    try
                    {
                        string[] parts = line.Split(' ');

                        string dateStr = parts[0].Trim('[', ']');
                        string requestType = parts[4].Trim('"');
                        string requestContent = parts[5];
                        string sid = isIdGroupChecked ? string.Empty : parts[10].Split('=')[1];
                        DateTime date = DateTime.ParseExact(dateStr, "dd/MMM/yyyy:HH:mm:ss", CultureInfo.InvariantCulture);
                        AccessContent content;
                        if (isIdGroupChecked)
                        {
                            content = new AccessContent(requestContent, requestType);
                        }
                        else
                        {
                            content = new AccessContent(sid, requestContent, requestType);
                        }

                        contentCounts.AddOrUpdate(content, 1, (key, oldValue) => oldValue + 1);
                        Interlocked.Increment(ref processedLines);
                        if (processedLines % 100 == 0)
                        {
                            Console.WriteLine($"Processed {processedLines} lines.");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Failed to parse at line " + processedLines);
                    }
                });

                readTask.Wait();

                var resultList = contentCounts
            .OrderByDescending(pair => pair.Value)
            .Select(pair => new PostAccessContent(pair.Key.SID, pair.Key.RequestContent, pair.Key.RequestType, pair.Value))
            .ToList();


                return resultList;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return new List<PostAccessContent>();
            }
        }


        public void SortFileContentByValues(List<Dictionary<Content, int>> fileContent)
        {
            fileContent.Sort((dict1, dict2) =>
                dict2.Values.Sum().CompareTo(dict1.Values.Sum()));

        }
        public List<PostErrorContent> ReadErrorLog(IFormFile file, bool groupById)
        {
            var contentCounts = new ConcurrentDictionary<ErrorContent, int>();
            var lines = new BlockingCollection<string>();
            int processedLines = 0;
            bool isIdGroupChecked = groupById;
            Task readTask = Task.Run(() =>
            {
                using (var reader = new StreamReader(file.OpenReadStream()))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        lines.Add(line);
                    }
                }
                lines.CompleteAdding();
            });

            try
            {
                Parallel.ForEach(lines.GetConsumingEnumerable(), new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, line =>
                {
                    try
                    {
                        string pattern = @"\[(.*?)\] \[(.*?):(.*?)\] \[pid (\d+)\] (\w+): (.+)";
                        Match match = Regex.Match(line, pattern);

                        if (match.Success)
                        {
                            try
                            {
                                string requestMessage = match.Groups[6].Value;
                                string requestContent = match.Groups[5].Value;
                                string sid = isIdGroupChecked ? string.Empty : match.Groups[4].Value;
                                ErrorContent content;
                                if (isIdGroupChecked)
                                {
                                    content = new ErrorContent(requestContent, requestMessage);
                                }
                                else
                                {
                                    content = new ErrorContent(sid, requestContent, requestMessage);
                                }

                                contentCounts.AddOrUpdate(content, 1, (key, oldValue) => oldValue + 1);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Failed to parse at line " + processedLines);
                            }

                        }
                        else
                        {
                            Console.WriteLine($"Regex failed on line: {line}");
                        }
                    }
                    catch
                    {
                        Console.WriteLine($"Error While Parsing on line: {line}");
                    }

                    Interlocked.Increment(ref processedLines);
                    if (processedLines % 100 == 0)
                    {
                        Console.WriteLine($"Processed {processedLines} lines.");
                    }
                });

                var resultList = contentCounts
                .OrderByDescending(pair => pair.Value)
                .Select(pair => new PostErrorContent(pair.Key.SID, pair.Key.RequestContent, pair.Key.RequestMessage, pair.Value))
                .ToList();

                return resultList;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return new List<PostErrorContent>();
            }
        }
    }
}
