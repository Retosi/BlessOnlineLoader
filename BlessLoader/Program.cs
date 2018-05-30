using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BlessLoader
{
    class Program
    {
        private static string consoleMainPrompt = "Detection Mode Options [ 1 , 2 ]\nInput 1 for game autodetection\nInput 2 for manual mode\nThen press enter: ";
        private static string consoleManualPrompt = "Enter the path of your Bless Online download folder\nExample: C:\\Program Files (x86)\\Steam\\steamapps\\common\\Bless Online\\ \nThen press enter: ";
        private static string consoleStartPrompt = "Starting the game...\n";
        //
        private static string defaultSteamPath = @"C:\Program Files (x86)\Steam\Steam.exe";
        private static string defaultSteamGamesPath = @"C:\Program Files (x86)\Steam\steamapps\common";
        //
        private static string steamPath = string.Empty;
        private static string blessPath = string.Empty;

        // Main
        static void Main(string[] args)
        {
            Init();
        }

        // Setup
        private static void Init()
        {
            HandleInput_Options();
        }

        // Prompt Input
        private static void HandleInput_Options()
        {
            var notValid = true;
            while (notValid)
            {
                // Prompt user for mode
                ConsoleWrite(consoleMainPrompt);
                // Get input from the user
                var consoleInput = ConsoleRead();
                if (consoleInput == "1")
                {
                    // Auto Mode
                    BlessAutoDetection();
                }
                else if (consoleInput == "2")
                {
                    // Manual Mode
                    HandleInput_Manual();
                }
                else
                {
                    // Not Valid
                    ConsoleWrite("Input was not valid.");
                }
            }
        }

        private static void HandleInput_Manual()
        {
            var notValid = true;
            while (notValid)
            {
                // Prompt user for directory
                ConsoleWrite(consoleManualPrompt);
                // Get input from the user
                var consoleInput = ConsoleRead();
                // Valid Location Check
                if (File.Exists(consoleInput + @"Binaries\Win64\Bless_BE.exe"))
                {
                    blessPath = consoleInput;
                    // Run
                    Run(true);
                }
                else
                {
                    // Not Valid
                    ConsoleWrite("Not a valid location.");
                }
            }
        }

        // Run
        private static void Run(bool isDefault)
        {
            Process[] processlist = Process.GetProcesses();
            foreach (Process theprocess in processlist)
            {
                if (theprocess.ProcessName == "Bless" || theprocess.ProcessName == "Bless_BE" || theprocess.ProcessName == "BlessLauncher")
                {
                    ConsoleWrite("Closing process: " + theprocess.ProcessName);
                    theprocess.Kill();
                }
            }

            ConsoleWrite(consoleStartPrompt);

            var blessPathExe = blessPath + @"Binaries\Win64\Bless_BE.exe";
            if (File.Exists(blessPathExe))
            {
                Process.Start(blessPathExe);
            }
            else
            {
                ConsoleWrite("Could not find Bless_BE.exe");
                Console.ReadLine();
            }

            var conAttempt = false;
            while (true)
            {
                var counter = 0;
                var othercounter = 0;

                var netStatItems = GetNetStatPorts();

                foreach (var item in netStatItems)
                {
                    if (item.process_name.Contains("Bless"))
                    {
                        if (item.connection == "ESTABLISHED")
                        {
                            counter++;
                        }
                        else
                        {
                            othercounter++;
                        }
                        //Console.WriteLine("{0} {1} {2} {3}", item.process_name, item.protocol, item.port_number, item.connection);
                    }
                }

                if (counter == 1 && othercounter == 2)
                {
                    ConsoleWrite("Connection attempt made");
                    conAttempt = true;
                }
                else if (counter == 2 && othercounter == 2)
                {
                    ConsoleWrite("Connection good");
                    Environment.Exit(0);
                }

                if (conAttempt == true && othercounter == 2 && counter == 0)
                {
                    ConsoleWrite("Connection failed, retrying");
                    Run(true);
                }

                //Console.WriteLine("");
                System.Threading.Thread.Sleep(500);
            }
        }

        private static string ConsoleRead()
        {
            return Console.ReadLine();
        }
        
        private static void ConsoleWrite(string content)
        {
            Console.WriteLine(content);
        }

        private static void BlessAutoDetection()
        {
            // Check default steam location
            if (File.Exists(defaultSteamPath))
            {
                // Steam path
                steamPath = defaultSteamPath.Replace("steam.exe", "");
                // Error catching
                try
                {
                    // Get list of games
                    var games = Directory.GetDirectories(defaultSteamGamesPath);
                    // Search for Bless
                    foreach (var item in games)
                    {
                        if (item.Contains("Bless Online"))
                        {
                            // Bless Path
                            blessPath = item + @"\";
                            // Run
                            Run(true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Write error
                    ConsoleWrite("Error: " + ex);
                    // Run
                    Run(false);
                }
            }
            else
            {
                // Run
                Run(false);
            }
        }

        // ===============================================
        // The Method That Parses The NetStat Output
        // And Returns A List Of Port Objects
        // ===============================================
        public static List<Port> GetNetStatPorts()
        {
            var Ports = new List<Port>();

            try
            {
                using (Process p = new Process())
                {

                    ProcessStartInfo ps = new ProcessStartInfo();
                    ps.Arguments = "-a -n -o";
                    ps.FileName = "netstat.exe";
                    ps.UseShellExecute = false;
                    ps.WindowStyle = ProcessWindowStyle.Hidden;
                    ps.RedirectStandardInput = true;
                    ps.RedirectStandardOutput = true;
                    ps.RedirectStandardError = true;

                    p.StartInfo = ps;
                    p.Start();

                    StreamReader stdOutput = p.StandardOutput;
                    StreamReader stdError = p.StandardError;

                    string content = stdOutput.ReadToEnd() + stdError.ReadToEnd();
                    string exitStatus = p.ExitCode.ToString();

                    if (exitStatus != "0")
                    {
                        // Command Errored. Handle Here If Need Be
                    }

                    //Get The Rows
                    string[] rows = Regex.Split(content, "\r\n");
                    foreach (string row in rows)
                    {
                        //Split it
                        string[] tokens = Regex.Split(row, "\\s+");
                        if (tokens.Length > 4 && (tokens[1].Equals("UDP") || tokens[1].Equals("TCP")))
                        {
                            string localAddress = Regex.Replace(tokens[2], @"\[(.*?)\]", "1.1.1.1");
                            Ports.Add(new Port
                            {
                                protocol = localAddress.Contains("1.1.1.1") ? String.Format("{0}v6", tokens[1]) : String.Format("{0}v4", tokens[1]),
                                port_number = localAddress.Split(':')[1],
                                process_name = tokens[1] == "UDP" ? LookupProcess(Convert.ToInt16(tokens[4])) : LookupProcess(Convert.ToInt16(tokens[5])),
                                connection = tokens[4]
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return Ports;
        }

        public static string LookupProcess(int pid)
        {
            string procName;
            try { procName = Process.GetProcessById(pid).ProcessName; }
            catch (Exception) { procName = "-"; }
            return procName;
        }

        // ===============================================
        // The Port Class We're Going To Create A List Of
        // ===============================================
        public class Port
        {
            public string name
            {
                get
                {
                    return string.Format("{0} ({1} port {2})", this.process_name, this.protocol, this.port_number);
                }
                set { }
            }
            public string port_number { get; set; }
            public string process_name { get; set; }
            public string protocol { get; set; }
            public string connection { get; set; }
        }
    }
}
