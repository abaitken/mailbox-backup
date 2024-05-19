# Mailbox Backup Utility

A small utility that will download cloud hosted e-mails for backup purposes.

## Features

- Download e-mails to local file system
- Organise remote e-mails into dated folders

## Usage

```
Mailbox Backup
  Download remote mail items to local filesystem

 -h                        (Optional) Display this help
                           (Other forms: -? --help)
                           Configuration key: HELP

 -c FILE                   (Optional) Configuration file
                           Loads configuration file when encountered and inserts arguments into the queue. Subsequent a
                           rguments will override previous values.
                           (Other forms: --config)
                           Configuration key: CONFIG

 -u TEXT                   Account username
                           (Other forms: --username)
                           Configuration key: USER

 -p TEXT                   Account password
                           Depends on -u
                           (Other forms: --password)
                           Configuration key: PASS

 -s TEXT                   Server address
                           (Other forms: --server)
                           Configuration key: SERVER

 --port ###                (Optional) Server port
                           Default value: 993
                           Configuration key: SERVER_PORT

 -o TEXT                   Output directory
                           (Other forms: --outdir)
                           Configuration key: OUTPUTDIR

 -if TEXT                  (Optional) Include folder regex
                           When supplied, only remote folder names matching the pattern will be downloaded. (Otherwise
                           all folders will be downloaded)
                           Configuration key: FOLDER_INC

 -xf TEXT                  (Optional) Exclude folder regex
                           When supplied, remote folders matching the pattern will not be downloaded
                           Configuration key: FOLDER_EXC

 --nodl                    (Optional) Do not download
                           Configuration key: DOWNLOAD_NO

 --tlsmode OPTION          (Optional) TLS Options
                           Default value: SslOnConnect
                           Options: None Auto SslOnConnect StartTls StartTlsWhenAvailable
                           Configuration key: TLSMODE

 --remotemove              (Optional) Move and organise messages remotely on the server
                           Configuration key: REMOTE_MOVE

 -il TEXT                  (Optional) IMAP log
                           (Other forms: --imaplog)
                           Configuration key: IMAP_LOG

 --remotehome TEXT         (Optional) Remote home path for organised file structure
                           Configuration key: REMOTE_HOME

 --localorgstrat OPTION    (Optional) Local downloaded file organisation strategy
                           Default value: FollowFolderStructure
                           Options: FollowFolderStructure DatedFolderStructure
                           Configuration key: LOCALORGSTRAT

 --filterage ###           (Optional) Filter e-mails older than provided age (in days)
                           Default value: 1
                           Configuration key: FILTER_AGE
```

## Example configuration

Use the configuration key values from above as the property names for a JSON object.

```
{
    "HELP": false,
    "USER": "USERNAME HERE",
    "SERVER" : "server address here",
    "DOWNLOAD_NO": false,
    "OUTPUTDIR": "path\\to\\output\\directory"
}
```