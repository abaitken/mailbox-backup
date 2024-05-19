# Mailbox Backup Utility

A small utility that will download cloud hosted e-mails for backup purposes.


## Usage

```
Mailbox Backup
  Download remote mail items to local filesystem

 -h            (Optional) Display this help
               (Other forms: -? --help)
               Configuration key: HELP

 -c FILE       (Optional) Configuration file
               Loads configuration file when encountered and inserts arguments into the queue. Subsequent arguments will override previous values.
               (Other forms: --config)
               Configuration key: CONFIG

 -u TEXT       Account username
               (Other forms: --username)
               Configuration key: USER

 -p TEXT       Account password
               Depends on -u
               (Other forms: --password)
               Configuration key: PASS

 -s TEXT       Server address
               (Other forms: --server)
               Configuration key: SERVER

 --port ###    (Optional) Server port
               Default value: 993
               Configuration key: SERVER_PORT

 -o TEXT       Output directory
               (Other forms: --outdir)
               Configuration key: OUTPUTDIR

 -if TEXT      (Optional) Include folder regex
               When supplied, only remote folder names matching the pattern will be downloaded. (Otherwise all folders will be downloaded)
               Configuration key: FOLDER_INC

 -xf TEXT      (Optional) Exclude folder regex
               When supplied, remote folders matching the pattern will not be downloaded
               Configuration key: FOLDER_EXC

```
