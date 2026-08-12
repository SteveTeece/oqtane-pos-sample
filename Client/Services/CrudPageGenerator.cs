using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Oqtane.Models;
using Oqtane.Services;
using Oqtane.Shared;
using Yosh.uwa.Models;

namespace Yosh.uwa.Services
{
    public interface ICrudPageGenerator
    {
        Task<CrudPageGenerationResult> GenerateAsync(CrudDefinition definition, int builderModuleId, int currentPageId);
    }

    public class CrudPageGenerator : ICrudPageGenerator
    {
        public const string ModuleDefinitionName = "Yosh.uwa.VisualCrud, Yosh.uwa.Client.Oqtane";
        public const string DefinitionIdSetting = "VisualCrud.DefinitionId";
        public const string ModeSetting = "VisualCrud.Mode";
        public const string BasePathSetting = "VisualCrud.BasePath";

        private readonly IPageService _pages;
        private readonly IModuleService _modules;
        private readonly IPageModuleService _pageModules;
        private readonly ISettingService _settings;
        private readonly IVisualCrudService _crud;
        private readonly SiteState _siteState;

        public CrudPageGenerator(IPageService pages, IModuleService modules, IPageModuleService pageModules, ISettingService settings, IVisualCrudService crud, SiteState siteState)
        {
            _pages = pages;
            _modules = modules;
            _pageModules = pageModules;
            _settings = settings;
            _crud = crud;
            _siteState = siteState;
        }

        public async Task<CrudPageGenerationResult> GenerateAsync(CrudDefinition definition, int builderModuleId, int currentPageId)
        {
            var configuration = Deserialize(definition.ConfigurationJson);
            var requirements = configuration.PageSet ??= new CrudPageSetRequirements();
            var basePath = NormalizePath(requirements.BasePath, definition.MenuTitle ?? definition.Name);
            requirements.BasePath = basePath;

            var siteId = _siteState.Alias.SiteId;
            var sourcePage = await _pages.GetPageAsync(currentPageId);
            var sourceModule = await _modules.GetModuleAsync(builderModuleId);
            var allPages = await _pages.GetPagesAsync(siteId);
            var allModules = await _modules.GetModulesAsync(siteId);
            var generated = new List<GeneratedCrudPage>();

            var listPage = await EnsurePageAsync(
                allPages, siteId, sourcePage, requirements.ParentPageId,
                definition.MenuTitle ?? definition.Name, basePath, definition.Icon, true);
            var listModule = await EnsureModuleAsync(allModules, listPage, sourceModule, requirements.Pane, definition, "List", basePath);
            generated.Add(ToGenerated("List", basePath, listPage, listModule));

            if (requirements.GenerateCreate)
                generated.Add(await EnsureChildAsync("Create", "New " + (definition.MenuTitle ?? definition.Name), basePath + "/new", listPage, sourcePage, sourceModule, allPages, allModules, requirements, definition));
            if (requirements.GenerateEdit)
                generated.Add(await EnsureChildAsync("Edit", "Edit " + (definition.MenuTitle ?? definition.Name), basePath + "/edit", listPage, sourcePage, sourceModule, allPages, allModules, requirements, definition));
            if (requirements.GenerateDetails)
                generated.Add(await EnsureChildAsync("Details", (definition.MenuTitle ?? definition.Name) + " Details", basePath + "/details", listPage, sourcePage, sourceModule, allPages, allModules, requirements, definition));

            configuration.GeneratedPages = generated;
            definition.ConfigurationJson = JsonSerializer.Serialize(configuration);
            definition.IsPublished = true;
            await _crud.UpdateDefinitionAsync(definition);

            return new CrudPageGenerationResult
            {
                Succeeded = true,
                Message = $"Generated {generated.Count} Oqtane pages.",
                ListPath = "/" + basePath,
                Pages = generated
            };
        }

        private async Task<GeneratedCrudPage> EnsureChildAsync(string role, string title, string path, Page listPage, Page sourcePage, Oqtane.Models.Module sourceModule, List<Page> allPages, List<Oqtane.Models.Module> allModules, CrudPageSetRequirements requirements, CrudDefinition definition)
        {
            var page = await EnsurePageAsync(allPages, listPage.SiteId, sourcePage, listPage.PageId, title, path, definition.Icon, false);
            var module = await EnsureModuleAsync(allModules, page, sourceModule, requirements.Pane, definition, role, requirements.BasePath);
            return ToGenerated(role, path, page, module);
        }

