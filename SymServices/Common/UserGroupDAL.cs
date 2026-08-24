using SymOrdinary;
using SymViewModel.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
namespace SymServices.Common
{
    public class UserGroupDAL
    {
        #region Global Variables
        private const string FieldDelimeter = DBConstant.FieldDelimeter;
        private DBSQLConnection _dbsqlConnection = new DBSQLConnection();

        #endregion

        private static bool UserGroupColumnExists(SqlConnection currConn, SqlTransaction transaction, string columnName)
        {
            string sql = @"
SELECT COUNT(1)
FROM sys.columns c
INNER JOIN sys.objects o ON c.object_id = o.object_id
WHERE o.name = 'UserGroup' AND c.name = @ColumnName";

            using (SqlCommand command = new SqlCommand(sql, currConn, transaction))
            {
                command.Parameters.AddWithValue("@ColumnName", columnName);
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private static string UserGroupBitExpression(SqlConnection currConn, SqlTransaction transaction, string preferredColumn, string fallbackColumn, string alias)
        {
            if (!string.IsNullOrWhiteSpace(preferredColumn) && UserGroupColumnExists(currConn, transaction, preferredColumn))
            {
                return ",ISNULL(" + preferredColumn + ",0)" + alias + Environment.NewLine;
            }

            if (!string.IsNullOrWhiteSpace(fallbackColumn) && UserGroupColumnExists(currConn, transaction, fallbackColumn))
            {
                return ",ISNULL(" + fallbackColumn + ",0)" + alias + Environment.NewLine;
            }

            return ",CAST(0 AS bit)" + alias + Environment.NewLine;
        }

        private static bool AddOptionalInsertColumn(List<string> columns, List<string> values, SqlCommand command, SqlConnection currConn, SqlTransaction transaction, string dbColumn, string parameter, object value)
        {
            if (!UserGroupColumnExists(currConn, transaction, dbColumn))
            {
                return false;
            }

            columns.Add(dbColumn);
            values.Add(parameter);
            command.Parameters.AddWithValue(parameter, value);
            return true;
        }

        private static bool AddOptionalUpdateColumn(List<string> setClauses, SqlCommand command, SqlConnection currConn, SqlTransaction transaction, string dbColumn, string parameter, object value)
        {
            if (!UserGroupColumnExists(currConn, transaction, dbColumn))
            {
                return false;
            }

            setClauses.Add(dbColumn + "=" + parameter);
            command.Parameters.AddWithValue(parameter, value);
            return true;
        }

        public List<UserGroupVM> SelectAll()
        {
            #region Variables

            SqlConnection currConn = null;
            string sqlText = "";
            List<UserGroupVM> VMs = new List<UserGroupVM>();
            UserGroupVM VM;
            #endregion
            try
            {
                #region open connection and transaction

                currConn = _dbsqlConnection.GetConnection();
                if (currConn.State != ConnectionState.Open)
                {
                    currConn.Open();
                }

                #endregion open connection and transaction
                #region sql statement

                sqlText = @"SELECT
Id
,GroupName
";
                sqlText += UserGroupBitExpression(currConn, null, null, "IsSuper", "IsSuper");
                sqlText += UserGroupBitExpression(currConn, null, null, "IsAdmin", "IsAdmin");
                sqlText += UserGroupBitExpression(currConn, null, "IsGL", "IsHRM", "IsHRM");
                sqlText += UserGroupBitExpression(currConn, null, null, "IsAttendance", "IsAttendance");
                sqlText += UserGroupBitExpression(currConn, null, null, "IsPayroll", "IsPayroll");
                sqlText += UserGroupBitExpression(currConn, null, null, "IsTAX", "IsTAX");
                sqlText += UserGroupBitExpression(currConn, null, null, "IsPF", "IsPF");
                sqlText += UserGroupBitExpression(currConn, null, "IsWPPF", "IsGF", "IsGF");
                sqlText += UserGroupBitExpression(currConn, null, null, "IsESS", "IsESS");
                sqlText += @"
,Remarks
,IsActive
,IsArchive
,CreatedBy
,CreatedAt
,CreatedFrom
,LastUpdateBy
,LastUpdateAt
,LastUpdateFrom
    From UserGroup
Where IsArchive=0
    ORDER BY GroupName
";


                SqlCommand objComm = new SqlCommand();
                objComm.Connection = currConn;
                objComm.CommandText = sqlText;
                objComm.CommandType = CommandType.Text;
                SqlDataReader dr;
                dr = objComm.ExecuteReader();
                while (dr.Read())
                {
                    VM = new UserGroupVM();
                    VM.Id = Convert.ToInt32(dr["Id"].ToString());
                    VM.GroupName = dr["GroupName"].ToString();
                    VM.IsSuper = Convert.ToBoolean(dr["IsSuper"]);
                    VM.IsAdmin = Convert.ToBoolean(dr["IsAdmin"]);
                    VM.IsHRM = Convert.ToBoolean(dr["IsHRM"]);
                    VM.IsAttendance = Convert.ToBoolean(dr["IsAttendance"]);
                    VM.IsPayroll = Convert.ToBoolean(dr["IsPayroll"]);
                    VM.IsTAX = Convert.ToBoolean(dr["IsTAX"]);
                    VM.IsPF = Convert.ToBoolean(dr["IsPF"]);
                    VM.IsGF = Convert.ToBoolean(dr["IsGF"]);
                    VM.IsESS = Convert.ToBoolean(dr["IsESS"]);
                    VM.Remarks = dr["Remarks"].ToString();
                    VM.IsActive = Convert.ToBoolean(dr["IsActive"]);
                    VM.CreatedAt = Ordinary.StringToDate(dr["CreatedAt"].ToString());
                    VM.CreatedBy = dr["CreatedBy"].ToString();
                    VM.CreatedFrom = dr["CreatedFrom"].ToString();
                    VM.LastUpdateAt = Ordinary.StringToDate(dr["LastUpdateAt"].ToString());
                    VM.LastUpdateBy = dr["LastUpdateBy"].ToString();
                    VM.LastUpdateFrom = dr["LastUpdateFrom"].ToString();
                    VMs.Add(VM);
                }
                dr.Close();
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
                if (currConn != null)
                {
                    if (currConn.State == ConnectionState.Open)
                    {
                        currConn.Close();
                    }
                }
            }

            #endregion
            return VMs;
        }
        //==================SelectByID=================
        public UserGroupVM SelectById(string GroupId)
        {
            #region Variables

            SqlConnection currConn = null;
            string sqlText = "";
            UserGroupVM VM = new UserGroupVM();

            #endregion
            try
            {
                #region open connection and transaction

                currConn = _dbsqlConnection.GetConnection();
                if (currConn.State != ConnectionState.Open)
                {
                    currConn.Open();
                }

                #endregion open connection and transaction
                #region sql statement

                sqlText = @"
SELECT
Id
,GroupName
";
                sqlText += UserGroupBitExpression(currConn, null, null, "IsSuper", "IsSuper");
                sqlText += UserGroupBitExpression(currConn, null, null, "IsAdmin", "IsAdmin");
                sqlText += UserGroupBitExpression(currConn, null, "IsGL", "IsHRM", "IsHRM");
                sqlText += UserGroupBitExpression(currConn, null, null, "IsAttendance", "IsAttendance");
                sqlText += UserGroupBitExpression(currConn, null, null, "IsPayroll", "IsPayroll");
                sqlText += UserGroupBitExpression(currConn, null, null, "IsTAX", "IsTAX");
                sqlText += UserGroupBitExpression(currConn, null, null, "IsPF", "IsPF");
                sqlText += UserGroupBitExpression(currConn, null, "IsWPPF", "IsGF", "IsGF");
                sqlText += UserGroupBitExpression(currConn, null, null, "IsESS", "IsESS");
                sqlText += @"
,Remarks
,IsActive
,IsArchive
,CreatedBy
,CreatedAt
,CreatedFrom
,LastUpdateBy
,LastUpdateAt
,LastUpdateFrom
    From UserGroup
Where  Id=@Id  and IsArchive=0
";
                //IsAdmin
                //IsHRM
                //IsAttendance
                //IsPayroll
                //IsTAX
                //IsPF
                //IsGF
                //IsESS
                SqlCommand objComm = new SqlCommand();
                objComm.Connection = currConn;
                objComm.CommandText = sqlText;
                objComm.CommandType = CommandType.Text;
                objComm.Parameters.AddWithValue("@Id", GroupId);

                SqlDataReader dr;
                dr = objComm.ExecuteReader();
                while (dr.Read())
                {
                    VM.Id = Convert.ToInt32(dr["Id"].ToString());
                    VM.GroupName = dr["GroupName"].ToString();
                    VM.IsSuper = Convert.ToBoolean(dr["IsSuper"]);
                    VM.IsAdmin = Convert.ToBoolean(dr["IsAdmin"]);
                    VM.IsHRM = Convert.ToBoolean(dr["IsHRM"]);
                    VM.IsAttendance = Convert.ToBoolean(dr["IsAttendance"]);
                    VM.IsPayroll = Convert.ToBoolean(dr["IsPayroll"]);
                    VM.IsTAX = Convert.ToBoolean(dr["IsTAX"]);
                    VM.IsPF = Convert.ToBoolean(dr["IsPF"]);
                    VM.IsGF = Convert.ToBoolean(dr["IsGF"]);
                    VM.IsESS = Convert.ToBoolean(dr["IsESS"]);
                    VM.Remarks = dr["Remarks"].ToString();
                    VM.IsActive = Convert.ToBoolean(dr["IsActive"]);
                    VM.CreatedAt = Ordinary.StringToDate(dr["CreatedAt"].ToString());
                    VM.CreatedBy = dr["CreatedBy"].ToString();
                    VM.CreatedFrom = dr["CreatedFrom"].ToString();
                    VM.LastUpdateAt = Ordinary.StringToDate(dr["LastUpdateAt"].ToString());
                    VM.LastUpdateBy = dr["LastUpdateBy"].ToString();
                    VM.LastUpdateFrom = dr["LastUpdateFrom"].ToString();

                }
                dr.Close();


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
                if (currConn != null)
                {
                    if (currConn.State == ConnectionState.Open)
                    {
                        currConn.Close();
                    }
                }
            }

            #endregion
            return VM;
        }
        //==================Insert =================
        public string[] Insert(UserGroupVM vm, SqlConnection VcurrConn, SqlTransaction Vtransaction)
        {

            #region Initializ
            string sqlText = "";
            int Id = 0;
            string[] retResults = new string[6];
            retResults[0] = "Fail";//Success or Fail
            retResults[1] = "Fail";// Success or Fail Message
            retResults[2] = Id.ToString();// Return Id
            retResults[3] = sqlText; //  SQL Query
            retResults[4] = "ex"; //catch ex
            retResults[5] = "InsertUserGroup"; //Method Name

            SqlConnection currConn = null;
            SqlTransaction transaction = null;


            #endregion

            #region Try

            try
            {

                #region Validation
                //if (string.IsNullOrEmpty(UserGroupVM.UserGroupId))
                //{
                //    retResults[1] = "Please Input Employee Travel Course";
                //    return retResults;
                //}
                #endregion Validation
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
                #region Exist
                CommonDAL cdal = new CommonDAL();
                bool check = false;
                string tableName = "UserGroup";
                string[] fieldName = { "GroupName" };
                string[] fieldValue = { vm.GroupName.Trim() };

                for (int i = 0; i < fieldName.Length; i++)
                {
                    check = cdal.CheckDuplicateInInsert(tableName, fieldName[i], fieldValue[i], currConn, transaction);
                    if (check == true)
                    {
                        retResults[1] = "This " + fieldName[i] + ": \"" + fieldValue[i] + "\" already used!";
                        throw new ArgumentNullException("This " + fieldName[i] + ": \"" + fieldValue[i] + "\" already used!", "");
                    }
                }
                #endregion Exist

                #region Save

                if (vm != null)
                {

                    SqlCommand cmdInsert = new SqlCommand();
                    cmdInsert.Connection = currConn;
                    cmdInsert.Transaction = transaction;

                    List<string> insertColumns = new List<string> { "GroupName" };
                    List<string> insertValues = new List<string> { "@GroupName" };
                    cmdInsert.Parameters.AddWithValue("@GroupName", vm.GroupName.Trim());

                    AddOptionalInsertColumn(insertColumns, insertValues, cmdInsert, currConn, transaction, "IsAdmin", "@IsAdmin", vm.IsAdmin);
                    AddOptionalInsertColumn(insertColumns, insertValues, cmdInsert, currConn, transaction, "IsPF", "@IsPF", vm.IsPF);
                    if (!AddOptionalInsertColumn(insertColumns, insertValues, cmdInsert, currConn, transaction, "IsGL", "@IsGL", vm.IsHRM))
                    {
                        AddOptionalInsertColumn(insertColumns, insertValues, cmdInsert, currConn, transaction, "IsHRM", "@IsHRM", vm.IsHRM);
                    }
                    if (!AddOptionalInsertColumn(insertColumns, insertValues, cmdInsert, currConn, transaction, "IsWPPF", "@IsWPPF", vm.IsGF))
                    {
                        AddOptionalInsertColumn(insertColumns, insertValues, cmdInsert, currConn, transaction, "IsGF", "@IsGF", vm.IsGF);
                    }

                    insertColumns.AddRange(new[] { "Remarks", "IsActive", "IsArchive", "CreatedBy", "CreatedAt", "CreatedFrom" });
                    insertValues.AddRange(new[] { "@Remarks", "@IsActive", "@IsArchive", "@CreatedBy", "@CreatedAt", "@CreatedFrom" });
                    cmdInsert.Parameters.AddWithValue("@Remarks", vm.Remarks ?? Convert.DBNull);
                    cmdInsert.Parameters.AddWithValue("@IsActive", true);
                    cmdInsert.Parameters.AddWithValue("@IsArchive", false);
                    cmdInsert.Parameters.AddWithValue("@CreatedBy", vm.CreatedBy);
                    cmdInsert.Parameters.AddWithValue("@CreatedAt", vm.CreatedAt);
                    cmdInsert.Parameters.AddWithValue("@CreatedFrom", vm.CreatedFrom);

                    sqlText = @" INSERT INTO UserGroup(" + string.Join(",", insertColumns) + @") 
                                VALUES (" + string.Join(",", insertValues) + @") 
                                SELECT SCOPE_IDENTITY()";
                    cmdInsert.CommandText = sqlText;
                    cmdInsert.ExecuteNonQuery();
                }
                else
                {
                    retResults[1] = "This UserGroup already used!";
                    throw new ArgumentNullException("Please Input UserGroup Value", "");
                }


                #endregion Save
                #region Commit
                if (Vtransaction == null)
                {
                    if (transaction != null)
                    {
                        transaction.Commit();
                    }
                }

                #endregion Commit

                #region SuccessResult

                retResults[0] = "Success";
                retResults[1] = "Data Save Successfully.";
                retResults[2] = vm.Id.ToString();

                #endregion SuccessResult

            }

            #endregion try

            #region Catch and Finall



            catch (Exception ex)
            {
                retResults[0] = "Fail";//Success or Fail
                retResults[4] = ex.Message.ToString(); //catch ex
                if (Vtransaction == null) { transaction.Rollback(); }
                return retResults;
            }

            finally
            {
                if (VcurrConn == null)
                {
                    if (currConn != null)
                    {
                        if (currConn.State == ConnectionState.Open)
                        {
                            currConn.Close();
                        }
                    }
                }
            }


            #endregion

            #region Results

            return retResults;
            #endregion


        }
        //==================Update =================
        public string[] Update(UserGroupVM vm, SqlConnection VcurrConn, SqlTransaction Vtransaction)
        {
            #region Variables

            string[] retResults = new string[6];
            retResults[0] = "Fail";//Success or Fail
            retResults[1] = "Fail";// Success or Fail Message
            retResults[2] = "0";
            retResults[3] = "sqlText"; //  SQL Query
            retResults[4] = "ex"; //catch ex
            retResults[5] = "Employee UserGroup Update"; //Method Name

            int transResult = 0;

            string sqlText = "";
            SqlConnection currConn = null;
            SqlTransaction transaction = null;
            bool iSTransSuccess = false;

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

                if (transaction == null) { transaction = currConn.BeginTransaction("UpdateToUserGroup"); }

                #endregion open connection and transaction

                #region Exist

                CommonDAL cdal = new CommonDAL();
                bool check = false;
                string tableName = "UserGroup";
                string[] fieldName = { "GroupName" };
                string[] fieldValue = { vm.GroupName.Trim() };

                for (int i = 0; i < fieldName.Length; i++)
                {
                    check = cdal.CheckDuplicateInUpdate(vm.Id.ToString(), tableName, fieldName[i], fieldValue[i], currConn, transaction);
                    if (check == true)
                    {
                        retResults[1] = "This " + fieldName[i] + ": \"" + fieldValue[i] + "\" already used!";
                        throw new ArgumentNullException("This " + fieldName[i] + ": \"" + fieldValue[i] + "\" already used!", "");
                    }
                }
                #endregion Exist



                if (vm != null)
                {
                    #region Update Settings

                    SqlCommand cmdUpdate = new SqlCommand();
                    cmdUpdate.Connection = currConn;
                    cmdUpdate.Transaction = transaction;

                    List<string> setClauses = new List<string> { "GroupName=@GroupName" };
                    cmdUpdate.Parameters.AddWithValue("@Id", vm.Id);
                    cmdUpdate.Parameters.AddWithValue("@GroupName", vm.GroupName.Trim());

                    AddOptionalUpdateColumn(setClauses, cmdUpdate, currConn, transaction, "IsAdmin", "@IsAdmin", vm.IsAdmin);
                    AddOptionalUpdateColumn(setClauses, cmdUpdate, currConn, transaction, "IsPF", "@IsPF", vm.IsPF);
                    if (!AddOptionalUpdateColumn(setClauses, cmdUpdate, currConn, transaction, "IsGL", "@IsGL", vm.IsHRM))
                    {
                        AddOptionalUpdateColumn(setClauses, cmdUpdate, currConn, transaction, "IsHRM", "@IsHRM", vm.IsHRM);
                    }
                    if (!AddOptionalUpdateColumn(setClauses, cmdUpdate, currConn, transaction, "IsWPPF", "@IsWPPF", vm.IsGF))
                    {
                        AddOptionalUpdateColumn(setClauses, cmdUpdate, currConn, transaction, "IsGF", "@IsGF", vm.IsGF);
                    }

                    setClauses.Add("IsActive=@IsActive");
                    setClauses.Add("LastUpdateBy=@LastUpdateBy");
                    setClauses.Add("LastUpdateAt=@LastUpdateAt");
                    setClauses.Add("LastUpdateFrom=@LastUpdateFrom");
                    cmdUpdate.Parameters.AddWithValue("@IsActive", vm.IsActive);
                    cmdUpdate.Parameters.AddWithValue("@LastUpdateBy", vm.LastUpdateBy);
                    cmdUpdate.Parameters.AddWithValue("@LastUpdateAt", vm.LastUpdateAt);
                    cmdUpdate.Parameters.AddWithValue("@LastUpdateFrom", vm.LastUpdateFrom);

                    sqlText = "update UserGroup set " + string.Join(",", setClauses) + " where Id=@Id";
                    cmdUpdate.CommandText = sqlText;
                    var exeRes = cmdUpdate.ExecuteNonQuery();
                    transResult = Convert.ToInt32(exeRes);

                    retResults[2] = vm.Id.ToString();// Return Id
                    retResults[3] = sqlText; //  SQL Query

                    #region Commit

                    if (transResult <= 0)
                    {
                        // throw new ArgumentNullException("Education Update", UserGroupVM.BranchId + " could not updated.");
                    }

                    #endregion Commit

                    #endregion Update Settings
                    iSTransSuccess = true;
                }
                else
                {
                    throw new ArgumentNullException("UserGroup Update", "Could not found any item.");
                }


                if (iSTransSuccess == true)
                {
                    if (Vtransaction == null)
                    {
                        if (transaction != null)
                        {
                            transaction.Commit();
                        }
                    }
                    retResults[0] = "Success";
                    retResults[1] = "Data Update Successfully.";

                }
                else
                {
                    retResults[1] = "Unexpected error to update UserGroup.";
                    throw new ArgumentNullException("", "");
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
                if (VcurrConn == null)
                {
                    if (currConn != null)
                    {
                        if (currConn.State == ConnectionState.Open)
                        {
                            currConn.Close();
                        }
                    }
                }
            }

            #endregion

            return retResults;
        }
        //==================Delete =================
        public string[] Delete(UserGroupVM vm, string[] ids, SqlConnection VcurrConn, SqlTransaction Vtransaction)
        {
            #region Variables

            string[] retResults = new string[6];
            retResults[0] = "Fail";//Success or Fail
            retResults[1] = "Fail";// Success or Fail Message
            retResults[2] = "0";// Return Id
            retResults[3] = "sqlText"; //  SQL Query
            retResults[4] = "ex"; //catch ex
            retResults[5] = "DeleteUserGroup"; //Method Name

            int transResult = 0;
            int countId = 0;
            string sqlText = "";
            SqlConnection currConn = null;
            SqlTransaction transaction = null;
            bool iSTransSuccess = false;

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

                if (transaction == null) { transaction = currConn.BeginTransaction("DeleteToUserGroup"); }

                #endregion open connection and transaction
                #region Check is  it used

                #endregion Check is  it used

                if (ids.Length >= 1)
                {
                    #region Update Settings
                    for (int i = 0; i < ids.Length - 1; i++)
                    {
                        sqlText = "";
                        sqlText = "update UserGroup set";
                        sqlText += " IsActive=@IsActive,";
                        sqlText += " IsArchive=@IsArchive,";
                        sqlText += " LastUpdateBy=@LastUpdateBy,";
                        sqlText += " LastUpdateAt=@LastUpdateAt,";
                        sqlText += " LastUpdateFrom=@LastUpdateFrom";
                        sqlText += " where Id=@Id";

                        SqlCommand cmdUpdate = new SqlCommand(sqlText, currConn);
                        cmdUpdate.Parameters.AddWithValue("@Id", ids[i]);
                        cmdUpdate.Parameters.AddWithValue("@IsActive", false);
                        cmdUpdate.Parameters.AddWithValue("@IsArchive", true);
                        cmdUpdate.Parameters.AddWithValue("@LastUpdateBy", vm.LastUpdateBy);
                        cmdUpdate.Parameters.AddWithValue("@LastUpdateAt", vm.LastUpdateAt);
                        cmdUpdate.Parameters.AddWithValue("@LastUpdateFrom", vm.LastUpdateFrom);

                        cmdUpdate.Transaction = transaction;
                        var exeRes = cmdUpdate.ExecuteNonQuery();
                        transResult = Convert.ToInt32(exeRes);
                    }


                    retResults[2] = "";// Return Id
                    retResults[3] = sqlText; //  SQL Query

                    #region Commit

                    if (transResult <= 0)
                    {
                        throw new ArgumentNullException("UserGroup Delete", vm.Id + " could not Delete.");
                    }

                    #endregion Commit

                    #endregion Update Settings
                    iSTransSuccess = true;
                }
                else
                {
                    throw new ArgumentNullException("UserGroup Information Delete", "Could not found any item.");
                }


                if (iSTransSuccess == true)
                {
                    if (Vtransaction == null)
                    {
                        if (transaction != null)
                        {
                            transaction.Commit();
                        }
                    }
                    retResults[0] = "Success";
                    retResults[1] = "Data Delete Successfully.";

                }
                else
                {
                    retResults[1] = "Unexpected error to delete UserGroup Information.";
                    throw new ArgumentNullException("", "");
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
                if (VcurrConn == null)
                {
                    if (currConn != null)
                    {
                        if (currConn.State == ConnectionState.Open)
                        {
                            currConn.Close();
                        }
                    }
                }
            }

            #endregion

            return retResults;
        }
        public List<UserGroupVM> DropDown()
        {

            #region Variables

            SqlConnection currConn = null;
            string sqlText = "";
            List<UserGroupVM> VMs = new List<UserGroupVM>();
            UserGroupVM vm;
            #endregion
            try
            {
                #region open connection and transaction

                currConn = _dbsqlConnection.GetConnection();
                if (currConn.State != ConnectionState.Open)
                {
                    currConn.Open();
                }

                #endregion open connection and transaction

                #region sql statement

                sqlText = @"SELECT
Id,
GroupName
   FROM UserGroup
WHERE IsArchive=0 and IsActive=1
    ORDER BY GroupName
";

                SqlCommand _objComm = new SqlCommand();
                _objComm.Connection = currConn;
                _objComm.CommandText = sqlText;
                _objComm.CommandType = CommandType.Text;

                SqlDataReader dr;
                dr = _objComm.ExecuteReader();
                while (dr.Read())
                {
                    vm = new UserGroupVM();
                    vm.Id = Convert.ToInt32(dr["Id"].ToString());
                    vm.Name = dr["GroupName"].ToString();
                    VMs.Add(vm);
                }
                dr.Close();


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
                if (currConn != null)
                {
                    if (currConn.State == ConnectionState.Open)
                    {
                        currConn.Close();
                    }
                }
            }

            #endregion

            return VMs;
        }
        public List<string> Autocomplete(string term)
        {

            #region Variables

            SqlConnection currConn = null;
            List<string> VMs = new List<string>();

            string sqlText = "";

            #endregion
            try
            {
                #region open connection and transaction

                currConn = _dbsqlConnection.GetConnection();
                if (currConn.State != ConnectionState.Open)
                {
                    currConn.Open();
                }

                #endregion open connection and transaction

                #region sql statement
                sqlText = "";
                sqlText = @"SELECT Id, GroupName    FROM UserGroup ";
                sqlText += @" WHERE GroupName like '%" + term + "%' and IsArchive=0 and IsActive=1 ORDER BY GroupName";



                SqlCommand _objComm = new SqlCommand();
                _objComm.Connection = currConn;
                _objComm.CommandText = sqlText;
                _objComm.CommandType = CommandType.Text;

                SqlDataReader dr;
                dr = _objComm.ExecuteReader();
                int i = 0;
                while (dr.Read())
                {
                    VMs.Insert(i, dr["GroupName"].ToString());
                    i++;
                }
                dr.Close();
                VMs.Sort();
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
                if (currConn != null)
                {
                    if (currConn.State == ConnectionState.Open)
                    {
                        currConn.Close();
                    }
                }
            }

            #endregion

            return VMs;
        }

    }
}
