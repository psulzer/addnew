// This file is part of addnew, Copyright © 2015 Peter Sulzer
// for license information see end of this file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace addnew {

class commandLineResult {
    public static string hotOpt="HOT"; // Sets hot true, upper case, so that we don't set it by accident
    public static string dontprintcopiedOpt="dontprintcopied"; // Set dontprintcopied below to true
    public static string recurseOpt="recursesubdirs"; // recurse into subdirectories
    public static string copyrightOpt="copyright"; // show copyright/license information
    public static string debugOpt="debug"; // print debugging informations
    public int err; // returns an error number or 0 if no error
    public int ndx; // returns current index of command line arguments
    public int argc; // number of arguments (if errorr no. of argument where error occured)
    public int optc; // number of options (if error no. of option where error occured)
    public string src // source directory (may optionaly preceed filepattern below)
        ,filepattern; // filename pattern, may contain wildcards (required, part after last '\' of full pathname)
    // all other command arguments are optional
    public string dst; // destination directory
    public bool hot=false; // option hot - WARNING: When true (set) program is HOT and copies files
    public bool dontprintcopied=false; // option -dontprintcopied, won't print files which would be copied
    public bool recurse=false; // option -recursesubdirs - recursively act on all subdirectoriese
    public bool copyright=false; // if true print copyright and license information for addnew
    public int debug=0; // if debug > 0 print debugging informations
    public string curr; // current pathname (source)
    public string currd; // current pathname (destination)
    public Exception except; // last exception (which led to error)
    public int fileno,dirno,dircrea;

    public commandLineResult() {
        err=0; argc=0; optc=0; src=""; filepattern=""; dst="";
        curr=""; currd=""; fileno=0; dirno=0; dircrea=0;
    }
};


class Program {
    const int
         ERR_NOT_FOUND=6
        ,ERR_SYNTAX=7
        ,ERR_BAD_PARAMETER=12
        ,ERR_NOT_IMPLEMENTED=17
        ,ERR_DEST_IN_SOURCE=101
        ,ERR_NO_SUCH_OPT=102
        ;
    const string VERSION="1.8";

