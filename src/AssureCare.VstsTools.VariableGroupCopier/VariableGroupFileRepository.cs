using System.IO;
using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace AssureCare.VstsTools.VariableGroupCopier
{
    public class VariableGroupFileRepository : IVariableGroupRepository
    {
        public VariableGroup Get(string location, string name)
        {
            if (!File.Exists(name)) return null;

            var serializedGroup = File.ReadAllText(name);

            return JsonUtility.FromString<VariableGroup>(serializedGroup);
        }

        public void Delete(string project, int id)
        {
        }

        public void Add(string project, string name, VariableGroup group)
        {
            var serializedGroup = JsonUtility.ToString(group);

            File.WriteAllText(name, serializedGroup);
        }
    }
}