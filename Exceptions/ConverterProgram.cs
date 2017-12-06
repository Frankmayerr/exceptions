using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using NLog;
using NLog.LayoutRenderers;

namespace Exceptions
{
	public class ConverterProgram
	{
		private static readonly Logger log = LogManager.GetCurrentClassLogger();

		public static void Main(params string[] args)
		{
			try
			{
				var filenames = args.Any() ? args : new[] { "text.txt" };
				var settings = LoadSettings();
				ConvertFiles(filenames, settings);
			}
			catch (Exception e)
			{
				log.Error(e);
			}
		}

		private static void ConvertFiles(string[] filenames, Settings settings)
		{
			var tasks = filenames
				.Select(fn => Task.Run(() => ConvertFile(fn, settings))
				)
				//.ContinueWith(task => HandleException(task))) 

				.ToArray();
			Task.WaitAll(tasks); 
		}

		private static Settings LoadSettings() 
		{
			//if (!File.Exists("settings.xml"))
			try
			{
				var serializer = new XmlSerializer(typeof(Settings));
				var content = File.ReadAllText("settings.xml");
				return (Settings) serializer.Deserialize(new StringReader(content));
			}
			catch (Exception e)
			{
				log.Error(e.InnerException, "Не удалось прочитать файл настроек");
				return Settings.Default;
			}
		}

		private static void ConvertFile(string filename, Settings settings)
		{
			Thread.CurrentThread.CurrentCulture = new CultureInfo(settings.SourceCultureName);
			if (settings.Verbose)
			{
				log.Info("Processing file " + filename);
				log.Info("Source Culture " + Thread.CurrentThread.CurrentCulture.Name);
			}
			IEnumerable<string> lines;

			try
			{
				lines = PrepareLines(filename);
				var convertedLines = lines
					.Select(ConvertLine)
					.Select(s => s.Length + " " + s);
				File.WriteAllLines(filename + ".out", convertedLines);

			}
			catch (FileNotFoundException e)
			{
				log.Error($"Не удалось сконвертировать {filename} FileNotFoundException");
				return;
			}
			catch
			{
				log.Error("Некорректная строка");
				return;
			}
		}


		private static IEnumerable<string> PrepareLines(string filename)
		{
			var lineIndex = 0;
			foreach (var line in File.ReadLines(filename))
			{
				if (line == "") continue;
				yield return line.Trim();
				lineIndex++;
			}
			yield return lineIndex.ToString();
		}

	    public static string ConvertLine(string arg) 
		{												
			if (DateTime.TryParse(arg, out var dateTime))
				return dateTime.ToString();
			if (double.TryParse(arg, out var doubleResult))
				return doubleResult.ToString();
			return ConvertAsCharIndexInstruction(arg);
		}

		private static string ConvertAsCharIndexInstruction(string s)
		{
			var parts = s.Split();
			if (parts.Length < 2) return null;
			var charIndex = int.Parse(parts[0]);
			if ((charIndex < 0) || (charIndex >= parts[1].Length))
				return null;
			var text = parts[1];
			return text[charIndex].ToString();
		}

		private static string ConvertAsDateTime(string arg)
		{
			return DateTime.Parse(arg).ToString(CultureInfo.InvariantCulture);
		}

		private static string ConvertAsDouble(string arg)
		{
			return double.Parse(arg).ToString(CultureInfo.InvariantCulture);
		}
	}
}