    static int Main(string[] args) {
    // return 1: Called with bad parameter (arguments)
    // return 2: Source or destination directory does not exist
    Program an=new Program(); // an stands for addnew (name of the program)

    commandLineResult cmdrslt=an.getCommandLineArguments(args);
    if (cmdrslt.copyright)
        an.printCopy();
    switch (cmdrslt.err) {
    case 0: // no error
        break;
    case ERR_NO_SUCH_OPT:
        Console.WriteLine("Command line argument[{0}] \"{1}: No such option",cmdrslt.ndx+1,args[cmdrslt.ndx]);
        return 1;
    default:
        Console.WriteLine("error when processing commandline, error number={0}\n",cmdrslt.err);
        return 1;
    }
    if (cmdrslt.filepattern == "" || cmdrslt.filepattern == null || cmdrslt.filepattern == string.Empty) {
        Console.WriteLine("addnew - copyright © 2015 Peter Sulzer\n");
        Console.WriteLine("addnew V{0}\nUsage: addnew [-Option ...] filename [destination_directory]",VERSION);
        Console.WriteLine(" Options:");
        Console.WriteLine("  -HOT         create/copy directories/files, without this option\n"+
                          "               program just prints what it would be doing with -HOT");
        Console.WriteLine("  -r[ecursesubdirs] recurse into subdirectories");
        Console.WriteLine("  -d[ontprintcopied] don't print out the files which\n"+
                          "               would be copied (without -HOT)");
        Console.WriteLine("  -c[opyright] show copyright and license information for this program");
        Console.WriteLine("  -de[bug]     print debugging information (for development)");
        return 1;
    }
    cmdrslt.src=cmdrslt.src.Replace('/','\\'); // addnew supports also '/' as directory separator
    if (cmdrslt.dst.Length > 0)
        cmdrslt.dst=cmdrslt.dst.Replace('/','\\'); // addnew supports also '/' as directory separator
    // Comment following statement after testing:
    // Console.WriteLine("err={0}\nargc={1}\noptc={2}\nsrc=\"{3}\"\nfilepattern=\"{4}\"\ndst=\"{5}\"\n"+
    //    "opt=\"n{0}\"\ncurr=\"{6}\"\ncurrd=\"{7}\"",
    //    cmdrslt.err,cmdrslt.argc,cmdrslt.optc,cmdrslt.src,cmdrslt.filepattern,cmdrslt.dst,
    //    cmdrslt.hot,cmdrslt.curr,cmdrslt.currd);

    if (!Directory.Exists(cmdrslt.src)) {
        Console.WriteLine("Source directory \"{0}\" does not exist",cmdrslt.src);
        return 2;
    }
    if (cmdrslt.dst != "" && !Directory.Exists(cmdrslt.dst)) {
        Console.WriteLine("Destination Directory \"n{0}\" does not exist",cmdrslt.dst);
        return 2;
    }
    string fpd=".";
    if (cmdrslt.dst != "")
        fpd=Path.GetFullPath(cmdrslt.dst); //                                         |
    if (Path.GetFullPath(fpd).StartsWith(Path.GetFullPath(cmdrslt.src))) { //<---+
        Console.WriteLine("Error {0}: Destination directory is contained in source directory.\n"+
                "Recursion is not allowed (results in endless loop)",ERR_DEST_IN_SOURCE);
        return 2;
    }

    // cmdrslt.curr=cmdrslt.src;
    if (cmdrslt.dst.Length > 1 && cmdrslt.dst.Substring(cmdrslt.dst.Length-1) == @"\")
        cmdrslt.currd=cmdrslt.dst.Substring(0,cmdrslt.dst.Length-1);
    else
        cmdrslt.currd=cmdrslt.dst;
    AddMissingFiles amf=new AddMissingFiles(cmdrslt);
    string[] de=Directory.GetFileSystemEntries(cmdrslt.src,cmdrslt.filepattern);
    if (de.Length > 0) {

        // Now copy (add) new files (create new directories) recursively:
        int rslt=amf.addFiles(de);

        if (rslt < 0) {
            switch (rslt) {
            case (-(int)AddMissingFiles.ERR.UNEXPECTED_ERR_FILE_INFO):
                Console.WriteLine("Error {0}: \"{1}\" at file \"{2}\"\nException: {3}",
                    AddMissingFiles.ERR.UNEXPECTED_ERR_FILE_INFO,cmdrslt.curr,cmdrslt.except);
                break;
            case (-(int)AddMissingFiles.ERR.UNEXPECTED_ERR_FILE_COPY):
                Console.WriteLine("Error {0}: \"{1}\" at \"{2}\"\nException: {3}",
                    -rslt,AddMissingFiles.ERRMSG[-rslt],cmdrslt.curr,cmdrslt.except);
                break;
            default:
                Console.WriteLine("Error {0}: \"{1}\" {2}",
                        -rslt,cmdrslt.curr,AddMissingFiles.ERRMSG[-rslt]);
                break;
            }
        }

        if (!cmdrslt.hot)
            Console.WriteLine(
                "\nWith option \"-HOT\" following no. of directories/files will be created/copied:");
        else
            Console.WriteLine();
        Console.WriteLine("{0} directories processed (searched) - {1} directories created",
                amf.DirsProcessed,amf.DirsCreated);
        Console.WriteLine("{0} files copied",amf.FilesCopied);
        if (rslt < 0)
            return 3;
        if (de.Length>0) return 0; // The if (...) just added to supress compiler warning
    }
    else {
        Console.WriteLine("No files/directories found in \"{0}\"",cmdrslt.src);
        return 0; // This is no error, all is correct, but there are no files to copy
    }
    return 0;
} // Main()


public commandLineResult getCommandLineArguments(string[] args) {
    commandLineResult cmdrslt=new commandLineResult();
    cmdrslt.argc=0; // argument counter (i. e. all arguments without leading "-", which are options)
    cmdrslt.optc=0; // option counter, counts number of options
    // bool isopt=false; // true if current argument is an optioon
    int argsSize=args.Length;
    for (cmdrslt.ndx=0; cmdrslt.ndx < argsSize; cmdrslt.ndx++) {
        if (args[cmdrslt.ndx].StartsWith("-")) {
            // isopt=true;
            ++cmdrslt.optc;
            cmdrslt.ndx=clrHandleOption(cmdrslt,args,cmdrslt.ndx);
            if (cmdrslt.err != 0)
                return cmdrslt;
        }
        else {
            // isopt=false;
            ++cmdrslt.argc;
            cmdrslt.ndx=clrHandleArgument(cmdrslt,args,cmdrslt.ndx);
            if (cmdrslt.err != 0)
                return cmdrslt;
        }
    }
    return cmdrslt;
}


int clrHandleOption(commandLineResult cmdrslt,string[] args,int i) {
    // cmdrslt.err=ERR_NOT_IMPLEMENTED;
    string opt=args[i].Substring(1);
    // switch (opt) { // possible in C#...
    // case "HOT":   // ...but not in C/C++ so for portability used if()...else if() ...else()
    if (opt == commandLineResult.hotOpt) {
            cmdrslt.hot=true;
    }
    // Handle options starting with -d
        // options may be abreviated e. g. -d instead of -dont... (don't forget to adapt the usage message!)
        // currently there is only one option starting with 'd# so we must just test if
        // the option starting with d is contained in dontprintcopiedOpt:
    else if (opt.StartsWith("d")) {
        if (commandLineResult.dontprintcopiedOpt.StartsWith(opt))
            cmdrslt.dontprintcopied=true;
        else if (commandLineResult.debugOpt.StartsWith(opt))
            cmdrslt.debug=1;
    }
    else if (commandLineResult.recurseOpt.StartsWith(opt)) {
        cmdrslt.recurse=true;
    }
    else if (commandLineResult.copyrightOpt.StartsWith(opt)) {
        cmdrslt.copyright=true;
    }
    else {
        cmdrslt.err=ERR_NO_SUCH_OPT;
    }
    return i;
}


int clrHandleArgument(commandLineResult cmdrslt,string[] args,int i) {
    string s="";
    switch (cmdrslt.argc) {
    case 1:
        s=Path.GetDirectoryName(args[i]);
        if (s != null && s != string.Empty)
            cmdrslt.src=s;
        s=Path.GetFileName(args[i]);
        if (s != null)
            cmdrslt.filepattern=s;
        else {
            cmdrslt.err=ERR_BAD_PARAMETER;
            return i;
        }
        return i;
    case 2:
        cmdrslt.dst=args[i]; // for destination only a directory is allowed
        return i;
    default:
        cmdrslt.err=ERR_BAD_PARAMETER;
        break;
    }
    return i;
}

void printCopy() {
    Console.WriteLine("addnew - add new files from one directory to another\n\n"+
                      "Copyright © 2015 Peter Sulzer\n");
    Console.WriteLine("This program is free software: you can redistribute it and/or modify\n"+
                      "it under the terms of the GNU General Public License as published by\n"+
                      "the Free Software Foundation, either version 1 of the License, or\n"+
                      "(at your option) any later version.\n");
    Console.WriteLine("This program is distributed in the hope that it will be useful,\n"+
                      "but WITHOUT ANY WARRANTY; without even the implied warranty of\n"+
                      "MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the\n"+
                      "GNU General Public License for more details.\n");
    Console.WriteLine("You should have received a copy of the GNU General Public License\n"+
                      "along with this program.  If not, see <http://www.gnu.org/licenses/>.\n");
}

} // class Program
} // namespace addnew

//This file is part of addnew - add new files from one directory to another

//Copyright © 2015 Peter Sulzer

//"addnew" is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 1 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program.  If not, see <http://www.gnu.org/licenses/>.
