using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Newtonsoft.Json;
using NLog;
using NLog.Fluent;

namespace AssureCare.VstsTools.VariableGroupCopier
{
    internal class VariableGroupFileRepository : IVariableGroupRepository
    {
        [NotNull] private readonly IKnownLiterals _knownLiterals;
        [NotNull] private readonly IParametersResolver _parametersResolver;
        private readonly JsonSerializer _jsonSerializer;

        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        public VariableGroupFileRepository([NotNull] IKnownLiterals knownLiterals,
            [NotNull] IParametersResolver parametersResolver)
        {
            _knownLiterals = knownLiterals ?? throw new ArgumentNullException(nameof(knownLiterals));
            _parametersResolver = parametersResolver ?? throw new ArgumentNullException(nameof(parametersResolver));
            _jsonSerializer = new JsonSerializer {Formatting = Formatting.Indented};
        }

        public IEnumerable<VariableGroup> GetAll(string project, string name)
        {
            // Assert we use it for the correct location
            AssertCorrectLocation(project);

            // Check if matching directory exists and if so get all files with matching extensions
            if (Directory.Exists(name))
            {
                foreach (var file in Directory.GetFiles(name, $"*{_knownLiterals.FileExt}"))
                {
                    var variableGroup = Get(project, file);

                    if (variableGroup != null) yield return variableGroup;
                }

                yield break;
            }

            yield return Get(project, name);
        }

        public VariableGroup Get(string project, string name)
        {
            AssertCorrectLocation(project);

            if (!File.Exists(name)) return null;

            try
            {
                var serializedGroup = File.ReadAllText(name);

                return JsonUtility.FromString<VariableGroup>(serializedGroup);
            }
            catch (Exception ex)
            {
                Logger.Warn()
                    .Property("FileName", name)
                    .Exception(ex)
                    .Message("Error reading input file. Skipping.")
                    .Write();
            }

            return null;
        }

        public void Delete(string project, int id)
        {
            AssertCorrectLocation(project);
        }

        public void Add(string project, string name, VariableGroup group)
        {
            AssertCorrectLocation(project);

            var writer = new StringWriter();
            _jsonSerializer.Serialize(writer, group);
            
            // Ensure folder exists
            var folder = Path.GetDirectoryName(name);
            if (folder != null) Directory.CreateDirectory(folder);

            File.WriteAllText(name, writer.ToString());
        }

        private void AssertCorrectLocation(string project)
        {
            if (!_parametersResolver.IsGroupLocationFile(project)) throw new InvalidOperationException("Using file repository on non-file location");
        }
    }
}