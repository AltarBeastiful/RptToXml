using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CommandLine;

namespace RptToXml
{
	class Program
	{
		class Options
		{

			[Value(0, Min = 1, Max = 2, MetaName = "<RPT filename | wildcard> [outputfilename]", 
				HelpText = "outputfilename argument is valid only with single filename in first argument")]
			public IEnumerable<string> Files { get; set; }

			[Option('r', "recursive", Required = false, Default = false, HelpText = "Recursive search in directory")]
			public bool Recursive { get; set; }

		}
		static void Main(string[] args)
		{
			if (args.Length < 1)
			{
				Console.WriteLine("Usage: RptToXml.exe <RPT filename | wildcard> [outputfilename]");
				Console.WriteLine("      ");
				return;
			}

			CommandLine.Parser.Default.ParseArguments<Options>(args)
			.WithParsed(RunOptions)
			.WithNotParsed(HandleParseError);
		}

		static void HandleParseError(IEnumerable<Error> errs)
		{
			Console.WriteLine("Argument parsing error");
			Console.WriteLine(errs);
		}

		static void RunOptions(Options opts)
		{
			string rptPathArg = opts.Files.First();
			bool wildCard = rptPathArg.Contains("*");
			if (!wildCard && !ReportFilenameValid(rptPathArg))
				return;

			if (wildCard && opts.Files.Count() > 1)
			{
				Console.WriteLine("Output filename may not be specified with wildcard.");
				return;
			}

			Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));

			var rptPaths = new List<string>();
			if (!wildCard)
			{
				rptPaths.Add(rptPathArg);
			}
			else
			{
				var directory = Path.GetDirectoryName(rptPathArg);
                if (String.IsNullOrEmpty(directory))
                {
                    directory = ".";
                }

				SearchOption searchOption = SearchOption.TopDirectoryOnly;
				if (opts.Recursive)
				{
					searchOption = SearchOption.AllDirectories;
				}

                var matchingFiles = Directory.GetFiles(directory, searchPattern: Path.GetFileName(rptPathArg), searchOption: searchOption);
                rptPaths.AddRange(matchingFiles.Where(ReportFilenameValid));
			}

			if (rptPaths.Count == 0)
			{
				Trace.WriteLine("No reports matched the wildcard.");
			}

			foreach (string rptPath in rptPaths)
			{
				Trace.WriteLine("Dumping " + rptPath);

				using (var writer = new RptDefinitionWriter(rptPath))
				{
					string xmlPath = opts.Files.Count() > 1 ?
						opts.Files.ElementAt(1) : Path.ChangeExtension(rptPath, "xml");
					writer.WriteToXml(xmlPath);
				}
			}
		}

		static bool ReportFilenameValid(string rptPath)
		{
			string extension = Path.GetExtension(rptPath);
			if (String.IsNullOrEmpty(extension) || !extension.Equals(".rpt", StringComparison.OrdinalIgnoreCase))
			{
				Console.WriteLine("Input filename [" + rptPath + "] does not end in .RPT");
				return false;
			}

			if (!File.Exists(rptPath))
			{
				Console.WriteLine("Report file [" + rptPath + "] does not exist.");
				return false;
			}

			return true;
		}
	}
}
