using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace AssureCare.VstsTools.VariableGroupCopier
{
    public class VariableGroupVstsRepository : IVariableGroupRepository, IDisposable
    {
        private readonly VssConnection _connection;
        private readonly TaskAgentHttpClient _client;

        private const string UrlFormat = "https://{0}.visualstudio.com/";

        public VariableGroupVstsRepository(string account, string token)
        {
            _connection = new VssConnection(new Uri(string.Format(UrlFormat, account)),
                new VssBasicCredential(string.Empty, token));

            _client = _connection.GetClient<TaskAgentHttpClient>();
        }

        public IEnumerable<VariableGroup> GetAll(string project, string name)
        {
            return _client.GetVariableGroupsAsync(project).SyncResult();
        }

        public VariableGroup Get(string project, string name)
        {
            return _client.GetVariableGroupsAsync(project, name).SyncResult().SingleOrDefault();
        }

        public void Delete(string project, int id)
        {
            _client.DeleteVariableGroupAsync(project, id).SyncResult();
        }

        public void Add(string project, string name, VariableGroup group)
        {
            group.Name = name;

            _client.AddVariableGroupAsync(project, group).SyncResult();
        }

        public void Dispose()
        {
            _client?.Dispose();

            _connection?.Disconnect();
        }
    }
}