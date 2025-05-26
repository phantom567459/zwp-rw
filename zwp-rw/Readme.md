Tool used to extract and build the data.zwp file from Star Wars: The Clone Wars (2002) by Pandemic Studios, along with various other useful features to help with modding that game.
Program is called zwp-rw, referencing the zwp file it modifies which was created from the original "zwagapack" which was the packer program referenced by the original game.

##Background
The PS2 and Xbox versions of the game rely on a data.zwp file to drive the content of the game (except for sounds and scripts).  Previously, aluigi at Zenhax had posted a script that allowed for extraction from the data.zwp but efforts to rebuild it other than just repacking it the same way seemed to have been futile.

After parsing the format and then pulling it back together to be one-to-one, every change I would make and then recompile would cause an error (in the case of the world files) as soon as I tried to load a mission, or do absolutely nothing (in the case of odf changes).  But I knew it had to be possible because in one of my failed attempts to rebuild the file I loaded a corrupted version of level 1 successfully until the game crashed mid mission.

I started digging into the GameCube version which was likely a semi-debug version of the game - it doesn't use Data.zwp, all the files are raw, and there's various files that are leftover from the build environment as well as a symbol table.  I loaded up the symbol table and tried to find common functions within the GameCube and Xbox version.  I noticed that the game was relentlessly making CRC checks on the files it would pull down from the ZWP file.  This seemed to lend to my theories that there was way more in the executable than we knew.  However, in testing, I managed to learn that the config.ini DOES in fact work (unlike a certain popular SW RPG on Xbox...), but the changes are extremely subtle. doCRC = 0 was my first test, but didn't seem to work.  After following the logic, I decided to turn off the option doBatch as well, despite not really knowing what it does.  After that, the custom ZWP would load.

After that it was fighting a little bit with creating a function to compress and decompress individual files at will, which currently will not give a byte-for-byte exact representation because Microsoft's compression algorithm is slightly different but still seems to work with TCW.  If you want a byte for byte representation, I recommend zlib-flate as a good ready-to-use program.

##Instructions
First, in your version of the game, go to the config.ini and set doBatch = 0 and add doCRC = 0.  I'm not sure if the 2nd is needed but it can't hurt.
Second, have this program in your possession.  Copy the data.zwp from your game into the same folder as the program.
Run these two commands via the command line:
>zwp-rw.exe x data.zwp
>zwp-rw.exe l data.zwp
So now you have an extracted file folder and a list of every file in it. (currently hardcoded to namesStored.txt)
You go into the folder and try to open something, like an odf.  Problem is, it's most likely compressed.
Take the file you wish to edit and put it in the main folder with zwp-rw.exe
Run this command:
>zwp-rw.exe d geonosis1.wld
This will create a decompiled version of Geonosis1.wld, which by the way is actually level 2.
Make your changes.  I'll give an example:
Change line 196 to this:
`Object("player", "rep_walk_assault_player")`
Save your file
Run this command:
>zwp-rw.exe cr geonosis1.wld
This will compress and replace geonosis1.wld in the extract folder
Run this command:
>zwp-rw.exe p namesStored.txt
This will compile the file into namesStored.txt.zwp
Copy this file into your game folder.
Back up the original data.zwp
Rename the new file to data.zwp
Copy to Xbox, boot up game, load into Mission 2, and you should be piloting the assault walker after the cutscene ends.

##What is supported
Replacing existing files
Creating new files (odf, model, and texture have been tested and added successfully - use the process above but make sure to add the file to namesStored.txt)
Modification of cfg files (allow modification of menus among other things)

##Future Improvements
Allow users more freedom to name finalized files within the program

##What this CANNOT do
This program will never be able to modify the script files for this game.  The scripts are hard-coded into the executable file for the game.  They are incredibly complex and thousands of lines in the symbol file. Unless there's some unknown potential of the "addon" folder, I really doubt we're getting totally new missions and going to have a difficult time modifying objectives.
Sounds.  Sounds in each version are a little bit different but the Xbox version just has them in plain wavs.  I wrote a program that secretly has support for PS2 sound extraction. But they don't get stored in the zwp so it's out of scope really.

##notes
Textures are DXT1.  Use TexConv.exe from directxtex library
Will need to add converter to strip DXT header and add XBT header
New meshes seem to work as is

##Credits
Dark_Phantom - creator
GTTeancum - for inspiring me to finally dig in and figure out the problem with this
aluigi - script for quickBMS was a great help
Pandemic Studios - SW:TCW and SWBF series
My family - for tolerating my obsessions when I get dug into a project like this

