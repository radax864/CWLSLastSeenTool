# Linkshell Tools

Tools to make managing Linkshells and Cross-world Linkshells easier.

## Basic Info

Formerly CWLSLastSeenTool. This is a set of tools to make keeping track of Cross-world and Home World Linkshell members easier. The default game UI for Cross-world Linkshells and Home World Linkshells provides no information for when someone was last online. So instead of manually keeping track of it in a spreadsheets this tool quickly marks it all down for you.

These tools requires manually opening the CWLS/Linkshell UI and waiting for the member list to load before caching the names into it.

## How To

1. `/shelltools` will open the main window. This displays the current lists of members recorded and the last date they were cached.
2. Open your CWLS/Linkshell UI to the CWLS/Linkshell you wish to record and wait for the members to finish loading.
3. Push `Cache Members` on the corresponding tab in the main window.
4. You now have a record of who was online at this time and date in your CWLS/Linkshell.

The tool records the character name, world name, online status, date and time and CWLS/Linkshell name as a CSV (TSV format) string within its config json.

If the character name is new (this also applies to characters that have server transferred) the tool will add them to its list. If the new character is offline at the time of recording they will be recorded with the current date but labelled as never having been seen online.

Individual members can be removed from this list by holding ctrl and clicking `Remove` from the table in the tools main window.

Within the plugin settings you can backup your lists to a separate string in the plugin json, restore this backup, or clear your whole list.

![Image](Images/icon.png)
