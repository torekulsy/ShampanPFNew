using SymServices.PF;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using SymOrdinary;
using SymServices.Common;
using SymViewModel.Common;
using SymViewModel.PF;
using System.IO;
using Excel;

namespace SymServices.GF
{
    public class GFProfitDistributionNewDAL
    {
        #region Global Variables
        private const string FieldDelimeter = DBConstant.FieldDelimeter;
        private DBSQLConnection _dbsqlConnection = new DBSQLConnection();
        CommonDAL _cDal = new CommonDAL();
        #endregion


        //==================SelectAll=================
        public List<ProfitDistributionNewVM> SelectAll(int Id = 0, string[] conditionFields = null, string[] conditionValues = null, SqlConnection VcurrConn = null, SqlTransaction Vtransaction = null, bool isGF = true)
        {
            #region Variables
            SqlConnection currConn = null;
            SqlTransaction transaction = null;
            string sqlText = "";
            List<ProfitDistributionNewVM> VMs = new List<ProfitDistributionNewVM>();
            ProfitDistributionNewVM vm;
            #endregion
            try
            {
                #region open connection and transaction
                #region New open connection and transaction
                if (VcurrConn != null)
                {
                    currConn = VcurrConn;
                }
                if (Vtransaction != null)
                {
                    transaction = Vtransaction;
                }
                #endregion New open connection and transaction
                if (currConn == null)
                {
                    currConn = _dbsqlConnection.GetConnection();
                    if (currConn.State != ConnectionState.Open)
                    {
                        currConn.Open();
                    }
                }
                if (transaction == null)
                {
                    transaction = currConn.BeginTransaction("");
                }
                #endregion open connection and transaction
                string hrmDB = ("[" + currConn.Database.Replace("]", "]]") + "]");
                string distributionTable = isGF ? "GFProfitDistributionNew" : "ProfitDistributionNew";
                string preDistributionFundColumn = isGF ? "GFPreDistributionFundId" : "PreDistributionFundId";
                string employeeContributionColumn = isGF ? "CAST(0 AS DECIMAL(18,2))" : "pd.EmployeeContribution";
                string employeeProfitColumn = isGF ? "CAST(0 AS DECIMAL(18,2))" : "pd.EmployeeProfit";
                string employeeProfitDistributionColumn = isGF ? "CAST(0 AS DECIMAL(18,2))" : "pd.EmployeeProfitDistribution";

                #region sql statement
                #region SqlText

                sqlText = @"
SELECT
 pd.Id
,fydFrom.PeriodName PeriodNameFrom

,pd.Id
,ve.Code
,ve.EmpName
,pd." + preDistributionFundColumn + @" PreDistributionFundId
,pd.EmployeeId
,pd.DistributionDate
,pd.FiscalYearDetailId
," + employeeContributionColumn + @" EmployeeContribution
,pd.EmployerContribution
," + employeeProfitColumn + @" EmployeeProfit
,pd.EmployerProfit
,pd.MultiplicationFactor
," + employeeProfitDistributionColumn + @" EmployeeProfitDistribution
,pd.EmployeerProfitDistribution
,pd.TotalProfit
,ISNULL(pd.IsPaid,0) IsPaid
,pd.Post
,pd.Remarks
,pd.IsActive
,pd.IsArchive
,pd.CreatedBy
,pd.CreatedAt
,pd.CreatedFrom
,pd.LastUpdateBy
,pd.LastUpdateAt
,pd.LastUpdateFrom

FROM " + distributionTable + @" pd
";
                sqlText = sqlText + @" LEFT OUTER JOIN " + hrmDB + ".[dbo].FiscalYearDetail fydFrom ON pd.FiscalYearDetailId=fydFrom.Id";
                sqlText = sqlText + @" LEFT OUTER JOIN " + hrmDB + ".[dbo].ViewEmployeeInformation ve ON ve.EmployeeId=pd.EmployeeId";

                sqlText = sqlText + @" WHERE  1=1 AND pd.IsActive = 1";


                if (Id > 0)
                {
                    sqlText += @" and pd." + preDistributionFundColumn + @"=@Id";
                }

                string cField = "";
                if (conditionFields != null && conditionValues != null && conditionFields.Length == conditionValues.Length)
                {
                    for (int i = 0; i < conditionFields.Length; i++)
                    {
                        if (string.IsNullOrWhiteSpace(conditionFields[i]) || string.IsNullOrWhiteSpace(conditionValues[i]))
                        {
                            continue;
                        }
                        cField = conditionFields[i].ToString();
                        cField = Ordinary.StringReplacing(cField);
                        sqlText += " AND " + conditionFields[i] + "=@" + cField;
                    }
                }
                #endregion SqlText
                #region SqlExecution

                SqlCommand objComm = new SqlCommand(sqlText, currConn, transaction);
                if (conditionFields != null && conditionValues != null && conditionFields.Length == conditionValues.Length)
                {
                    for (int j = 0; j < conditionFields.Length; j++)
                    {
                        if (string.IsNullOrWhiteSpace(conditionFields[j]) || string.IsNullOrWhiteSpace(conditionValues[j]))
                        {
                            continue;
                        }
                        cField = conditionFields[j].ToString();
                        cField = Ordinary.StringReplacing(cField);
                        objComm.Parameters.AddWithValue("@" + cField, conditionValues[j]);
                    }
                }

                if (Id > 0)
                {
                    objComm.Parameters.AddWithValue("@Id", Id);
                }
                SqlDataReader dr;
                dr = objComm.ExecuteReader();
                while (dr.Read())
                {
                    vm = new ProfitDistributionNewVM();
                    vm.Id = Convert.ToInt32(dr["Id"]);
                    vm.PeriodNameFrom = dr["PeriodNameFrom"].ToString();
                    vm.EmployeeCode = dr["Code"].ToString();
                    vm.EmployeeName = dr["EmpName"].ToString();

                    //vm.FiscalYearDetailId = Convert.ToInt32(dr["FiscalYearDetailId"]);
                    //vm.PFDetailFiscalYearDetailIds = dr["PFDetailFiscalYearDetailIds"].ToString();
                    //vm.PreDistributionFundIds = dr["PreDistributionFundIds"].ToString();
                    //vm.DistributionDate = Ordinary.StringToDate(dr["DistributionDate"].ToString());
                    //vm.TotalEmployeeContribution = Convert.ToDecimal(dr["TotalEmployeeContribution"]);
                    //vm.TotalEmployerContribution = Convert.ToDecimal(dr["TotalEmployerContribution"]);
                    //vm.TotalProfit = Convert.ToDecimal(dr["TotalProfit"]);

                    //vm.FiscalYearDetailIdTo = Convert.ToInt32(dr["FiscalYearDetailIdTo"]);
                    //vm.TotalExpense = Convert.ToDecimal(dr["TotalExpense"]);
                    //vm.AvailableDistributionAmount = Convert.ToDecimal(dr["AvailableDistributionAmount"]);

                    //vm.TotalWeightedContribution = Convert.ToDecimal(dr["TotalWeightedContribution"]);
                    //vm.MultiplicationFactor = Convert.ToDecimal(dr["MultiplicationFactor"]);
                    vm.TotalProfit = Convert.ToDecimal(dr["TotalProfit"]);

                    vm.PreDistributionFundId = Convert.ToString(dr["PreDistributionFundId"]);
                    vm.EmployeeId = Convert.ToString(dr["PreDistributionFundId"]);
                    vm.DistributionDate = Ordinary.StringToDate(dr["DistributionDate"].ToString());
                    vm.FiscalYearDetailId = Convert.ToInt32(dr["FiscalYearDetailId"]);
                    vm.EmployeeContribution = Convert.ToDecimal(dr["EmployeeContribution"]);
                    vm.EmployerContribution = Convert.ToDecimal(dr["EmployerContribution"]);
                    vm.EmployeeProfit = Convert.ToDecimal(dr["EmployeeProfit"]);
                    vm.EmployerProfit = Convert.ToDecimal(dr["EmployerProfit"]);
                    vm.MultiplicationFactor = Convert.ToDecimal(dr["MultiplicationFactor"]);
                    vm.EmployeeProfitDistribution = Convert.ToDecimal(dr["EmployeeProfitDistribution"]);
                    vm.EmployeerProfitDistribution = Convert.ToDecimal(dr["EmployeerProfitDistribution"]);


                    vm.IsPaid = Convert.ToBoolean(dr["IsPaid"]);
                    //  vm.TransactionType = dr["TransactionType"].ToString();
                    vm.Post = Convert.ToBoolean(dr["Post"]);
                    vm.Remarks = dr["Remarks"].ToString();
                    vm.IsActive = Convert.ToBoolean(dr["IsActive"]);
                    vm.CreatedAt = Ordinary.StringToDate(dr["CreatedAt"].ToString());
                    vm.CreatedBy = dr["CreatedBy"].ToString();
                    vm.CreatedFrom = dr["CreatedFrom"].ToString();
                    vm.LastUpdateAt = Ordinary.StringToDate(dr["LastUpdateAt"].ToString());
                    vm.LastUpdateBy = dr["LastUpdateBy"].ToString();
                    vm.LastUpdateFrom = dr["LastUpdateFrom"].ToString();
                    VMs.Add(vm);
                }
                dr.Close();
                #endregion SqlExecution

                if (Vtransaction == null && transaction != null)
                {
                    transaction.Commit();
                }
                #endregion
            }
            #region catch
            catch (SqlException sqlex)
            {
                throw new ArgumentNullException("", "SQL:" + sqlText + FieldDelimeter + sqlex.Message.ToString());
            }
            catch (Exception ex)
            {
                throw new ArgumentNullException("", "SQL:" + sqlText + FieldDelimeter + ex.Message.ToString());
            }
            #endregion
            #region finally
            finally
            {
                if (VcurrConn == null && currConn != null && currConn.State == ConnectionState.Open)
                {
                    currConn.Close();
                }
            }
            #endregion
            return VMs;
        }

