addnew - a Windows command line tool to synchronise directories

addnew alone prints a short help text

New Version 1.8 (2017 Mar 03)
- When using on a windows pad for all existing (identical files) there was
  the message, that files differ in date or length. Corrected:
  * Now it is tested if .LastWriteTime is supported by File system, if
    not, .CreationTime is compared
  * A warning is printed if file attributes (date, length) cannot be
    retrieved

For copyright and license see directory div.

For a short description (sorry only in German) see my homepage:

http://peter-sulzer.bplaced.net and select "Downloads" in the
navigation bar.
[END OF FILE]
