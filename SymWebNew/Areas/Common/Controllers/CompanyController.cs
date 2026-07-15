using JQueryDataTables.Models;
using SymOrdinary;
using SymRepository.Common;
using SymViewModel.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;
namespace SymWebUI.Areas.Common.Controllers
{
    [Authorize]
    public class CompanyController : Controller
    {
        //
        // GET: /Common/Company/

        SymUserRoleRepo _reposur = new SymUserRoleRepo();
        ShampanIdentity identity = (ShampanIdentity)Thread.CurrentPrincipal.Identity;
        CompanyRepo compRepo = new CompanyRepo();

        /// <summary>
        /// Created: 10 Feb 2025  
        /// Created By: Md Torekul Islam  
        /// Retrieves all Company information.
        /// </summary>      
        /// <returns>View containing Company</returns>
        public ActionResult Index()
        {
            //List<CompanyVM> company = compRepo.SelectAll();
            //return View(company);
            return View("~/Areas/Common/Views/Company/Index.cshtml");
        }


        public ActionResult _index(JQueryDataTableParamModel param)
        {
            #region Column Search

            var Id = Convert.ToString(Request["sSearch_0"]);
            var Code = Convert.ToString(Request["sSearch_1"]);
            var Name = Convert.ToString(Request["sSearch_2"]);
            var Address = Convert.ToString(Request["sSearch_3"]);
            var Phone = Convert.ToString(Request["sSearch_4"]);
            var Remarks = Convert.ToString(Request["sSearch_5"]);
            var IsActive = Convert.ToString(Request["sSearch_6"]);

            #endregion Column Search

            #region Get Data

            var getAllData = compRepo.SelectAll() ?? new List<CompanyVM>();

            IEnumerable<CompanyVM> filteredData = getAllData;

            #endregion Get Data

            #region Global Search

            if (!string.IsNullOrWhiteSpace(param.sSearch))
            {
                var searchText = param.sSearch.Trim().ToLower();

                var isSearchable0 = Convert.ToBoolean(Request["bSearchable_0"]);
                var isSearchable1 = Convert.ToBoolean(Request["bSearchable_1"]);
                var isSearchable2 = Convert.ToBoolean(Request["bSearchable_2"]);
                var isSearchable3 = Convert.ToBoolean(Request["bSearchable_3"]);
                var isSearchable4 = Convert.ToBoolean(Request["bSearchable_4"]);
                var isSearchable5 = Convert.ToBoolean(Request["bSearchable_5"]);
                var isSearchable6 = Convert.ToBoolean(Request["bSearchable_6"]);

                filteredData = filteredData.Where(c =>
                       (isSearchable0 &&
                        Convert.ToString(c.Id).ToLower().Contains(searchText))

                    || (isSearchable1 &&
                        Convert.ToString(c.Code).ToLower().Contains(searchText))

                    || (isSearchable2 &&
                        Convert.ToString(c.Name).ToLower().Contains(searchText))

                    || (isSearchable3 &&
                        Convert.ToString(c.Address).ToLower().Contains(searchText))

                    || (isSearchable4 &&
                        Convert.ToString(c.Phone).ToLower().Contains(searchText))

                    || (isSearchable5 &&
                        Convert.ToString(c.Remarks).ToLower().Contains(searchText))

                    || (isSearchable6 &&
                        (
                            Convert.ToString(c.IsActive)
                                .ToLower()
                                .Contains(searchText)

                            || (c.IsActive ? "yes" : "no")
                                .Contains(searchText)

                            || (c.IsActive ? "active" : "inactive")
                                .Contains(searchText)
                        ))
                );
            }

            #endregion Global Search

            #region Individual Column Filtering

            if (!string.IsNullOrWhiteSpace(Code)
                || !string.IsNullOrWhiteSpace(Name)
                || !string.IsNullOrWhiteSpace(Address)
                || !string.IsNullOrWhiteSpace(Phone)
                || !string.IsNullOrWhiteSpace(Remarks)
                || !string.IsNullOrWhiteSpace(IsActive))
            {
                filteredData = filteredData.Where(c =>
                       (string.IsNullOrWhiteSpace(Code)
                        || Convert.ToString(c.Code)
                            .ToLower()
                            .Contains(Code.Trim().ToLower()))

                    && (string.IsNullOrWhiteSpace(Name)
                        || Convert.ToString(c.Name)
                            .ToLower()
                            .Contains(Name.Trim().ToLower()))

                    && (string.IsNullOrWhiteSpace(Address)
                        || Convert.ToString(c.Address)
                            .ToLower()
                            .Contains(Address.Trim().ToLower()))

                    && (string.IsNullOrWhiteSpace(Phone)
                        || Convert.ToString(c.Phone)
                            .ToLower()
                            .Contains(Phone.Trim().ToLower()))

                    && (string.IsNullOrWhiteSpace(Remarks)
                        || Convert.ToString(c.Remarks)
                            .ToLower()
                            .Contains(Remarks.Trim().ToLower()))

                    && IsActiveMatches(c.IsActive, IsActive)
                );
            }

            #endregion Individual Column Filtering

            #region Sorting

            var isSortable0 = Convert.ToBoolean(Request["bSortable_0"]);
            var isSortable1 = Convert.ToBoolean(Request["bSortable_1"]);
            var isSortable2 = Convert.ToBoolean(Request["bSortable_2"]);
            var isSortable3 = Convert.ToBoolean(Request["bSortable_3"]);
            var isSortable4 = Convert.ToBoolean(Request["bSortable_4"]);
            var isSortable5 = Convert.ToBoolean(Request["bSortable_5"]);
            var isSortable6 = Convert.ToBoolean(Request["bSortable_6"]);

            var sortColumnIndex = 0;

            if (!string.IsNullOrWhiteSpace(Request["iSortCol_0"]))
            {
                int.TryParse(Request["iSortCol_0"], out sortColumnIndex);
            }

            Func<CompanyVM, string> orderingFunction = c =>
                sortColumnIndex == 0 && isSortable0
                    ? Convert.ToString(c.Id)

                : sortColumnIndex == 1 && isSortable1
                    ? Convert.ToString(c.Code)

                : sortColumnIndex == 2 && isSortable2
                    ? Convert.ToString(c.Name)

                : sortColumnIndex == 3 && isSortable3
                    ? Convert.ToString(c.Address)

                : sortColumnIndex == 4 && isSortable4
                    ? Convert.ToString(c.Phone)

                : sortColumnIndex == 5 && isSortable5
                    ? Convert.ToString(c.Remarks)

                : sortColumnIndex == 6 && isSortable6
                    ? Convert.ToString(c.IsActive)

                : Convert.ToString(c.Id);

            var sortDirection = Convert.ToString(Request["sSortDir_0"]);

            if (sortDirection.Equals(
                "desc",
                StringComparison.OrdinalIgnoreCase))
            {
                filteredData = filteredData
                    .OrderByDescending(orderingFunction);
            }
            else
            {
                filteredData = filteredData
                    .OrderBy(orderingFunction);
            }

            #endregion Sorting

            #region Pagination

            var totalDisplayRecords = filteredData.Count();

            var displayedCompanies = filteredData
                .Skip(param.iDisplayStart)
                .Take(param.iDisplayLength);

            #endregion Pagination

            #region Result

            var result = from c in displayedCompanies
                         select new[]
                         {
                             Convert.ToString(c.Id),
                             Convert.ToString(c.Code),
                             Convert.ToString(c.Name),
                             Convert.ToString(c.Address),
                             Convert.ToString(c.Phone),
                             Convert.ToString(c.Remarks),
                             c.IsActive ? "Yes" : "No"
                         };

            return Json(
                new
                {
                    sEcho = param.sEcho,
                    iTotalRecords = getAllData.Count(),
                    iTotalDisplayRecords = totalDisplayRecords,
                    aaData = result
                },
                JsonRequestBehavior.AllowGet);

            #endregion Result
        }

