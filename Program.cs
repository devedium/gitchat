using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using OpenAI.GPT3;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace GitChat
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder()
               .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("env") ?? "dev"}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var apiKey = config.GetSection("apiKey").Get<string>();

            while (true)
            {
                Console.Write(">");
                string? userInput = Console.ReadLine()?.ToLower();
                string? gitCommand = "";
                string? response = "";

                var gpt3 = new OpenAIService(new OpenAiOptions()
                {
                    ApiKey = apiKey
                });


                var completionResult = await gpt3.Completions.CreateCompletion(new CompletionCreateRequest()
                {
                    Prompt = $"[SYSTEM]: Outpupt a git command only from the following instructions, no explanation or other text \n --- \n Instructions: {userInput} \n Git Command:",                    
                    Model = Models.TextDavinciV2,
                    Temperature = 0F,
                    MaxTokens = 100,
                    N = 1
                });

                if (completionResult.Successful)
                {
                    response = completionResult.Choices[0].Text;
                }
                else
                {
                    if (completionResult.Error == null)
                    {
                        throw new Exception("Unknown Error");
                    }
                    Console.WriteLine($"{completionResult.Error.Code}: {completionResult.Error.Message}");
                }
                gitCommand = response.Trim(' ', '\r', '\n');
                var gitCommands = gitCommand.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (gitCommands == null)
                {
                    Console.WriteLine(response);
                    continue;
                }
                foreach(var cmd in gitCommands) 
                { 
                    gitCommand = cmd.Trim().ToLowerInvariant();
                    if (gitCommand.StartsWith("git "))
                    {
                        Console.Write($"{gitCommand} (y/n)");
                        var confirm = Console.ReadLine()?.ToLower();
                        if ((confirm == "y") || (confirm == ""))
                        {
                            ExecuteCommand(gitCommand);
                        }
                        Console.WriteLine("");
                    }
                    else
                    {
                        Console.Write($"{gitCommand}");
                    }
                }                
            }
        }



        static void ExecuteCommand(string command)
        {
            Process process = new Process();

            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/c " + command;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = false;

            var ret = process.Start();
            if (ret)
            {
                process.WaitForExit();
                var output = process.StandardOutput.ReadToEnd();
                if (!string.IsNullOrEmpty(output))
                    Console.WriteLine(output);
                var error = process.StandardError.ReadToEnd();
                if (!string.IsNullOrEmpty(error))
                    Console.WriteLine(error);
            }
            else
            {
                Console.WriteLine("failed to run!");
            }
        }
        
    }
}
