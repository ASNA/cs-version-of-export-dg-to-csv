using System;
using System.Linq;
using System.Text;
using ASNA.DataGateHelper;
using System.Reflection;
using CommandLineUtility;

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
            return CodeRunner(args);
        }

        static int CodeRunner(string[] args)
        {
            
            ExporterArgs exportArgs = new ExporterArgs();
            CmdArgManager cam = new CmdArgManager(exportArgs, args, "Export a DataGate file to CSV with C#");

            CmdArgManager.ExitCode result = cam.ParseArgs();
            if (result == CmdArgManager.ExitCode.HelpShown)
            {
                return (int)ExitCode.Success;
            }

            if (result != CmdArgManager.ExitCode.Success)
            {
                Console.WriteLine("**ERROR**");
                Console.WriteLine(cam.ErrorMessage);
                return (int)ExitCode.Failure;

            }

            Exporter export = new Exporter(exportArgs);

            try
            {
                int ElapsedMilliseconds = export.Run();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Time to export: {0:#,###}ms {1:#,##0}min", ElapsedMilliseconds, ElapsedMilliseconds / 60000);
                Console.ForegroundColor = OriginalForegroundColor;

                if (exportArgs.Pause)
                {
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                }

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
            this.dgfr = new ASNA.DataGateHelper.DGFileReader(apiDGDB, this.exportArgs.BlockFactor);
            this.dgfr.AfterRowRead += OnAfterRowRead;

            this.dgfr.ReadEntireFile("examples", "cmastnew");
            this.exportArgs.CloseOutfileStream();

            Console.ForegroundColor = ConsoleColor.Green;

            //Console.WriteLine(String.Format(@"Exported to: {0}", this.exportArgs.OutputFileName));

            Console.WriteLine(String.Format(@"Exported from Database Name{0}: {1}\{2}",
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

                if (this.exportArgs.WriteSchemafileFlag)
                {
                    writeSchemaFile(e.FieldNames, e.FieldTypes);
                }
                if (! this.exportArgs.IncludeHeadingFlag)
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
        const bool REQUIRED = true;
        const bool OPTIONAL = false;
        const int DEFAULT_BLOCKING_FACTOR = 500;

        [CmdArg("--databasename", "-d", REQUIRED, "Database name")]
        public string DatabaseName { get; set; }

        [CmdArg("--library", "-l", REQUIRED, "Library")]
        public string LibraryName { get; set; }

        [CmdArg("--file", "-f", REQUIRED, "File name")]
        public string FileName { get; set; }

        [CmdArg("--outputpath", "-p", OPTIONAL, "Output path")]
        public string OutputDirectory { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        [CmdArg("--blockfactor", "-b", OPTIONAL, "Recording blocking factor")]
        public int BlockFactor { get; set; } = 500;

        [CmdArg("--noheadings", "-nh", OPTIONAL, "Do not include headings row")]
        public bool IncludeHeadingFlag { get; set; } = false;

        [CmdArg("--tabdelimiter", "-t", OPTIONAL, "Use tab as field delimiter")]
        public bool TabDelimiterFlag { get; set; } = false;

        [CmdArg("--showprogress", "-x", OPTIONAL, "Show export progress")]
        public bool ShowProgressFlag { get; set; } = false;

        [CmdArg("--writeschemafile", "-s", OPTIONAL, "Write schema file")]
        public bool WriteSchemafileFlag { get; set; } = false;
        
        [CmdArg("--pause", "-ps", OPTIONAL, "Pause screen after export--usually for debugging purposes")]
        public bool Pause { get; set; } = false;

        public string LibraryNameForOutputFile { get; set; } 
        public string OutputFileName { get; set; }
        public string OutputSchemaFileName { get; set; }
        public string Delimiter { get; set; }

        public System.IO.StreamWriter outfileStream;

        public ExporterArgs()
        {
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
