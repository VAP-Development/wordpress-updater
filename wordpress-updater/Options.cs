using System.Collections.Generic;
using CommandLine;

namespace vap.development.wordpress_updater
{
    internal class Options
    {
        [Option('f', "folder", Required = true, HelpText = "Folder to search for WordPress installs under")]
        public string SearchFolder { get; set; }

        [Option('e', "extract", Required = true, HelpText = "Path to download and extract WordPress zip file to")]
        public string ExtractPath { get; set; }

        [Option('n', "name", Default = "latest.zip", HelpText = "File name to save the LatestUrl as")]
        public string ZipFileName { get; set; }

        [Option('l', "latest", Default = "https://wordpress.org/latest.zip",
            HelpText = "URL to WordPress latest zip file")]
        public string LatestUrl { get; set; }

        [Option('c', "check", Default = "wp-login.php",
            HelpText = "URL to to add to website URL to check to see if WordPress is used no beginning /")]
        public string WpCheckUrl { get; set; }

        [Option('d', "db", Default = "wp-admin/upgrade.php?step=1&backto=%2Fwp-admin%2F",
            HelpText = "URL to to add to website URL to update the database no beginning /")]
        public string DbUpdateUrl { get; set; }

        [Option('s', "search", Default = "wp-config.php", HelpText = "File to search for to find WordPress install path"
            )]
        public string SearchFile { get; set; }

        [Option('w', "web", Default = "\\wwwroot\\", HelpText = "File to search for to find WordPress install path")]
        public string WebUrlLocation { get; set; }

        [Option('p', "paths", Separator = ',', HelpText = "Comma seperated list of site paths, bypasses site search")]
        public IEnumerable<string> SiteFolderPaths { get; set; }

        [Option('u', "urls", Separator = ',',
            HelpText = "Comma seperates list of site URLs with trailing /, bypasses site search")]
        public IEnumerable<string> SiteUrls { get; set; }
    }
}