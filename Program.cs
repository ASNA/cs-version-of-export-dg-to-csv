using System;
using System.Linq;
using System.Text;
using ASNA.DataGateHelper;
using System.Reflection;

namespace ExportDGFileToCSV
{
    class Program
    {
        static ConsoleColor OriginalForegroundColor = Console.ForegroundColor;

        enum ExitCode : int
        {
            Success = 0,
            Failure = 1
        }

        static int Main(string[] args)
        {
            int result;
            result = CodeRunner(args);

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();

            return result;
        }

        static int CodeRunner(string[] args)
        {
            const int DATABASENAME = 0;
            const int LIBRARYNAME = 1;
            const int FILENAME = 2;
            const int OUTPUT_PATH = 3;
            const int MIN_ARGS = 3;

            if (args.Length < MIN_ARGS || args.Contains("-help")) {
                showHelp();
                return (int)ExitCode.Failure;
            }

            ExporterArgs exportArgs = new ExporterArgs();
            exportArgs.DatabaseName = args[DATABASENAME];
            exportArgs.LibraryName = args[LIBRARYNAME];
            exportArgs.FileName = args[FILENAME];

            exportArgs.OutputDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            // Does args[OUTPUT_PATH] specifies an existing output directory. 
            if (args.Length > MIN_ARGS && !args[OUTPUT_PATH].StartsWith("-")) {
                if (args[OUTPUT_PATH].EndsWith(@"\")) {
                     args[OUTPUT_PATH] = Utils.RemoveLastCharacter(args[OUTPUT_PATH]);
                }

                if (System.IO.Directory.Exists(args[OUTPUT_PATH])) {
                    exportArgs.OutputDirectory = args[OUTPUT_PATH];
                }
                else {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("{0} output directory not found", args[OUTPUT_PATH]);
                    Console.ForegroundColor = OriginalForegroundColor;
                    return (int)ExitCode.Failure;
                }
            }

            exportArgs.IncludeHeadingFlag = Array.IndexOf(args, "-noheadings") == -1;
            exportArgs.ShowProgressFlag = Array.IndexOf(args, "-showprogress") > -1;
            exportArgs.TabDelimiterFlag = Array.IndexOf(args, "-tabdelimiter") > -1;
            exportArgs.WriteSchemaFileFlag = Array.IndexOf(args, "-writeschemafile") > -1;

            getFlagValue(args, exportArgs, "BlockingFactor", "-blockingfactor");  
            if (exportArgs.BlockingFactor != ExporterArgs.DEFAULT_BLOCKING_FACTOR)
            {
                Console.WriteLine("BlockingFactor overridden to {0}", exportArgs.BlockingFactor); 
            }

            Exporter export = new Exporter(exportArgs);

            try
            {
                int ElapsedMilliseconds = export.Run();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Time to export: {0:#,###}ms {1:#,##0}min", ElapsedMilliseconds, ElapsedMilliseconds / 60000);
                Console.ForegroundColor = OriginalForegroundColor;
                return (int)ExitCode.Success;
            }
            catch (System.Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ForegroundColor = OriginalForegroundColor;
                return (int)ExitCode.Failure;
            }
        }

        static void getFlagValue(string[] args, ExporterArgs exportArgs, string propertyName, string flagName)
        {
            if (Array.IndexOf(args, flagName) == -1) {
                return;
            }

            Type type = exportArgs.GetType();
            PropertyInfo prop = type.GetProperty(propertyName);
            
            int flagIndex = Array.IndexOf(args, flagName);
            if (flagIndex == args.Length - 1)
            {
                Console.WriteLine("The value for {0} was not provided", flagName);
                Environment.Exit((int)(ExitCode.Failure));
            }
            if (flagIndex > 0 && flagIndex <= args.Length - 1)
            {
                string flagValue = args[flagIndex + 1];
                if (flagValue.StartsWith("-"))
                {
                    Console.WriteLine("The value for {0} was not provided", flagName);
                    Environment.Exit((int)(ExitCode.Failure));
                }

                if (prop.PropertyType.Name == "Int32") {
                    if (flagValue.All(char.IsDigit))
                    {
                        prop.SetValue(exportArgs, Int32.Parse(flagValue), null);
                        return;    
                    }
                    else
                    {
                        Console.WriteLine("The value for {0} must be a number", flagName);
                        Environment.Exit((int)(ExitCode.Failure));
                    }
                }
                
                if (prop.PropertyType.Name == "String")
                {
                    prop.SetValue(exportArgs, flagValue, null);
                    return;
                }
            }
            else
            {
                Console.WriteLine("The value for {0} is not provided.", flagName);
                Environment.Exit((int)(ExitCode.Failure));
            }
        }

        static void showHelp()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Export a DataGate file to either a comma- or tab-separated file.");
            Console.WriteLine("");
            Console.WriteLine("Usage:");
            Console.WriteLine("   exporttocsv <databaseName> <library> <file> <outputPath> -noheadings -showprogress -tabdelimiter -writeschemafile");
            Console.WriteLine("");
            Console.WriteLine("Required arguments--must be provided in the order shown");
            Console.WriteLine("    <databaseName>......ASNA Database Name. If the name includes blanks surround it with double quotes.");
            Console.WriteLine("    <library>...........Library name.");
            Console.WriteLine("    <file>..............File name.");
            Console.WriteLine("");
            Console.WriteLine("Optional arguments");
            Console.WriteLine("    <outputPath>........Path to which output files are written. If provided, this must be the fourth");
            Console.WriteLine("                        argument. The default output path is the current user's 'Documents' folder.");
            Console.WriteLine("");
            Console.WriteLine("Optional flags--flags can be provided in any order");
            Console.WriteLine("    -help...............Show this help.");
            Console.WriteLine("    -noheadings.........Do not include field names as first row.");
            Console.WriteLine("    -showprogress.......Show progress as records are exported.");
            Console.WriteLine("    -tabdelimiter.......Delimit fields with a tab character instead of a comma.");
            Console.WriteLine("    -writeschemafile....Write schema file which shows column data types.");
            Console.WriteLine("    -blockingfactor.....Record blocking factor. Setting this to a value between 500-1000 _may_ help performance.");
            Console.WriteLine("                        A values higher than 1000 will likely impede performance. The default value is 500.");
            Console.WriteLine("");
            Console.WriteLine("Output file is written to the target folder in the format:");
            Console.WriteLine("    <databaseName>-<library>-<file>.txt");
            Console.WriteLine("");
            Console.WriteLine("Schema file is written to the target folder in the format:");
            Console.WriteLine("    <databaseName>-<library>-<file>.schema.txt");
            Console.WriteLine("");
            Console.WriteLine("In the output file, the Database Name has any special characters removed to make it work as part of a Windows filename.");
            Console.WriteLine("For example, '*PUBLIC/DG Net Local' gets translated to 'public_dg_net_local' in the output file name.");

            Console.ForegroundColor = OriginalForegroundColor;
        }
    }

