using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Oqtane.Modules;
using Yosh.uwa.Models;

namespace Yosh.uwa.Repository
{
    public interface ICrudDefinitionRepository
    {
        IEnumerable<CrudDefinition> GetDefinitions(int moduleId);
        CrudDefinition GetDefinition(int definitionId, bool tracking = true);
        CrudDefinition AddDefinition(CrudDefinition definition);
        CrudDefinition UpdateDefinition(CrudDefinition definition);
        void DeleteDefinition(int definitionId);
    }

    public class CrudDefinitionRepository : ICrudDefinitionRepository, ITransientService
    {
        private readonly IDbContextFactory<Context> _factory;

        public CrudDefinitionRepository(IDbContextFactory<Context> factory)
        {
            _factory = factory;
        }

        public IEnumerable<CrudDefinition> GetDefinitions(int moduleId)
        {
            using var db = _factory.CreateDbContext();
            return db.CrudDefinitions.AsNoTracking()
                .Where(item => item.ModuleId == moduleId)
                .OrderBy(item => item.MenuTitle)
                .ThenBy(item => item.Name)
                .ToList();
        }

        public CrudDefinition GetDefinition(int definitionId, bool tracking = true)
        {
            using var db = _factory.CreateDbContext();
            return tracking
                ? db.CrudDefinitions.Find(definitionId)
                : db.CrudDefinitions.AsNoTracking().FirstOrDefault(item => item.CrudDefinitionId == definitionId);
        }

        public CrudDefinition AddDefinition(CrudDefinition definition)
        {
            using var db = _factory.CreateDbContext();
            db.CrudDefinitions.Add(definition);
            db.SaveChanges();
            return definition;
        }

        public CrudDefinition UpdateDefinition(CrudDefinition definition)
        {
            using var db = _factory.CreateDbContext();
            db.Entry(definition).State = EntityState.Modified;
            db.SaveChanges();
            return definition;
        }

        public void DeleteDefinition(int definitionId)
        {
            using var db = _factory.CreateDbContext();
            var definition = db.CrudDefinitions.Find(definitionId);
            if (definition != null)
            {
                db.CrudDefinitions.Remove(definition);
                db.SaveChanges();
            }
        }
    }
}