        private async Task<Page> EnsurePageAsync(List<Page> pages, int siteId, Page template, int? parentId, string title, string path, string icon, bool navigation)
        {
            var page = pages.FirstOrDefault(item => string.Equals(NormalizeStoredPath(item.Path), path, StringComparison.OrdinalIgnoreCase));
            if (page == null)
            {
                page = await _pages.AddPageAsync(new Page
                {
                    SiteId = siteId,
                    ParentId = parentId,
                    Name = title,
                    Title = title,
                    Path = path,
                    Icon = string.IsNullOrWhiteSpace(icon) ? "fa-solid fa-table" : icon,
                    IsNavigation = navigation,
                    IsClickable = true,
                    Order = pages.Where(item => item.ParentId == parentId).Select(item => item.Order).DefaultIfEmpty(-1).Max() + 1,
                    ThemeType = template?.ThemeType,
                    DefaultContainerType = template?.DefaultContainerType,
                    PermissionList = ClonePermissions(template?.PermissionList, EntityNames.Page)
                });
                if (page == null)
                    throw new InvalidOperationException($"Oqtane did not create the page '/{path}'. Check the server event log for the underlying page error.");
                pages.Add(page);
            }
            else
            {
                page.ParentId = parentId;
                page.Name = title;
                page.Title = title;
                page.Icon = string.IsNullOrWhiteSpace(icon) ? "fa-solid fa-table" : icon;
                page.IsNavigation = navigation;
                page.IsClickable = true;
                page.PermissionList = ClonePermissions(template?.PermissionList, EntityNames.Page);
                page = await _pages.UpdatePageAsync(page);
                if (page == null)
                    throw new InvalidOperationException($"Oqtane did not update the page '/{path}'. Check the server event log for the underlying page error.");
            }
            return page;
        }

        private async Task<Oqtane.Models.Module> EnsureModuleAsync(List<Oqtane.Models.Module> modules, Page page, Oqtane.Models.Module template, string pane, CrudDefinition definition, string mode, string basePath)
        {
            var module = modules.FirstOrDefault(item => item.PageId == page.PageId && item.ModuleDefinitionName == ModuleDefinitionName);
            if (module == null)
            {
                module = await _modules.AddModuleAsync(new Oqtane.Models.Module
                {
                    SiteId = page.SiteId,
                    PageId = page.PageId,
                    ModuleDefinitionName = ModuleDefinitionName,
                    Pane = string.IsNullOrWhiteSpace(pane) ? "Default" : pane,
                    Order = 0,
                    Title = definition.MenuTitle ?? definition.Name,
                    PermissionList = ClonePermissions(template?.PermissionList, EntityNames.Module)
                });
                if (module == null)
                    throw new InvalidOperationException($"Oqtane did not add the Visual CRUD module to '/{page.Path}'. Check the server event log for the underlying module error.");

                var pageModule = await _pageModules.AddPageModuleAsync(new PageModule
                {
                    PageId = page.PageId,
                    ModuleId = module.ModuleId,
                    Title = definition.MenuTitle ?? definition.Name,
                    Pane = string.IsNullOrWhiteSpace(pane) ? "Default" : pane,
                    Order = 0,
                    ContainerType = template?.ContainerType ?? string.Empty
                });
                if (pageModule == null)
                    throw new InvalidOperationException($"Oqtane created the module but did not attach it to '/{page.Path}'. Check the server event log for the underlying page-module error.");
                modules.Add(module);
            }

            var settings = await _settings.GetModuleSettingsAsync(module.ModuleId);
            _settings.SetSetting(settings, DefinitionIdSetting, definition.CrudDefinitionId.ToString());
            _settings.SetSetting(settings, ModeSetting, mode);
            _settings.SetSetting(settings, BasePathSetting, basePath);
            await _settings.UpdateModuleSettingsAsync(settings, module.ModuleId);
            return module;
        }

        private static GeneratedCrudPage ToGenerated(string role, string path, Page page, Oqtane.Models.Module module)
            => new() { Role = role, Path = "/" + path, PageId = page.PageId, ModuleId = module.ModuleId };

        private static List<Permission> ClonePermissions(IEnumerable<Permission> permissions, string entityName)
            => permissions?.Select(permission => new Permission
            {
                SiteId = permission.SiteId,
                EntityName = entityName,
                EntityId = 0,
                PermissionName = permission.PermissionName,
                RoleId = permission.RoleId,
                UserId = permission.UserId,
                IsAuthorized = permission.IsAuthorized
            }).ToList() ?? new List<Permission>();

        public static string NormalizePath(string path, string fallback)
        {
            var value = string.IsNullOrWhiteSpace(path) ? fallback : path;
            value = string.IsNullOrWhiteSpace(value) ? "crud" : value;
            value = string.Join("-", value.Trim().Trim('/').ToLowerInvariant().Split(new[] { ' ', '_', '/' }, StringSplitOptions.RemoveEmptyEntries));
            return string.IsNullOrWhiteSpace(value) ? "crud" : value;
        }

        private static string NormalizeStoredPath(string path) => (path ?? string.Empty).Trim().Trim('/');

        private static CrudBuilderConfiguration Deserialize(string json)
            => string.IsNullOrWhiteSpace(json) ? new CrudBuilderConfiguration() : JsonSerializer.Deserialize<CrudBuilderConfiguration>(json) ?? new();
    }
}
