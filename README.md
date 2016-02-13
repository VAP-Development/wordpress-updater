# wordpress-updater
A .NET app to update WordPress sites on a local server

##Usage

###Site Search
Searches the specified folder for WordPress installs and update them

``` wordpress-updater.exe -f C:\Sites -e C:\Temp\WordPress```

###Specified Sites
Will use the specified folder paths and URLs and update them

``` wordpress-updater.exe -f C:\Sites -e C:\Temp\WordPress -p C:\Sites\site1.com\wwwroot,C:\site2.com\wwwroot\WP -u http://site1.com/,http://site2.com/WP/```

###All Arguments

```
WordPress Updater 1.0.0.0
Apache License

  -f, --folder     Required. Folder to search for WordPress installs under

  -e, --extract    Required. Path to download and extract WordPress zip file to

  -n, --name       (Default: latest.zip) File name to save the LatestUrl as

  -l, --latest     (Default: https://wordpress.org/latest.zip) URL to WordPress
                   latest zip file

  -c, --check      (Default: wp-login.php) URL to to add to website URL to
                   check to see if WordPress is used no beginning /

  -d, --db         (Default: wp-admin/upgrade.php?step=1&backto=%2Fwp-admin%2F)
                   URL to to add to website URL to update the database no
                   beginning /

  -s, --search     (Default: wp-config.php) File to search for to find
                   WordPress install path

  -w, --web        (Default: \wwwroot\) File to search for to find WordPress
                   install path

  -p, --paths      Comma seperated list of site paths, bypasses site search

  -u, --urls       Comma seperates list of site URLs with trailing /, bypasses
                   site search

  --help           Display this help screen.

  --version        Display version information.
  ```
