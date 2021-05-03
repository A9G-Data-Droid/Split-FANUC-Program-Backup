# Split-FANUC-Program-Backup

When you run a backup of a FANUC CNC control it makes a file named `ALL-PROG.TXT` that contains all CNC programs in the control memory. 

This program splits a FANUC `ALL-PROG.TXT` program backup file in to individual files.

## Usage
At least one argument is required. Enter the full path of the file you would like to split.

### EXAMPLE

    Split-FANUC-Program-Backup.exe "C:\temp\ALL-PROG.TXT"
    
## Output
It will make a subfolder in the same location as the text file you passed in. This folder will be named like the file. e.g. `C:\temp\ALL-PROG\`.
All of the CNC programs found in the backup will be placed in this folder.
