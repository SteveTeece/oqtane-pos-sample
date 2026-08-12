using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Oqtane.Enums;
using Oqtane.Infrastructure;
using Oqtane.Interfaces;
using Oqtane.Models;
using Oqtane.Modules;
using Oqtane.Repository;
using Yosh.uwa.Models;
using Yosh.uwa.Repository;

namespace Yosh.uwa.Manager
{
    public class VisualCrudManager : MigratableModuleBase, IInstallable, IPortable, ISearchable
    {
        private readonly ICrudDefinitionRepository _definitions;
        private readonly IDBContextDependencies _dependencies;

        public VisualCrudManager(ICrudDefinitionRepository definitions, IDBContextDependencies dependencies)
        {
            _definitions = definitions;
            _dependencies = dependencies;
        }

        public bool Install(Tenant tenant, string version) => Migrate(new Context(_dependencies), tenant, MigrationType.Up);
        public bool Uninstall(Tenant tenant) => Migrate(new Context(_dependencies), tenant, MigrationType.Down);

        public string ExportModule(Module module)
            => JsonSerializer.Serialize(_definitions.GetDefinitions(module.ModuleId));

        public void ImportModule(Module module, string content, string version)
        {
            if (string.IsNullOrWhiteSpace(content)) return;
            var definitions = JsonSerializer.Deserialize<List<CrudDefinition>>(content) ?? new();
            foreach (var definition in definitions)
            {
                definition.CrudDefinitionId = 0;
                definition.ModuleId = module.ModuleId;
                _definitions.AddDefinition(definition);
            }
        }

        public Task<List<SearchContent>> GetSearchContentsAsync(PageModule pageModule, DateTime lastIndexedOn)
        {
            var content = _definitions.GetDefinitions(pageModule.ModuleId)
                .Where(definition => definition.IsPublished && definition.ModifiedOn >= lastIndexedOn)
                .Select(definition => new SearchContent
                {
                    EntityName = "VisualCrudDefinition",
                    EntityId = definition.CrudDefinitionId.ToString(),
                    Title = definition.MenuTitle ?? definition.Name,
                    Body = $"{definition.Name} {definition.SourceSchema}.{definition.SourceTable}",
                    ContentModifiedBy = definition.ModifiedBy,
                    ContentModifiedOn = definition.ModifiedOn
                }).ToList();
            return Task.FromResult(content);
        }
    }
}