        private bool IsActiveMatches(bool companyIsActive, string searchValue)
        {
            if (string.IsNullOrWhiteSpace(searchValue))
            {
                return true;
            }

            var value = searchValue.Trim().ToLower();

            if (value == "true"
                || value == "yes"
                || value == "active"
                || value == "1")
            {
                return companyIsActive;
            }

            if (value == "false"
                || value == "no"
                || value == "inactive"
                || value == "0")
            {
                return !companyIsActive;
            }

            return Convert.ToString(companyIsActive)
                .ToLower()
                .Contains(value);
        }




        /// <summary>
        /// Displays the view for creating a new entry. 
        /// This method checks the user's permission for the "add" action in the "1_7" role. 
        /// If the user does not have the appropriate permissions, they are redirected to a different page.
        /// </summary>
        /// <returns>
        /// The "Create" view, or a redirect to another page if the user lacks the required permissions.
        /// </returns>
        /// <remarks>
        /// This action method fetches the user's permission status for adding an entry (via the `SymRoleSession` method).
        /// If the user has the necessary permissions, the "Create" view is returned. 
        /// If the user does not have permission, they are redirected to a common home page.
        /// </remarks>
        [Authorize(Roles = "Master,Admin,Account")]
        [HttpGet]
        public ActionResult Create()
        {
            CompanyVM company = compRepo.SelectAll().FirstOrDefault();
            if (company !=null)
            {
               // return RedirectToAction("Edit");
            }
            return View();
        }
        /// <summary>
        /// Creates a new branch entry. This method accepts a `BranchVM` model, populates some additional fields 
        /// such as `CreatedAt`, `CreatedBy`, and `CreatedFrom` before calling the repository's `Insert` method to save the data.
        /// If the operation is successful, the user is redirected to the "Index" page, otherwise an error message is logged and 
        /// the same view is returned with the model.
        /// </summary>
        /// <param name="BranchVM">The branch view model containing the data to be created.</param>
        /// <returns>
        /// If the insertion is successful, redirects to the "Index" action.
        /// If the insertion fails, logs the error and returns the same view with the model.
        /// </returns>
        /// <remarks>
        /// The method uses the `BranchRepo`'s `Insert` method to save the branch details. In case of an exception, 
        /// the error details are logged using the `FileLogger` class and a failure message is stored in the session.
        /// </remarks>
        [Authorize(Roles = "Master,Admin,Account")]
        [HttpPost]
        public ActionResult Create(CompanyVM company, HttpPostedFileBase file)
        {
            string[] result = new string[6];
            company.CreatedAt = DateTime.Now.ToString("yyyyMMddHHmmss");
            company.CreatedBy = Ordinary.UserName;
            company.CreatedFrom = Ordinary.WorkStationIP;
            try
            {
                result = compRepo.Insert(company);

                    if (file != null && file.ContentLength > 0)
                    {
                        string logoName = "LOGO_Sym.png";
                        var photoResult = compRepo.UpdatePhoto( logoName);
                        if (photoResult[0] == "Success")
                        {
                            string dirPath = Server.MapPath("~/Images/");
                            if (!Directory.Exists(dirPath))
                                Directory.CreateDirectory(dirPath);

                            string filePath = Path.Combine(dirPath, logoName);
                            file.SaveAs(filePath);
                        }
                    }

                Session["result"] = result[0] + "~" + result[1];
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                Session["result"] = "Fail~Data Not Succeessfully!";
                FileLogger.Log(result[0].ToString() + Environment.NewLine + result[2].ToString() + Environment.NewLine + result[5].ToString(), this.GetType().Name, result[4].ToString() + Environment.NewLine + result[3].ToString());
                return View(company);
            }
        }
        /// <summary>
        /// Handles the HTTP GET request to load the edit view for a specific department.
        /// Checks user permission and retrieves Company data by ID.
        /// </summary>
        /// <param name="id">The ID of the Company to be edited.</param>
        /// <returns>
        /// A <see cref="PartialViewResult"/> containing the <see cref="CompanyVM"/> to populate the edit form.
        /// </returns>
        [Authorize(Roles = "Master,Admin,Account")]
        [HttpGet]
        public ActionResult Edit(int Id)
        {
            ShampanIdentity identity = (ShampanIdentity)Thread.CurrentPrincipal.Identity;
            CompanyVM company = compRepo.SelectById(Convert.ToInt32(Id));

            return View(company);
        }

