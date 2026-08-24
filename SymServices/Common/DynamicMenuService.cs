using Newtonsoft.Json;
using SymOrdinary;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Web;

namespace SymServices.Common
{
    public class DynamicMenuDefinition
    {
        public string renderArea { get; set; }
        public string permissionArea { get; set; }
        public string permissionKey { get; set; }
        public string group { get; set; }
        public string subGroup { get; set; }
        public string title { get; set; }
        public string url { get; set; }
        public int sortOrder { get; set; }
        public int layoutColumns { get; set; }
    }

    public class DynamicMenuModule
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public bool IsCurrent { get; set; }
        public bool IsVisible { get; set; }
    }

    public class DynamicMenuItem
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public bool IsCurrent { get; set; }
    }

    public class DynamicMenuSubGroup
    {
        public string Title { get; set; }
        public List<DynamicMenuItem> Items { get; set; }
    }

    public class DynamicMenuNode
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string CssClass { get; set; }
        public int SortOrder { get; set; }
        public int LayoutColumns { get; set; }
        public bool IsDropdown { get; set; }
        public bool IsCurrent { get; set; }
        public List<DynamicMenuItem> Items { get; set; }
        public List<DynamicMenuSubGroup> SubGroups { get; set; }
    }

    public class DynamicNavbarViewModel
    {
        public DynamicNavbarViewModel()
        {
            Modules = new List<DynamicMenuModule>();
            Nodes = new List<DynamicMenuNode>();
        }

        public string CurrentArea { get; set; }
        public string CurrentAreaLabel { get; set; }
        public string HomeUrl { get; set; }
        public List<DynamicMenuModule> Modules { get; set; }
        public List<DynamicMenuNode> Nodes { get; set; }
    }

    public class DynamicMenuService
    {
        private static readonly object CacheLock = new object();
        private static List<DynamicMenuDefinition> _definitions = new List<DynamicMenuDefinition>();
        private static DateTime _definitionStamp = DateTime.MinValue;

        public List<DynamicMenuDefinition> GetDefinitions()
        {
            string path = GetDefinitionPath();
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return new List<DynamicMenuDefinition>();
            }

            DateTime lastWriteTime = File.GetLastWriteTimeUtc(path);
            if (_definitions.Count > 0 && _definitionStamp == lastWriteTime)
            {
                return _definitions;
            }

            lock (CacheLock)
            {
                if (_definitions.Count > 0 && _definitionStamp == lastWriteTime)
                {
                    return _definitions;
                }

                string json = File.ReadAllText(path);
                List<DynamicMenuDefinition> definitions = JsonConvert.DeserializeObject<List<DynamicMenuDefinition>>(json);
                _definitions = definitions ?? new List<DynamicMenuDefinition>();
                _definitionStamp = lastWriteTime;
            }

            return _definitions;
        }

        public bool HasPermittedDefinitions(string renderArea, DataTable permissions)
        {
            return GetPermittedDefinitions(renderArea, permissions).Any();
        }

        public List<DynamicMenuDefinition> GetPermittedDefinitions(string renderArea, DataTable permissions)
        {
            string normalizedArea = NormalizeArea(renderArea);

            return GetDefinitions()
                .Where(d => string.Equals(NormalizeArea(d.renderArea), normalizedArea, StringComparison.OrdinalIgnoreCase))
                .Where(d => HasAnyPermission(permissions, d.permissionArea, d.permissionKey))
                .OrderBy(d => d.sortOrder)
                .ThenBy(d => d.title)
                .ToList();
        }

        public DynamicNavbarViewModel BuildNavbarViewModel(
            string currentArea,
            DataTable permissions,
            IEnumerable<DynamicMenuModule> modules,
            string currentUrl,
            DynamicMenuDefinition currentDefinition)
        {
            string normalizedArea = NormalizeArea(currentArea);
            List<DynamicMenuDefinition> definitions = GetPermittedDefinitions(normalizedArea, permissions);

            DynamicNavbarViewModel model = new DynamicNavbarViewModel
            {
                CurrentArea = normalizedArea,
                CurrentAreaLabel = GetAreaLabel(normalizedArea),
                HomeUrl = "/Common/Home/",
                Modules = modules == null
                    ? new List<DynamicMenuModule>()
                    : modules.Where(m => m != null && m.IsVisible).ToList()
            };

            foreach (DynamicMenuDefinition definition in definitions.Where(d => string.IsNullOrWhiteSpace(d.group)))
            {
                model.Nodes.Add(new DynamicMenuNode
                {
                    Title = definition.title,
                    Url = definition.url,
                    CssClass = ToCssClass(definition.title),
                    SortOrder = definition.sortOrder,
                    LayoutColumns = Math.Max(1, definition.layoutColumns),
                    IsDropdown = false,
                    IsCurrent = IsCurrentDefinition(definition, currentDefinition, currentUrl),
                    Items = new List<DynamicMenuItem>(),
                    SubGroups = new List<DynamicMenuSubGroup>()
                });
            }

            foreach (IGrouping<string, DynamicMenuDefinition> group in definitions
                .Where(d => !string.IsNullOrWhiteSpace(d.group))
                .GroupBy(d => d.group))
            {
                List<DynamicMenuDefinition> groupedDefinitions = group.OrderBy(d => d.sortOrder).ThenBy(d => d.title).ToList();
                DynamicMenuNode node = new DynamicMenuNode
                {
                    Title = group.Key,
                    CssClass = ToCssClass(group.Key),
                    SortOrder = groupedDefinitions.Min(d => d.sortOrder),
                    LayoutColumns = Math.Max(1, groupedDefinitions.Max(d => d.layoutColumns)),
                    IsDropdown = true,
                    Items = groupedDefinitions
                        .Where(d => string.IsNullOrWhiteSpace(d.subGroup))
                        .Select(d => CreateMenuItem(d, currentDefinition, currentUrl))
                        .ToList(),
                    SubGroups = groupedDefinitions
                        .Where(d => !string.IsNullOrWhiteSpace(d.subGroup))
                        .GroupBy(d => d.subGroup)
                        .OrderBy(subGroup => subGroup.Min(d => d.sortOrder))
                        .ThenBy(subGroup => subGroup.Key)
                        .Select(subGroup => new DynamicMenuSubGroup
                        {
                            Title = subGroup.Key,
                            Items = subGroup
                                .OrderBy(d => d.sortOrder)
                                .ThenBy(d => d.title)
                                .Select(d => CreateMenuItem(d, currentDefinition, currentUrl))
                                .ToList()
                        })
                        .ToList()
                };

                node.IsCurrent = node.Items.Any(i => i.IsCurrent)
                    || node.SubGroups.Any(sg => sg.Items.Any(i => i.IsCurrent));

                model.Nodes.Add(node);
            }

            model.Nodes = model.Nodes
                .OrderBy(n => n.SortOrder)
                .ThenBy(n => n.Title)
                .ToList();

            return model;
        }

        public DynamicNavbarViewModel BuildNavbarViewModel(string currentArea)
        {
            HttpContext context = HttpContext.Current;
            ShampanIdentity identity = Thread.CurrentPrincipal.Identity as ShampanIdentity;
            DataTable permissions = new DataTable();

            if (context != null && context.Session != null && identity != null && !string.IsNullOrWhiteSpace(identity.UserId))
            {
                DataTable sessionPermissions = context.Session[identity.UserId.Trim() + "-SymRoll"] as DataTable;
                if (sessionPermissions != null)
                {
                    permissions = sessionPermissions;
                }
            }

            string currentUrl = context != null && context.Request != null
                ? context.Request.RawUrl
                : string.Empty;

            DynamicMenuDefinition currentDefinition = ResolveDefinition(
                GetRouteValue(context, "area"),
                GetRouteValue(context, "controller"),
                GetRouteValue(context, "action"),
                currentUrl);

            return BuildNavbarViewModel(
                currentArea,
                permissions,
                BuildModules(identity, permissions, currentArea),
                currentUrl,
                currentDefinition);
        }

        public DynamicMenuDefinition ResolveCurrentDefinition()
        {
            HttpContext context = HttpContext.Current;
            if (context == null || context.Request == null)
            {
                return null;
            }

            return ResolveDefinition(
                GetRouteValue(context, "area"),
                GetRouteValue(context, "controller"),
                GetRouteValue(context, "action"),
                context.Request.RawUrl);
        }

        public DynamicMenuDefinition ResolveDefinition(string area, string controller, string action, string requestUrl)
        {
            List<DynamicMenuDefinition> definitions = GetDefinitions();
            if (definitions.Count == 0)
            {
                return null;
            }

            string normalizedArea = NormalizeArea(area);
            string normalizedController = NormalizeSegment(controller);
            string normalizedAction = NormalizeSegment(action);
            string normalizedRequestUrl = NormalizeUrl(requestUrl);
            string requestPath = GetPathOnly(normalizedRequestUrl);
            string requestQuery = GetQueryOnly(normalizedRequestUrl);

            return definitions
                .Select(d => new
                {
                    Definition = d,
                    Score = CalculateMatchScore(d, normalizedArea, normalizedController, normalizedAction, normalizedRequestUrl, requestPath, requestQuery)
                })
                .Where(x => x.Score >= 0)
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Definition.sortOrder)
                .Select(x => x.Definition)
                .FirstOrDefault();
        }

        public bool TryResolvePermission(DataTable permissions, string defaultRollId, string actionName, out bool isAllowed)
        {
            isAllowed = false;

            if (permissions == null || permissions.Rows.Count == 0)
            {
                return false;
            }

            DynamicMenuDefinition definition = ResolveCurrentDefinition();
            if (definition != null)
            {
                List<DataRow> matchingRows = FindPermissionRows(permissions, definition.permissionArea, definition.permissionKey).ToList();
                if (matchingRows.Count > 0)
                {
                    isAllowed = matchingRows.Any(row => GetPermissionValue(row, actionName));
                    if (isAllowed)
                    {
                        return true;
                    }
                }
            }

            DataRow fallbackRow = FindRowByDefaultRollId(permissions, defaultRollId);
            if (fallbackRow != null)
            {
                isAllowed = GetPermissionValue(fallbackRow, actionName);
                return true;
            }

            return false;
        }

        public bool TryResolvePermissionByAreaController(DataTable permissions, string area, string controller, string actionName, out bool isAllowed)
        {
            isAllowed = false;

            if (permissions == null || permissions.Rows.Count == 0)
            {
                return false;
            }

            List<DataRow> matchingRows = permissions.AsEnumerable()
                .Where(row => string.Equals(GetString(row, "symArea"), area, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(GetString(row, "symController"), controller, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matchingRows.Count == 0)
            {
                return false;
            }

            isAllowed = matchingRows.Any(row => GetPermissionValue(row, actionName));
            return true;
        }

        private static DynamicMenuItem CreateMenuItem(DynamicMenuDefinition definition, DynamicMenuDefinition currentDefinition, string currentUrl)
        {
            return new DynamicMenuItem
            {
                Title = definition.title,
                Url = definition.url,
                IsCurrent = IsCurrentDefinition(definition, currentDefinition, currentUrl)
            };
        }

        private static bool IsCurrentDefinition(DynamicMenuDefinition definition, DynamicMenuDefinition currentDefinition, string currentUrl)
        {
            if (definition == null)
            {
                return false;
            }

            if (currentDefinition != null
                && string.Equals(definition.permissionArea, currentDefinition.permissionArea, StringComparison.OrdinalIgnoreCase)
                && string.Equals(definition.permissionKey, currentDefinition.permissionKey, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string normalizedCurrentUrl = NormalizeUrl(currentUrl);
            string currentPath = GetPathOnly(normalizedCurrentUrl);
            string definitionPath = GetPathOnly(NormalizeUrl(definition.url));

            if (string.IsNullOrWhiteSpace(currentPath) || string.IsNullOrWhiteSpace(definitionPath))
            {
                return false;
            }

            return string.Equals(currentPath, definitionPath, StringComparison.OrdinalIgnoreCase)
                || currentPath.StartsWith(definitionPath + "/", StringComparison.OrdinalIgnoreCase);
        }

        private static IEnumerable<DataRow> FindPermissionRows(DataTable permissions, string permissionArea, string permissionKey)
        {
            if (permissions == null || permissions.Rows.Count == 0)
            {
                return Enumerable.Empty<DataRow>();
            }

            return permissions.AsEnumerable()
                .Where(row => string.Equals(GetString(row, "symArea"), permissionArea, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(GetString(row, "symController"), permissionKey, StringComparison.OrdinalIgnoreCase));
        }

        private static DataRow FindRowByDefaultRollId(DataTable permissions, string defaultRollId)
        {
            if (permissions == null || permissions.Rows.Count == 0 || string.IsNullOrWhiteSpace(defaultRollId))
            {
                return null;
            }

            return permissions.AsEnumerable()
                .FirstOrDefault(row => string.Equals(GetString(row, "DefaultRollId"), defaultRollId, StringComparison.OrdinalIgnoreCase));
        }

        private static bool HasAnyPermission(DataTable permissions, string permissionArea, string permissionKey)
        {
            ShampanIdentity identity = Thread.CurrentPrincipal.Identity as ShampanIdentity;
            if (identity != null && identity.IsAdmin
                && string.Equals(permissionArea, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            List<DataRow> rows = FindPermissionRows(permissions, permissionArea, permissionKey).ToList();
            if (rows.Count == 0)
            {
                return false;
            }

            return rows
                .Any(row => ToBoolean(row["IsIndex"])
                    || ToBoolean(row["IsAdd"])
                    || ToBoolean(row["IsEdit"])
                    || ToBoolean(row["IsDelete"])
                    || ToBoolean(row["IsReport"])
                    || ToBoolean(row["IsProcess"]));
        }

        private static bool GetPermissionValue(DataRow row, string actionName)
        {
            if (row == null)
            {
                return false;
            }

            string normalizedAction = NormalizeSegment(actionName);
            switch (normalizedAction)
            {
                case "index":
                    return ToBoolean(row["IsIndex"]);
                case "add":
                case "create":
                    return ToBoolean(row["IsAdd"]);
                case "edit":
                case "update":
                    return ToBoolean(row["IsEdit"]);
                case "delete":
                case "remove":
                    return ToBoolean(row["IsDelete"]);
                case "report":
                    return ToBoolean(row["IsReport"]);
                case "process":
                    return ToBoolean(row["IsProcess"]);
                default:
                    return ToBoolean(row["IsIndex"]);
            }
        }

        private static int CalculateMatchScore(
            DynamicMenuDefinition definition,
            string normalizedArea,
            string normalizedController,
            string normalizedAction,
            string normalizedRequestUrl,
            string requestPath,
            string requestQuery)
        {
            if (definition == null)
            {
                return -1;
            }

            string definitionUrl = NormalizeUrl(definition.url);
            string definitionPath = GetPathOnly(definitionUrl);
            string definitionQuery = GetQueryOnly(definitionUrl);
            string definitionArea = GetUrlSegment(definitionPath, 0);
            string definitionController = GetUrlSegment(definitionPath, 1);
            string definitionAction = GetUrlSegment(definitionPath, 2);

            int score = -1;

            if (!string.IsNullOrWhiteSpace(normalizedRequestUrl) && string.Equals(definitionUrl, normalizedRequestUrl, StringComparison.OrdinalIgnoreCase))
            {
                score = 300;
            }
            else if (!string.IsNullOrWhiteSpace(requestPath) && string.Equals(definitionPath, requestPath, StringComparison.OrdinalIgnoreCase))
            {
                score = 260;
            }
            else if (!string.IsNullOrWhiteSpace(requestPath)
                && !string.IsNullOrWhiteSpace(definitionPath)
                && requestPath.StartsWith(definitionPath + "/", StringComparison.OrdinalIgnoreCase))
            {
                score = 220;
            }
            else if (!string.IsNullOrWhiteSpace(normalizedArea)
                && !string.IsNullOrWhiteSpace(normalizedController)
                && string.Equals(NormalizeArea(definitionArea), normalizedArea, StringComparison.OrdinalIgnoreCase)
                && string.Equals(definitionController, normalizedController, StringComparison.OrdinalIgnoreCase))
            {
                score = 180;
            }

            if (score < 0)
            {
                return -1;
            }

            if (!string.IsNullOrWhiteSpace(normalizedAction))
            {
                if (string.Equals(definitionAction, normalizedAction, StringComparison.OrdinalIgnoreCase))
                {
                    score += 40;
                }
                else if (string.IsNullOrWhiteSpace(definitionAction) || string.Equals(definitionAction, "index", StringComparison.OrdinalIgnoreCase))
                {
                    score += 10;
                }
            }

            if (!string.IsNullOrWhiteSpace(requestQuery))
            {
                if (string.Equals(definitionQuery, requestQuery, StringComparison.OrdinalIgnoreCase))
                {
                    score += 20;
                }
                else if (string.IsNullOrWhiteSpace(definitionQuery))
                {
                    score += 3;
                }
            }
            else if (string.IsNullOrWhiteSpace(definitionQuery))
            {
                score += 3;
            }

            return score;
        }

        private static string GetDefinitionPath()
        {
            HttpContext context = HttpContext.Current;
            if (context != null && context.Server != null)
            {
                return context.Server.MapPath("~/App_Data/dynamic-menu-definitions.json");
            }

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "dynamic-menu-definitions.json");
        }

        private static string GetRouteValue(HttpContext context, string key)
        {
            if (context == null || context.Request == null || context.Request.RequestContext == null || context.Request.RequestContext.RouteData == null)
            {
                return string.Empty;
            }

            object dataToken = context.Request.RequestContext.RouteData.DataTokens[key];
            if (dataToken != null)
            {
                return Convert.ToString(dataToken);
            }

            object routeValue = context.Request.RequestContext.RouteData.Values[key];
            if (routeValue != null)
            {
                return Convert.ToString(routeValue);
            }

            return string.Empty;
        }

        private static string GetAreaLabel(string area)
        {
            if (string.IsNullOrWhiteSpace(area))
            {
                return "Admin";
            }

            return area;
        }

        private List<DynamicMenuModule> BuildModules(ShampanIdentity identity, DataTable permissions, string currentArea)
        {
            List<DynamicMenuModule> modules = new List<DynamicMenuModule>();
            string currentModule = string.IsNullOrWhiteSpace(currentArea) ? "Admin" : currentArea.Trim();

            if (identity == null)
            {
                return modules;
            }

            List<string> dynamicAreas = new List<string> { "Admin", "PF", "GL", "WPPF" };

            foreach (string areaName in dynamicAreas)
            {
                bool hasPermission = HasPermittedDefinitions(areaName, permissions)
                    || (identity.IsAdmin && IsCurrentArea(currentModule, areaName));
                bool isModuleEnabled = IsModuleEnabled(identity, areaName);

                modules.Add(CreateModule(
                    areaName,
                    GetHomeUrl(areaName),
                    hasPermission && isModuleEnabled,
                    currentModule));
            }

            return modules
                .Where(m => m.IsVisible)
                .OrderBy(m => GetModuleDisplayOrder(m.Title))
                .ThenBy(m => m.Title)
                .ToList();
        }

        private static string GetHomeUrl(string area)
        {
            string normalizedArea = NormalizeArea(area);
            if (string.Equals(normalizedArea, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return "/Common/Home/";
            }

            if (string.Equals(normalizedArea, "GL", StringComparison.OrdinalIgnoreCase))
            {
                return "/PF/Journal?JournalType=1";
            }

            return "/" + normalizedArea + "/Home/";
        }

        private static bool IsModuleEnabled(ShampanIdentity identity, string areaName)
        {
            if (identity == null)
            {
                return false;
            }

            if (string.Equals(areaName, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return identity.IsAdmin;
            }

            if (string.Equals(areaName, "PF", StringComparison.OrdinalIgnoreCase))
            {
                return identity.IsAdmin || identity.IsPF;
            }

            if (string.Equals(areaName, "WPPF", StringComparison.OrdinalIgnoreCase))
            {
                return identity.IsAdmin || identity.IsGF;
            }

            if (string.Equals(areaName, "GL", StringComparison.OrdinalIgnoreCase))
            {
                return identity.IsAdmin || identity.IsAccount || identity.IsHRM;
            }

            string propertyName = "Is" + areaName;
            PropertyInfo property = typeof(ShampanIdentity).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

            if (property == null || property.PropertyType != typeof(bool))
            {
                return true;
            }

            return Convert.ToBoolean(property.GetValue(identity, null));
        }

        private static DynamicMenuModule CreateModule(string title, string url, bool isVisible, string currentArea)
        {
            return new DynamicMenuModule
            {
                Title = title,
                Url = url,
                IsVisible = isVisible || IsCurrentArea(currentArea, title),
                IsCurrent = IsCurrentArea(currentArea, title)
            };
        }

        private static bool IsCurrentArea(string currentArea, string areaName)
        {
            return string.Equals(
                string.IsNullOrWhiteSpace(currentArea) ? "Admin" : currentArea.Trim(),
                areaName,
                StringComparison.OrdinalIgnoreCase);
        }

        private static int GetModuleDisplayOrder(string moduleTitle)
        {
            string normalizedTitle = (moduleTitle ?? string.Empty).Trim();

            if (string.Equals(normalizedTitle, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return 10;
            }

            if (string.Equals(normalizedTitle, "HRM", StringComparison.OrdinalIgnoreCase))
            {
                return 20;
            }

            if (string.Equals(normalizedTitle, "Payroll", StringComparison.OrdinalIgnoreCase))
            {
                return 30;
            }

            if (string.Equals(normalizedTitle, "PF", StringComparison.OrdinalIgnoreCase))
            {
                return 40;
            }

            if (string.Equals(normalizedTitle, "GL", StringComparison.OrdinalIgnoreCase))
            {
                return 50;
            }

            if (string.Equals(normalizedTitle, "WPPF", StringComparison.OrdinalIgnoreCase))
            {
                return 60;
            }

            if (string.Equals(normalizedTitle, "Appraisal", StringComparison.OrdinalIgnoreCase))
            {
                return 70;
            }

            if (string.Equals(normalizedTitle, "Recruitment", StringComparison.OrdinalIgnoreCase))
            {
                return 80;
            }

            if (string.Equals(normalizedTitle, "Attendance", StringComparison.OrdinalIgnoreCase))
            {
                return 90;
            }

            if (string.Equals(normalizedTitle, "ESS", StringComparison.OrdinalIgnoreCase))
            {
                return 100;
            }

            return 1000;
        }

        private static string NormalizeArea(string area)
        {
            string normalizedArea = NormalizeSegment(area);
            if (string.Equals(normalizedArea, "common", StringComparison.OrdinalIgnoreCase))
            {
                return "Admin";
            }

            if (string.IsNullOrWhiteSpace(normalizedArea))
            {
                return "Admin";
            }

            if (string.Equals(normalizedArea, "admin", StringComparison.OrdinalIgnoreCase))
            {
                return "Admin";
            }

            return normalizedArea;
        }

        private static string NormalizeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return string.Empty;
            }

            string normalizedUrl = url.Trim();

            Uri uri;
            if (Uri.TryCreate(normalizedUrl, UriKind.Absolute, out uri))
            {
                normalizedUrl = uri.PathAndQuery;
            }

            int hashIndex = normalizedUrl.IndexOf('#');
            if (hashIndex >= 0)
            {
                normalizedUrl = normalizedUrl.Substring(0, hashIndex);
            }

            if (!normalizedUrl.StartsWith("/"))
            {
                normalizedUrl = "/" + normalizedUrl.TrimStart('~');
            }

            normalizedUrl = normalizedUrl.Replace("//", "/");

            string query = string.Empty;
            int queryIndex = normalizedUrl.IndexOf('?');
            if (queryIndex >= 0)
            {
                query = normalizedUrl.Substring(queryIndex);
                normalizedUrl = normalizedUrl.Substring(0, queryIndex);
            }

            normalizedUrl = normalizedUrl.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(normalizedUrl))
            {
                normalizedUrl = "/";
            }

            return normalizedUrl + query;
        }

        private static string GetPathOnly(string url)
        {
            string normalizedUrl = NormalizeUrl(url);
            int queryIndex = normalizedUrl.IndexOf('?');
            if (queryIndex >= 0)
            {
                return normalizedUrl.Substring(0, queryIndex);
            }

            return normalizedUrl;
        }

        private static string GetQueryOnly(string url)
        {
            string normalizedUrl = NormalizeUrl(url);
            int queryIndex = normalizedUrl.IndexOf('?');
            if (queryIndex < 0)
            {
                return string.Empty;
            }

            return normalizedUrl.Substring(queryIndex + 1);
        }

        private static string GetUrlSegment(string path, int index)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            string[] segments = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length <= index)
            {
                return string.Empty;
            }

            if (index == 0)
            {
                return NormalizeArea(segments[index]);
            }

            return NormalizeSegment(segments[index]);
        }

        private static string NormalizeSegment(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
        }

        private static string ToCssClass(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string cssClass = new string(value
                .Where(ch => char.IsLetterOrDigit(ch))
                .ToArray());

            return cssClass;
        }

        private static string GetString(DataRow row, string columnName)
        {
            if (row == null || row.Table == null || !row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
            {
                return string.Empty;
            }

            return Convert.ToString(row[columnName]).Trim();
        }

        private static bool ToBoolean(object value)
        {
            if (value == null || value == DBNull.Value)
            {
                return false;
            }

            bool parsedValue;
            if (bool.TryParse(Convert.ToString(value), out parsedValue))
            {
                return parsedValue;
            }

            int parsedNumber;
            if (int.TryParse(Convert.ToString(value), out parsedNumber))
            {
                return parsedNumber != 0;
            }

            return false;
        }
    }
}