        public List<ProfitDistributionNewVM> SelectForEdit(int Id = 0, string[] conditionFields = null, string[] conditionValues = null, SqlConnection VcurrConn = null, SqlTransaction Vtransaction = null, bool isGF = true)
        {
            #region Variables
            SqlConnection currConn = null;
            SqlTransaction transaction = null;
            string sqlText = "";
            List<ProfitDistributionNewVM> VMs = new List<ProfitDistributionNewVM>();
            ProfitDistributionNewVM vm;
            #endregion
            try
            {
                #region open connection and transaction
                #region New open connection and transaction
                if (VcurrConn != null)
                {
                    currConn = VcurrConn;
                }
                if (Vtransaction != null)
                {
                    transaction = Vtransaction;
                }
                #endregion New open connection and transaction
                if (currConn == null)
                {
                    currConn = _dbsqlConnection.GetConnection();
                    if (currConn.State != ConnectionState.Open)
                    {
                        currConn.Open();
                    }
                }
                if (transaction == null)
                {
                    transaction = currConn.BeginTransaction("");
                }
                #endregion open connection and transaction
                string hrmDB = ("[" + currConn.Database.Replace("]", "]]") + "]");
                string distributionTable = isGF ? "GFProfitDistributionNew" : "ProfitDistributionNew";
                string preDistributionFundColumn = isGF ? "GFPreDistributionFundId" : "PreDistributionFundId";
                string employeeContributionColumn = isGF ? "CAST(0 AS DECIMAL(18,2))" : "pd.EmployeeContribution";
                string employeeProfitColumn = isGF ? "CAST(0 AS DECIMAL(18,2))" : "pd.EmployeeProfit";
                string employeeProfitDistributionColumn = isGF ? "CAST(0 AS DECIMAL(18,2))" : "pd.EmployeeProfitDistribution";

                #region sql statement
                #region SqlText

                sqlText = @"
SELECT
 pd.Id
,fydFrom.PeriodName PeriodNameFrom

,pd.Id
,ve.Code
,ve.EmpName
,pd." + preDistributionFundColumn + @" PreDistributionFundId
,pd.EmployeeId
,pd.DistributionDate
,pd.FiscalYearDetailId
," + employeeContributionColumn + @" EmployeeContribution
,pd.EmployerContribution
," + employeeProfitColumn + @" EmployeeProfit
,pd.EmployerProfit
,pd.MultiplicationFactor
," + employeeProfitDistributionColumn + @" EmployeeProfitDistribution
,pd.EmployeerProfitDistribution
,pd.TotalProfit
,ISNULL(pd.IsPaid,0) IsPaid
,pd.Post
,pd.Remarks
,pd.IsActive
,pd.IsArchive
,pd.CreatedBy
,pd.CreatedAt
,pd.CreatedFrom
,pd.LastUpdateBy
,pd.LastUpdateAt
,pd.LastUpdateFrom

FROM " + distributionTable + @" pd
";
                sqlText = sqlText + @" LEFT OUTER JOIN " + hrmDB + ".[dbo].FiscalYearDetail fydFrom ON pd.FiscalYearDetailId=fydFrom.Id";
                sqlText = sqlText + @" LEFT OUTER JOIN " + hrmDB + ".[dbo].ViewEmployeeInformation ve ON ve.EmployeeId=pd.EmployeeId";

                sqlText = sqlText + @" WHERE  1=1 AND pd.IsActive = 1";


                if (Id > 0)
                {
                    sqlText += @" and pd.Id=@Id";
                }

                string cField = "";
                if (conditionFields != null && conditionValues != null && conditionFields.Length == conditionValues.Length)
                {
                    for (int i = 0; i < conditionFields.Length; i++)
                    {
                        if (string.IsNullOrWhiteSpace(conditionFields[i]) || string.IsNullOrWhiteSpace(conditionValues[i]))
                        {
                            continue;
                        }
                        cField = conditionFields[i].ToString();
                        cField = Ordinary.StringReplacing(cField);
                        sqlText += " AND " + conditionFields[i] + "=@" + cField;
                    }
                }
                #endregion SqlText
                #region SqlExecution

                SqlCommand objComm = new SqlCommand(sqlText, currConn, transaction);
                if (conditionFields != null && conditionValues != null && conditionFields.Length == conditionValues.Length)
                {
                    for (int j = 0; j < conditionFields.Length; j++)
                    {
                        if (string.IsNullOrWhiteSpace(conditionFields[j]) || string.IsNullOrWhiteSpace(conditionValues[j]))
                        {
                            continue;
                        }
                        cField = conditionFields[j].ToString();
                        cField = Ordinary.StringReplacing(cField);
                        objComm.Parameters.AddWithValue("@" + cField, conditionValues[j]);
                    }
                }

                if (Id > 0)
                {
                    objComm.Parameters.AddWithValue("@Id", Id);
                }
                SqlDataReader dr;
                dr = objComm.ExecuteReader();
                while (dr.Read())
                {
                    vm = new ProfitDistributionNewVM();
                    vm.Id = Convert.ToInt32(dr["Id"]);
                    vm.PeriodNameFrom = dr["PeriodNameFrom"].ToString();
                    vm.EmployeeCode = dr["Code"].ToString();
                    vm.EmployeeName = dr["EmpName"].ToString();

                    //vm.FiscalYearDetailId = Convert.ToInt32(dr["FiscalYearDetailId"]);
                    //vm.PFDetailFiscalYearDetailIds = dr["PFDetailFiscalYearDetailIds"].ToString();
                    //vm.PreDistributionFundIds = dr["PreDistributionFundIds"].ToString();
                    //vm.DistributionDate = Ordinary.StringToDate(dr["DistributionDate"].ToString());
                    //vm.TotalEmployeeContribution = Convert.ToDecimal(dr["TotalEmployeeContribution"]);
                    //vm.TotalEmployerContribution = Convert.ToDecimal(dr["TotalEmployerContribution"]);
                    //vm.TotalProfit = Convert.ToDecimal(dr["TotalProfit"]);

                    //vm.FiscalYearDetailIdTo = Convert.ToInt32(dr["FiscalYearDetailIdTo"]);
                    //vm.TotalExpense = Convert.ToDecimal(dr["TotalExpense"]);
                    //vm.AvailableDistributionAmount = Convert.ToDecimal(dr["AvailableDistributionAmount"]);

                    //vm.TotalWeightedContribution = Convert.ToDecimal(dr["TotalWeightedContribution"]);
                    //vm.MultiplicationFactor = Convert.ToDecimal(dr["MultiplicationFactor"]);
                    vm.TotalProfit = Convert.ToDecimal(dr["TotalProfit"]);

                    vm.PreDistributionFundId = Convert.ToString(dr["PreDistributionFundId"]);
                    vm.EmployeeId = Convert.ToString(dr["PreDistributionFundId"]);
                    vm.DistributionDate = Ordinary.StringToDate(dr["DistributionDate"].ToString());
                    vm.FiscalYearDetailId = Convert.ToInt32(dr["FiscalYearDetailId"]);
                    vm.EmployeeContribution = Convert.ToDecimal(dr["EmployeeContribution"]);
                    vm.EmployerContribution = Convert.ToDecimal(dr["EmployerContribution"]);
                    vm.EmployeeProfit = Convert.ToDecimal(dr["EmployeeProfit"]);
                    vm.EmployerProfit = Convert.ToDecimal(dr["EmployerProfit"]);
                    vm.MultiplicationFactor = Convert.ToDecimal(dr["MultiplicationFactor"]);
                    vm.EmployeeProfitDistribution = Convert.ToDecimal(dr["EmployeeProfitDistribution"]);
                    vm.EmployeerProfitDistribution = Convert.ToDecimal(dr["EmployeerProfitDistribution"]);


                    vm.IsPaid = Convert.ToBoolean(dr["IsPaid"]);
                    //  vm.TransactionType = dr["TransactionType"].ToString();
                    vm.Post = Convert.ToBoolean(dr["Post"]);
                    vm.Remarks = dr["Remarks"].ToString();
                    vm.IsActive = Convert.ToBoolean(dr["IsActive"]);
                    vm.CreatedAt = Ordinary.StringToDate(dr["CreatedAt"].ToString());
                    vm.CreatedBy = dr["CreatedBy"].ToString();
                    vm.CreatedFrom = dr["CreatedFrom"].ToString();
                    vm.LastUpdateAt = Ordinary.StringToDate(dr["LastUpdateAt"].ToString());
                    vm.LastUpdateBy = dr["LastUpdateBy"].ToString();
                    vm.LastUpdateFrom = dr["LastUpdateFrom"].ToString();
                    VMs.Add(vm);
                }
                dr.Close();
                #endregion SqlExecution

                if (Vtransaction == null && transaction != null)
                {
                    transaction.Commit();
                }
                #endregion
            }
            #region catch
            catch (SqlException sqlex)
            {
                throw new ArgumentNullException("", "SQL:" + sqlText + FieldDelimeter + sqlex.Message.ToString());
            }
            catch (Exception ex)
            {
                throw new ArgumentNullException("", "SQL:" + sqlText + FieldDelimeter + ex.Message.ToString());
            }
            #endregion
            #region finally
            finally
            {
                if (VcurrConn == null && currConn != null && currConn.State == ConnectionState.Open)
                {
                    currConn.Close();
                }
            }
            #endregion
            return VMs;
        }

