using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security.Principal;
using CommandLine;
using Serilog;

namespace vap.development.wordpress_updater
{
    internal class Program
    {
        public static Options Options;
        public static string SearchFolder { get; set; }
        public static string LatestUrl { get; set; }
        public static string WpCheckUrl { get; set; }
        public static string DbUpdateUrl { get; set; }
        public static string ExtractPath { get; set; }
        public static string SearchFile { get; set; }
        public static string WebUrlLocation { get; set; }
        public static string ZipFileName { get; set; }
        public static IEnumerable<string> SiteFolderPaths { get; set; }
        public static IEnumerable<string> SiteUrls { get; set; }

        private static bool IsElevated
            => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

        private static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
               .ReadFrom.AppSettings()
               .CreateLogger();
            Log.Information($"The global logger has been configured");

            var commandLineParseResult = Parser.Default.ParseArguments<Options>(args);
            var parsed = commandLineParseResult as Parsed<Options>;
            if (parsed == null)
            {
                return; // not parsed
            }
            Options = parsed.Value;

            WritetoUser($"WordPress Updater");

            SearchFolder = Options.SearchFolder;
            LatestUrl = Options.LatestUrl;
            WpCheckUrl = Options.WpCheckUrl;
            DbUpdateUrl = Options.DbUpdateUrl;
            ExtractPath = Options.ExtractPath;
            SearchFile = Options.SearchFile;
            WebUrlLocation = Options.WebUrlLocation;
            SiteFolderPaths = Options.SiteFolderPaths;
            SiteUrls = Options.SiteUrls;
            ZipFileName = Options.ZipFileName;

            var sites = new List<WordPressSite>();

            if (SiteFolderPaths.Count() != 0 && SiteUrls.Count() != 0 && SiteFolderPaths.Count() == SiteUrls.Count())
            {
                WritetoUser($"Using Passed in folders and sites");
                sites = CreateSites();
            }
            else
            {
                WritetoUser($"Finding folders and sites");
                sites = FindSites();
            }
            if (sites.Any())
            {
                WritetoUser($"Processing {sites.Count()} sites");
                CleanFolder();
                var downloadPath = Download();
                var extractPath = ExtractZip(downloadPath);
                ProcessSites(extractPath, sites);
                WritetoUser($"Program Exiting");
#if DEBUG
                WritetoUser($"Press any key to exit.");
                WritetoUser($"");
                Console.ReadKey();
#endif
            }
            else
            {
                ThrowError($"No sites were specified or found");
            }
        }

        /// <summary>
        ///     This uses the passed in folders and sites and puts them in a list
        /// </summary>
        /// <returns>List of WordPress sites</returns>
        private static List<WordPressSite> CreateSites()
        {
            var sites = new List<WordPressSite>();
            var folderArray = SiteFolderPaths.Cast<string>().ToArray();
            var siteArray = SiteUrls.Cast<string>().ToArray();

            for (var i = 0; i < folderArray.Count(); i ++)
            {
                sites.Add(new WordPressSite {FolderPath = folderArray[i], Url = siteArray[i]});
            }
            return sites;
        }

