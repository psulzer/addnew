// This file is part of addnew, Copyright © 2015 Peter Sulzer
// for license information see end of this file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;


namespace addnew {

class AddMissingFiles {
    commandLineResult cmdrslt;
    int filesCopied;
    int dirsProcessed;
    int dirsCreated;
    public static string[] ERRMSG = {
        // WARNING: Whenever you append another error message you must also append
        // an error type to the enum ERR below (it would be better to use a struct
        // with int and string, but then we must implement System.Collection.Ienumerable)
        // so that we can initialise it with = { new {0,"no error}, ... }
         "no error"         // 0
        ,"file not found"   // 1
        ,"no permission"    // 2
        ,"invalid pathname" // 3
        ,"problem with pathname (e. g. does not exist)" // 5
        ,"this is a directory in source, but a file in destination" // 6
        ,"creating directory in destination failed" // 7
        ,"unexpected error destination directory does not exist" // 8
        ,"unexpected error at FileInfo()" // 9
        ,"unexpected error at File.Copy()" // 10
        ,"unexpected error at Directory.GetFileSystemEntries()" // 11
    };
    public enum ERR {
         NO_ERROR
        ,SOURCE_DIR_NOT_FOUND
        ,NO_PERMISSION
        ,INVALID_PATHNAME
        ,PROBLEM_WITH_PATH
        ,DIR_IN_SOURCE_FILE_IN_DEST
        ,CREATE_DEST_DIR_FAILED
        ,UNEXPECTED_DEST_DIR_NOT_FOUND
        ,UNEXPECTED_ERR_FILE_INFO
        ,UNEXPECTED_ERR_FILE_COPY
        ,UNEXPECTED_ERR_GETFILESYSTEMENTRIES
    };

    public AddMissingFiles(commandLineResult crslt) {
        cmdrslt=crslt;
        filesCopied=0;
        dirsProcessed=0;
        dirsCreated=0;
    }

    public int FilesCopied { get { return filesCopied; } }
    public int DirsProcessed { get { return dirsProcessed; } }
    public int DirsCreated { get { return dirsCreated; } }

