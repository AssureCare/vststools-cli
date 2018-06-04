using System;
using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace AssureCare.VstsTools.VariableGroupCopier
{
    internal interface IVariableGroupRepository
    {
        VariableGroup Get(string project, string name);

        void Delete(string project, int id);
        
        void Add(string project, string name, VariableGroup group);
    }
}