        public ResultVM Process(ProfitDistributionNewVM vm, SqlConnection VcurrConn = null, SqlTransaction Vtransaction = null)
        {
            return new ResultVM
            {
                Status = "Fail",
                Message = "GF profit distribution processing is unavailable: the source calculation uses PF data. GF-specific calculation logic is required."
            };
        }

        public string[] InsertExportData(ProfitDistributionNewVM paramVM, SqlConnection VcurrConn, SqlTransaction Vtransaction)
        {
            return new[] { "Fail", "GF profit distribution import is unavailable: the source importer updates PF data. GF-specific import logic is required.", "", "", "", "GF Profit Distribution Import" };
        }

        


        //==================Update =================
        public string[] Update(ProfitDistributionNewVM vm, SqlConnection VcurrConn = null, SqlTransaction Vtransaction = null, bool isGF = true)
        {
            #region Variables
            string[] retResults = new string[6];
            retResults[0] = "Fail";//Success or Fail
            retResults[1] = "Fail";// Success or Fail Message
            retResults[2] = "0";
            retResults[3] = "sqlText"; //  SQL Query
            retResults[4] = "ex"; //catch ex
            retResults[5] = "Employee ProfitDistributionNew Update"; //Method Name
            int transResult = 0;
            string sqlText = "";
            SqlConnection currConn = null;
            SqlTransaction transaction = null;
            #endregion
            try
            {
                #region open connection and transaction
                #region New open connection and transaction
                if (VcurrConn != null)
                {
                    currConn = VcurrConn;
                }
                if (Vtransaction != null)
                {
                    transaction = Vtransaction;
                }
                #endregion New open connection and transaction
                if (currConn == null)
                {
                    currConn = _dbsqlConnection.GetConnection();
                    if (currConn.State != ConnectionState.Open)
                    {
                        currConn.Open();
                    }
                }
                if (transaction == null) { transaction = currConn.BeginTransaction("UpdateToProfitDistributionNew"); }
                #endregion open connection and transaction

                if (vm != null)
                {
                    string distributionTable = isGF ? "GFProfitDistributionNew" : "ProfitDistributionNew";
                    #region Update Settings
                    #region SqlText
                    sqlText = "";
                    sqlText = "UPDATE " + distributionTable + " SET";
                    sqlText += "  TotalProfit=@TotalProfit";
                    if (!isGF)
                    {
                        sqlText += " , EmployeeProfit=@EmployeeProfit";
                    }
                    sqlText += " , EmployerProfit=@EmployerProfit";
                    if (!isGF)
                    {
                        sqlText += " , EmployeeProfitDistribution=@EmployeeProfitDistribution";
                    }
                    sqlText += " , EmployeerProfitDistribution=@EmployeerProfitDistribution";                   
                    sqlText += " , LastUpdateBy=@LastUpdateBy";
                    sqlText += " , LastUpdateAt=@LastUpdateAt";
                    sqlText += " , LastUpdateFrom=@LastUpdateFrom";                  
                    sqlText += " WHERE Id=@Id";
                    #endregion SqlText
                    #region SqlExecution
                    SqlCommand cmdUpdate = new SqlCommand(sqlText, currConn);
                    cmdUpdate.Parameters.AddWithValue("@Id", vm.Id);
                    cmdUpdate.Parameters.AddWithValue("@TotalProfit", isGF ? vm.EmployerProfit : vm.EmployeeProfit + vm.EmployerProfit);
                    if (!isGF)
                    {
                        cmdUpdate.Parameters.AddWithValue("@EmployeeProfit", vm.EmployeeProfit);
                    }
                    cmdUpdate.Parameters.AddWithValue("@EmployerProfit", vm.EmployerProfit);
                    if (!isGF)
                    {
                        cmdUpdate.Parameters.AddWithValue("@EmployeeProfitDistribution", vm.EmployeeProfit);
                    }
                    cmdUpdate.Parameters.AddWithValue("@EmployeerProfitDistribution", vm.EmployerProfit);                
                    cmdUpdate.Parameters.AddWithValue("@LastUpdateBy", vm.LastUpdateBy);
                    cmdUpdate.Parameters.AddWithValue("@LastUpdateAt", vm.LastUpdateAt);
                    cmdUpdate.Parameters.AddWithValue("@LastUpdateFrom", vm.LastUpdateFrom);
                

                    cmdUpdate.Transaction = transaction;
                    var exeRes = cmdUpdate.ExecuteNonQuery();
                    transResult = Convert.ToInt32(exeRes);
                    if (transResult <= 0)
                    {
                        retResults[3] = sqlText;
                        throw new ArgumentNullException("Unexpected error to update PreDistributionFunds.", "");
                    }
                    #endregion SqlExecution

                    retResults[2] = vm.Id.ToString();// Return Id
                    retResults[3] = sqlText; //  SQL Query
                    #region Commit
                    if (transResult <= 0)
                    {
                        // throw new ArgumentNullException("PreDistributionFund Update", vm.BranchId + " could not updated.");
                    }
                    #endregion Commit
                    #endregion Update Settings
                }
                else
                {
                    throw new ArgumentNullException("PreDistributionFund Update", "Could not found any item.");
                }
                if (Vtransaction == null && transaction != null)
                {
                    transaction.Commit();
                    retResults[0] = "Success";
                    retResults[1] = "Data Update Successfully.";
                }
            }
            #region catch
            catch (Exception ex)
            {
                retResults[0] = "Fail";//Success or Fail
                retResults[4] = ex.Message; //catch ex
                if (Vtransaction == null) { transaction.Rollback(); }
                return retResults;
            }
            finally
            {
                if (VcurrConn == null && currConn != null && currConn.State == ConnectionState.Open)
                {
                    currConn.Close();
                }
            }
            #endregion
            return retResults;
        }

