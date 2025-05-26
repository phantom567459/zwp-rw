//Author: phantom567459 (Dark_Phantom)
//Released 05/2025
//revisions kept in readme file 

using System.Reflection.PortableExecutable;
using System.Text;
using zwp_rw;

BinaryHelpers instance = new BinaryHelpers();

var ArgArray = new List<string>();
string extractedFolderName = "extract";
string outputFile = "namesStored.txt";

StringBuilder builder = new();
foreach (var arg in args)
{
    ArgArray.Add(arg.ToString());
    builder.AppendLine($"Argument={arg}");
}
#if (DEBUG)
Console.WriteLine(builder.ToString());
#endif

//ArgArray[0] currently is <option>
//ArgArray[1] currently is <input-file>
//ArgArray[2] currently is <output-file>, generally optional
if (ArgArray.Count > 1 && File.Exists(ArgArray[1]))
{
    string fnameWExt = ArgArray[1];
    string fnameWOExt = ArgArray[1].Substring(0, ArgArray[1].IndexOf("."));
    string fnameExt = ArgArray[1].Substring(ArgArray[1].IndexOf("."));

    if (ArgArray.Count > 2) 
    {
        outputFile = ArgArray[2];
    }

    if (ArgArray[0] == "l")
    {
        ZWPHeader header = new ZWPHeader();
        header = BinaryHelpers.GatherZWPHeaderInfo(fnameWExt);

#if (DEBUG)
        Console.WriteLine(header.versionNum.ToString());
        Console.WriteLine(header.fileSize.ToString());
        Console.WriteLine(header.fileCount.ToString());
        Console.WriteLine(header.dictionaryOffset.ToString());
#endif

        BinaryHelpers.WriteFileListFromZWPHeader(fnameWExt, header, outputFile);
    }
    else if (ArgArray[0] == "x")
    {
        ZWPHeader header = new ZWPHeader();
        header = BinaryHelpers.GatherZWPHeaderInfo(fnameWExt);

#if (DEBUG)
        Console.WriteLine(header.versionNum.ToString());
        Console.WriteLine(header.fileSize.ToString());
        Console.WriteLine(header.fileCount.ToString());
        Console.WriteLine(header.dictionaryOffset.ToString());
#endif

        BinaryHelpers.ExtractFilesFromZWP(fnameWExt, header, extractedFolderName);
    }
    else if (ArgArray[0] == "p")
    {
        BinaryHelpers.PackFilesIntoZWP(fnameWExt, extractedFolderName);
    }
    else if (ArgArray[0] == "d")
    {
        byte[] decompdfileContents = BinaryHelpers.Decompress(fnameWExt);
        string newDecompdFile = fnameWOExt + "dec" + fnameExt;
        File.WriteAllBytes(newDecompdFile, decompdfileContents);
        Console.WriteLine("New File Written");
    }
    else if (ArgArray[0] == "c" || ArgArray[0] == "cr")
    {
        byte[] compdfileContents = BinaryHelpers.CompressZlibCompatible(fnameWExt);
    
        if (ArgArray[0] == "cr")
        {
            string replacementFile = extractedFolderName + "\\" + fnameWExt;
            File.WriteAllBytes(replacementFile, compdfileContents);
        }
        else
        {
            string newCompdFile = fnameWOExt + "cmp" + fnameExt;
            File.WriteAllBytes(newCompdFile, compdfileContents);
        }
        Console.WriteLine("New File Written");
    }
    else
    {
        Console.WriteLine("Please enter a valid argument");
    }

}
else
{
    Console.WriteLine(@"Please run the program like this:
<program-name> <option> <input-file>

Options:
p --> pack file list into a new pack
x --> extract files from existing pack
l --> list files in existing pack (can specify a file name after <input-file>
d --> decompress specific file
c --> compress specific file

Some of the commands have sub-commands associated with them:
cr --> compress and replace in extracted folder

Example: zwp-rw.exe p 'filelisting.txt' 'data.zwp'");
}
