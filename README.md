# CWLSLastSeenTool

A tool to make keeping track of when CWLS members last logged in easier.

## Basic Info

Pretty much what it says on the can. This is a tool I made to make keeping track of CWLS members easier as space is limited and so is my memory for names. The default game UI for Cross-world Linkshells provides no information for when someone was last online. So instead of manually keeping track of it in a spreadsheet I wanted a tool to quickly mark it all down.

The plugin does not differentiate between multiple Cross-world Linkshells and will add all members seen to the same internal list. This plugin requires manually opening the CWLS UI and waiting for the member list to load before caching the names into the plugin.

## How To

1. `/cwlslst` will open the main window for the tool. This displays the current list of members recorded and the last date they were cached.
2. Open your CWLS UI to the CWLS you wish to record and wait for the members to finish loading.
3. Push `Cache CWLS Members` on the main window of the tool.
4. You now have a record of who was online at this time and date in your CWLS.

The tool records the character name, world ID, online status, date and time as a CSV string within its config json.

If the character name is new (this also applies to characters that have server transferred) the tool will add them to its list. If the new character is offline at the time of recording they will be recorded with the current date but labelled as never having been seen online.

Individual members can be removed from this list by holding ctrl and clicking `Remove` from the table in the tools main window.

Within the plugin settings you can backup your list CSV to a separate string in the plugin json, restore this backup, or clear your whole list.

There is also an option to display World Names in the list as World IDs instead.