        public string[] Delete(ProfitDistributionNewVM vm, SqlConnection VcurrConn = null, SqlTransaction Vtransaction = null, bool isGF = true)
        {
            #region Variables
            string[] retResults = new string[6];
            retResults[0] = "Fail";//Success or Fail
            retResults[1] = "Fail";// Success or Fail Message
            retResults[2] = "0";
            retResults[3] = "sqlText"; //  SQL Query
            retResults[4] = "ex"; //catch ex
            retResults[5] = "Employee ProfitDistributionNew Update"; //Method Name
            int transResult = 0;
            string sqlText = "";
            SqlConnection currConn = null;
            SqlTransaction transaction = null;
            #endregion
            try
            {
                #region open connection and transaction
                #region New open connection and transaction
                if (VcurrConn != null)
                {
                    currConn = VcurrConn;
                }
                if (Vtransaction != null)
                {
                    transaction = Vtransaction;
                }
                #endregion New open connection and transaction
                if (currConn == null)
                {
                    currConn = _dbsqlConnection.GetConnection();
                    if (currConn.State != ConnectionState.Open)
                    {
                        currConn.Open();
                    }
                }
                if (transaction == null) { transaction = currConn.BeginTransaction("UpdateToProfitDistributionNew"); }
                #endregion open connection and transaction

                if (vm != null)
                {
                    string distributionTable = isGF ? "GFProfitDistributionNew" : "ProfitDistributionNew";
                    #region Update Settings
                    #region SqlText
                    sqlText = "";
                    sqlText = "UPDATE " + distributionTable + " SET";
                    sqlText += "  IsActive=@IsActive";                  
                    sqlText += " , LastUpdateBy=@LastUpdateBy";
                    sqlText += " , LastUpdateAt=@LastUpdateAt";
                    sqlText += " , LastUpdateFrom=@LastUpdateFrom";
                    sqlText += " WHERE Id=@Id";
                    #endregion SqlText
                    #region SqlExecution
                    SqlCommand cmdUpdate = new SqlCommand(sqlText, currConn);
                    cmdUpdate.Parameters.AddWithValue("@Id", vm.Id);
                    cmdUpdate.Parameters.AddWithValue("@IsActive", false);                   
                    cmdUpdate.Parameters.AddWithValue("@LastUpdateBy", vm.LastUpdateBy);
                    cmdUpdate.Parameters.AddWithValue("@LastUpdateAt", vm.LastUpdateAt);
                    cmdUpdate.Parameters.AddWithValue("@LastUpdateFrom", vm.LastUpdateFrom);


                    cmdUpdate.Transaction = transaction;
                    var exeRes = cmdUpdate.ExecuteNonQuery();
                    transResult = Convert.ToInt32(exeRes);
                    if (transResult <= 0)
                    {
                        retResults[3] = sqlText;
                        throw new ArgumentNullException("Unexpected error to update PreDistributionFunds.", "");
                    }
                    #endregion SqlExecution

                    retResults[2] = vm.Id.ToString();// Return Id
                    retResults[3] = sqlText; //  SQL Query
                    #region Commit
                    if (transResult <= 0)
                    {
                        // throw new ArgumentNullException("PreDistributionFund Update", vm.BranchId + " could not updated.");
                    }
                    #endregion Commit
                    #endregion Update Settings
                }
                else
                {
                    throw new ArgumentNullException("PreDistributionFund Deleted", "Could not found any item.");
                }
                if (Vtransaction == null && transaction != null)
                {
                    transaction.Commit();
                    retResults[0] = "Success";
                    retResults[1] = "Data Deleted Successfully.";
                }
            }
            #region catch
            catch (Exception ex)
            {
                retResults[0] = "Fail";//Success or Fail
                retResults[4] = ex.Message; //catch ex
                if (Vtransaction == null) { transaction.Rollback(); }
                return retResults;
            }
            finally
            {
                if (VcurrConn == null && currConn != null && currConn.State == ConnectionState.Open)
                {
                    currConn.Close();
                }
            }
            #endregion
            return retResults;
        }
    }
}
