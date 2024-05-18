# Mailbox Backup Utility

A small utility that will download cloud hosted e-mails for backup purposes.


## Usage

```
Mailbox Backup
  Download remote mail items to local filesystem

 -h          (Optional) Display this help
             (Other forms: -? --help)

 -u TEXT     Account username
             (Other forms: --username)

 -p TEXT     Account password
             Depends on -u
             (Other forms: --password)

 -s TEXT     Server address
             (Other forms: --server)

 -o TEXT     Output directory
             (Other forms: --outdir)

 -if TEXT    (Optional) Include folder regex
             When supplied, only remote folder names matching the pattern will be downloaded. (Otherwise all folders wi
             ll be downloaded)

 -xf TEXT    (Optional) Exclude folder regex
             When supplied, remote folders matching the pattern will not be downloaded
```