    public class Exporter {
        ASNA.DataGateHelper.DGFileReader dgfr;
        ConsoleColor OriginalForeGroundColor = Console.ForegroundColor;

        ExporterArgs exportArgs;

        public Exporter(ExporterArgs exportArgs)
        {
            this.exportArgs = exportArgs;
            this.exportArgs.TransformExportArgs();
        }

        void restoreForegroundColor()
        {
            Console.ForegroundColor = this.OriginalForeGroundColor;
        }

        public int Run()
        {
            ASNA.DataGate.Client.AdgConnection apiDGDB = new ASNA.DataGate.Client.AdgConnection("*Public/DG NET Local");
            dgfr = new ASNA.DataGateHelper.DGFileReader(apiDGDB, this.exportArgs.BlockingFactor);
            dgfr.AfterRowRead += OnAfterRowRead;

            this.dgfr.ReadEntireFile("examples", "cmastnew");
            this.exportArgs.CloseOutfileStream();

            Console.ForegroundColor = ConsoleColor.Green;

            Console.WriteLine(String.Format(@"Exported {0}\{1}\{2}", 
                        this.exportArgs.DatabaseName,
                        this.exportArgs.LibraryName,
                        this.exportArgs.FileName));

            Console.WriteLine("{0} created on {1}.", this.exportArgs.OutputFileName, DateTime.Now.ToString("f"));
            Console.WriteLine("{0:#,000} rows written.", dgfr.TotalRowsCounter);
            restoreForegroundColor();

            apiDGDB.Close();
            return dgfr.ReadMilliseconds;
        }

