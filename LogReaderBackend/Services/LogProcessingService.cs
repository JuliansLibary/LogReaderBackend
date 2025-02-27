﻿using LogReaderBackend.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace LogReaderBackend.Services
{
    public class LogProcessingService
    {
        public async Task<List<PostAccessContent>> ReadAccessLogAsync(Stream fileStream, bool isChecked, string startTime, string endTime)
        {
            var contentCounts = new ConcurrentDictionary<AccessContent, int>();
            var lines = new BlockingCollection<string>();
            int processedLines = 0;
            bool isIdGroupChecked = isChecked;
            Task readTask =  Task.Run(() =>
            {
                using (var reader = new StreamReader(fileStream))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        lines.Add(line);
                    }
                }
                lines.CompleteAdding();
            });

            DateTime? startDateTime = null;
            DateTime? endDateTime = null;

            if (!string.IsNullOrEmpty(startTime))
            {
                startDateTime = DateTime.Today.Add(TimeSpan.Parse(startTime));
            }

            if (!string.IsNullOrEmpty(endTime))
            {
                endDateTime = DateTime.Today.Add(TimeSpan.Parse(endTime));
            }


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
                        string nodeId = "";
                        try
                        {
                             nodeId = parts[13].Split('=')[1];

                        }
                        catch
                        {
                             nodeId = parts[12].Split('=')[1];

                        }
                        DateTime date = DateTime.ParseExact(dateStr, "dd/MMM/yyyy:HH:mm:ss", CultureInfo.InvariantCulture);
                        if ((startDateTime == null || date.TimeOfDay >= startDateTime.Value.TimeOfDay) &&
                        (endDateTime == null || date.TimeOfDay <= endDateTime.Value.TimeOfDay))
                        {
                            AccessContent content;
                            if (isIdGroupChecked)
                            {
                                content = new AccessContent(requestContent, requestType, nodeId);
                            }
                            else
                            {
                                content = new AccessContent(sid, requestContent, requestType, nodeId);
                            }

                            contentCounts.AddOrUpdate(content, 1, (key, oldValue) => oldValue + 1);
                            Interlocked.Increment(ref processedLines);
                        }
                        if (processedLines % 100 == 0)
                        {
                           // Console.WriteLine($"Processed {processedLines} lines.");
                        }
                        
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Failed to parse at line " + e);
                    }
                });

                readTask.Wait();
                var resultList = contentCounts
            .OrderByDescending(pair => pair.Value)
            .Select(pair => new PostAccessContent(pair.Key.SID, pair.Key.RequestContent, pair.Key.RequestType, pair.Key.NodeId,pair.Value))
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
        public async Task<List<PostErrorContent>> ReadErrorLogAsync(Stream fileStream, bool groupById, string startTime, string endTime)
        {
            // Der restliche Code bleibt gleich, aber verwenden Sie `fileStream` anstelle von `file.OpenReadStream()`
            var contentCounts = new ConcurrentDictionary<ErrorContent, int>();
            var lines = new BlockingCollection<string>();
            int processedLines = 0;
            bool isIdGroupChecked = groupById;
            Task readTask = Task.Run(() =>
            {
                using (var reader = new StreamReader(fileStream))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        lines.Add(line);
                    }
                }
                lines.CompleteAdding();
            });
            DateTime? startDateTime = null;
            DateTime? endDateTime = null;

            if (!string.IsNullOrEmpty(startTime))
            {
                startDateTime = DateTime.Today.Add(TimeSpan.Parse(startTime));
            }

            if (!string.IsNullOrEmpty(endTime))
            {
                endDateTime = DateTime.Today.Add(TimeSpan.Parse(endTime));
            }

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
                                string dateStr = match.Groups[1].Value;
                                string cleanedDateTimeStr = Regex.Replace(dateStr, @"\.\d+", "");
                                DateTime date = DateTime.ParseExact(cleanedDateTimeStr, "ddd MMM dd HH:mm:ss yyyy", CultureInfo.InvariantCulture);
                                if ((startDateTime == null || date.TimeOfDay >= startDateTime.Value.TimeOfDay) &&
                                (endDateTime == null || date.TimeOfDay <= endDateTime.Value.TimeOfDay))
                                {
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
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Failed to parse at line " + processedLines + e);
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