    public int addFiles(string[] de) { // recursive function!
        // Console.WriteLine("Now the files will be added, once the program is finished\n");
        int rslt=0;
        string currd=cmdrslt.currd; // we need a local copy because of this function is recursive!
        foreach (string path in de) {
            cmdrslt.curr=path;
            if (cmdrslt.debug >= 1)
                Console.WriteLine("In addFiles(string[] de) in foreach-loop after cmdrslt.curr=path; path=\n{0}",
                        cmdrslt.curr);
            if (Directory.Exists(cmdrslt.curr)) {
                if (!cmdrslt.recurse) // -recursesubdirs not given
                    continue; // so ignore directory entries
                //cmdrslt.curr=Path.GetDirectoryName(cmdrslt.curr)+@"\"+
                //        Path.GetFileName(cmdrslt.curr);
                if (cmdrslt.currd == "" || cmdrslt.currd == @"\" ||
                        cmdrslt.currd.Substring(cmdrslt.currd.Length-1) == ":")
                    cmdrslt.currd+=Path.GetFileName(cmdrslt.curr);
                else {
                    if (cmdrslt.debug >= 1)
                        Console.WriteLine("In addFiles(string[] de) before cmdrrslt.currd+="+
                            "@\"\\\"+..., cmdrslt.currd:\n{0}",cmdrslt.currd);
                    cmdrslt.currd+=@"\"+Path.GetFileName(cmdrslt.curr); // H E R E  may be a problem
                }
                // Console.WriteLine("-----------{0}",Directory.GetCurrentDirectory());
                if (!Directory.Exists(cmdrslt.currd)) {
                    if (File.Exists(cmdrslt.currd))
                        return -(int)ERR.DIR_IN_SOURCE_FILE_IN_DEST;
                    if (cmdrslt.hot) {
                        try {
                            Directory.CreateDirectory(cmdrslt.currd);
                        } catch {
                            return -(int)ERR.CREATE_DEST_DIR_FAILED;
                        }
                    }
                    dirsCreated++;
                }
                string[] de2;
                try {
                    de2=Directory.GetFileSystemEntries(cmdrslt.curr);
                } catch {
                    return (int)ERR.UNEXPECTED_ERR_GETFILESYSTEMENTRIES;
                }
                dirsProcessed++;
                if (de2.Length > 0) {
                    rslt=addFiles(de2);
                    // cmdrslt.currd=currd; // restore old destination
                    if (rslt < 0)
                        return rslt;
                }
                cmdrslt.currd=currd; // restore old destination
                continue;
            }
            if (!File.Exists(cmdrslt.curr)) { // if error
                return -(int)ERR.PROBLEM_WITH_PATH; // error messages must be negative
            }
            // Now we have found a file. Next we must check if if
            // the file exist in destination directory.
            // If it exists and it is older print a message.
            // If it doesn't exist, copy the file

            string destpath="";
            if (cmdrslt.currd == "" || cmdrslt.currd == @"\" ||
                    (cmdrslt.currd.Substring(cmdrslt.currd.Length-1) == ":" && cmdrslt.currd.Length == 2))
                destpath=cmdrslt.currd+Path.GetFileName(cmdrslt.curr);
            else
                destpath+=cmdrslt.currd+@"\"+Path.GetFileName(cmdrslt.curr);
            if (cmdrslt.debug >= 1)
                Console.WriteLine("In addFiles(string[] de) before GetFullPath(destpath), destpath:\n{0}",
                        destpath);
            destpath=Path.GetDirectoryName(Path.GetFullPath(destpath));
            if (!Directory.Exists(destpath)) {
                // A   T R Y   at Tue 08 Sep 2015 (seems to work - but just 1 test):
                if (cmdrslt.hot) {
                    Console.WriteLine("Error {0}: \"{1}\" {2}",(int)ERR.UNEXPECTED_DEST_DIR_NOT_FOUND,
                        cmdrslt.currd,ERRMSG[(int)ERR.UNEXPECTED_DEST_DIR_NOT_FOUND]);
                    return -(int)ERR.UNEXPECTED_DEST_DIR_NOT_FOUND; // WARNING: This return NOT TESTED (difficult)
                }
            }
            else  {

              // H I E R   W U R D E   E T W A S   N I C H T   D U R C H D A C H T
              // Warum ??? Ist mir nicht mehr klar. Evtl. hinfällig dank Einführung der automatischen
              // Variablen curr und currd in addFiles() (diese sind lokal zu jeder rekursiven Instanz
              // von addFile(), sind nach der Rückkehr also wieder auf dem alten Wert und cmdrslt.currd
              // wird unmittelbar nach dem rekursiven Aufruf von addFiles() wieder auf currd gesetzt.
              // Diesen Kommentar löschen, wenn ausgetestet.

                string f=destpath+"\\"+Path.GetFileName(cmdrslt.curr);
                if (!File.Exists(f)) {
                    // --------------
                    // File does NOT exist in destination directory, so copy it:
                    // --------------
                    // Console.WriteLine("Processing {0}\n  Dest-Dir.: {1}",
                    //        cmdrslt.curr,destpath);
                    if (cmdrslt.hot) {
                        try {
                            // To test the "catch" below uncomment the following line:
                            // throw new UnauthorizedAccessException();
                            File.Copy(cmdrslt.curr,f,true); // Because of "true" this is not Multitasking safe!
                        } catch (Exception e) {
                            cmdrslt.except=e;
                            return -(int)ERR.UNEXPECTED_ERR_FILE_COPY;
                        }
                    }
                    else {
                        if (!cmdrslt.dontprintcopied)
                            Console.WriteLine("\"{0}\" will be copied to \"{1}\" with -HOT",
                                cmdrslt.curr,destpath+@"\");
                    }
                    filesCopied++;
                }
                else {                      // file exists in destination directory:
                    // ----------------
                    //File exists in destination directory, so look what to do:
                    // -----------------
                    FileInfo fis,fid;
                    try {
                        // To test the "catch" below uncomment the following line:
                        // throw new UnauthorizedAccessException();
                        fid=new FileInfo(f);
                        fis=new FileInfo(cmdrslt.curr);
                    } catch (Exception e) {
                        cmdrslt.except=e;
                        return -(int)ERR.UNEXPECTED_ERR_FILE_INFO;
                    }
// 2017-03-03:      // added: use .LastWriteTime if it is supported, else use CreationTime:
                    bool lastWrite = true; // assume .LastWriteTime matches. Needed if
                                           // .LastWriteTime is not supported by file system
                    try {
                        if (fid.LastWriteTime != null) // if .LastWriteTime is supported
                            lastWrite = (fid.LastWriteTime != fis.LastWriteTime); // set if
                                // .LastWriteTime of files is equal or not
                        else // when .LastWriteTime is not supported, use .CreationTime
                            lastWrite = (fid.CreationTime != fis.CreationTime);
                    }
                    catch {
                        Console.WriteLine(
                            "Warning \"{0}\": not copied(!), file date cannot not be compared",fis);
                    }
                    try {
                        if (lastWrite || fid.Length != fis.Length) {
                            Console.WriteLine("\"{0}\" not copied(!), but date or length differ",fis);
                        }
                    }
                    catch {
                        Console.WriteLine(
                            "Warning \"{0}\": not copied(!), file length cannot not be compared",fis);
                    }
                }
            }
        } // foreach (string path in de) {
        return 0;
    }

    int getDirEntries(string[] de) {
        try {
            // Obtain the file system entries in the directory path.
            de=Directory.GetFileSystemEntries(cmdrslt.src+@"\"); 
        }
        catch (ArgumentNullException) {
            // System.Console.WriteLine("Path is a null reference.");
            return (int)ERR.SOURCE_DIR_NOT_FOUND;
        }
        catch (System.Security.SecurityException) {
            // System.Console.WriteLine("The caller does not have the " +
            //    "required permission.");
            return (int)ERR.NO_PERMISSION;
        }
        catch (ArgumentException) {
            // System.Console.WriteLine("Path is an empty string, " +
            //    "contains only white spaces, " + 
            //    "or contains invalid characters.");
            return (int)ERR.INVALID_PATHNAME;
        }
        catch (System.IO.DirectoryNotFoundException) {
            // System.Console.WriteLine("The path encapsulated in the " + 
            //    "Directory object does not exist.");
            return (int)ERR.PROBLEM_WITH_PATH;
        }
        return 0;
}

} // class AddMissingFiles

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