        void OnAfterRowRead(System.Object sender, ASNA.DataGateHelper.AfterRowReadArgs e)
        {
            StringBuilder exportLine = new StringBuilder();
            int counter = 0;

            if (e.CurrentRowCounter == 1)
            {
                this.exportArgs.OpenOutfileStream();

                if (this.exportArgs.WriteSchemaFileFlag)
                {
                    writeSchemaFile(e.FieldNames, e.FieldTypes);
                }
                if (this.exportArgs.IncludeHeadingFlag)
                {
                    writeColumnHeadings(e.FieldNames);
                }
            }

            foreach (string fieldName in e.FieldNames)
            {
                if (e.FieldTypes[counter].ToLower() == "string")
                {
                    exportLine.Append(String.Format("\"{0}\"{1}", e.DataRow[fieldName].ToString().Trim(), this.exportArgs.Delimiter));
                }
                else {
                    exportLine.Append(String.Format("{0}{1}", e.DataRow[fieldName].ToString(), this.exportArgs.Delimiter));
                }
                counter++;
            }

            if (this.exportArgs.ShowProgressFlag && Utils.Mod(e.CurrentRowCounter, 500)) {
                showProgress(e.CurrentRowCounter, e.TotalRowsCounter);
            }

            this.exportArgs.outfileStream.WriteLine(Utils.RemoveLastCharacter(exportLine.ToString()));
        }

        void showProgress(int currentRowCounter, long totalRowsCounter)
        {
            int cursorLeft;
            int cursorTop;

            cursorLeft = Console.CursorLeft;
            cursorTop = Console.CursorTop;
            Console.WriteLine("{0} of {1}", currentRowCounter, totalRowsCounter);
            Console.CursorLeft = cursorLeft;
            Console.CursorTop = cursorTop;
        }
    
        void writeColumnHeadings(string[] fieldNames)
        {
            StringBuilder lineBuilder = new StringBuilder();
            string headingLine;

            foreach (string fieldName in fieldNames)
            {
                lineBuilder.Append(String.Format("{0}{1}", fieldName, this.exportArgs.Delimiter));
            }
            headingLine = Utils.RemoveLastCharacter(lineBuilder.ToString());
            this.exportArgs.outfileStream.WriteLine(headingLine);
        }

        void writeSchemaFile(string[] fieldNames, string[] fieldTypes)
        {
            int columnCounter = 0;

            using (System.IO.StreamWriter outfileStream = new System.IO.StreamWriter(this.exportArgs.OutputSchemaFileName))
            {
                outfileStream.WriteLine("{0,-24}{1}", "Column name", "Data type");
                outfileStream.WriteLine("{0,-24}{1}", "-----------", "---------");

                foreach (string fieldName in fieldNames)
                {
                    outfileStream.WriteLine(String.Format("{0,-24}{1}", fieldName, fieldTypes[columnCounter]));
                    columnCounter++;
                }
            }
        }

    }

    public class ExporterArgs
    {
        public const int DEFAULT_BLOCKING_FACTOR = 500;

        public string DatabaseName { get; set; }
        public string LibraryName { get; set; }
        public string FileName { get; set; }
        public string OutputDirectory { get; set; }
        public bool IncludeHeadingFlag { get; set; }
        public bool ShowProgressFlag { get; set; }
        public bool TabDelimiterFlag { get; set; }
        public bool WriteSchemaFileFlag { get; set; }
        public int BlockingFactor { get; set; }
        public string LibraryNameForOutputFile { get; set; }
        public string OutputFileName { get; set; }
        public string OutputSchemaFileName { get; set; }
        public string Delimiter { get; set; }

        public System.IO.StreamWriter outfileStream;

        public ExporterArgs()
        {
            this.BlockingFactor = DEFAULT_BLOCKING_FACTOR;
        }
        public void TransformExportArgs()
        {
            const string TAB = "\t";
            const string COMMA = ",";

            this.Delimiter = (this.TabDelimiterFlag) ? TAB : COMMA;

            if (this.LibraryName == @"\" || this.LibraryName == @"/")
            {
                this.LibraryNameForOutputFile = "#root";
            }
            else
            {
                this.LibraryNameForOutputFile = this.LibraryName;
            }

            this.OutputFileName = String.Format(@"{0}\{1}_{2}_{3}.txt",
                                          this.OutputDirectory,
                                          Utils.NormalizeDatabaseName(this.DatabaseName),
                                          this.LibraryNameForOutputFile,
                                          this.FileName);

            this.OutputSchemaFileName = String.Format(@"{0}\{1}_{2}_{3}.schema.txt",
                                      this.OutputDirectory,
                                      Utils.NormalizeDatabaseName(this.DatabaseName),
                                      this.LibraryNameForOutputFile,
                                      this.FileName);
        }


        public void OpenOutfileStream()
        {
            this.outfileStream = new System.IO.StreamWriter(this.OutputFileName);
        }

        public void CloseOutfileStream()
        {
            this.outfileStream.Dispose();
        }

    }



}
