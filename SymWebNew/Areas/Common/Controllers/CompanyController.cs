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
           
            var getAllData = compRepo.SelectAll();
            IEnumerable<CompanyVM> filteredData;

            if (!string.IsNullOrEmpty(param.sSearch))
            {
                filteredData = getAllData.Where(c =>
                    c.Code.ToLower().Contains(param.sSearch.ToLower()) ||
                    c.Name.ToLower().Contains(param.sSearch.ToLower()) ||
                    c.Address.ToLower().Contains(param.sSearch.ToLower()) ||
                    c.City.ToLower().Contains(param.sSearch.ToLower()) ||
                    c.PostalCode.ToLower().Contains(param.sSearch.ToLower()) ||
                    c.Phone.ToLower().Contains(param.sSearch.ToLower()) ||
                    c.Mobile.ToLower().Contains(param.sSearch.ToLower()) ||
                    c.Fax.ToLower().Contains(param.sSearch.ToLower()) ||
                    c.Remarks.ToLower().Contains(param.sSearch.ToLower()) ||
                    c.IsActive.ToString().ToLower().Contains(param.sSearch.ToLower())
                );
            }
            else
            {
                filteredData = getAllData;
            }

            var displayedCompanies = filteredData
                .Skip(param.iDisplayStart)
                .Take(param.iDisplayLength);

            var result = from c in displayedCompanies
                         select new[] 
                 {
                     Convert.ToString(c.Id),
                     c.Code,
                     c.Name,                              
                     c.Address,                           
                     //c.City,                              
                     //c.PostalCode,                        
                     c.Phone,                             
                     //c.Mobile,                            
                     //c.Fax,                               
                     c.Remarks,                           
                     c.IsActive ? "Yes" : "No"            
                 };

            return Json(new
            {
                sEcho = param.sEcho,
                iTotalRecords = getAllData.Count(),
                iTotalDisplayRecords = filteredData.Count(),
                aaData = result
            }, JsonRequestBehavior.AllowGet);
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
            CompanyVM company = compRepo.SelectById(Convert.ToInt32(identity.CompanyId));

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
