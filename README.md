### Export a DataGate file to CSV with C#

This C# program exports a file to a CSV comma-separated text file. It  uses the [`DGFileReader`](https://github.com/ASNA/ASNA.DataGateHelper) class to read the file it exports. 

>[See the ASNA Visual RPG version of this example](https://github.com/ASNA/avr-version-of-export-dg-to-csv). 

>[See the repo that provides the DGFileReader ](https://github.com/ASNA/ASNA.DataGateHelper). This repo's README provides more technical detail.

Export a DataGate file to either a comma- or tab-separated file.
 
    Usage:
    exporttocsv <databaseName> <library> <file> 
                <outputPath> -noheadings -showprogress 
                -tabdelimiter -writeschemafile -blockingfactor nnn

    Required arguments--must be provided in the order shown
        <databaseName>......ASNA Database Name. If the name includes blanks surround it with double quotes.
        <library>...........Library name.
        <file>..............File name.

    Optional arguments
        <outputPath>........Path to which output files are written. If provided, this must be the fourth argument. The default output path is the current user's 'Documents' folder.

    Optional flags--flags can be provided in any order
        -help...............Show this help.
        -noheadings.........Do not include field names as first row.
        -showprogress.......Show progress as records are exported.
        -tabdelimiter.......Delimit fields with a tab character instead of a comma.
        -writeschemafile....Write schema file which shows column data types.
        -blockingfactor.....Record blocking factor. Setting this to a value 
        between 500-1000 _may_ help performance. A values higher than 1000 
        will likely impede performance. The default value is 500.

        Any other flags entered are ignored.

    Output file is written to the target folder in the format:
        <databaseName>-<library>-<file>.txt

    Schema file is written to the target folder in the format:
        <databaseName>-<library>-<file>.schema.txt

    In the output file, the Database Name has any special characters removed to 
    make it work as part of a Windows filename. For example, 
    `*PUBLIC/DG Net Local` gets translated to `public_dg_net_local` in the
     output file name.