        /// <summary>
        ///     This finds all sites under the SearchFolder
        /// </summary>
        /// <returns>List of found WordPress sites</returns>
        private static List<WordPressSite> FindSites()
        {
            var sites = new List<WordPressSite>();
            var returnSites = new List<WordPressSite>();
            if (Directory.Exists(SearchFolder))
            {
                if (File.Exists(Path.Combine(SearchFolder, SearchFile)))
                {
                    sites.Add(new WordPressSite {FolderPath = SearchFolder});
                }
                else
                {
                    foreach (var file in Directory.EnumerateFiles(SearchFolder, SearchFile, SearchOption.AllDirectories)
                        )
                    {
                        var folder = file.Remove(file.Length - SearchFile.Length, SearchFile.Length);
                        sites.Add(new WordPressSite {FolderPath = folder});
                        Log.Debug("File {file} found at {folder}", file, folder);
                    }
                }
            }
            else
            {
                ThrowError($"{SearchFolder} does not exist. Please enter a valid directory.");
            }

            if (sites.Count > 0)
            {
                foreach (var site in sites)
                {
                    var siteFolder = site.FolderPath.Remove(site.FolderPath.IndexOf(WebUrlLocation));
                    var siteSubFolder =
                        site.FolderPath.Substring(site.FolderPath.IndexOf(WebUrlLocation) + WebUrlLocation.Length)
                            .Replace("\\", "/");
                    var Url = "http://" + siteFolder.Substring(siteFolder.LastIndexOf("\\") + 1) + "/";
                    if (siteSubFolder != "")
                    {
                        Url = Url + siteSubFolder;
                    }

                    site.Url = Url;

                    if (IsWordPressSite(site.Url))
                    {
                        returnSites.Add(site);
                    }
                }
            }
            else
            {
                ThrowError($"{SearchFile} was not found anywhere under {SearchFolder}");
            }

            return returnSites;
        }

