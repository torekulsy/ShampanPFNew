using SymOrdinary;
using SymViewModel.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace SymServices.Common
{
    public class SymUserRollDAL
    {
        #region Global Variables
        private const string FieldDelimeter = DBConstant.FieldDelimeter;
        private DBSQLConnection _dbsqlConnection = new DBSQLConnection();
        #endregion

        private static void EnsureDynamicRoleColumns(SqlConnection currConn, SqlTransaction transaction)
        {
            CommonDAL commonDal = new CommonDAL();
            commonDal.TableFieldAdd("SymUserRoll", "symArea", "varchar(200)", currConn, transaction);
            commonDal.TableFieldAdd("SymUserRoll", "symController", "varchar(500)", currConn, transaction);
        }

        private List<DynamicMenuDefinition> GetDynamicDefaultRollDefinitions()
        {
            List<DynamicMenuDefinition> definitions = new DynamicMenuService().GetDefinitions();
            List<DynamicMenuDefinition> filteredDefinitions = new List<DynamicMenuDefinition>();
            HashSet<string> uniqueKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (DynamicMenuDefinition definition in definitions)
            {
                if (definition == null
                    || string.IsNullOrWhiteSpace(definition.renderArea)
                    || string.IsNullOrWhiteSpace(definition.permissionKey))
                {
                    continue;
                }

                string key = definition.renderArea.Trim() + "|" + definition.permissionKey.Trim();
                if (uniqueKeys.Add(key))
                {
                    filteredDefinitions.Add(definition);
                }
            }

            return filteredDefinitions;
        }

        private static string BuildDynamicDefaultRollId(DynamicMenuDefinition definition)
        {
            string source = (definition.renderArea ?? "") + "|" + (definition.permissionKey ?? "") + "|" + definition.sortOrder.ToString();
            uint checksum = 2166136261;
            foreach (char item in source)
            {
                checksum ^= item;
                checksum *= 16777619;
            }

            return "D" + checksum.ToString("X8");
        }

        private static string SanitizeDynamicRollToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "NA";
            }

            StringBuilder builder = new StringBuilder();
            foreach (char item in value.Trim())
            {
                if (char.IsLetterOrDigit(item))
                {
                    builder.Append(item);
                }
                else if (builder.Length == 0 || builder[builder.Length - 1] != '_')
                {
                    builder.Append('_');
                }
            }

            string token = builder.ToString().Trim('_');
            return string.IsNullOrWhiteSpace(token) ? "NA" : token;
        }

        private void AddDynamicRoleRows(List<SymUserRollVM> roles, string groupId, string symArea)
        {
            if (roles == null || string.IsNullOrWhiteSpace(symArea))
            {
                return;
            }

            int parsedGroupId = 0;
            int.TryParse(groupId, out parsedGroupId);

            HashSet<string> existingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (SymUserRollVM role in roles)
            {
                if (role == null)
                {
                    continue;
                }

                existingKeys.Add((role.symArea ?? "").Trim() + "|" + (role.symController ?? "").Trim());
            }

            foreach (DynamicMenuDefinition definition in GetDynamicDefaultRollDefinitions())
            {
                if (!string.Equals((definition.renderArea ?? "").Trim(), symArea.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string key = definition.renderArea.Trim() + "|" + definition.permissionKey.Trim();
                if (!existingKeys.Add(key))
                {
                    continue;
                }

                roles.Add(new SymUserRollVM
                {
                    Id = "",
                    GroupId = parsedGroupId,
                    symArea = definition.renderArea.Trim(),
                    symController = definition.permissionKey.Trim(),
                    IsIndex = false,
                    IsAdd = false,
                    IsEdit = false,
                    IsDelete = false,
                    IsReport = false,
                    IsProcess = false,
                    IsActive = true,
                    IsArchive = false
                });
            }
        }

        #region Methods
        //==================SelectAll=================
        public List<SymUserRollVM> SelectAll()
        {

            #region Variables

            SqlConnection currConn = null;
            string sqlText = "";
            List<SymUserRollVM> VMs = new List<SymUserRollVM>();
            SymUserRollVM VM;
            #endregion
            try
            {
                #region open connection and transaction

                currConn = _dbsqlConnection.GetConnection();
                if (currConn.State != ConnectionState.Open)
                {
                    currConn.Open();
                }
                EnsureDynamicRoleColumns(currConn, null);

                #endregion open connection and transaction

                #region sql statement

                sqlText = @"SELECT
Id
,BranchId
,GroupId
,symArea
,symController
,IsIndex
,IsAdd
,IsEdit
,IsDelete
,IsReport
,IsProcess
,Remarks
,IsActive
,IsArchive
,CreatedBy
,CreatedAt
,CreatedFrom
,LastUpdateBy
,LastUpdateAt
,LastUpdateFrom
From SymUserRoll
Where IsArchive=0 

ORDER BY symArea
";

                SqlCommand objComm = new SqlCommand();
                objComm.Connection = currConn;
                objComm.CommandText = sqlText;
                objComm.CommandType = CommandType.Text;
                SqlDataReader dr;
                dr = objComm.ExecuteReader();
                while (dr.Read())
                {
                    VM = new SymUserRollVM();
                    VM.Id = dr["Id"].ToString();
                    VM.BranchId = Convert.ToInt32(dr["BranchId"]);
                    VM.GroupId = Convert.ToInt32(dr["GroupId"].ToString());
                    VM.symArea = dr["symArea"].ToString();
                    VM.symController = dr["symController"].ToString();
                    VM.IsIndex = Convert.ToBoolean(dr["IsIndex"].ToString());
                    VM.IsAdd = Convert.ToBoolean(dr["IsAdd"].ToString());
                    VM.IsEdit = Convert.ToBoolean(dr["IsEdit"].ToString());
                    VM.IsDelete = Convert.ToBoolean(dr["IsDelete"].ToString());
                    VM.IsReport = Convert.ToBoolean(dr["IsReport"].ToString());
                    VM.IsProcess = Convert.ToBoolean(dr["IsProcess"].ToString());
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

        public List<SymUserRollVM> SelectAllByGroupId(string GroupId, string SymArea = null)
        {

            #region Variables

            SqlConnection currConn = null;
            string sqlText = "";
            List<SymUserRollVM> VMs = new List<SymUserRollVM>();
            SymUserRollVM VM;
            #endregion
            try
            {
                #region open connection and transaction

                currConn = _dbsqlConnection.GetConnection();
                if (currConn.State != ConnectionState.Open)
                {
                    currConn.Open();
                }
                EnsureDynamicRoleColumns(currConn, null);

                #endregion open connection and transaction

                #region sql statement

                sqlText = @"SELECT
SymUserRoll.Id
,SymUserRoll.BranchId
,SymUserRoll.GroupId
,ISNULL(sd.symArea, CONVERT(varchar(200), SymUserRoll.symArea)) symArea
,ISNULL(sd.symController, CONVERT(varchar(500), SymUserRoll.symController)) symController
,SymUserRoll.IsIndex
,SymUserRoll.IsAdd
,SymUserRoll.IsEdit
,SymUserRoll.IsDelete
,SymUserRoll.IsReport
,SymUserRoll.IsProcess
,SymUserRoll.Remarks
,SymUserRoll.IsActive
,SymUserRoll.IsArchive
,SymUserRoll.CreatedBy
,SymUserRoll.CreatedAt
,SymUserRoll.CreatedFrom
,SymUserRoll.LastUpdateBy
,SymUserRoll.LastUpdateAt
,SymUserRoll.LastUpdateFrom
From SymUserRoll
left outer join SymUserDefaultRoll sd on sd.Id=SymUserRoll.DefaultRollId
Where SymUserRoll.IsArchive=0 
and SymUserRoll.GroupId=@GroupId  

";
                if (!string.IsNullOrWhiteSpace(SymArea))
                {
                    sqlText += " and ISNULL(sd.symArea, CONVERT(varchar(200), SymUserRoll.symArea))=@symArea ";
                }
                SqlCommand objComm = new SqlCommand();
                objComm.Connection = currConn;
                objComm.CommandText = sqlText;
                objComm.CommandType = CommandType.Text;
                objComm.Parameters.AddWithValue("@GroupId", GroupId);
                if (!string.IsNullOrWhiteSpace(SymArea))
                {
                    objComm.Parameters.AddWithValue("@symArea", SymArea);
                }
                SqlDataReader dr;
                dr = objComm.ExecuteReader();
                while (dr.Read())
                {
                    VM = new SymUserRollVM();
                    VM.Id = dr["Id"].ToString();
                    VM.BranchId = Convert.ToInt32(dr["BranchId"]);
                    VM.GroupId = Convert.ToInt32(dr["GroupId"]);
                    VM.symArea = dr["symArea"].ToString();
                    VM.symController = dr["symController"].ToString();
                    VM.IsIndex = Convert.ToBoolean(dr["IsIndex"].ToString());
                    VM.IsAdd = Convert.ToBoolean(dr["IsAdd"].ToString());
                    VM.IsEdit = Convert.ToBoolean(dr["IsEdit"].ToString());
                    VM.IsDelete = Convert.ToBoolean(dr["IsDelete"].ToString());
                    VM.IsReport = Convert.ToBoolean(dr["IsReport"].ToString());
                    VM.IsProcess = Convert.ToBoolean(dr["IsProcess"].ToString());
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

                AddDynamicRoleRows(VMs, GroupId, SymArea);

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
        //==================SelectAll=================
        public List<SymUserDefaultRollVM> SelectSymUserDefaultRollAll(string GroupId)
        {
            #region Variables

            SqlConnection currConn = null;
            string sqlText = "";
            List<SymUserDefaultRollVM> VMs = new List<SymUserDefaultRollVM>();
            SymUserDefaultRollVM VM;
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
,BranchId
,symArea
,symController
,IsIndex
,IsAdd
,IsEdit
,IsDelete
,IsReport
,IsProcess
From SymUserDefaultRoll
Where IsArchive=0
and id not in(
select distinct DefaultRollId from SymUserRoll where GroupId=@GroupId
)
ORDER BY symArea
";
                SqlCommand objComm = new SqlCommand();
                objComm.Connection = currConn;
                objComm.CommandText = sqlText;
                objComm.CommandType = CommandType.Text;
                objComm.Parameters.AddWithValue("@GroupId", GroupId);
                SqlDataReader dr;
                dr = objComm.ExecuteReader();
                while (dr.Read())
                {
                    VM = new SymUserDefaultRollVM();
                    VM.Id = dr["Id"].ToString();
                    VM.BranchId = Convert.ToInt32(dr["BranchId"]);
                    VM.symArea = dr["symArea"].ToString();
                    VM.symController = dr["symController"].ToString();
                    VM.IsIndex = Convert.ToBoolean(dr["IsIndex"].ToString());
                    VM.IsAdd = Convert.ToBoolean(dr["IsAdd"].ToString());
                    VM.IsEdit = Convert.ToBoolean(dr["IsEdit"].ToString());
                    VM.IsDelete = Convert.ToBoolean(dr["IsDelete"].ToString());
                    VM.IsReport = Convert.ToBoolean(dr["IsReport"].ToString());
                    VM.IsProcess = Convert.ToBoolean(dr["IsProcess"].ToString());
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
        //==================SelectAll=================
        public List<UserLogsVM> SelectAllUser()
        {
            #region Variables
            SqlConnection currConn = null;
            string sqlText = "";
            UserLogsVM userVM = new UserLogsVM();
            List<UserLogsVM> userVMs = new List<UserLogsVM>();
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
,FullName
,Email
,LogId
,Password
,VerificationCode
,BranchId
,EmployeeId
,IsAdmin
,IsActive
,IsVerified
,IsArchived
From [User]";
                SqlCommand objComm = new SqlCommand();
                objComm.Connection = currConn;
                objComm.CommandText = sqlText;
                objComm.CommandType = CommandType.Text;
                SqlDataReader dr;
                dr = objComm.ExecuteReader();
                while (dr.Read())
                {
                    userVM = new UserLogsVM();
                    userVM.Id = dr["Id"].ToString();
                    userVM.BranchId = Convert.ToInt32(dr["BranchId"]);
                    userVM.FullName = dr["FullName"].ToString();
                    userVM.Email = dr["Email"].ToString();
                    userVM.LogID = dr["LogId"].ToString();
                    userVM.Password = dr["Password"].ToString();
                    userVM.VerificationCode = dr["VerificationCode"].ToString();
                    userVM.EmployeeId = dr["EmployeeId"].ToString();
                    userVM.IsAdmin = Convert.ToBoolean(dr["IsAdmin"]);
                    userVM.IsActive = Convert.ToBoolean(dr["IsActive"]);
                    userVM.IsVerified = Convert.ToBoolean(dr["IsVerified"]);
                    userVM.IsArchived = Convert.ToBoolean(dr["IsArchived"]);
                    userVMs.Add(userVM);
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
            return userVMs;
        }
        public UserLogsVM SelectGroupId(string Id)
        {
            #region Variables
            SqlConnection currConn = null;
            string sqlText = "";
            UserLogsVM userVM = new UserLogsVM();
            List<UserLogsVM> userVMs = new List<UserLogsVM>();
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
,FullName
,Email
,LogId
,Password
,VerificationCode
,BranchId
,EmployeeId
,IsAdmin
,IsActive
,IsVerified
,IsArchived
From [User]
where Id=@Id"
;
                SqlCommand objComm = new SqlCommand();
                objComm.Connection = currConn;
                objComm.CommandText = sqlText;
                objComm.CommandType = CommandType.Text;
                objComm.Parameters.AddWithValue("@Id", Id);
                SqlDataReader dr;
                dr = objComm.ExecuteReader();
                while (dr.Read())
                {
                    userVM.Id = dr["Id"].ToString();
                    userVM.BranchId = Convert.ToInt32(dr["BranchId"]);
                    userVM.FullName = dr["FullName"].ToString();
                    userVM.Email = dr["Email"].ToString();
                    userVM.LogID = dr["LogId"].ToString();
                    userVM.Password = dr["Password"].ToString();
                    userVM.VerificationCode = dr["VerificationCode"].ToString();
                    userVM.EmployeeId = dr["EmployeeId"].ToString();
                    userVM.IsAdmin = Convert.ToBoolean(dr["IsAdmin"]);
                    userVM.IsActive = Convert.ToBoolean(dr["IsActive"]);
                    userVM.IsVerified = Convert.ToBoolean(dr["IsVerified"]);
                    userVM.IsArchived = Convert.ToBoolean(dr["IsArchived"]);
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
            return userVM;
        }
        public UserLogsVM SelectUserByEmployee(string EmployeeId)
        {

            #region Variables

            SqlConnection currConn = null;
            string sqlText = "";
            UserLogsVM userVM = new UserLogsVM();
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

                sqlText = @"SELECT top 1
 Id
,FullName
,Email
,LogId
,Password
,VerificationCode
,BranchId
,EmployeeId
,IsAdmin
,IsActive
,IsVerified
,IsArchived
From [User]
Where EmployeeId=@EmployeeId
";
                SqlCommand objComm = new SqlCommand();
                objComm.Connection = currConn;
                objComm.CommandText = sqlText;
                objComm.CommandType = CommandType.Text;
                objComm.Parameters.AddWithValue("@EmployeeId", EmployeeId);

                SqlDataReader dr;
                dr = objComm.ExecuteReader();
                while (dr.Read())
                {
                    userVM.Id = dr["Id"].ToString();
                    userVM.BranchId = Convert.ToInt32(dr["BranchId"]);
                    userVM.FullName = dr["FullName"].ToString();
                    userVM.Email = dr["Email"].ToString();
                    userVM.LogID = dr["LogId"].ToString();
                    userVM.Password = dr["Password"].ToString();
                    userVM.VerificationCode = dr["VerificationCode"].ToString();
                    userVM.EmployeeId = dr["EmployeeId"].ToString();
                    userVM.IsAdmin = Convert.ToBoolean(dr["IsAdmin"]);
                    userVM.IsActive = Convert.ToBoolean(dr["IsActive"]);
                    userVM.IsVerified = Convert.ToBoolean(dr["IsVerified"]);
                    userVM.IsArchived = Convert.ToBoolean(dr["IsArchived"]);
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

            return userVM;
        }
        //==================SelectByID=================
        public SymUserRollVM SelectById(string Id)
        {

            #region Variables

            SqlConnection currConn = null;
            string sqlText = "";
            SymUserRollVM VM = new SymUserRollVM();

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
,BranchId
,GroupId
,symArea
,symController
,IsIndex
,IsAdd
,IsEdit
,IsDelete
,IsReport
,IsProcess
,Remarks
,IsActive
,IsArchive
,CreatedBy
,CreatedAt
,CreatedFrom
,LastUpdateBy
,LastUpdateAt
,LastUpdateFrom
From SymUserRoll
Where id=@Id and IsArchive=0
";

                SqlCommand objComm = new SqlCommand();
                objComm.Connection = currConn;
                objComm.CommandText = sqlText;
                objComm.CommandType = CommandType.Text;
                objComm.Parameters.AddWithValue("@Id", Id);

                SqlDataReader dr;
                dr = objComm.ExecuteReader();
                while (dr.Read())
                {
                    VM.Id = dr["Id"].ToString();
                    VM.BranchId = Convert.ToInt32(dr["BranchId"]);
                    VM.GroupId = Convert.ToInt32(dr["GroupId"].ToString());
                    VM.symArea = dr["symArea"].ToString();
                    VM.symController = dr["symController"].ToString();
                    VM.IsIndex = Convert.ToBoolean(dr["IsIndex"].ToString());
                    VM.IsAdd = Convert.ToBoolean(dr["IsAdd"].ToString());
                    VM.IsEdit = Convert.ToBoolean(dr["IsEdit"].ToString());
                    VM.IsDelete = Convert.ToBoolean(dr["IsDelete"].ToString());
                    VM.IsReport = Convert.ToBoolean(dr["IsReport"].ToString());
                    VM.IsProcess = Convert.ToBoolean(dr["IsProcess"].ToString());
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
        //==================SelectByID=================
        public List<SymUserDefaultRollVM> SelectSymUserById(string Id)
        {
            #region Variables

            SqlConnection currConn = null;
            string sqlText = "";
            SymUserDefaultRollVM VM = new SymUserDefaultRollVM();
            List<SymUserDefaultRollVM> VMs = new List<SymUserDefaultRollVM>();
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
                sqlText = @"SELECT* from 
(select s.Id
,s.DefaultRollId ,s.BranchId,us.Id GroupId,symArea,symController,IsIndex,IsAdd,IsEdit,IsDelete,IsReport,IsProcess
,Remarks,s.IsActive,s.IsArchive,s.CreatedBy,s.CreatedAt,s.CreatedFrom,s.LastUpdateBy,s.LastUpdateAt,s.LastUpdateFrom
,us.FullName,us.Email,us.Password from SymUserRoll s
LEFT OUTER JOIN dbo.[User] us ON us.Id=s.GroupId
where us.id=@Id
 union all
select '0' Id
,id DefaultRollId,BranchId,'0' GroupId,symArea,symController,IsIndex,IsAdd,IsEdit,IsDelete,IsReport,IsProcess
,Remarks,IsActive,IsArchive,CreatedBy,CreatedAt,CreatedFrom,LastUpdateBy,LastUpdateAt
,LastUpdateFrom,'NA'FullName,'NA'Email,'NA' Password
from SymUserDefaultRoll where 1=1
and id not in( select DefaultRollId from SymUserRoll)) as a";
                SqlCommand objComm = new SqlCommand();
                objComm.Connection = currConn;
                objComm.CommandText = sqlText;
                objComm.CommandType = CommandType.Text;
                objComm.Parameters.AddWithValue("@Id", Id);
                SqlDataReader dr;
                dr = objComm.ExecuteReader();
                while (dr.Read())
                {
                    VM = new SymUserDefaultRollVM();
                    VM.Id = dr["Id"].ToString();
                    VM.BranchId = Convert.ToInt32(dr["BranchId"]);
                    VM.GroupId = Convert.ToInt32(dr["GroupId"]);
                    VM.FullName = dr["FullName"].ToString();
                    VM.Email = dr["Email"].ToString();
                    VM.Password = dr["Password"].ToString();
                    VM.symArea = dr["symArea"].ToString();
                    VM.symController = dr["symController"].ToString();
                    VM.IsIndex = Convert.ToBoolean(dr["IsIndex"].ToString());
                    VM.IsAdd = Convert.ToBoolean(dr["IsAdd"].ToString());
                    VM.IsEdit = Convert.ToBoolean(dr["IsEdit"].ToString());
                    VM.IsDelete = Convert.ToBoolean(dr["IsDelete"].ToString());
                    VM.IsReport = Convert.ToBoolean(dr["IsReport"].ToString());
                    VM.IsProcess = Convert.ToBoolean(dr["IsProcess"].ToString());
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
        public SymUserRollVM SelectById(string Id, string symArea, string symController)
        {

            #region Variables

            SqlConnection currConn = null;
            string sqlText = "";
            SymUserRollVM VM = new SymUserRollVM();

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
,BranchId
,GroupId
,symArea
,symController
,IsIndex
,IsAdd
,IsEdit
,IsDelete
,IsReport
,IsProcess
,Remarks
,IsActive
,IsArchive
,CreatedBy
,CreatedAt
,CreatedFrom
,LastUpdateBy
,LastUpdateAt
,LastUpdateFrom
From SymUserRoll
Where IsArchive=0
ORDER BY symArea
Where Id=@Id and symArea=@symArea and symController=@symController  and IsArchive=0
";

                SqlCommand objComm = new SqlCommand();
                objComm.Connection = currConn;
                objComm.CommandText = sqlText;
                objComm.CommandType = CommandType.Text;
                objComm.Parameters.AddWithValue("@Id", Id);
                objComm.Parameters.AddWithValue("@symArea", symArea);
                objComm.Parameters.AddWithValue("@symController", symController);

                SqlDataReader dr;
                dr = objComm.ExecuteReader();
                while (dr.Read())
                {
                    VM.Id = dr["Id"].ToString();
                    VM.BranchId = Convert.ToInt32(dr["BranchId"]);
                    VM.GroupId = Convert.ToInt32(dr["GroupId"]);
                    VM.symArea = dr["symArea"].ToString();
                    VM.symController = dr["symController"].ToString();
                    VM.IsIndex = Convert.ToBoolean(dr["IsIndex"].ToString());
                    VM.IsAdd = Convert.ToBoolean(dr["IsAdd"].ToString());
                    VM.IsEdit = Convert.ToBoolean(dr["IsEdit"].ToString());
                    VM.IsDelete = Convert.ToBoolean(dr["IsDelete"].ToString());
                    VM.IsReport = Convert.ToBoolean(dr["IsReport"].ToString());
                    VM.IsProcess = Convert.ToBoolean(dr["IsProcess"].ToString());
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
        public List<SymUserDefaultRollVM> UserRollDetail(string empId, string symArea)
        {

            #region Variables

            SqlConnection currConn = null;
            string sqlText = "";
            SymUserDefaultRollVM VM = new SymUserDefaultRollVM();
            List<SymUserDefaultRollVM> VMs = new List<SymUserDefaultRollVM>();

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

                sqlText = @"SELECT* from 
(select s.Id
,s.DefaultRollId ,s.BranchId,us.Id GroupId,symArea,symController
,IsIndex,IsAdd,IsEdit,IsDelete,IsReport
,IsProcess,Remarks,s.IsActive,s.IsArchive,s.CreatedBy,s.CreatedAt,s.CreatedFrom,s.LastUpdateBy
,s.LastUpdateAt
,s.LastUpdateFrom
,us.FullName,us.Email,us.Password from SymUserRoll s
LEFT OUTER JOIN dbo.[UserGroup] us ON us.Id=s.GroupId
where 1=1";
                if (!string.IsNullOrWhiteSpace(empId) && empId != "undefined" && empId != "null")
                {
                    sqlText += @" and s.GroupId=@Id";
                }
                if (!string.IsNullOrWhiteSpace(symArea) && symArea != "" && symArea != "null" && symArea != "undefined")
                {
                    sqlText += @" and s.symArea=@symArea";
                }


                sqlText += @" 
 union all
select '0' Id
,id DefaultRollId
,BranchId
,'0' GroupId
,symArea
,symController
,IsIndex
,IsAdd
,IsEdit
,IsDelete
,IsReport
,IsProcess
,Remarks
,IsActive
,IsArchive
,CreatedBy
,CreatedAt
,CreatedFrom
,LastUpdateBy
,LastUpdateAt
,LastUpdateFrom,'NA'FullName,'NA'Email,'NA' Password
from SymUserDefaultRoll where 1=1";
                if (!string.IsNullOrWhiteSpace(symArea) && symArea != "" && symArea != "null" && symArea != "undefined")
                {
                    sqlText += @" and symArea=@symArea";
                }


                sqlText += @"  and id not in( select DefaultRollId from SymUserRoll)) as a ";
                SqlCommand objComm = new SqlCommand();
                objComm.Connection = currConn;
                objComm.CommandText = sqlText;
                objComm.CommandType = CommandType.Text;
                if (!string.IsNullOrWhiteSpace(empId) && empId != "undefined" && empId != "null")
                    objComm.Parameters.AddWithValue("@Id", empId);
                if (!string.IsNullOrWhiteSpace(symArea) && symArea != "" && symArea != "null")
                    objComm.Parameters.AddWithValue("@symArea", symArea);

                SqlDataReader dr;
                dr = objComm.ExecuteReader();
                while (dr.Read())
                {
                    VM = new SymUserDefaultRollVM();
                    VM.Id = dr["Id"].ToString();
                    VM.BranchId = Convert.ToInt32(dr["BranchId"]);
                    VM.GroupId = Convert.ToInt32(dr["GroupId"]);
                    VM.FullName = dr["FullName"].ToString();
                    VM.Email = dr["Email"].ToString();
                    VM.Password = dr["Password"].ToString();
                    VM.symArea = dr["symArea"].ToString();
                    VM.symController = dr["symController"].ToString();
                    VM.IsIndex = Convert.ToBoolean(dr["IsIndex"].ToString());
                    VM.IsAdd = Convert.ToBoolean(dr["IsAdd"].ToString());
                    VM.IsEdit = Convert.ToBoolean(dr["IsEdit"].ToString());
                    VM.IsDelete = Convert.ToBoolean(dr["IsDelete"].ToString());
                    VM.IsReport = Convert.ToBoolean(dr["IsReport"].ToString());
                    VM.IsProcess = Convert.ToBoolean(dr["IsProcess"].ToString());
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
        public string[] SelectAllSymRollwithInsert(SymUserRollVM vm, SqlConnection VcurrConn, SqlTransaction Vtransaction)
        {
            #region Initializ
            string sqlText = "";
            string sqlText1 = "";
            int Id = 0;
            string[] retResults = new string[6];
            retResults[0] = "Fail";//Success or Fail
            retResults[1] = "Fail";// Success or Fail Message
            retResults[2] = Id.ToString();// Return Id
            retResults[3] = sqlText; //  SQL Query
            retResults[4] = "ex"; //catch ex
            retResults[5] = "InsertSymUserRoll"; //Method Name

            SqlConnection currConn = null;
            SqlTransaction transaction = null;

            #endregion
            #region Try
            try
            {
                #region Validation
                //if (string.IsNullOrEmpty(SymUserRollVM.DepartmentId))
                //{
                //    retResults[1] = "Please Input Employee Travel Course";
                //    return retResults;
                //}
                CommonDAL cdal = new CommonDAL();
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
                EnsureDynamicRoleColumns(currConn, transaction);
                #endregion open connection and transaction
                #region Save
                sqlText1 = "Select isnull(max(convert(int,  SUBSTRING(CONVERT(varchar(10), id),CHARINDEX('_', CONVERT(varchar(10), id))+1,10))),0) from SymUserRoll where BranchId=@BranchId";
                SqlCommand cmd2 = new SqlCommand(sqlText1, currConn);
                cmd2.Parameters.AddWithValue("@BranchId", vm.BranchId);
                cmd2.Transaction = transaction;
                var exeRes = cmd2.ExecuteScalar();
                int count = Convert.ToInt32(exeRes);
                var SymDefaultRollList = SelectSymUserDefaultRollAll(vm.GroupId.ToString());
                foreach (var item in SymDefaultRollList)
                {

                    sqlText = "  ";
                    sqlText += @" INSERT INTO SymUserRoll(Id,BranchId,DefaultRollId,GroupId,IsIndex,IsAdd,IsEdit,IsDelete,IsReport,IsProcess,Remarks,IsActive,IsArchive,CreatedBy,CreatedAt,CreatedFrom) 
                                VALUES (@Id,@BranchId,@DefaultRollId,@GroupId,@IsIndex,@IsAdd,@IsEdit,@IsDelete,@IsReport,@IsProcess,@Remarks,@IsActive,@IsArchive,@CreatedBy,@CreatedAt,@CreatedFrom)";
                    SqlCommand cmdInsert = new SqlCommand(sqlText, currConn);
                    var symId = vm.BranchId.ToString() + "_" + (count + 1);
                    cmdInsert.Parameters.AddWithValue("@Id", symId);
                    cmdInsert.Parameters.AddWithValue("@GroupId", vm.GroupId);
                    cmdInsert.Parameters.AddWithValue("@BranchId", vm.BranchId);
                    cmdInsert.Parameters.AddWithValue("@DefaultRollId", item.Id.Trim());
                    cmdInsert.Parameters.AddWithValue("@IsIndex", item.IsIndex);
                    cmdInsert.Parameters.AddWithValue("@IsAdd", item.IsAdd);
                    cmdInsert.Parameters.AddWithValue("@IsEdit", item.IsEdit);
                    cmdInsert.Parameters.AddWithValue("@IsDelete", item.IsDelete);
                    cmdInsert.Parameters.AddWithValue("@IsReport", item.IsReport);
                    cmdInsert.Parameters.AddWithValue("@IsProcess", item.IsProcess);
                    cmdInsert.Parameters.AddWithValue("@Remarks", item.Remarks ?? Convert.DBNull);//, SymUserRollVM.Remarks);
                    cmdInsert.Parameters.AddWithValue("@IsActive", true);
                    cmdInsert.Parameters.AddWithValue("@IsArchive", false);
                    cmdInsert.Parameters.AddWithValue("@CreatedBy", vm.CreatedBy);
                    cmdInsert.Parameters.AddWithValue("@CreatedAt", vm.CreatedAt);
                    cmdInsert.Parameters.AddWithValue("@CreatedFrom", vm.CreatedFrom);
                    cmdInsert.Transaction = transaction;
                    cmdInsert.ExecuteNonQuery();
                    count++;

                }

                foreach (DynamicMenuDefinition definition in GetDynamicDefaultRollDefinitions())
                {
                    sqlText = @"IF NOT EXISTS (
    SELECT 1
    FROM SymUserRoll sur
    LEFT OUTER JOIN SymUserDefaultRoll sd ON sd.Id=sur.DefaultRollId
    WHERE sur.GroupId=@GroupId
    AND sur.IsArchive=0
    AND ISNULL(sd.symArea, CONVERT(varchar(200), sur.symArea))=@symArea
    AND ISNULL(sd.symController, CONVERT(varchar(500), sur.symController))=@symController
)
BEGIN
    INSERT INTO SymUserRoll(Id,BranchId,DefaultRollId,GroupId,symArea,symController,IsIndex,IsAdd,IsEdit,IsDelete,IsReport,IsProcess,Remarks,IsActive,IsArchive,CreatedBy,CreatedAt,CreatedFrom)
    VALUES (@Id,@BranchId,@DefaultRollId,@GroupId,@symArea,@symController,@IsIndex,@IsAdd,@IsEdit,@IsDelete,@IsReport,@IsProcess,@Remarks,@IsActive,@IsArchive,@CreatedBy,@CreatedAt,@CreatedFrom)
END";

                    SqlCommand cmdInsertDynamic = new SqlCommand(sqlText, currConn);
                    var symId = vm.BranchId.ToString() + "_" + (count + 1);
                    cmdInsertDynamic.Parameters.AddWithValue("@Id", symId);
                    cmdInsertDynamic.Parameters.AddWithValue("@GroupId", vm.GroupId);
                    cmdInsertDynamic.Parameters.AddWithValue("@BranchId", vm.BranchId);
                    cmdInsertDynamic.Parameters.AddWithValue("@DefaultRollId", BuildDynamicDefaultRollId(definition));
                    cmdInsertDynamic.Parameters.AddWithValue("@symArea", definition.renderArea.Trim());
                    cmdInsertDynamic.Parameters.AddWithValue("@symController", definition.permissionKey.Trim());
                    cmdInsertDynamic.Parameters.AddWithValue("@IsIndex", false);
                    cmdInsertDynamic.Parameters.AddWithValue("@IsAdd", false);
                    cmdInsertDynamic.Parameters.AddWithValue("@IsEdit", false);
                    cmdInsertDynamic.Parameters.AddWithValue("@IsDelete", false);
                    cmdInsertDynamic.Parameters.AddWithValue("@IsReport", false);
                    cmdInsertDynamic.Parameters.AddWithValue("@IsProcess", false);
                    cmdInsertDynamic.Parameters.AddWithValue("@Remarks", Convert.DBNull);
                    cmdInsertDynamic.Parameters.AddWithValue("@IsActive", true);
                    cmdInsertDynamic.Parameters.AddWithValue("@IsArchive", false);
                    cmdInsertDynamic.Parameters.AddWithValue("@CreatedBy", vm.CreatedBy);
                    cmdInsertDynamic.Parameters.AddWithValue("@CreatedAt", vm.CreatedAt);
                    cmdInsertDynamic.Parameters.AddWithValue("@CreatedFrom", vm.CreatedFrom);
                    cmdInsertDynamic.Transaction = transaction;
                    cmdInsertDynamic.ExecuteNonQuery();
                    count++;
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
                //retResults[2] = vm.Id;

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
        //==================Insert =================
        public string[] Insert(SymUserRollVM vm, SqlConnection VcurrConn, SqlTransaction Vtransaction)
        {
            #region Initializ
            string sqlText = "";
            string sqlText1 = "";
            int Id = 0;
            string[] retResults = new string[6];
            retResults[0] = "Fail";//Success or Fail
            retResults[1] = "Fail";// Success or Fail Message
            retResults[2] = Id.ToString();// Return Id
            retResults[3] = sqlText; //  SQL Query
            retResults[4] = "ex"; //catch ex
            retResults[5] = "InsertSymUserRoll"; //Method Name

            SqlConnection currConn = null;
            SqlTransaction transaction = null;

            #endregion
            #region Try
            try
            {
                #region Validation
                //if (string.IsNullOrEmpty(SymUserRollVM.DepartmentId))
                //{
                //    retResults[1] = "Please Input Employee Travel Course";
                //    return retResults;
                //}
                CommonDAL cdal = new CommonDAL();
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
                #region Save
                foreach (var item in vm.SymUserDefaultRollVMs)
                {

                    if (1 == 1)
                    {
                        CommonDAL _cdal = new CommonDAL();
                        var checkexit = _cdal.CheckDuplicateInInsert("SymUserRoll", "GroupId", vm.UserlogVM.Id, null, null);
                        if (checkexit == false)
                        {
                            sqlText = "  ";
                            sqlText += @" INSERT INTO SymUserRoll(Id,BranchId,DefaultRollId,GroupId,symArea,symController,IsIndex,IsAdd,IsEdit,IsDelete,IsReport,IsProcess,Remarks,IsActive,IsArchive,CreatedBy,CreatedAt,CreatedFrom) 
                                VALUES (@Id,@BranchId,@DefaultRollId,@GroupId,@symArea,@symController,@IsIndex,@IsAdd,@IsEdit,@IsDelete,@IsReport,@IsProcess,@Remarks,@IsActive,@IsArchive,@CreatedBy,@CreatedAt,@CreatedFrom)";


                        }
                        else
                        {
                            sqlText = "";
                            sqlText = "update SymUserRoll set";
                            sqlText += "  IsIndex=@IsIndex";
                            sqlText += " , IsAdd=@IsAdd";
                            sqlText += " , IsEdit=@IsEdit";
                            sqlText += " , IsDelete=@IsDelete";
                            sqlText += " , IsReport=@IsReport";
                            sqlText += " , IsProcess=@IsProcess";
                            sqlText += " , Remarks=@Remarks";
                            sqlText += " , LastUpdateBy=@LastUpdateBy";
                            sqlText += " , LastUpdateAt=@LastUpdateAt";
                            sqlText += " , LastUpdateFrom=@LastUpdateFrom";
                            sqlText += " where symArea=@symArea and symController=@symController and GroupId=@GroupId ";

                        }
                        SqlCommand cmdInsert = new SqlCommand(sqlText, currConn);
                        sqlText1 = "Select isnull(max(convert(int,  SUBSTRING(CONVERT(varchar(10), id),CHARINDEX('_', CONVERT(varchar(10), id))+1,10))),0) from SymUserRoll where BranchId=@BranchId";
                        SqlCommand cmd2 = new SqlCommand(sqlText1, currConn);
                        cmd2.Parameters.AddWithValue("@BranchId", vm.BranchId);
                        cmd2.Transaction = transaction;
                        var exeRes = cmd2.ExecuteScalar();
                        int count = Convert.ToInt32(exeRes);
                        var symId = vm.BranchId.ToString() + "_" + (count + 1);
                        if (checkexit == false)
                        {
                            cmdInsert.Parameters.AddWithValue("@Id", symId);
                        }
                        cmdInsert.Parameters.AddWithValue("@GroupId", vm.UserlogVM.Id);
                        cmdInsert.Parameters.AddWithValue("@symArea", item.symArea.Trim());
                        cmdInsert.Parameters.AddWithValue("@symController", item.symController.Trim());
                        cmdInsert.Parameters.AddWithValue("@IsIndex", item.IsIndex);
                        cmdInsert.Parameters.AddWithValue("@IsAdd", item.IsAdd);
                        cmdInsert.Parameters.AddWithValue("@IsEdit", item.IsEdit);
                        cmdInsert.Parameters.AddWithValue("@IsDelete", item.IsDelete);
                        cmdInsert.Parameters.AddWithValue("@IsReport", item.IsReport);
                        cmdInsert.Parameters.AddWithValue("@IsProcess", item.IsProcess);
                        cmdInsert.Parameters.AddWithValue("@Remarks", item.Remarks ?? Convert.DBNull);//, SymUserRollVM.Remarks);
                        cmdInsert.Parameters.AddWithValue("@LastUpdateBy", vm.CreatedBy);
                        cmdInsert.Parameters.AddWithValue("@LastUpdateAt", vm.CreatedAt);
                        cmdInsert.Parameters.AddWithValue("@LastUpdateFrom", vm.CreatedFrom);
                        cmdInsert.Transaction = transaction;
                        cmdInsert.ExecuteNonQuery();


                    }
                    else
                    {
                        retResults[1] = "This SymUserRoll already used";
                        throw new ArgumentNullException("Please Input SymUserRoll Value", "");
                    }
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
                //retResults[2] = vm.Id;

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
            retResults[5] = "Employee SymUserRoll Update"; //Method Name

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

                if (transaction == null) { transaction = currConn.BeginTransaction("UpdateToSymUserRoll"); }
                EnsureDynamicRoleColumns(currConn, transaction);

                #endregion open connection and transaction

                foreach (var item in vm.SymUserRollVMs)
                    if (vm != null)
                    {
                        #region Update Settings
                        sqlText = "";
                        sqlText = "update SymUserRoll set";
                        sqlText += "  GroupId=@GroupId";

                        sqlText += " , IsIndex=@IsIndex";
                        sqlText += " , IsAdd=@IsAdd";
                        sqlText += " , IsEdit=@IsEdit";
                        sqlText += " , IsDelete=@IsDelete";
                        sqlText += " , IsReport=@IsReport";
                        sqlText += " , IsProcess=@IsProcess";
                        sqlText += " , Remarks=@Remarks";
                        sqlText += " , IsActive=@IsActive";
                        sqlText += " , LastUpdateBy=@LastUpdateBy";
                        sqlText += " , LastUpdateAt=@LastUpdateAt";
                        sqlText += " , LastUpdateFrom=@LastUpdateFrom";
                        sqlText += " where Id=@Id";
                        SqlCommand cmdUpdate = new SqlCommand(sqlText, currConn);
                        cmdUpdate.Parameters.AddWithValue("@Id", item.Id);
                        cmdUpdate.Parameters.AddWithValue("@GroupId", item.GroupId.ToString().Trim());

                        cmdUpdate.Parameters.AddWithValue("@IsIndex", item.IsIndex);
                        cmdUpdate.Parameters.AddWithValue("@IsAdd", item.IsAdd);
                        cmdUpdate.Parameters.AddWithValue("@IsEdit", item.IsEdit);
                        cmdUpdate.Parameters.AddWithValue("@IsDelete", item.IsDelete);
                        cmdUpdate.Parameters.AddWithValue("@IsReport", item.IsReport);
                        cmdUpdate.Parameters.AddWithValue("@IsProcess", item.IsProcess);
                        cmdUpdate.Parameters.AddWithValue("@Remarks", item.Remarks ?? Convert.DBNull);//, SymUserRollVM.Remarks);
                        cmdUpdate.Parameters.AddWithValue("@IsActive", true);
                        cmdUpdate.Parameters.AddWithValue("@LastUpdateBy", vm.LastUpdateBy);
                        cmdUpdate.Parameters.AddWithValue("@LastUpdateAt", vm.LastUpdateAt);
                        cmdUpdate.Parameters.AddWithValue("@LastUpdateFrom", vm.LastUpdateFrom);

                        cmdUpdate.Transaction = transaction;
                        var exeRes = cmdUpdate.ExecuteNonQuery();
                        transResult = Convert.ToInt32(exeRes);

                        retResults[2] = vm.Id.ToString();// Return Id
                        retResults[3] = sqlText; //  SQL Query

                        #region Commit

                        if (transResult <= 0)
                        {
                            if (!string.IsNullOrWhiteSpace(item.symArea) && !string.IsNullOrWhiteSpace(item.symController))
                            {
                                sqlText = @"DECLARE @BranchId int
SELECT TOP 1 @BranchId=BranchId FROM SymUserRoll WHERE GroupId=@GroupId
IF @BranchId IS NULL SET @BranchId=1

DECLARE @NextId int
SELECT @NextId=isnull(max(convert(int, SUBSTRING(CONVERT(varchar(20), id), CHARINDEX('_', CONVERT(varchar(20), id))+1, 20))),0)
FROM SymUserRoll
WHERE BranchId=@BranchId

INSERT INTO SymUserRoll(Id,BranchId,DefaultRollId,GroupId,symArea,symController,IsIndex,IsAdd,IsEdit,IsDelete,IsReport,IsProcess,Remarks,IsActive,IsArchive,CreatedBy,CreatedAt,CreatedFrom)
VALUES (CONVERT(varchar(20), @BranchId) + '_' + CONVERT(varchar(20), @NextId + 1),@BranchId,@DefaultRollId,@GroupId,@symArea,@symController,@IsIndex,@IsAdd,@IsEdit,@IsDelete,@IsReport,@IsProcess,@Remarks,@IsActive,@IsArchive,@LastUpdateBy,@LastUpdateAt,@LastUpdateFrom)";

                                SqlCommand cmdInsert = new SqlCommand(sqlText, currConn);
                                cmdInsert.Parameters.AddWithValue("@GroupId", item.GroupId.ToString().Trim());
                                cmdInsert.Parameters.AddWithValue("@DefaultRollId", BuildDynamicDefaultRollId(new DynamicMenuDefinition { renderArea = item.symArea, permissionKey = item.symController, sortOrder = 0 }));
                                cmdInsert.Parameters.AddWithValue("@symArea", item.symArea.Trim());
                                cmdInsert.Parameters.AddWithValue("@symController", item.symController.Trim());
                                cmdInsert.Parameters.AddWithValue("@IsIndex", item.IsIndex);
                                cmdInsert.Parameters.AddWithValue("@IsAdd", item.IsAdd);
                                cmdInsert.Parameters.AddWithValue("@IsEdit", item.IsEdit);
                                cmdInsert.Parameters.AddWithValue("@IsDelete", item.IsDelete);
                                cmdInsert.Parameters.AddWithValue("@IsReport", item.IsReport);
                                cmdInsert.Parameters.AddWithValue("@IsProcess", item.IsProcess);
                                cmdInsert.Parameters.AddWithValue("@Remarks", item.Remarks ?? Convert.DBNull);
                                cmdInsert.Parameters.AddWithValue("@IsActive", true);
                                cmdInsert.Parameters.AddWithValue("@IsArchive", false);
                                cmdInsert.Parameters.AddWithValue("@LastUpdateBy", vm.LastUpdateBy);
                                cmdInsert.Parameters.AddWithValue("@LastUpdateAt", vm.LastUpdateAt);
                                cmdInsert.Parameters.AddWithValue("@LastUpdateFrom", vm.LastUpdateFrom);
                                cmdInsert.Transaction = transaction;
                                transResult = Convert.ToInt32(cmdInsert.ExecuteNonQuery());
                            }
                        }

                        #endregion Commit

                        #endregion Update Settings
                        iSTransSuccess = true;
                    }
                    else
                    {
                        throw new ArgumentNullException("SymUserRoll Update", "Could not found any item.");
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
                    retResults[1] = "Unexpected error to update SymUserRoll.";
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
        public string[] Delete(SymUserRollVM SymUserRollVM, string[] ids, SqlConnection VcurrConn, SqlTransaction Vtransaction)
        {
            #region Variables

            string[] retResults = new string[6];
            retResults[0] = "Fail";//Success or Fail
            retResults[1] = "Fail";// Success or Fail Message
            retResults[2] = "0";// Return Id
            retResults[3] = "sqlText"; //  SQL Query
            retResults[4] = "ex"; //catch ex
            retResults[5] = "DeleteSymUserRoll"; //Method Name

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

                if (transaction == null) { transaction = currConn.BeginTransaction("DeleteToSymUserRoll"); }

                #endregion open connection and transaction
                #region Check is  it used

                #endregion Check is  it used

                if (ids.Length >= 1)
                {
                    #region Update Settings
                    for (int i = 0; i < ids.Length - 1; i++)
                    {
                        sqlText = "";
                        sqlText = "update SymUserRoll set";
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
                        cmdUpdate.Parameters.AddWithValue("@LastUpdateBy", SymUserRollVM.LastUpdateBy);
                        cmdUpdate.Parameters.AddWithValue("@LastUpdateAt", SymUserRollVM.LastUpdateAt);
                        cmdUpdate.Parameters.AddWithValue("@LastUpdateFrom", SymUserRollVM.LastUpdateFrom);

                        cmdUpdate.Transaction = transaction;
                        var exeRes = cmdUpdate.ExecuteNonQuery();
                        transResult = Convert.ToInt32(exeRes);
                    }


                    retResults[2] = "";// Return Id
                    retResults[3] = sqlText; //  SQL Query

                    #region Commit

                    if (transResult <= 0)
                    {
                        throw new ArgumentNullException("SymUserRoll Delete", SymUserRollVM.Id + " could not Delete.");
                    }

                    #endregion Commit

                    #endregion Update Settings
                    iSTransSuccess = true;
                }
                else
                {
                    throw new ArgumentNullException("SymUserRoll Information Delete", "Could not found any item.");
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
                    retResults[1] = "Unexpected error to delete SymUserRoll Information.";
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

        public List<SymUserRollVM> DropDownsymArea()
        {
            #region Variables
            SqlConnection currConn = null;
            string sqlText = "";
            List<SymUserRollVM> VMs = new List<SymUserRollVM>();
            SymUserRollVM vm;
            #endregion
            try
            {
                #region open connection and transaction

                currConn = _dbsqlConnection.GetConnection();
                if (currConn.State != ConnectionState.Open)
                {
                    currConn.Open();
                }
                EnsureDynamicRoleColumns(currConn, null);

                #endregion open connection and transaction

                #region sql statement

                sqlText = @"SELECT 'Admin' symArea, 1 SortOrder
UNION ALL
SELECT 'PF' symArea, 2 SortOrder
UNION ALL
SELECT 'GL' symArea, 3 SortOrder
UNION ALL
SELECT 'WPPF' symArea, 4 SortOrder
ORDER BY SortOrder
";

                SqlCommand _objComm = new SqlCommand();
                _objComm.Connection = currConn;
                _objComm.CommandText = sqlText;
                _objComm.CommandType = CommandType.Text;

                SqlDataReader dr;
                dr = _objComm.ExecuteReader();
                while (dr.Read())
                {
                    vm = new SymUserRollVM();

                    vm.symArea = dr["symArea"].ToString();
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

        public List<SymUserRollVM> DropDownsymController()
        {
            #region Variables
            SqlConnection currConn = null;
            string sqlText = "";
            List<SymUserRollVM> VMs = new List<SymUserRollVM>();
            SymUserRollVM vm;
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

                sqlText = @"SELECT DISTINCT
symController
   FROM SymUserDefaultRoll
WHERE IsArchive=0 and IsActive=1
    ORDER BY symController
";

                SqlCommand _objComm = new SqlCommand();
                _objComm.Connection = currConn;
                _objComm.CommandText = sqlText;
                _objComm.CommandType = CommandType.Text;

                SqlDataReader dr;
                dr = _objComm.ExecuteReader();
                while (dr.Read())
                {
                    vm = new SymUserRollVM();
                    vm.symController = dr["symController"].ToString();
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
        #endregion


        public DataTable RollByGroupId(string userId)
        {

            #region Variables

            SqlConnection currConn = null;
            string sqlText = "";
            DataTable dt = new DataTable();

            #endregion
            try
            {
                #region open connection and transaction

                currConn = _dbsqlConnection.GetConnection();
                if (currConn.State != ConnectionState.Open)
                {
                    currConn.Open();
                }
                EnsureDynamicRoleColumns(currConn, null);

                #endregion open connection and transaction

                #region sql statement

                sqlText = @"SELECT
SymUserRoll.Id
,SymUserRoll.DefaultRollId
,SymUserRoll.BranchId
,SymUserRoll.GroupId
,ISNULL(r.symArea, CONVERT(varchar(200), SymUserRoll.symArea)) symArea
,ISNULL(r.symController, CONVERT(varchar(500), SymUserRoll.symController)) symController
,SymUserRoll.IsIndex
,SymUserRoll.IsAdd
,SymUserRoll.IsEdit
,SymUserRoll.IsDelete
,SymUserRoll.IsReport
,SymUserRoll.IsProcess

From SymUserRoll 
left outer join UserGroup ug on SymUserRoll.GroupId=ug.Id
left outer join [User] u on u.GroupId=ug.Id
left outer join SymUserDefaultRoll r on r.Id=SymUserRoll.DefaultRollId
Where u.id=@userId 
";

                SqlCommand objComm = new SqlCommand();
                objComm.Connection = currConn;
                objComm.CommandText = sqlText;
                objComm.CommandType = CommandType.Text;
                objComm.Parameters.AddWithValue("@userId", userId);
                SqlDataAdapter da = new SqlDataAdapter(objComm);
                da.Fill(dt);



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
            return dt;
        }


        public bool SymRollSessionBackup(string UserId, string symArea, string symController, string symAction)
        {
            bool result = false;

            try
            {
                ShampanIdentity identity = HttpContext.Current.User != null ? HttpContext.Current.User.Identity as ShampanIdentity : null;
                if (identity != null && identity.IsAdmin)
                {
                    return true;
                }

                DataTable dt = HttpContext.Current.Session[UserId.ToString().Trim() + "-SymRoll"] as DataTable;
                if (dt == null || dt.Rows.Count == 0)
                {
                    return false;
                }

                string area = (symArea ?? "").Replace("'", "''");
                string controller = (symController ?? "").Replace("'", "''");
                DataRow[] rows = dt.Select("symArea='" + area + "' and symController='" + controller + "'");
                if (rows.Length == 0)
                {
                    return false;
                }

                if (symAction.ToLower() == "index")
                    result = Convert.ToBoolean(rows[0]["IsIndex"]);
                else if (symAction.ToLower() == "add")
                    result = Convert.ToBoolean(rows[0]["IsAdd"]);
                else if (symAction.ToLower() == "edit")
                    result = Convert.ToBoolean(rows[0]["IsEdit"]);
                else if (symAction.ToLower() == "delete")
                    result = Convert.ToBoolean(rows[0]["IsDelete"]);
                else if (symAction.ToLower() == "report")
                    result = Convert.ToBoolean(rows[0]["IsReport"]);
                else if (symAction.ToLower() == "process")
                    result = Convert.ToBoolean(rows[0]["IsProcess"]);
            }
            catch (Exception)
            {
                return result;

            }
            return result;
        }

        public bool SymRoleSession(string UserId, string DefaultRollId, string symAction)
        {
            bool result = false;

            try
            {
                ShampanIdentity identity = HttpContext.Current.User != null ? HttpContext.Current.User.Identity as ShampanIdentity : null;
                if (identity != null && identity.IsAdmin)
                {
                    return true;
                }

                DataTable dt = HttpContext.Current.Session[UserId.ToString().Trim() + "-SymRoll"] as DataTable;
                if (dt == null || dt.Rows.Count == 0)
                {
                    return false;
                }

                string defaultRollId = (DefaultRollId ?? "").Replace("'", "''");
                DataRow[] rows = dt.Select("DefaultRollId='" + defaultRollId + "'");
                if (rows.Length == 0)
                {
                    return false;
                }

                if (symAction.ToLower() == "index")
                    result = Convert.ToBoolean(rows[0]["IsIndex"]);
                else if (symAction.ToLower() == "add")
                    result = Convert.ToBoolean(rows[0]["IsAdd"]);
                else if (symAction.ToLower() == "edit")
                    result = Convert.ToBoolean(rows[0]["IsEdit"]);
                else if (symAction.ToLower() == "delete")
                    result = Convert.ToBoolean(rows[0]["IsDelete"]);
                else if (symAction.ToLower() == "report")
                    result = Convert.ToBoolean(rows[0]["IsReport"]);
                else if (symAction.ToLower() == "process")
                    result = Convert.ToBoolean(rows[0]["IsProcess"]);
            }
            catch (Exception)
            {
                return result;

            }
            return result;
        }

    }
}
