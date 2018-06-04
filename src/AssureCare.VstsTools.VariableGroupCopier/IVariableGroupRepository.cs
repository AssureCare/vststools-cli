using System.Collections.Generic;
using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace AssureCare.VstsTools.VariableGroupCopier
{
    internal interface IVariableGroupRepository
    {
        IEnumerable<VariableGroup> GetAll(string project, string name);

        VariableGroup Get(string project, string name);

        void Delete(string project, int id);
        
        void Add(string project, string name, VariableGroup group);
    }
}