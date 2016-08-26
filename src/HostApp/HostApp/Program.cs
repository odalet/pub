using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HostApi;

namespace HostApp
{
    internal struct PluginInfo
    {
        public string AssemblyFileName;
        public Type Type;
    }

    internal class Program
    {
        private readonly List<PluginInfo> plugins = new List<PluginInfo>();

        private void Run(string[] args)
        {
            WL("This is the Host application. Looking for plugins...");
            WD("Arguments: " + string.Join(", ", args));

            LoadPlugins();

            if (plugins.Count == 0)
            {
                WL("No plugins found");
                return;
            }

            // And this is the wonderful repl!
            while (true)
            {
                WL();
                ShowHelp();
                Console.Write("> ");
                var input = RL();
                if (input == "q" || input == "Q")
                {
                    WL("Bye!");
                    return;
                }

                int index;
                if (!int.TryParse(input, out index))
                {
                    WL("Invalid command.");
                    continue;
                }

                index--; // plugins are 1-based in the UI, 0-based in the list
                if (index < 0 || index >= plugins.Count)
                {
                    WL("No plugin at this index.");
                    continue;
                }

                var pluginInfo = plugins[index];
                RunPlugin(pluginInfo);
            }
        }

        private void LoadPlugins()
        {
            var rootdir = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "plugins");

            if (!Directory.Exists(rootdir))
            {
                WD($"Directory {rootdir} does not exist.");
                return;
            }

            foreach (var file in Directory.GetFiles(rootdir, "*.dll"))
                TryLoadPlugin(file);
        }

        private void TryLoadPlugin(string filename)
        {
            Assembly assy = null;
            try
            {
                assy = Assembly.LoadFrom(filename);
            }
            catch (Exception ex)
            {
                WD($"File {filename} is not an assembly or could not be loaded: {ex.Message}. Skipping.");
            }

            if (assy == null) return;

            // Let's see if this assembly contains plugins
            foreach (var pluginType in assy.GetTypes().Where(t => typeof(Plugin).IsAssignableFrom(t)))
                plugins.Add(new PluginInfo
                {
                    AssemblyFileName = filename,
                    Type = pluginType
                });
        }

        private void RunPlugin(PluginInfo info)
        {
            Plugin instance;
            try
            {
                instance = (Plugin)Activator.CreateInstance(info.Type);
            }
            catch (Exception ex)
            {
                WE($"Could not create an instance of this plugin: {ex.Message}");
                return;
            }

            if (instance == null)
            {
                WE("Could not create an instance of this plugin");
                return;
            }

            try
            {
                instance.Run();
            }
            catch (Exception ex)
            {
                WE($"Execution of this plugin produced an error: {ex.Message}");
            }
        }

        private void ShowHelp()
        {
            WL("Choose one of the commands below:");
            for (var i = 0; i < plugins.Count; i++)
                WL($"{i + 1} - Executes plugin '{Path.GetFileName(plugins[i].AssemblyFileName)} -> {plugins[i].Type}'.");
            WL("q - Exits the application.");
            WL();
        }

        // Adapted from Snippet Compiler default template
        #region Helper methods

        private static void Main(string[] args)
        {
            try
            {
                new Program().Run(args);
            }
            catch (Exception e)
            {
                string error = string.Format("---\nThe following error occurred while executing the snippet:\n{0}\n---", e.ToString());
                Console.WriteLine(error);
            }
            finally
#pragma warning disable S108 // Nested blocks of code should not be left empty
            {
#if false
                Console.Write("Press any key to continue...");
                Console.ReadKey();
#endif
            }
#pragma warning restore S108 // Nested blocks of code should not be left empty
        }

        private static void WE(object text, params object[] args) => WL("ERROR: " + text, args);
        private static void WD(object text, params object[] args)
        {
#if DEBUG
            Console.WriteLine("DEBUG: " + text.ToString(), args);
#endif
        }

        private static void WL() => Console.WriteLine();
        private static void WL(object text, params object[] args) => Console.WriteLine(text.ToString(), args);
        private static string RL() => Console.ReadLine();

        private static void Break() => System.Diagnostics.Debugger.Break();

        #endregion
    }
}