        /// <summary>
        ///     Cleans the ExtractPath
        /// </summary>
        private static void CleanFolder()
        {
            if (Directory.Exists(ExtractPath))
            {
                if (Directory.GetFiles(ExtractPath).Any())
                {
                    try
                    {
                        WritetoUser($"Emptying {ExtractPath}");
                        var directory = new DirectoryInfo(ExtractPath);
                        foreach (var file in directory.GetFiles()) file.Delete();
                        foreach (var subDirectory in directory.GetDirectories()) subDirectory.Delete(true);
                    }
                    catch (Exception ex)
                    {
                        ThrowError($"Unable to empty {ExtractPath}, please empty it, {ex.Message}", ex);
                    }
                }
            }
            else
            {
                try
                {
                    Directory.CreateDirectory(ExtractPath);
                }
                catch (Exception ex)
                {
                    ThrowError($"Unable to create {ExtractPath}, please create it, {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        ///     Downloads the WordPress latest zip file
        /// </summary>
        /// <returns>downlaodPath</returns>
        private static string Download()
        {
            var downloadPath = Path.Combine(ExtractPath, ZipFileName);
            try
            {
                using (var client = new WebClient())
                {
                    WritetoUser($"Downloading {LatestUrl} to {downloadPath}");
                    client.DownloadFile(LatestUrl, downloadPath);
                }
            }
            catch (Exception ex)
            {
                ThrowError($"Unable to download WordPress Latest File {ex.Message}", ex);
            }
            return downloadPath;
        }

        /// <summary>
        ///     Extracts the passed zipFile to the ExtractPath
        /// </summary>
        /// <param name="zipFile">zip file path and name</param>
        private static string ExtractZip(string zipFile)
        {
            try
            {
                WritetoUser($"Extracting {zipFile} to {ExtractPath}");
                ZipFile.ExtractToDirectory(zipFile, ExtractPath);
            }
            catch (Exception ex)
            {
                ThrowError($"Unable to extract {zipFile} to {ExtractPath}, {ex.Message}", ex);
            }
            return Path.Combine(ExtractPath, "wordpress");
        }

        /// <summary>
        ///     Processes the WordPress upgrade for all of the sites
        /// </summary>
        /// <param name="extractPath">The folder path the WordPress install files are at</param>
        /// <param name="sites">The list of sites to run for</param>
        private static void ProcessSites(string extractPath, List<WordPressSite> sites)
        {
            foreach (var site in sites)
            {
                try
                {
                    WritetoUser($"Copying {extractPath} to {site.FolderPath}");
                    var diSource = new DirectoryInfo(extractPath);
                    var diTarget = new DirectoryInfo(site.FolderPath);

                    CopyAll(diSource, diTarget);
                }
                catch (Exception ex)
                {
                    WritetoUser($"Unable to copy {extractPath} to {site.FolderPath}, {ex.Message}", ex);
#if DEBUG
                    Console.WriteLine("Press any key to continue");
                    Console.Write("");
                    Console.ReadKey();
#endif
                }

                try
                {
                    WritetoUser($"Upgrading database for {site.Url}");
                    UpgradeDatabase(site.Url);
                }
                catch (Exception ex)
                {
                    WritetoUser($"Unable to upgrade database for {site.Url}, {ex.Message}", ex);
#if DEBUG
                    Console.WriteLine("Press any key to continue");
                    Console.Write("");
                    Console.ReadKey();
#endif
                }
            }
        }

        /// <summary>
        ///     Checks if site is using WordPress
        /// </summary>
        /// <param name="siteUrl">The URL to the site with a trailing /</param>
        /// <returns>bool isWordPressSite</returns>
        private static bool IsWordPressSite(string siteUrl)
        {
            siteUrl = siteUrl + WpCheckUrl;

            bool isWordPressSite;
            var request = WebRequest.Create(siteUrl);
            request.Timeout = 60000;

            try
            {
                var response = request.GetResponse();
                isWordPressSite = true;
                response.Close();
            }
            catch (Exception ex)
            {
                Log.Debug("{siteUrl} is not a WordPress site, or has an error {@ex}", siteUrl, ex);
                isWordPressSite = false;
            }
            return isWordPressSite;
        }

        /// <summary>
        ///     Copies all from one directory to another
        /// </summary>
        /// <param name="source">Source Directory</param>
        /// <param name="target">Target Directory</param>
        public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (var fi in source.GetFiles())
            {
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (var diSourceSubDir in source.GetDirectories())
            {
                var nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }

        /// <summary>
        /// Writes the message to the console and to the log file
        /// </summary>
        /// <param name="message"></param>
        private static void WritetoUser(string message)
        {
            Console.WriteLine(message);
            Log.Information(message);
        }

        /// <summary>
        /// Writes the message to the console and details to the log file as a warning
        /// </summary>
        /// <param name="message">The message to log and write to the user</param>
        /// <param name="details">The details to log</param>
        private static void WritetoUser(string message, object details)
        {
            Console.WriteLine(message);
            Log.Warning("{message} Details: {@details}", message, details);
        }

        /// <summary>
        ///     Does a WebRequest to upgrade the database for the site
        /// </summary>
        /// <param name="siteUrl">Url of the site to check with a trailing slash</param>
        private static void UpgradeDatabase(string siteUrl)
        {
            var dbSiteUrl = siteUrl + DbUpdateUrl;

            var request = WebRequest.Create(dbSiteUrl);
            request.Timeout = 300000;

            try
            {
                var response = request.GetResponse();
                response.Close();
            }
            catch (Exception ex)
            {
                WritetoUser($"Unable to upgrade database for {siteUrl}, {ex.Message}", ex);
#if DEBUG
                Console.WriteLine("Press any key to continue");
                Console.Write("");
                Console.ReadKey();
#endif
            }
        }

        /// <summary>
        ///     Writes the message to the console, and throws an exception.
        /// </summary>
        /// <param name="message"></param>
        private static void ThrowError(string message)
        {
            Console.WriteLine($"Error: {message}");
            Log.Error("Error: {message}", message);
            Console.WriteLine($"Program Exiting");
#if DEBUG
            Console.WriteLine("Press any key to exit.");
            Console.Write("");
            Console.ReadKey();
#endif
            Environment.Exit(1);
        }

        /// <summary>
        ///     Writes the message to the console, and throws an exception.
        /// </summary>
        /// <param name="message">The message to display</param>
        /// <param name="details">An object to log out</param>
        private static void ThrowError(string message, object details)
        {
            Console.WriteLine($"Error: {message}");
            Log.Error("Error: {message} Details: {@details}", message, details);
            Console.WriteLine($"Program Exiting");
#if DEBUG
            Console.WriteLine("Press any key to exit.");
            Console.Write("");
            Console.ReadKey();
#endif
            Environment.Exit(1);
        }
    }
}