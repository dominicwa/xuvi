using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace UpdateVersionInfo
{
    internal class Program
    {
        static String AssemblyVersionExpression = @"^\s*\[assembly:\s*(?<attribute>(?:System\.)?(?:Reflection\.)?AssemblyVersion(?:Attribute)?\s*\(\s*""(?<version>[^""]+)""\s*\)\s*)\s*\]\s*$";
        static String AssemblyFileVersionExpression = @"^\s*\[assembly:\s*(?<attribute>(?:System\.)?(?:Reflection\.)?AssemblyFileVersion(?:Attribute)?\s*\(\s*""(?<version>[^""]+)""\s*\)\s*)\s*\]\s*$";

        static readonly Regex assemblyVersionRegEx = new Regex(AssemblyVersionExpression, RegexOptions.Multiline | RegexOptions.Compiled);
        static readonly Regex assemblyFileVersionRegEx = new Regex(AssemblyFileVersionExpression, RegexOptions.Multiline | RegexOptions.Compiled);

        static void Main(string[] args)
        {
            var commandLine = new CommandLineArguments(args);
            var validCL = false;
            try
            {
                validCL = ValidateCommandLine(commandLine);

                if (validCL && !commandLine.ShowHelp)
                {
                    Version version = new Version(
                        commandLine.Major,
                        commandLine.Minor,
                        commandLine.Build.HasValue ? commandLine.Build.Value : 0,
                        commandLine.Revision.HasValue ? commandLine.Revision.Value : 0);

                    if (commandLine.RevisionStamp)
                    {
                        version = new Version(
                            version.Major,
                            version.Minor,
                            version.Build,
                            (DateTime.Now.Hour * 60 * 60) + (DateTime.Now.Minute * 60) + DateTime.Now.Second);
                    }
                    
                    if (!String.IsNullOrEmpty(commandLine.VersionCsPath))
                    {
                        if (commandLine.IncBuild)
                        {
                            version = new Version(
                                version.Major,
                                version.Minor,
                                GetCurrentCSVersionBuild(commandLine.VersionCsPath) + 1,
                                version.Revision);
                        }
                        UpdateCSVersionInfo(commandLine.VersionCsPath, version);
                    }
                    if (!String.IsNullOrEmpty(commandLine.AndroidManifestPath))
                    {
                        if (commandLine.IncBuild)
                        {
                            version = new Version(
                                version.Major,
                                version.Minor,
                                GetCurrentAndroidVersionBuild(commandLine.AndroidManifestPath) + 1,
                                version.Revision);
                        }
                        UpdateAndroidVersionInfo(commandLine.AndroidManifestPath, version);
                    }
                    if (!String.IsNullOrEmpty(commandLine.TouchPListPath))
                    {
                        if (commandLine.IncBuild)
                        {
                            version = new Version(
                                version.Major,
                                version.Minor,
                                GetCurrentTouchVersionBuild(commandLine.TouchPListPath) + 1,
                                version.Revision);
                        }
                        UpdateTouchVersionInfo(commandLine.TouchPListPath, version);
                    }

                    Console.WriteLine("Version information successfully updated.");
                }
            }
            catch (Exception e)
            {
                WriteHelp(commandLine, "An unexpected error was encountered:" + e.Message);
            }
            try
            {
                if (!validCL || commandLine.DoNotExit)
                {
                    Console.WriteLine("Waiting for key press before exiting...");
                    ConsoleKeyInfo key = Console.ReadKey(true);
                }
            }
            catch (Exception e)
            {
                WriteHelp(commandLine, "An unexpected error was encountered:" + e.Message);
            }
        }

        private static int GetCurrentCSVersionBuild(string path)
        {
            String contents;
            using (var reader = new StreamReader(path))
            {
                contents = reader.ReadToEnd();
            }
            contents = assemblyVersionRegEx.Matches(contents)[0].Value;
            contents = contents.Substring(contents.IndexOf('"', "[assembly: System.Reflection.AssemblyVersion(".Length));
            contents = contents.Substring(1, contents.IndexOf('"', 1) - 1);
            contents = contents.Substring(contents.IndexOf('.') + 1);
            contents = contents.Substring(contents.IndexOf('.') + 1);
            contents = contents.Substring(0, contents.IndexOf('.'));
            //Console.WriteLine(contents);
            return int.Parse(contents);
        }

        private static void UpdateCSVersionInfo(string path, Version version)
        {
            String contents;
            using (var reader = new StreamReader(path))
            {
                contents = reader.ReadToEnd();
            }
            contents = assemblyVersionRegEx.Replace(contents, "[assembly: System.Reflection.AssemblyVersion(\"" + version.ToString() + "\")]");
            if (assemblyFileVersionRegEx.IsMatch(contents))
            {
                contents = assemblyFileVersionRegEx.Replace(contents, "[assembly: System.Reflection.AssemblyFileVersion(\"" + version.ToString() + "\")]");
            }
            using (StreamWriter writer = new StreamWriter(path, false))
            {
                writer.Write(contents);
            }
        }

        private static int GetCurrentAndroidVersionBuild(string path)
        {
            const string androidNS = "http://schemas.android.com/apk/res/android";
            XName versionCodeAttributeName = XName.Get("versionCode", androidNS);
            XDocument doc = XDocument.Load(path);
            var contents = doc.Root.Attribute(versionCodeAttributeName).Value;
            contents = contents.Substring(0, contents.IndexOf('.'));
            //Console.WriteLine(contents);
            return int.Parse(contents);
        }

        private static void UpdateAndroidVersionInfo(string path, Version version)
        {
            //https://developer.android.com/tools/publishing/versioning.html
            const string androidNS = "http://schemas.android.com/apk/res/android";
            XName versionCodeAttributeName = XName.Get("versionCode", androidNS);
            XName versionNameAttributeName = XName.Get("versionName", androidNS);
            XDocument doc = XDocument.Load(path);
            doc.Root.SetAttributeValue(versionCodeAttributeName, version.Build + "." + version.Revision);
            doc.Root.SetAttributeValue(versionNameAttributeName, version.Major + "." + version.Minor);
            doc.Save(path);
        }

        private static int GetCurrentTouchVersionBuild(string path)
        {
            XDocument doc = XDocument.Load(path);
            var bundleVersionElement = doc.XPathSelectElement("plist/dict/key[string()='CFBundleVersion']");
            var versionElement = bundleVersionElement.NextNode as XElement;
            var contents = versionElement.Value;
            contents = contents.Substring(0, contents.IndexOf('.'));
            //Console.WriteLine(contents);
            return int.Parse(contents);
        }

        private static void UpdateTouchVersionInfo(string path, Version version)
        {
            //https://developer.apple.com/library/mac/documentation/General/Reference/InfoPlistKeyReference/Articles/CoreFoundationKeys.html
            XDocument doc = XDocument.Load(path);
            var shortVersionElement = doc.XPathSelectElement("plist/dict/key[string()='CFBundleShortVersionString']");
            var bundleVersionElement = doc.XPathSelectElement("plist/dict/key[string()='CFBundleVersion']");
            var versionElement = shortVersionElement.NextNode as XElement;
            versionElement.Value = version.Major + "." + version.Minor;
            versionElement = bundleVersionElement.NextNode as XElement;
            versionElement.Value = version.Build + "." + version.Revision;
            doc.Save(path);
        }

        private static bool ValidateCommandLine(CommandLineArguments commandLine)
        {
            if (commandLine.ShowHelp)
            {
                WriteHelp(commandLine);
                return true;
            }
            var errors = new System.Text.StringBuilder();
            if (commandLine.Major < 0)
            {
                errors.AppendLine("You must supply a positive major version number.");
            }
            if (commandLine.Minor < 0)
            {
                errors.AppendLine("You must supply a positive minor version number.");
            }
            if (!commandLine.Build.HasValue && !commandLine.IncBuild)
            {
                errors.AppendLine("You must supply a numeric build number (or set incremental build flag to true).");
            }
            if (String.IsNullOrEmpty(commandLine.VersionCsPath) || !IsValidCSharpVersionFile(commandLine.VersionCsPath))
            {
                errors.AppendLine("You must supply valid path to a writable C# file containing assembly version information.");
            }
            if (!String.IsNullOrEmpty(commandLine.AndroidManifestPath) && !IsValidAndroidManifest(commandLine.AndroidManifestPath))
            {
                errors.AppendLine("You must supply valid path to a writable android manifest file.");
            }
            if (!String.IsNullOrEmpty(commandLine.TouchPListPath) && !IsValidTouchPList(commandLine.TouchPListPath))
            {
                errors.AppendLine("You must supply valid path to a writable plist file containing version information.");
            }
            if (errors.Length > 0)
            {
                WriteHelp(commandLine, "Invalid command line:\n" + errors.ToString());
                return false;
            }
            return true;
        }

        private static bool IsValidCSharpVersionFile(String path)
        {
            if (!File.Exists(path)) return false;
            if ((new FileInfo(path).Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly) return false;

            try
            {
                String contents;
                using (var reader = new StreamReader(path))
                {
                    contents = reader.ReadToEnd();
                }

                if (assemblyVersionRegEx.IsMatch(contents))
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.TraceError(e.Message);
            }
            
            return false;
        }

        private static bool IsValidAndroidManifest(String path)
        {
            if (!File.Exists(path)) return false;
            if ((new FileInfo(path).Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly) return false;

            try
            {
                // <manifest ...
                XDocument doc = XDocument.Load(path);
                var rootElement = doc.Root as XElement;
                if (rootElement != null && rootElement.Name == "manifest") return true;
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.TraceError(e.Message);
            }
            return false;
        }

        private static bool IsValidTouchPList(String path)
        {
            if (!File.Exists(path)) return false;
            if ((new FileInfo(path).Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly) return false;

            try
            {
                //<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
                XDocument doc = XDocument.Load(path);
                if (doc.DocumentType.Name == "plist")
                {
                    var ok = true;
                    var shortVersionElement = doc.XPathSelectElement("plist/dict/key[string()='CFBundleShortVersionString']");
                    if (shortVersionElement != null)
                    {
                        var valueElement = shortVersionElement.NextNode as XElement;
                        if (valueElement == null || valueElement.Name != "string") ok = false;
                    }
                    var bundleVersionElement = doc.XPathSelectElement("plist/dict/key[string()='CFBundleVersion']");
                    if (bundleVersionElement != null)
                    {
                        var valueElement = bundleVersionElement.NextNode as XElement;
                        if (valueElement == null || valueElement.Name != "string") ok = false;
                    }
                    return ok;
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.TraceError(e.Message);
            }
            
            return false;
        }

        private static void WriteHelp(CommandLineArguments commandLine, String message = null)
        {
            if (!String.IsNullOrEmpty(message))
            {
                Console.WriteLine(message);
            }
            commandLine.WriteHelp(Console.Out);
        }
    }
}