        /// <summary>
        /// Handles the HTTP GET request to load the edit view for a specific department.
        /// Checks user permission and retrieves Bank Company by ID.
        /// </summary>
        /// <param name="id">The ID of the Company to be edited.</param>
        /// <returns>
        /// A <see cref="PartialViewResult"/> containing the <see cref="CompanyVM"/> to populate the edit form.
        /// </returns>
        [Authorize(Roles = "Master,Admin,Account")]
        [HttpPost]
        public ActionResult Edit(CompanyVM company, HttpPostedFileBase file)
        { 
            string[] result = new string[6];            
            ShampanIdentity identity = (ShampanIdentity)Thread.CurrentPrincipal.Identity;
            company.LastUpdateAt = DateTime.Now.ToString("yyyyMMddHHmmss");
            company.LastUpdateBy = Ordinary.UserName;
            company.LastUpdateFrom = Ordinary.WorkStationIP;
            company.CurrentBranch=Convert.ToInt32(identity.BranchId);
            try
            {
                result = compRepo.Update(company);

                if (file != null && file.ContentLength > 0)
                {
                    string logoName = "LOGO_Sym.png";
                    var photoResult = compRepo.UpdatePhoto(logoName);
                    if (photoResult[0] == "Success")
                    {
                        string dirPath = Server.MapPath("~/Images/");
                        if (!Directory.Exists(dirPath))
                            Directory.CreateDirectory(dirPath);

                        string filePath = Path.Combine(dirPath, logoName);
                        file.SaveAs(filePath);
                    }
                }

                Session["result"] = result[0] + "~" + result[1];
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                Session["result"] = "Fail~Data Not Succeessfully!";
                FileLogger.Log(result[0].ToString() + Environment.NewLine + result[2].ToString() + Environment.NewLine + result[5].ToString(), this.GetType().Name, result[4].ToString() + Environment.NewLine + result[3].ToString());
            }
            try
            {
                company.Year= DateTime.Parse(company.YearStart).ToString("yyyy");
            }
            catch (Exception)
            {
            }
            return View(company);
        }
    }
}
